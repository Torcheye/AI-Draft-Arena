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
        public int CurrentPickNumber { get; private set; } // Track which pick we're on (1, 2, 3)
        public int TotalPicksThisRound { get; private set; }

        // Events
        public event Action<List<ICombination>> OnPlayerOptionsGenerated;
        public event Action OnPlayerWaiting; // Player has no pick this round (comeback bonus for opponent)
        public event Action<float> OnTimerUpdated; // remaining time
        public event Action OnTimerWarning; // 5 seconds warning
        public event Action<ICombination> OnPlayerSelected;
        public event Action<ICombination> OnAISelected;
        public event Action<ICombination, ICombination> OnPickCompleted; // Fired after each pick (for reveals)
        public event Action<List<ICombination>, List<ICombination>> OnDraftCompleted; // All picks done

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

        /// <summary>
        /// Starts multi-pick draft phase. Returns all picks made by both sides.
        /// </summary>
        public async UniTask<(List<ICombination> playerPicks, List<ICombination> aiPicks)> StartMultiPickDraftAsync(
            MatchState state,
            int playerPickCount,
            int aiPickCount,
            CancellationToken cancellationToken)
        {
            // Cancel any existing draft
            draftCts?.Cancel();
            draftCts?.Dispose();
            draftCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            matchState = state;
            IsDraftActive = true;

            // Clear previous selections
            var playerPicks = new List<ICombination>();
            var aiPicks = new List<ICombination>();

            // Determine total picks (max of player and AI)
            TotalPicksThisRound = Mathf.Max(playerPickCount, aiPickCount);

            Debug.Log($"Multi-pick draft started! Player picks: {playerPickCount}, AI picks: {aiPickCount}, Total rounds: {TotalPicksThisRound}");

            try
            {
                // Run multiple pick loops
                for (int pickNum = 1; pickNum <= TotalPicksThisRound; pickNum++)
                {
                    CurrentPickNumber = pickNum;

                    bool playerShouldPick = pickNum <= playerPickCount;
                    bool aiShouldPick = pickNum <= aiPickCount;

                    Debug.Log($"=== Pick {pickNum}/{TotalPicksThisRound} | Player: {playerShouldPick}, AI: {aiShouldPick} ===");

                    var (playerPick, aiPick) = await RunSinglePickAsync(playerShouldPick, aiShouldPick, draftCts.Token);

                    if (playerPick != null) playerPicks.Add(playerPick);
                    if (aiPick != null) aiPicks.Add(aiPick);

                    // Fire pick completed event for reveal
                    OnPickCompleted?.Invoke(playerPick, aiPick);

                    // Wait for reveal to complete (timing configured in GameConfig.revealTotalDuration)
                    await UniTask.Delay(TimeSpan.FromSeconds(config.revealTotalDuration), cancellationToken: draftCts.Token);
                }

                IsDraftActive = false;
                OnDraftCompleted?.Invoke(playerPicks, aiPicks);

                Debug.Log($"Multi-pick draft completed - Player: {playerPicks.Count} picks, AI: {aiPicks.Count} picks");
                return (playerPicks, aiPicks);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Multi-pick draft was cancelled");
                IsDraftActive = false;
                throw;
            }
        }

        private void GenerateDraftOptions(bool showToPlayer = true)
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

            // Only notify UI if player should see options this pick
            if (showToPlayer)
            {
                OnPlayerOptionsGenerated?.Invoke(CurrentPlayerOptions);
            }
            else
            {
                OnPlayerWaiting?.Invoke(); // Notify UI that player is waiting (opponent's comeback bonus pick)
            }

            Debug.Log($"Generated draft options - Player: {CurrentPlayerOptions.Count} | AI: {CurrentAIOptions.Count} | Show to player: {showToPlayer}");
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

        /// <summary>
        /// Runs a single pick for both player and AI (or just one side if needed).
        /// </summary>
        private async UniTask<(ICombination playerPick, ICombination aiPick)> RunSinglePickAsync(
            bool playerShouldPick,
            bool aiShouldPick,
            CancellationToken cancellationToken)
        {
            // Reset selection state
            DraftTimeRemaining = config.draftDuration;
            playerHasSelected = false;
            aiHasSelected = false;
            warningTriggered = false;
            PlayerSelection = null;
            AISelection = null;

            // Generate fresh draft options for this pick (only show to player if they should pick)
            GenerateDraftOptions(playerShouldPick);

            // AI selects immediately if it should pick
            if (aiShouldPick)
            {
                PerformAISelection();
            }

            // Player draft loop with timer (only if player should pick)
            if (playerShouldPick)
            {
                while (DraftTimeRemaining > 0 && !playerHasSelected)
                {
                    DraftTimeRemaining -= Time.deltaTime;
                    OnTimerUpdated?.Invoke(DraftTimeRemaining);

                    // Warning at 3 seconds (reduced from 5 since timer is 10s now)
                    if (!warningTriggered && DraftTimeRemaining <= 3f)
                    {
                        warningTriggered = true;
                        OnTimerWarning?.Invoke();
                        Debug.Log("Draft timer warning! 3 seconds remaining");
                    }

                    await UniTask.Yield(cancellationToken);
                }

                // Auto-select if player didn't pick
                if (!playerHasSelected)
                {
                    AutoSelectForPlayer();
                }
            }

            // Ensure valid selections (fallback if needed)
            if (playerShouldPick) PlayerSelection = EnsureValidSelection(PlayerSelection, "Player");
            if (aiShouldPick) AISelection = EnsureValidSelection(AISelection, "AI");

            Debug.Log($"Single pick completed - Player: {PlayerSelection?.DisplayName ?? "None"} | AI: {AISelection?.DisplayName ?? "None"}");

            return (playerShouldPick ? PlayerSelection : null, aiShouldPick ? AISelection : null);
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

            OnPlayerSelected?.Invoke(PlayerSelection);
            Debug.Log($"Player auto-selected: {PlayerSelection.DisplayName} (timeout)");
        }

        private ICombination EnsureValidSelection(ICombination selection, string side)
        {
            // Fallback to base combinations if selection is invalid
            if (selection == null)
            {
                if (matchState.BaseCombinations != null && matchState.BaseCombinations.Count > 0)
                {
                    selection = matchState.BaseCombinations[0];
                    Debug.LogWarning($"{side} selection was null - using fallback: {selection.DisplayName}");
                }
                else
                {
                    Debug.LogError($"Cannot ensure valid {side} selection - BaseCombinations is empty!");
                }
            }
            return selection;
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
