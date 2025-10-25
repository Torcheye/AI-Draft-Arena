using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using AdaptiveDraftArena.Draft;
using AdaptiveDraftArena.Modules;

namespace AdaptiveDraftArena.UI
{
    /// <summary>
    /// Draft screen controller managing card display, timer, and user interactions.
    /// </summary>
    public class DraftUI : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private DraftController draftController;

        [Header("Draft Cards")]
        [SerializeField] private DraftCard[] draftCards;

        [Header("Text Elements")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private TMP_Text titleText;

        [Header("Screen Control")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("UI Text")]
        [SerializeField] private string defaultPromptText = "Click a card to select";

        [Header("Timer Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color warningColor = new Color(1f, 0.92f, 0.016f); // Yellow
        [SerializeField] private Color urgentColor = new Color(1f, 0.32f, 0.32f); // Red

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.4f;
        [SerializeField] private float fadeOutDuration = 0.3f;

        private DraftCard selectedCard;
        private Tweener timerPulseTween;
        private Tweener fadeInTween;
        private Tweener fadeOutTween;

        private void Start()
        {
            if (draftController == null)
            {
                Debug.LogError("DraftUI: DraftController not assigned in Inspector!", this);
                enabled = false;
                return;
            }

            SubscribeToEvents();
            ValidateReferences();

            // Subscribe to card click events
            if (draftCards != null)
            {
                foreach (var card in draftCards)
                {
                    if (card != null)
                    {
                        card.OnCardClicked += HandleCardClicked;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            // Unsubscribe from card events
            if (draftCards != null)
            {
                foreach (var card in draftCards)
                {
                    if (card != null)
                    {
                        card.OnCardClicked -= HandleCardClicked;
                    }
                }
            }

            // Kill all tweens
            timerPulseTween?.Kill();
            fadeInTween?.Kill();
            fadeOutTween?.Kill();
        }

        private void SubscribeToEvents()
        {
            if (draftController == null) return;

            draftController.OnPlayerOptionsGenerated += DisplayDraftOptions;
            draftController.OnTimerUpdated += UpdateTimer;
            draftController.OnTimerWarning += TriggerTimerWarning;
            draftController.OnPlayerSelected += OnPlayerSelected;
            draftController.OnDraftCompleted += OnDraftCompleted;
        }

        private void UnsubscribeFromEvents()
        {
            if (draftController == null) return;

            draftController.OnPlayerOptionsGenerated -= DisplayDraftOptions;
            draftController.OnTimerUpdated -= UpdateTimer;
            draftController.OnTimerWarning -= TriggerTimerWarning;
            draftController.OnPlayerSelected -= OnPlayerSelected;
            draftController.OnDraftCompleted -= OnDraftCompleted;
        }

        private void ValidateReferences()
        {
            #if UNITY_EDITOR
            if (draftCards == null || draftCards.Length == 0)
            {
                Debug.LogWarning($"DraftUI '{name}': No draft cards assigned!", this);
            }

            if (draftController == null) Debug.LogWarning($"DraftUI '{name}': draftController not assigned!", this);
            if (timerText == null) Debug.LogWarning($"DraftUI '{name}': timerText not assigned!", this);
            if (promptText == null) Debug.LogWarning($"DraftUI '{name}': promptText not assigned!", this);
            if (canvasGroup == null) Debug.LogWarning($"DraftUI '{name}': canvasGroup not assigned!", this);
            #endif
        }

        /// <summary>
        /// Displays draft options on cards.
        /// </summary>
        private void DisplayDraftOptions(List<TroopCombination> options)
        {
            if (options == null || options.Count == 0)
            {
                Debug.LogError("DraftUI: Received null or empty options list!");
                return;
            }

            if (draftCards == null || draftCards.Length == 0)
            {
                Debug.LogError("DraftUI: No draft cards available!");
                return;
            }

            // Ensure we have enough cards
            var displayCount = Mathf.Min(options.Count, draftCards.Length);

            for (var i = 0; i < displayCount; i++)
            {
                if (draftCards[i] != null && options[i] != null)
                {
                    draftCards[i].SetCombination(options[i]);
                    draftCards[i].SetInteractable(true);
                    draftCards[i].ResetCard();
                }
            }

            // Fade in draft screen
            ShowDraftScreen();

            Debug.Log($"DraftUI: Displayed {displayCount} draft options");
        }

        /// <summary>
        /// Updates timer display with color transitions.
        /// </summary>
        private void UpdateTimer(float remainingTime)
        {
            if (timerText == null) return;

            // Update text
            var seconds = Mathf.CeilToInt(remainingTime);
            timerText.SetText("{0}", seconds);

            // Color transitions based on remaining time
            if (remainingTime <= 5f)
            {
                timerText.color = urgentColor;
            }
            else if (remainingTime <= 10f)
            {
                timerText.color = warningColor;
            }
            else
            {
                timerText.color = normalColor;
            }
        }

        /// <summary>
        /// Triggers warning animation at 5 seconds.
        /// </summary>
        private void TriggerTimerWarning()
        {
            if (timerText == null) return;

            // Pulse animation - bouncy and attention-grabbing
            timerPulseTween?.Kill();
            timerPulseTween = timerText.transform
                .DOPunchScale(Vector3.one * 0.3f, 0.6f, 5, 0.5f)
                .SetEase(Ease.OutBounce);

            // Optional: Screen shake or other feedback
            Debug.Log("DraftUI: Timer warning triggered!");
        }

        /// <summary>
        /// Handles player selection feedback.
        /// </summary>
        private void OnPlayerSelected(TroopCombination selected)
        {
            if (selectedCard != null)
            {
                selectedCard.ShowSelected();
            }

            // Disable all other cards
            foreach (var card in draftCards)
            {
                if (card != null && card != selectedCard)
                {
                    card.SetInteractable(false);
                }
            }

            // Update prompt text
            if (promptText != null)
            {
                promptText.text = $"Selected: {selected.DisplayName}";
            }

            Debug.Log($"DraftUI: Player selected {selected.DisplayName}");
        }

        /// <summary>
        /// Handles draft completion and fades out screen.
        /// </summary>
        private void OnDraftCompleted(TroopCombination playerPick, TroopCombination aiPick)
        {
            Debug.Log($"DraftUI: Draft completed - Fading out screen");
            HideDraftScreen();
        }

        /// <summary>
        /// Handles card click and notifies DraftController.
        /// </summary>
        private void HandleCardClicked(DraftCard card)
        {
            if (card == null || draftController == null) return;

            selectedCard = card;

            var combination = card.GetCombination();
            if (combination != null)
            {
                draftController.SelectCombination(combination);
            }
        }

        /// <summary>
        /// Shows draft screen with fade-in animation.
        /// </summary>
        public void ShowDraftScreen()
        {
            if (canvasGroup == null)
            {
                gameObject.SetActive(true);
                return;
            }

            fadeInTween?.Kill();
            gameObject.SetActive(true);
            canvasGroup.alpha = 0f;
            fadeInTween = canvasGroup.DOFade(1f, fadeInDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject)
                .SetAutoKill(true);
        }

        /// <summary>
        /// Hides draft screen with fade-out animation.
        /// </summary>
        public void HideDraftScreen()
        {
            if (canvasGroup == null)
            {
                gameObject.SetActive(false);
                return;
            }

            fadeOutTween?.Kill();
            fadeOutTween = canvasGroup.DOFade(0f, fadeOutDuration)
                .SetEase(Ease.InQuad)
                .SetLink(gameObject)
                .SetAutoKill(true)
                .OnComplete(() =>
                {
                    // Null checks to prevent issues if destroyed mid-animation
                    if (this != null && gameObject != null)
                        gameObject.SetActive(false);
                });
        }

        /// <summary>
        /// Resets UI for next draft phase.
        /// </summary>
        public void ResetUI()
        {
            selectedCard = null;

            if (draftCards != null)
            {
                foreach (var card in draftCards)
                {
                    if (card != null)
                    {
                        card.ResetCard();
                    }
                }
            }

            if (timerText != null)
            {
                timerText.color = normalColor;
            }

            if (promptText != null)
            {
                promptText.text = defaultPromptText;
            }
        }
    }
}
