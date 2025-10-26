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
        }

        private void UnsubscribeFromEvents()
        {
            if (matchController != null)
            {
                matchController.OnPhaseChanged -= HandlePhaseChanged;
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
                    ShowRevealScreen();
                    break;

                case MatchPhase.Spawn:
                    // Keep reveal screen visible during spawn (brief phase)
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

        private void ShowRevealScreen()
        {
            if (draftUI != null)
            {
                draftUI.HideDraftScreen();
            }

            if (revealUI != null && matchController != null && matchController.State != null)
            {
                var playerSelection = matchController.State.PlayerSelectedCombo;
                var aiSelection = matchController.State.AISelectedCombo;

                if (playerSelection != null && aiSelection != null)
                {
                    // Show reveal screen with animation (fire and forget)
                    revealUI.ShowRevealAsync(playerSelection, aiSelection).Forget();
                }
                else
                {
                    Debug.LogWarning("UIManager: Cannot show reveal - selections are null!");
                }
            }

            Debug.Log("UIManager: Reveal screen shown");
        }

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
