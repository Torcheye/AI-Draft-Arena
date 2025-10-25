using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using AdaptiveDraftArena.Core;
using AdaptiveDraftArena.Combat;

namespace AdaptiveDraftArena.Battle
{
    public class BattleController : MonoBehaviour
    {
        public float BattleTimeRemaining { get; private set; }
        public bool IsBattleActive { get; private set; }

        // Events
        public event Action OnBattleStarted;
        public event Action<float> OnTimerUpdated; // remaining time
        public event Action<Team, bool, float, float> OnBattleEnded; // winner, timerExpired, playerHP, aiHP

        private GameConfig config;
        private CancellationTokenSource battleCts;

        private void Start()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("BattleController requires GameManager in scene!");
                enabled = false;
                return;
            }

            config = GameManager.Instance.Config;

            if (config == null)
            {
                Debug.LogError("GameConfig is null in GameManager!");
                enabled = false;
                return;
            }
        }

        public async UniTask<(Team winner, bool timerExpired, float playerHP, float aiHP)> StartBattleAsync(CancellationToken cancellationToken)
        {
            // Cancel any existing battle
            battleCts?.Cancel();
            battleCts?.Dispose();
            battleCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            IsBattleActive = true;
            BattleTimeRemaining = config.battleDuration;

            OnBattleStarted?.Invoke();
            Debug.Log($"Battle started! Duration: {config.battleDuration}s");

            try
            {
                var result = await RunBattleLoop(battleCts.Token);
                return result;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Battle was cancelled");
                IsBattleActive = false;
                throw;
            }
        }

        private async UniTask<(Team winner, bool timerExpired, float playerHP, float aiHP)> RunBattleLoop(CancellationToken cancellationToken)
        {
            while (BattleTimeRemaining > 0 && IsBattleActive)
            {
                // Check for instant victory (one side eliminated)
                var playerCount = TargetingSystem.GetAliveCount(Team.Player);
                var aiCount = TargetingSystem.GetAliveCount(Team.AI);

                if (playerCount == 0 && aiCount == 0)
                {
                    // Both teams eliminated simultaneously - Player wins (tie-breaker rule)
                    return EndBattle(Team.Player, false, 0f, 0f, cancellationToken);
                }
                else if (playerCount == 0)
                {
                    // Player eliminated - AI wins instantly
                    var aiHP = TargetingSystem.GetTotalHP(Team.AI);
                    return EndBattle(Team.AI, false, 0f, aiHP, cancellationToken);
                }
                else if (aiCount == 0)
                {
                    // AI eliminated - Player wins instantly
                    var playerHP = TargetingSystem.GetTotalHP(Team.Player);
                    return EndBattle(Team.Player, false, playerHP, 0f, cancellationToken);
                }

                // Update timer
                BattleTimeRemaining -= Time.deltaTime;
                OnTimerUpdated?.Invoke(BattleTimeRemaining);

                await UniTask.Yield(cancellationToken);
            }

            // Timer expired - determine winner by HP comparison
            var finalPlayerHP = TargetingSystem.GetTotalHP(Team.Player);
            var finalAIHP = TargetingSystem.GetTotalHP(Team.AI);

            Team winner;
            if (finalPlayerHP > finalAIHP)
            {
                winner = Team.Player;
            }
            else if (finalAIHP > finalPlayerHP)
            {
                winner = Team.AI;
            }
            else
            {
                // Exact tie - Player wins (tie-breaker rule)
                winner = Team.Player;
            }

            return EndBattle(winner, true, finalPlayerHP, finalAIHP, cancellationToken);
        }

        private (Team winner, bool timerExpired, float playerHP, float aiHP) EndBattle(
            Team winner,
            bool timerExpired,
            float playerHP,
            float aiHP,
            CancellationToken cancellationToken)
        {
            IsBattleActive = false;
            BattleTimeRemaining = 0f;

            OnBattleEnded?.Invoke(winner, timerExpired, playerHP, aiHP);

            var victoryType = timerExpired ? "Timer Expiration" : "Elimination";
            Debug.Log($"Battle ended! Winner: {winner} ({victoryType}) | Player HP: {playerHP:F1} | AI HP: {aiHP:F1}");

            return (winner, timerExpired, playerHP, aiHP);
        }

        public void StopBattle()
        {
            battleCts?.Cancel();
            IsBattleActive = false;
        }

        private void OnDestroy()
        {
            battleCts?.Cancel();
            battleCts?.Dispose();
        }
    }
}
