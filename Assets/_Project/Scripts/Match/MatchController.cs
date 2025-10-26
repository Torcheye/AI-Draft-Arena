using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using AdaptiveDraftArena.Core;
using AdaptiveDraftArena.Modules;
using AdaptiveDraftArena.Draft;
using AdaptiveDraftArena.Battle;
using AdaptiveDraftArena.Combat;
using AdaptiveDraftArena.AI;

namespace AdaptiveDraftArena.Match
{
    public class MatchController : MonoBehaviour
    {
        [Header("Controllers")]
        [SerializeField] private DraftController draftController;
        [SerializeField] private BattleController battleController;
        [SerializeField] private TroopSpawner troopSpawner;
        [SerializeField] private AIGenerationOrchestrator aiOrchestrator;

        public MatchState State { get; private set; }
        public MatchPhase CurrentPhase => State?.CurrentPhase ?? MatchPhase.MatchStart;

        // Events
        public event Action<MatchPhase, MatchPhase> OnPhaseChanged;
        public event Action<int> OnRoundStarted;
        public event Action<RoundResult> OnRoundEnded;
        public event Action<Team> OnMatchEnded;

        private GameConfig config;
        private CancellationTokenSource matchCts;

        private void Awake()
        {
            // Auto-populate controller references if not assigned
            if (draftController == null)
            {
                draftController = GetComponent<DraftController>();
            }

            if (battleController == null)
            {
                battleController = GetComponent<BattleController>();
            }

            if (troopSpawner == null)
            {
                troopSpawner = GetComponent<TroopSpawner>();
            }

            if (aiOrchestrator == null)
            {
                aiOrchestrator = GetComponent<AIGenerationOrchestrator>();
            }
        }

        private void Start()
        {
            // Critical: Validate GameManager exists (after GameManager.Awake() has run)
            if (GameManager.Instance == null)
            {
                Debug.LogError("MatchController requires GameManager in scene! Disabling component.");
                enabled = false;
                return;
            }

            config = GameManager.Instance.Config;

            // Critical: Validate Config is assigned
            if (config == null)
            {
                Debug.LogError("GameConfig is null in GameManager! Disabling MatchController.");
                enabled = false;
                return;
            }

            // Validate controller references
            if (draftController == null)
            {
                Debug.LogError("DraftController not found! Please assign in Inspector or add component.");
                enabled = false;
                return;
            }

            if (battleController == null)
            {
                Debug.LogError("BattleController not found! Please assign in Inspector or add component.");
                enabled = false;
                return;
            }

            if (troopSpawner == null)
            {
                Debug.LogError("TroopSpawner not found! Please assign in Inspector or add component.");
                enabled = false;
                return;
            }

            if (aiOrchestrator == null)
            {
                Debug.LogError("AIGenerationOrchestrator not found! Please assign in Inspector or add component.");
                enabled = false;
                return;
            }

            Debug.Log("MatchController initialized successfully");
        }

        public async UniTask StartMatchAsync()
        {
            // Cancel any existing match
            matchCts?.Cancel();
            matchCts?.Dispose();
            matchCts = new CancellationTokenSource();

            Debug.Log("Match starting...");
            State = new MatchState();

            // Load base combinations
            LoadBaseCombinations();

            try
            {
                // Run match loop with cancellation support
                await RunMatchLoop(matchCts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Match was cancelled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Match error: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            // Clean up cancellation token on destruction
            matchCts?.Cancel();
            matchCts?.Dispose();
        }

        private void LoadBaseCombinations()
        {
            // Load base combinations from Resources
            var combos = Resources.LoadAll<TroopCombination>("Data/BaseCombinations");
            State.BaseCombinations.AddRange(combos);

            Debug.Log($"Loaded {State.BaseCombinations.Count} base combinations");
        }

        private async UniTask RunMatchLoop(CancellationToken cancellationToken)
        {
            ChangePhase(MatchPhase.MatchStart);
            await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);

            // Main match loop - best of 7
            for (int round = 1; round <= config.maxRounds; round++)
            {
                State.CurrentRound = round;
                OnRoundStarted?.Invoke(round);

                await RunRound(round, cancellationToken);

                // Check if match is over
                if (State.IsMatchOver())
                {
                    break;
                }

                // Brief pause between rounds
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);
            }

            // Match end
            ChangePhase(MatchPhase.MatchEnd);
            var winner = State.GetMatchWinner();
            OnMatchEnded?.Invoke(winner);

            Debug.Log($"Match ended! Winner: {winner}");
        }

        /// <summary>
        /// Generates 4 random AI combinations starting from Round 2.
        /// Adds them to the pool for both player and AI drafting.
        /// </summary>
        private async UniTask GenerateAICombinations(int roundNumber, CancellationToken ct)
        {
            Debug.Log($"[MatchController] Generating 4 random AI combos for Round {roundNumber}...");

            var newCombos = new List<ICombination>();

            for (int i = 0; i < 4; i++)
            {
                var combo = GenerateRandomCombo();
                if (combo != null && combo.IsValid())
                {
                    var runtimeCombo = combo as RuntimeTroopCombination;
                    if (runtimeCombo != null)
                    {
                        runtimeCombo.generationRound = roundNumber;
                    }
                    newCombos.Add(combo);
                    Debug.Log($"[MatchController] Generated: {combo.DisplayName}");
                }
                else
                {
                    Debug.LogWarning($"[MatchController] Failed to generate valid combo #{i + 1}");
                }
            }

            // Add to pool
            State.AIGeneratedCombinations.AddRange(newCombos);

            // Update bags in DraftController (so new combos are immediately available)
            // Note: Bags will be updated when DraftController's GenerateDraftOptions is called

            Debug.Log($"[MatchController] Added {newCombos.Count} combos to pool (total: {State.GetFullDraftPool().Count})");

            // No delay - generation is instant
            await UniTask.Yield(ct);
        }

        /// <summary>
        /// Generates a single random troop combination by mixing modules.
        /// Truly random (not strategic) - creates variety and discovery.
        /// Validates module pools and ensures no null references.
        /// </summary>
        private RuntimeTroopCombination GenerateRandomCombo()
        {
            // Validate module pools are not empty and contain valid (non-null) entries
            if (config.Bodies.Count == 0 || config.Weapons.Count == 0 ||
                config.Abilities.Count == 0 || config.Effects.Count == 0)
            {
                Debug.LogError("[MatchController] Module pools are empty! Cannot generate random combo.");
                return null;
            }

            // Pick random modules (with retry if null)
            BodyModule body = null;
            WeaponModule weapon = null;
            AbilityModule ability = null;
            EffectModule effect = null;

            // Try up to 5 times to get valid modules
            for (int attempt = 0; attempt < 5; attempt++)
            {
                body = config.Bodies[UnityEngine.Random.Range(0, config.Bodies.Count)];
                weapon = config.Weapons[UnityEngine.Random.Range(0, config.Weapons.Count)];
                ability = config.Abilities[UnityEngine.Random.Range(0, config.Abilities.Count)];
                effect = config.Effects[UnityEngine.Random.Range(0, config.Effects.Count)];

                // If all are valid, break
                if (body != null && weapon != null && ability != null && effect != null)
                    break;

                if (attempt == 4)
                {
                    Debug.LogError("[MatchController] Module pools contain null entries! Fix GameConfig.");
                    return null;
                }
            }

            // Create combo
            var combo = new RuntimeTroopCombination
            {
                body = body,
                weapon = weapon,
                ability = ability,
                effect = effect,
                amount = PickRandomAmount(),
                isAIGenerated = true
            };

            return combo;
        }

        /// <summary>
        /// Picks a random amount from valid values (1, 2, 3, 5).
        /// </summary>
        private int PickRandomAmount()
        {
            int[] validAmounts = { 1, 2, 3, 5 };
            return validAmounts[UnityEngine.Random.Range(0, validAmounts.Length)];
        }

        private async UniTask RunRound(int roundNumber, CancellationToken cancellationToken)
        {
            Debug.Log($"=== Round {roundNumber} Start ===");

            // Generate AI combos (starting from Round 2)
            if (roundNumber >= 2)
            {
                await GenerateAICombinations(roundNumber, cancellationToken);
            }

            // Draft Phase
            await RunDraftPhase(cancellationToken);

            // Reveal Phase
            await RunRevealPhase(cancellationToken);

            // Spawn Phase
            await RunSpawnPhase(cancellationToken);

            // Battle Phase
            await RunBattlePhase(cancellationToken);

            // Round End Phase
            await RunRoundEndPhase(cancellationToken);
        }

        private async UniTask RunDraftPhase(CancellationToken cancellationToken)
        {
            ChangePhase(MatchPhase.Draft);
            Debug.Log("Draft phase started");

            var (playerPick, aiPick) = await draftController.StartDraftAsync(State, cancellationToken);

            State.PlayerSelectedCombo = playerPick;
            State.AISelectedCombo = aiPick;

            Debug.Log($"Draft phase ended - Player: {playerPick?.DisplayName} | AI: {aiPick?.DisplayName}");
        }

        private async UniTask RunRevealPhase(CancellationToken cancellationToken)
        {
            ChangePhase(MatchPhase.Reveal);
            Debug.Log("Reveal phase started");

            // Hold for 2-3 seconds to show player and AI selections
            // The UI will be animated by RevealUI component via UIManager
            await UniTask.Delay(System.TimeSpan.FromSeconds(2.5f), cancellationToken: cancellationToken);

            Debug.Log($"Reveal phase ended - Player: {State.PlayerSelectedCombo?.DisplayName} vs AI: {State.AISelectedCombo?.DisplayName}");
        }

        private async UniTask RunSpawnPhase(CancellationToken cancellationToken)
        {
            ChangePhase(MatchPhase.Spawn);
            Debug.Log("Spawn phase started");

            // Clear any remaining troops from previous round
            TargetingSystem.ClearAll();

            // Spawn player troops
            if (State.PlayerSelectedCombo != null)
            {
                troopSpawner.SpawnTroops(State.PlayerSelectedCombo, Team.Player);
            }
            else
            {
                Debug.LogWarning("Player selected combo is null! Skipping player spawn.");
            }

            // Spawn AI troops
            if (State.AISelectedCombo != null)
            {
                troopSpawner.SpawnTroops(State.AISelectedCombo, Team.AI);
            }
            else
            {
                Debug.LogWarning("AI selected combo is null! Skipping AI spawn.");
            }

            // Brief pause to show formations
            await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);

            Debug.Log("Spawn phase ended");
        }

        private async UniTask RunBattlePhase(CancellationToken cancellationToken)
        {
            ChangePhase(MatchPhase.Battle);
            Debug.Log("Battle phase started");

            var (winner, timerExpired, playerHP, aiHP) = await battleController.StartBattleAsync(cancellationToken);

            // Store battle result in state for RoundEnd phase
            State.RoundHistory.Add(new RoundResult
            {
                RoundNumber = State.CurrentRound,
                Winner = winner,
                PlayerHP = (int)playerHP,
                AIHP = (int)aiHP,
                TimerExpired = timerExpired,
                BattleDuration = config.battleDuration - battleController.BattleTimeRemaining
            });

            Debug.Log($"Battle phase ended - Winner: {winner} | Method: {(timerExpired ? "HP Comparison" : "Elimination")}");
        }

        private async UniTask RunRoundEndPhase(CancellationToken cancellationToken)
        {
            ChangePhase(MatchPhase.RoundEnd);
            Debug.Log("Round end phase started");

            // Get the latest round result
            var roundResult = State.RoundHistory[State.RoundHistory.Count - 1];

            // Award the win
            State.AwardRoundWin(roundResult.Winner);

            // Emit round ended event
            OnRoundEnded?.Invoke(roundResult);

            Debug.Log($"Round {State.CurrentRound} winner: {roundResult.Winner}");
            Debug.Log($"Score - Player: {State.PlayerWins}, AI: {State.AIWins}");

            // Store player and AI picks in history for pattern analysis
            if (State.PlayerSelectedCombo != null)
            {
                State.PlayerPickHistory.Add(State.PlayerSelectedCombo);
            }

            if (State.AISelectedCombo != null)
            {
                State.AIPickHistory.Add(State.AISelectedCombo);
            }

            // Generate AI counter for next round (if not final round)
            if (State.CurrentRound < 7)
            {
                var counter = await aiOrchestrator.GenerateCounterAsync(State, cancellationToken);
                if (counter != null)
                {
                    State.AIGeneratedCombinations.Add(counter);
                    Debug.Log($"AI generated counter: {counter.DisplayName}");
                }
                else
                {
                    Debug.LogWarning("AI failed to generate counter! Will use base combinations only.");
                }
            }

            await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: cancellationToken);

            Debug.Log("Round end phase ended");
        }

        private void ChangePhase(MatchPhase newPhase)
        {
            var oldPhase = State?.CurrentPhase ?? MatchPhase.MatchStart;
            if (State != null)
            {
                State.CurrentPhase = newPhase;
            }
            OnPhaseChanged?.Invoke(oldPhase, newPhase);

            Debug.Log($"Phase changed: {oldPhase} → {newPhase}");
        }
    }
}
