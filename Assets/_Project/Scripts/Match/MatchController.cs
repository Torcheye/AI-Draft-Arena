using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using AdaptiveDraftArena.Core;
using AdaptiveDraftArena.Modules;
using AdaptiveDraftArena.Draft;
using AdaptiveDraftArena.Battle;
using AdaptiveDraftArena.Combat;

namespace AdaptiveDraftArena.Match
{
    public class MatchController : MonoBehaviour
    {
        [Header("Controllers")]
        [SerializeField] private DraftController draftController;
        [SerializeField] private BattleController battleController;
        [SerializeField] private TroopSpawner troopSpawner;

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

        private async UniTask RunRound(int roundNumber, CancellationToken cancellationToken)
        {
            Debug.Log($"=== Round {roundNumber} Start ===");

            // Draft Phase
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

            var (playerPick, aiPick) = await draftController.StartDraftAsync(State, cancellationToken);

            State.PlayerSelectedCombo = playerPick;
            State.AISelectedCombo = aiPick;

            Debug.Log($"Draft phase ended - Player: {playerPick?.DisplayName} | AI: {aiPick?.DisplayName}");
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

            // TODO: AI generation would happen here

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
