using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using AdaptiveDraftArena.Core;
using AdaptiveDraftArena.Match;
using AdaptiveDraftArena.Modules;

namespace AdaptiveDraftArena.Draft
{
    public class DraftController : MonoBehaviour
    {
        public float DraftTimeRemaining { get; private set; }
        public bool IsDraftActive { get; private set; }
        public List<TroopCombination> CurrentPlayerOptions { get; private set; }
        public List<TroopCombination> CurrentAIOptions { get; private set; }
        public TroopCombination PlayerSelection { get; private set; }
        public TroopCombination AISelection { get; private set; }

        // Events
        public event Action<List<TroopCombination>> OnPlayerOptionsGenerated;
        public event Action<float> OnTimerUpdated; // remaining time
        public event Action OnTimerWarning; // 5 seconds warning
        public event Action<TroopCombination> OnPlayerSelected;
        public event Action<TroopCombination> OnAISelected;
        public event Action<TroopCombination, TroopCombination> OnDraftCompleted; // player, AI

        private GameConfig config;
        private MatchState matchState;
        private CancellationTokenSource draftCts;
        private bool playerHasSelected;
        private bool aiHasSelected;
        private bool warningTriggered;

        private void Awake()
        {
            CurrentPlayerOptions = new List<TroopCombination>();
            CurrentAIOptions = new List<TroopCombination>();
        }

        private void Start()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("DraftController requires GameManager in scene!");
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

        public async UniTask<(TroopCombination playerPick, TroopCombination aiPick)> StartDraftAsync(
            MatchState state,
            CancellationToken cancellationToken)
        {
            // Cancel any existing draft
            draftCts?.Cancel();
            draftCts?.Dispose();
            draftCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            matchState = state;
            IsDraftActive = true;
            DraftTimeRemaining = config.draftDuration;
            playerHasSelected = false;
            aiHasSelected = false;
            warningTriggered = false;
            PlayerSelection = null;
            AISelection = null;

            // Generate draft options
            GenerateDraftOptions();

            Debug.Log($"Draft started! Duration: {config.draftDuration}s | Options: {config.draftOptionsCount}");

            try
            {
                var result = await RunDraftLoop(draftCts.Token);
                return result;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Draft was cancelled");
                IsDraftActive = false;
                throw;
            }
        }

        private void GenerateDraftOptions()
        {
            var fullPool = matchState.GetFullDraftPool();

            if (fullPool.Count == 0)
            {
                Debug.LogError("Draft pool is empty! Cannot generate options.");
                return;
            }

            // Generate player options (3 random combinations)
            CurrentPlayerOptions = GetRandomCombinations(fullPool, config.draftOptionsCount);
            matchState.PlayerDraftOptions = new List<TroopCombination>(CurrentPlayerOptions);

            // Generate AI options (3 random combinations)
            CurrentAIOptions = GetRandomCombinations(fullPool, config.draftOptionsCount);
            matchState.AIDraftOptions = new List<TroopCombination>(CurrentAIOptions);

            OnPlayerOptionsGenerated?.Invoke(CurrentPlayerOptions);

            Debug.Log($"Generated draft options - Player: {CurrentPlayerOptions.Count} | AI: {CurrentAIOptions.Count}");
        }

        private List<TroopCombination> GetRandomCombinations(List<TroopCombination> pool, int count)
        {
            if (pool.Count <= count)
            {
                // If pool is smaller than requested count, return shuffled copy using Fisher-Yates
                var result = new List<TroopCombination>(pool);
                for (var i = result.Count - 1; i > 0; i--)
                {
                    var j = UnityEngine.Random.Range(0, i + 1);
                    (result[i], result[j]) = (result[j], result[i]);
                }
                return result;
            }

            // Get random unique combinations using HashSet for O(1) lookups
            var selected = new List<TroopCombination>(count);
            var usedIndices = new HashSet<int>();

            while (selected.Count < count)
            {
                var index = UnityEngine.Random.Range(0, pool.Count);
                if (usedIndices.Add(index))
                {
                    selected.Add(pool[index]);
                }
            }

            return selected;
        }

        private async UniTask<(TroopCombination playerPick, TroopCombination aiPick)> RunDraftLoop(CancellationToken cancellationToken)
        {
            // AI selects immediately (simple random for now)
            PerformAISelection();

            // Player draft loop with timer
            while (DraftTimeRemaining > 0 && !playerHasSelected)
            {
                DraftTimeRemaining -= Time.deltaTime;
                OnTimerUpdated?.Invoke(DraftTimeRemaining);

                // Warning at 5 seconds
                if (!warningTriggered && DraftTimeRemaining <= 5f)
                {
                    warningTriggered = true;
                    OnTimerWarning?.Invoke();
                    Debug.Log("Draft timer warning! 5 seconds remaining");
                }

                await UniTask.Yield(cancellationToken);
            }

            // Auto-select if player didn't pick
            if (!playerHasSelected)
            {
                AutoSelectForPlayer();
            }

            // Ensure both selections are valid
            EnsureValidSelections();

            // Complete draft
            IsDraftActive = false;
            OnDraftCompleted?.Invoke(PlayerSelection, AISelection);

            Debug.Log($"Draft completed - Player: {PlayerSelection?.DisplayName ?? "None"} | AI: {AISelection?.DisplayName ?? "None"}");

            return (PlayerSelection, AISelection);
        }

        public void SelectCombination(TroopCombination combination)
        {
            if (!IsDraftActive)
            {
                Debug.LogWarning("Cannot select - draft is not active");
                return;
            }

            if (playerHasSelected)
            {
                Debug.LogWarning("Player has already selected");
                return;
            }

            if (!CurrentPlayerOptions.Contains(combination))
            {
                Debug.LogWarning("Selected combination is not in player options");
                return;
            }

            PlayerSelection = combination;
            playerHasSelected = true;
            matchState.PlayerSelectedCombo = combination;
            matchState.PlayerPickHistory.Add(combination);

            OnPlayerSelected?.Invoke(combination);
            Debug.Log($"Player selected: {combination.DisplayName}");
        }

        private void PerformAISelection()
        {
            if (CurrentAIOptions.Count == 0)
            {
                Debug.LogError("AI has no draft options!");
                return;
            }

            // Simple random selection for now
            var randomIndex = UnityEngine.Random.Range(0, CurrentAIOptions.Count);
            AISelection = CurrentAIOptions[randomIndex];
            aiHasSelected = true;
            matchState.AISelectedCombo = AISelection;

            OnAISelected?.Invoke(AISelection);
            Debug.Log($"AI selected: {AISelection.DisplayName}");
        }

        private void AutoSelectForPlayer()
        {
            if (CurrentPlayerOptions.Count == 0)
            {
                Debug.LogError("Cannot auto-select - player has no options!");
                return;
            }

            // Random auto-selection
            var randomIndex = UnityEngine.Random.Range(0, CurrentPlayerOptions.Count);
            PlayerSelection = CurrentPlayerOptions[randomIndex];
            playerHasSelected = true;
            matchState.PlayerSelectedCombo = PlayerSelection;
            matchState.PlayerPickHistory.Add(PlayerSelection);

            OnPlayerSelected?.Invoke(PlayerSelection);
            Debug.Log($"Player auto-selected: {PlayerSelection.DisplayName} (timeout)");
        }

        private void EnsureValidSelections()
        {
            // Fallback to base combinations if selections are invalid
            if (PlayerSelection == null)
            {
                if (matchState.BaseCombinations != null && matchState.BaseCombinations.Count > 0)
                {
                    PlayerSelection = matchState.BaseCombinations[0];
                    matchState.PlayerSelectedCombo = PlayerSelection;
                    Debug.LogWarning($"Player selection was null - using fallback: {PlayerSelection.DisplayName}");
                }
                else
                {
                    Debug.LogError("Cannot ensure valid player selection - BaseCombinations is empty!");
                }
            }

            if (AISelection == null)
            {
                if (matchState.BaseCombinations != null && matchState.BaseCombinations.Count > 0)
                {
                    AISelection = matchState.BaseCombinations[0];
                    matchState.AISelectedCombo = AISelection;
                    Debug.LogWarning($"AI selection was null - using fallback: {AISelection.DisplayName}");
                }
                else
                {
                    Debug.LogError("Cannot ensure valid AI selection - BaseCombinations is empty!");
                }
            }
        }

        public void StopDraft()
        {
            draftCts?.Cancel();
            IsDraftActive = false;
        }

        private void OnDestroy()
        {
            draftCts?.Cancel();
            draftCts?.Dispose();
        }
    }
}
