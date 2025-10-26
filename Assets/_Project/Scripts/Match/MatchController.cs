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
        /// Calculates pick counts for player and AI based on last round winner.
        /// Last round loser gets bonus pick (comeback mechanic).
        /// First round: both get base picks.
        /// </summary>
        private (int playerPicks, int aiPicks) CalculatePickCounts()
        {
            int basePicks = config.basePicksPerRound;
            int bonusPick = config.bonusPickForLoser;

            // First round: both get base picks
            if (State.CurrentRound == 1)
            {
                Debug.Log($"[MatchController] Round 1: Both sides get {basePicks} picks");
                return (basePicks, basePicks);
            }

            // Subsequent rounds: loser gets bonus
            bool playerWonLast = State.LastRoundWinner == Team.Player;
            int playerPicks = playerWonLast ? basePicks : basePicks + bonusPick;
            int aiPicks = playerWonLast ? basePicks + bonusPick : basePicks;

            Debug.Log($"[MatchController] Round {State.CurrentRound}: Player {playerPicks} picks, AI {aiPicks} picks (Last winner: {State.LastRoundWinner})");
            return (playerPicks, aiPicks);
        }

        /// <summary>
        /// Generates AI combinations starting from Round 2.
        /// Count is determined by config.aiCombosPerRound (default 8).
        /// Adds them to the pool for both player and AI drafting.
        /// </summary>
        private async UniTask GenerateAICombinations(int roundNumber, CancellationToken ct)
        {
            int comboCount = config.aiCombosPerRound;
            Debug.Log($"[MatchController] Generating {comboCount} random AI combos for Round {roundNumber}...");

            var newCombos = new List<ICombination>();

            for (int i = 0; i < comboCount; i++)
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

            // Draft Phase (includes reveals after each pick)
            await RunDraftPhase(cancellationToken);

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

            // Calculate pick counts based on last round winner
            var (playerPickCount, aiPickCount) = CalculatePickCounts();

            // Run multi-pick draft (includes per-pick reveals)
            var (playerPicks, aiPicks) = await draftController.StartMultiPickDraftAsync(
                State,
                playerPickCount,
                aiPickCount,
                cancellationToken);

            // Store selections in state
            State.PlayerSelectedCombos = playerPicks;
            State.AISelectedCombos = aiPicks;

            Debug.Log($"Draft phase ended - Player: {playerPicks.Count} picks | AI: {aiPicks.Count} picks");
        }

        // RunRevealPhase removed - reveals now happen during draft phase after each pick

        private async UniTask RunSpawnPhase(CancellationToken cancellationToken)
        {
            ChangePhase(MatchPhase.Spawn);
            Debug.Log("Spawn phase started");

            // Clear any remaining troops from previous round
            TargetingSystem.ClearAll();

            // Spawn all player troops from picks
            if (State.PlayerSelectedCombos != null && State.PlayerSelectedCombos.Count > 0)
            {
                foreach (var combo in State.PlayerSelectedCombos)
                {
                    if (combo != null)
                    {
                        troopSpawner.SpawnTroops(combo, Team.Player);
                    }
                }
                Debug.Log($"Spawned {State.PlayerSelectedCombos.Count} player troop groups");
            }
            else
            {
                Debug.LogWarning("Player has no selected combos! Skipping player spawn.");
            }

            // Spawn all AI troops from picks
            if (State.AISelectedCombos != null && State.AISelectedCombos.Count > 0)
            {
                foreach (var combo in State.AISelectedCombos)
                {
                    if (combo != null)
                    {
                        troopSpawner.SpawnTroops(combo, Team.AI);
                    }
                }
                Debug.Log($"Spawned {State.AISelectedCombos.Count} AI troop groups");
            }
            else
            {
                Debug.LogWarning("AI has no selected combos! Skipping AI spawn.");
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

            // Award the win and track winner for next round's pick calculation
            State.AwardRoundWin(roundResult.Winner);
            State.LastRoundWinner = roundResult.Winner;

            // Emit round ended event
            OnRoundEnded?.Invoke(roundResult);

            Debug.Log($"Round {State.CurrentRound} winner: {roundResult.Winner}");
            Debug.Log($"Score - Player: {State.PlayerWins}, AI: {State.AIWins}");

            // Store all player and AI picks in history for pattern analysis
            if (State.PlayerSelectedCombos != null)
            {
                foreach (var combo in State.PlayerSelectedCombos)
                {
                    if (combo != null)
                    {
                        State.PlayerPickHistory.Add(combo);
                    }
                }
            }

            if (State.AISelectedCombos != null)
            {
                foreach (var combo in State.AISelectedCombos)
                {
                    if (combo != null)
                    {
                        State.AIPickHistory.Add(combo);
                    }
                }
            }

            // Generate AI counter for next round (if not final round)
            if (State.CurrentRound < config.maxRounds)
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
