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
        public List<ICombination> CurrentPlayerOptions { get; private set; }
        public List<ICombination> CurrentAIOptions { get; private set; }
        public ICombination PlayerSelection { get; private set; }
        public ICombination AISelection { get; private set; }

        // Events
        public event Action<List<ICombination>> OnPlayerOptionsGenerated;
        public event Action<float> OnTimerUpdated; // remaining time
        public event Action OnTimerWarning; // 5 seconds warning
        public event Action<ICombination> OnPlayerSelected;
        public event Action<ICombination> OnAISelected;
        public event Action<ICombination, ICombination> OnDraftCompleted; // player, AI

        private GameConfig config;
        private MatchState matchState;
        private CancellationTokenSource draftCts;
        private bool playerHasSelected;
        private bool aiHasSelected;
        private bool warningTriggered;

        // 7-bag randomization for player and AI draft options
        private CombinationBag playerBag;
        private CombinationBag aiBag;

        private void Awake()
        {
            CurrentPlayerOptions = new List<ICombination>();
            CurrentAIOptions = new List<ICombination>();
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

        public async UniTask<(ICombination playerPick, ICombination aiPick)> StartDraftAsync(
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

            // Initialize bags if null (first round)
            if (playerBag == null)
            {
                playerBag = new CombinationBag(fullPool);
                Debug.Log("[DraftController] Initialized player bag");
            }
            else
            {
                // Update bag with new AI-generated combos (if any were added this round)
                var newCombos = matchState.AIGeneratedCombinations;
                if (newCombos != null && newCombos.Count > 0)
                {
                    // Only add the newly generated combos (not all of them)
                    // This is a simplified approach - in production you'd track which are new
                    var baseCombosCount = matchState.BaseCombinations.Count;
                    var expectedNewCombos = fullPool.Count - baseCombosCount;

                    // Add new combos to player bag
                    var combosToAdd = new List<ICombination>();
                    for (int i = baseCombosCount; i < fullPool.Count; i++)
                    {
                        combosToAdd.Add(fullPool[i]);
                    }

                    if (combosToAdd.Count > 0)
                    {
                        playerBag.AddToPool(combosToAdd);
                    }
                }
            }

            if (aiBag == null)
            {
                aiBag = new CombinationBag(fullPool);
                Debug.Log("[DraftController] Initialized AI bag");
            }
            else
            {
                // Update bag with new AI-generated combos (if any were added this round)
                var newCombos = matchState.AIGeneratedCombinations;
                if (newCombos != null && newCombos.Count > 0)
                {
                    var baseCombosCount = matchState.BaseCombinations.Count;

                    // Add new combos to AI bag
                    var combosToAdd = new List<ICombination>();
                    for (int i = baseCombosCount; i < fullPool.Count; i++)
                    {
                        combosToAdd.Add(fullPool[i]);
                    }

                    if (combosToAdd.Count > 0)
                    {
                        aiBag.AddToPool(combosToAdd);
                    }
                }
            }

            // Draw player options from bag (pure 7-bag randomization)
            CurrentPlayerOptions = playerBag.Draw(config.draftOptionsCount);
            matchState.PlayerDraftOptions = new List<ICombination>(CurrentPlayerOptions);

            // Draw AI options from bag (with guaranteed counter if available)
            CurrentAIOptions = GenerateAIOptions(fullPool, config.draftOptionsCount);
            matchState.AIDraftOptions = new List<ICombination>(CurrentAIOptions);

            OnPlayerOptionsGenerated?.Invoke(CurrentPlayerOptions);

            Debug.Log($"Generated draft options - Player: {CurrentPlayerOptions.Count} | AI: {CurrentAIOptions.Count}");
        }

        /// <summary>
        /// Generates AI draft options, guaranteeing the latest generated counter is included.
        /// Uses 7-bag algorithm for variety, but inserts strategic counter manually.
        /// </summary>
        private List<ICombination> GenerateAIOptions(List<ICombination> pool, int count)
        {
            // Check if we have a recently generated counter
            ICombination latestCounter = null;
            if (matchState.AIGeneratedCombinations != null && matchState.AIGeneratedCombinations.Count > 0)
            {
                latestCounter = matchState.AIGeneratedCombinations[matchState.AIGeneratedCombinations.Count - 1];
                Debug.Log($"[DraftController] Including latest AI counter in options: {latestCounter.DisplayName}");
            }

            // If no counter generated yet, just use bag
            if (latestCounter == null)
            {
                return aiBag.Draw(count);
            }

            // Build AI options: 1 guaranteed counter + (count - 1) from bag
            var aiOptions = new List<ICombination> { latestCounter };

            // Get remaining options from bag (bag automatically avoids recent picks)
            var bagOptions = aiBag.Draw(count - 1);
            aiOptions.AddRange(bagOptions);

            return aiOptions;
        }

        private async UniTask<(ICombination playerPick, ICombination aiPick)> RunDraftLoop(CancellationToken cancellationToken)
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

        public void SelectCombination(ICombination combination)
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

            // Phase 1: AI strongly prefers first option (generated counter) with 80% probability
            // Remaining 20%: picks randomly from all options for variety
            int selectedIndex;
            if (matchState.AIGeneratedCombinations != null && matchState.AIGeneratedCombinations.Count > 0)
            {
                // If we have a generated counter (it's in position 0), prefer it
                float roll = UnityEngine.Random.Range(0f, 1f);
                if (roll < 0.8f)
                {
                    selectedIndex = 0; // Pick the counter (80% chance)
                    Debug.Log("[DraftController] AI choosing generated counter (strategic pick)");
                }
                else
                {
                    selectedIndex = UnityEngine.Random.Range(0, CurrentAIOptions.Count); // Random (20% chance)
                    Debug.Log("[DraftController] AI choosing random option (variety)");
                }
            }
            else
            {
                // Round 1: No counter generated yet, pick randomly
                selectedIndex = UnityEngine.Random.Range(0, CurrentAIOptions.Count);
                Debug.Log("[DraftController] AI choosing random option (no counter yet)");
            }

            AISelection = CurrentAIOptions[selectedIndex];
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
