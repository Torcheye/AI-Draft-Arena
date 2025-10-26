using UnityEngine;
using Cysharp.Threading.Tasks;
using AdaptiveDraftArena.Match;

namespace AdaptiveDraftArena.UI
{
    /// <summary>
    /// Manages UI screen visibility based on match phase transitions.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private MatchController matchController;
        [SerializeField] private Draft.DraftController draftController;

        [Header("UI Screens")]
        [SerializeField] private DraftUI draftUI;
        [SerializeField] private BattleUI battleUI;
        [SerializeField] private RevealUI revealUI;

        private void Start()
        {
            if (matchController == null)
            {
                Debug.LogError("UIManager: MatchController not assigned in Inspector!", this);
                enabled = false;
                return;
            }

            if (draftController == null)
            {
                Debug.LogError("UIManager: DraftController not assigned in Inspector!", this);
                enabled = false;
                return;
            }

            ValidateReferences();
            SubscribeToEvents();

            // Initialize with all screens hidden
            HideAllScreens();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (matchController != null)
            {
                matchController.OnPhaseChanged += HandlePhaseChanged;
            }

            if (draftController != null)
            {
                draftController.OnPickCompleted += HandlePickCompleted;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (matchController != null)
            {
                matchController.OnPhaseChanged -= HandlePhaseChanged;
            }

            if (draftController != null)
            {
                draftController.OnPickCompleted -= HandlePickCompleted;
            }
        }

        private void ValidateReferences()
        {
            #if UNITY_EDITOR
            if (draftUI == null) Debug.LogWarning($"UIManager '{name}': draftUI not assigned!", this);
            if (battleUI == null) Debug.LogWarning($"UIManager '{name}': battleUI not assigned!", this);
            if (revealUI == null) Debug.LogWarning($"UIManager '{name}': revealUI not assigned!", this);
            #endif
        }

        /// <summary>
        /// Handles match phase transitions and shows/hides appropriate screens.
        /// </summary>
        private void HandlePhaseChanged(MatchPhase oldPhase, MatchPhase newPhase)
        {
            Debug.Log($"UIManager: Phase transition {oldPhase} → {newPhase}");

            switch (newPhase)
            {
                case MatchPhase.MatchStart:
                    HideAllScreens();
                    break;

                case MatchPhase.Draft:
                    ShowDraftScreen();
                    break;

                case MatchPhase.Reveal:
                    // Reveal phase removed - reveals now happen during draft after each pick
                    break;

                case MatchPhase.Spawn:
                    // Hide draft screen when spawning
                    if (draftUI != null)
                    {
                        draftUI.HideDraftScreen();
                    }
                    break;

                case MatchPhase.Battle:
                    ShowBattleScreen();
                    break;

                case MatchPhase.RoundEnd:
                    // Keep battle screen visible to show victory banner
                    break;

                case MatchPhase.MatchEnd:
                    // Keep battle screen visible to show final results
                    break;
            }
        }

        /// <summary>
        /// Handles per-pick reveals during draft phase.
        /// Called after each pick is made by both sides.
        /// </summary>
        private void HandlePickCompleted(Modules.ICombination playerPick, Modules.ICombination aiPick)
        {
            if (revealUI == null) return;

            // Validate that at least one pick is valid
            if (playerPick == null && aiPick == null)
            {
                Debug.LogWarning("UIManager: Both picks are null, skipping reveal");
                return;
            }

            // Hide draft screen during reveal
            if (draftUI != null)
            {
                draftUI.HideDraftScreen();
            }

            // Determine if this is a comeback bonus pick (only one side picks)
            bool isOneSidedPick = (playerPick == null) != (aiPick == null); // XOR: one is null, other is not
            bool showComebackBonus = isOneSidedPick;

            Debug.Log($"UIManager: Showing per-pick reveal | Player: {playerPick?.DisplayName ?? "None"} | AI: {aiPick?.DisplayName ?? "None"} | Comeback: {showComebackBonus}");

            // Show reveal with error handling
            revealUI.ShowRevealAsync(playerPick, aiPick, showComebackBonus)
                .ContinueWith(() => Debug.Log("UIManager: Reveal completed successfully"))
                .SuppressCancellationThrow();
        }

        private void ShowDraftScreen()
        {
            if (battleUI != null)
            {
                battleUI.HideBattleScreen();
                battleUI.ResetUI(); // Clear victory banner and reset timer
            }

            if (revealUI != null)
            {
                revealUI.HideRevealScreen();
            }

            if (draftUI != null)
            {
                draftUI.ResetUI();
                draftUI.ShowDraftScreen();
            }

            Debug.Log("UIManager: Draft screen shown");
        }

        // ShowRevealScreen removed - reveals now triggered by OnPickCompleted event during draft

        private void ShowBattleScreen()
        {
            if (draftUI != null)
            {
                draftUI.HideDraftScreen();
            }

            if (revealUI != null)
            {
                revealUI.HideRevealScreen();
            }

            if (battleUI != null)
            {
                battleUI.ResetUI(); // Reset UI before showing
                battleUI.ShowBattleScreen();
            }

            Debug.Log("UIManager: Battle screen shown");
        }

        private void HideAllScreens()
        {
            if (draftUI != null)
            {
                draftUI.HideDraftScreen();
            }

            if (revealUI != null)
            {
                revealUI.HideRevealScreen();
            }

            if (battleUI != null)
            {
                battleUI.HideBattleScreen();
            }

            Debug.Log("UIManager: All screens hidden");
        }
    }
}
