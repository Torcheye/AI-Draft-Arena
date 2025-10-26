using UnityEngine;
using TMPro;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using AdaptiveDraftArena.Modules;

namespace AdaptiveDraftArena.UI
{
    /// <summary>
    /// Reveal screen showing player and AI selections side-by-side before battle.
    /// </summary>
    public class RevealUI : MonoBehaviour
    {
        [Header("Card Displays")]
        [SerializeField] private DraftCard playerCard;
        [SerializeField] private DraftCard aiCard;

        [Header("Screen Control")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation Settings")]
        [SerializeField] private float slideInDuration = 0.5f;
        [SerializeField] private float holdDuration = 2f;
        [SerializeField] private float fadeOutDuration = 0.3f;
        [SerializeField] private float cardSlideDistance = 800f;

        private RectTransform playerCardRect;
        private RectTransform aiCardRect;
        private Tweener playerSlideTween;
        private Tweener aiSlideTween;
        private Tweener fadeTween;

        private void Awake()
        {
            if (playerCard != null)
            {
                playerCardRect = playerCard.GetComponent<RectTransform>();
            }

            if (aiCard != null)
            {
                aiCardRect = aiCard.GetComponent<RectTransform>();
            }

            // Hide initially
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void OnDestroy()
        {
            // Kill all tweens
            playerSlideTween?.Kill();
            aiSlideTween?.Kill();
            fadeTween?.Kill();
        }

        private void Start()
        {
            ValidateReferences();
        }

        private void ValidateReferences()
        {
            #if UNITY_EDITOR
            if (playerCard == null) Debug.LogWarning($"RevealUI '{name}': playerCard not assigned!", this);
            if (aiCard == null) Debug.LogWarning($"RevealUI '{name}': aiCard not assigned!", this);
            if (canvasGroup == null) Debug.LogWarning($"RevealUI '{name}': canvasGroup not assigned!", this);
            #endif
        }

        /// <summary>
        /// Shows reveal screen with player and AI selections, then auto-hides after duration.
        /// </summary>
        public async UniTask ShowRevealAsync(ICombination playerSelection, ICombination aiSelection)
        {
            if (playerSelection == null || aiSelection == null)
            {
                Debug.LogError("RevealUI: Cannot show reveal with null selections!");
                return;
            }

            // Set card data
            if (playerCard != null)
            {
                playerCard.SetCombination(playerSelection);
                playerCard.SetInteractable(false); // Not clickable during reveal
            }

            if (aiCard != null)
            {
                aiCard.SetCombination(aiSelection);
                aiCard.SetInteractable(false);
            }

            // Show and animate (fade in canvas group)
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            // Animate cards sliding in
            await AnimateCardsIn();

            // Hold for duration
            await UniTask.Delay(System.TimeSpan.FromSeconds(holdDuration));

            // Fade out
            await AnimateOut();

            Debug.Log($"RevealUI: Shown {playerSelection.DisplayName} vs {aiSelection.DisplayName}");
        }

        private async UniTask AnimateCardsIn()
        {
            if (playerCardRect != null)
            {
                // Start player card off-screen left
                var playerOriginalPos = playerCardRect.anchoredPosition;
                playerCardRect.anchoredPosition = new Vector2(playerOriginalPos.x - cardSlideDistance, playerOriginalPos.y);

                // Slide in from left
                playerSlideTween?.Kill();
                playerSlideTween = playerCardRect.DOAnchorPos(playerOriginalPos, slideInDuration)
                    .SetEase(Ease.OutBack)
                    .SetLink(gameObject)
                    .SetAutoKill(true);
            }

            if (aiCardRect != null)
            {
                // Start AI card off-screen right
                var aiOriginalPos = aiCardRect.anchoredPosition;
                aiCardRect.anchoredPosition = new Vector2(aiOriginalPos.x + cardSlideDistance, aiOriginalPos.y);

                // Slide in from right
                aiSlideTween?.Kill();
                aiSlideTween = aiCardRect.DOAnchorPos(aiOriginalPos, slideInDuration)
                    .SetEase(Ease.OutBack)
                    .SetLink(gameObject)
                    .SetAutoKill(true);
            }

            // Wait for animations to complete
            await UniTask.Delay(System.TimeSpan.FromSeconds(slideInDuration));
        }

        private async UniTask AnimateOut()
        {
            if (canvasGroup == null) return;

            fadeTween?.Kill();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            fadeTween = canvasGroup.DOFade(0f, fadeOutDuration)
                .SetEase(Ease.InQuad)
                .SetLink(gameObject)
                .SetAutoKill(true);

            await UniTask.Delay(System.TimeSpan.FromSeconds(fadeOutDuration));
        }

        /// <summary>
        /// Shows reveal screen (without animation, for manual control).
        /// </summary>
        public void ShowRevealScreen()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        /// <summary>
        /// Hides reveal screen with fade-out animation.
        /// </summary>
        public void HideRevealScreen()
        {
            if (canvasGroup != null)
            {
                fadeTween?.Kill();
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                fadeTween = canvasGroup.DOFade(0f, fadeOutDuration)
                    .SetEase(Ease.InQuad)
                    .SetLink(gameObject)
                    .SetAutoKill(true);
            }
        }

        /// <summary>
        /// Resets UI for next reveal.
        /// </summary>
        public void ResetUI()
        {
            if (playerCardRect != null)
            {
                playerCardRect.anchoredPosition = Vector2.zero;
            }

            if (aiCardRect != null)
            {
                aiCardRect.anchoredPosition = Vector2.zero;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }
    }
}
