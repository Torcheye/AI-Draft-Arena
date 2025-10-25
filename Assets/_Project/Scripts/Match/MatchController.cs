using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using AdaptiveDraftArena.Core;
using AdaptiveDraftArena.Modules;

namespace AdaptiveDraftArena.Match
{
    public class MatchController : MonoBehaviour
    {
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
            // Critical: Validate GameManager exists
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

            // TODO: Implement draft logic
            // For now, just wait for draft duration
            await UniTask.Delay(TimeSpan.FromSeconds(config.draftDuration), cancellationToken: cancellationToken);

            Debug.Log("Draft phase ended");
        }

        private async UniTask RunSpawnPhase(CancellationToken cancellationToken)
        {
            ChangePhase(MatchPhase.Spawn);
            Debug.Log("Spawn phase started");

            // TODO: Implement spawn logic
            // For now, just wait 1 second
            await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);

            Debug.Log("Spawn phase ended");
        }

        private async UniTask RunBattlePhase(CancellationToken cancellationToken)
        {
            ChangePhase(MatchPhase.Battle);
            Debug.Log("Battle phase started");

            // TODO: Implement battle logic
            // For now, just wait for battle duration
            await UniTask.Delay(TimeSpan.FromSeconds(config.battleDuration), cancellationToken: cancellationToken);

            Debug.Log("Battle phase ended");
        }

        private async UniTask RunRoundEndPhase(CancellationToken cancellationToken)
        {
            ChangePhase(MatchPhase.RoundEnd);
            Debug.Log("Round end phase started");

            // TODO: Determine winner, update scores
            // For now, randomly award round
            var roundWinner = UnityEngine.Random.value > 0.5f ? Team.Player : Team.AI;
            State.AwardRoundWin(roundWinner);

            var roundResult = new RoundResult
            {
                RoundNumber = State.CurrentRound,
                Winner = roundWinner,
                TimerExpired = true,
                BattleDuration = config.battleDuration
            };

            State.RoundHistory.Add(roundResult);
            OnRoundEnded?.Invoke(roundResult);

            Debug.Log($"Round {State.CurrentRound} winner: {roundWinner}");
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
