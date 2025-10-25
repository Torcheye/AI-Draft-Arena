using System;
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

        private void Awake()
        {
            config = GameManager.Instance.Config;
        }

        public async void StartMatch()
        {
            Debug.Log("Match starting...");
            State = new MatchState();

            // Load base combinations
            LoadBaseCombinations();

            // Run match loop
            await RunMatchLoop();
        }

        private void LoadBaseCombinations()
        {
            // Load base combinations from Resources
            var combos = Resources.LoadAll<TroopCombination>("Data/BaseCombinations");
            State.BaseCombinations.AddRange(combos);

            Debug.Log($"Loaded {State.BaseCombinations.Count} base combinations");
        }

        private async UniTask RunMatchLoop()
        {
            ChangePhase(MatchPhase.MatchStart);
            await UniTask.Delay(TimeSpan.FromSeconds(1));

            // Main match loop - best of 7
            for (int round = 1; round <= config.maxRounds; round++)
            {
                State.CurrentRound = round;
                OnRoundStarted?.Invoke(round);

                await RunRound(round);

                // Check if match is over
                if (State.IsMatchOver())
                {
                    break;
                }

                // Brief pause between rounds
                await UniTask.Delay(TimeSpan.FromSeconds(1));
            }

            // Match end
            ChangePhase(MatchPhase.MatchEnd);
            var winner = State.GetMatchWinner();
            OnMatchEnded?.Invoke(winner);

            Debug.Log($"Match ended! Winner: {winner}");
        }

        private async UniTask RunRound(int roundNumber)
        {
            Debug.Log($"=== Round {roundNumber} Start ===");

            // Draft Phase
            await RunDraftPhase();

            // Spawn Phase
            await RunSpawnPhase();

            // Battle Phase
            await RunBattlePhase();

            // Round End Phase
            await RunRoundEndPhase();
        }

        private async UniTask RunDraftPhase()
        {
            ChangePhase(MatchPhase.Draft);
            Debug.Log("Draft phase started");

            // TODO: Implement draft logic
            // For now, just wait for draft duration
            await UniTask.Delay(TimeSpan.FromSeconds(config.draftDuration));

            Debug.Log("Draft phase ended");
        }

        private async UniTask RunSpawnPhase()
        {
            ChangePhase(MatchPhase.Spawn);
            Debug.Log("Spawn phase started");

            // TODO: Implement spawn logic
            // For now, just wait 1 second
            await UniTask.Delay(TimeSpan.FromSeconds(1));

            Debug.Log("Spawn phase ended");
        }

        private async UniTask RunBattlePhase()
        {
            ChangePhase(MatchPhase.Battle);
            Debug.Log("Battle phase started");

            // TODO: Implement battle logic
            // For now, just wait for battle duration
            await UniTask.Delay(TimeSpan.FromSeconds(config.battleDuration));

            Debug.Log("Battle phase ended");
        }

        private async UniTask RunRoundEndPhase()
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

            await UniTask.Delay(TimeSpan.FromSeconds(2));

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
