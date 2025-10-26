using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using AdaptiveDraftArena.Battle;
using AdaptiveDraftArena.Combat;

namespace AdaptiveDraftArena.UI
{
    /// <summary>
    /// Battle screen controller managing timer, HP display, and victory banner.
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private BattleController battleController;

        [Header("Screen Control")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Timer Display")]
        [SerializeField] private TMP_Text timerText;

        [Header("Victory Banner")]
        [SerializeField] private GameObject victoryBanner;
        [SerializeField] private TMP_Text winnerText;
        [SerializeField] private CanvasGroup bannerCanvasGroup;

        [Header("UI Text")]
        [SerializeField] private string victoryText = "VICTORY!";
        [SerializeField] private string defeatText = "DEFEAT";

        [Header("Timer Settings")]
        [SerializeField] private float warningThreshold = 10f;
        [SerializeField] private float urgentThreshold = 5f;

        [Header("Timer Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color warningColor = new Color(1f, 0.92f, 0.016f); // Yellow
        [SerializeField] private Color urgentColor = new Color(1f, 0.32f, 0.32f); // Red

        [Header("HP Bar Colors")]
        [SerializeField] private Color playerHPColor = new Color(0.13f, 0.59f, 0.95f); // Blue
        [SerializeField] private Color aiHPColor = new Color(0.96f, 0.26f, 0.21f); // Red

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.4f;
        [SerializeField] private float fadeOutDuration = 0.3f;
        [SerializeField] private float bannerFadeDuration = 0.6f;
        [SerializeField] private float bannerScaleDelay = 0.2f;
        [SerializeField] private float bannerScaleDuration = 0.5f;

        private float maxPlayerHP;
        private float maxAIHP;
        private Tweener fadeInTween;
        private Tweener fadeOutTween;
        private Tweener bannerFadeTween;
        private Tweener bannerScaleTween;
        private Image playerHPFill;
        private Image aiHPFill;

        private void Awake()
        {
            // Hide screen and victory banner initially
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (victoryBanner != null)
            {
                victoryBanner.SetActive(false);
            }
        }

        private void Start()
        {
            if (battleController == null)
            {
                Debug.LogError("BattleUI: BattleController not assigned in Inspector!", this);
                enabled = false;
                return;
            }

            ValidateReferences();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            // Kill all tweens
            fadeInTween?.Kill();
            fadeOutTween?.Kill();
            bannerFadeTween?.Kill();
            bannerScaleTween?.Kill();
        }

        private void SubscribeToEvents()
        {
            if (battleController == null) return;

            battleController.OnBattleStarted += OnBattleStarted;
            battleController.OnTimerUpdated += UpdateTimer;
            battleController.OnBattleEnded += OnBattleEnded;
        }

        private void UnsubscribeFromEvents()
        {
            if (battleController == null) return;

            battleController.OnBattleStarted -= OnBattleStarted;
            battleController.OnTimerUpdated -= UpdateTimer;
            battleController.OnBattleEnded -= OnBattleEnded;
        }

        private void ValidateReferences()
        {
            #if UNITY_EDITOR
            if (battleController == null) Debug.LogWarning($"BattleUI '{name}': battleController not assigned!", this);
            if (canvasGroup == null) Debug.LogWarning($"BattleUI '{name}': canvasGroup not assigned!", this);
            if (timerText == null) Debug.LogWarning($"BattleUI '{name}': timerText not assigned!", this);
            if (victoryBanner == null) Debug.LogWarning($"BattleUI '{name}': victoryBanner not assigned!", this);
            if (winnerText == null) Debug.LogWarning($"BattleUI '{name}': winnerText not assigned!", this);
            if (bannerCanvasGroup == null) Debug.LogWarning($"BattleUI '{name}': bannerCanvasGroup not assigned!", this);
            #endif
        }

        private void OnBattleStarted()
        {
            // Initialize max HP based on starting troops
            maxPlayerHP = TargetingSystem.GetTotalHP(Team.Player);
            maxAIHP = TargetingSystem.GetTotalHP(Team.AI);

            // Hide victory banner
            if (victoryBanner != null)
            {
                victoryBanner.SetActive(false);
            }

            Debug.Log($"BattleUI: Battle started - Player HP: {maxPlayerHP:F1} | AI HP: {maxAIHP:F1}");
        }

        private void UpdateTimer(float remainingTime)
        {
            if (timerText == null) return;

            // Update text
            var seconds = Mathf.CeilToInt(remainingTime);
            timerText.SetText("{0}", seconds);

            // Color transitions based on remaining time
            if (remainingTime <= urgentThreshold)
            {
                timerText.color = urgentColor;
            }
            else if (remainingTime <= warningThreshold)
            {
                timerText.color = warningColor;
            }
            else
            {
                timerText.color = normalColor;
            }
        }

        private void OnBattleEnded(Team winner, bool timerExpired, float playerHP, float aiHP)
        {
            ShowVictoryBanner(winner, timerExpired);

            Debug.Log($"BattleUI: Battle ended - Winner: {winner} | " +
                      $"Timer Expired: {timerExpired} | Player HP: {playerHP:F1} | AI HP: {aiHP:F1}");
        }

        private void ShowVictoryBanner(Team winner, bool timerExpired)
        {
            if (victoryBanner == null) return;

            // Set winner text
            if (winnerText != null)
            {
                winnerText.text = winner == Team.Player ? victoryText : defeatText;
            }

            // Show and animate banner
            victoryBanner.SetActive(true);

            if (bannerCanvasGroup != null)
            {
                // Fade in canvas group
                bannerFadeTween?.Kill();
                bannerCanvasGroup.alpha = 0f;
                bannerFadeTween = bannerCanvasGroup.DOFade(1f, bannerFadeDuration)
                    .SetEase(Ease.OutCubic)
                    .SetLink(gameObject)
                    .SetAutoKill(true);
            }

            if (winnerText != null)
            {
                // Scale up winner text with bounce
                bannerScaleTween?.Kill();
                winnerText.transform.localScale = Vector3.zero;
                bannerScaleTween = winnerText.transform.DOScale(1f, bannerScaleDuration)
                    .SetEase(Ease.OutBack)
                    .SetDelay(bannerScaleDelay)
                    .SetLink(gameObject)
                    .SetAutoKill(true);
            }
        }

        /// <summary>
        /// Shows battle screen with fade-in animation.
        /// </summary>
        public void ShowBattleScreen()
        {
            if (canvasGroup == null)
            {
                return;
            }

            fadeInTween?.Kill();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            fadeInTween = canvasGroup.DOFade(1f, fadeInDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject)
                .SetAutoKill(true);
        }

        /// <summary>
        /// Hides battle screen with fade-out animation.
        /// </summary>
        public void HideBattleScreen()
        {
            if (canvasGroup == null)
            {
                return;
            }

            fadeOutTween?.Kill();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            fadeOutTween = canvasGroup.DOFade(0f, fadeOutDuration)
                .SetEase(Ease.InQuad)
                .SetLink(gameObject)
                .SetAutoKill(true);
        }

        /// <summary>
        /// Resets UI for next battle.
        /// </summary>
        public void ResetUI()
        {
            if (timerText != null)
            {
                timerText.color = normalColor;
            }

            if (victoryBanner != null)
            {
                victoryBanner.SetActive(false);
            }
        }
    }
}
