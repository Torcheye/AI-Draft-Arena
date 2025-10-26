using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using AdaptiveDraftArena.Modules;

namespace AdaptiveDraftArena.UI
{
    /// <summary>
    /// Individual draft card displaying a troop combination with hover/click interactions.
    /// </summary>
    public class DraftCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Module Icons")]
        [SerializeField] private Image bodyIcon;
        [SerializeField] private Image weaponIcon;
        [SerializeField] private Image abilityIcon;
        [SerializeField] private Image effectIcon;

        [Header("Text Elements")]
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text dmgText;

        [Header("Visual Feedback")]
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image selectionGlow;
        [SerializeField] private Button button;

        [Header("Animation Settings")]
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float hoverDuration = 0.2f;

        private ICombination currentCombination;
        private bool isSelected;
        private Tweener scaleTween;
        private Tweener glowTween;
        private System.Collections.Generic.Dictionary<Color, Color> tintCache = new System.Collections.Generic.Dictionary<Color, Color>();

        public event Action<DraftCard> OnCardClicked;

        private void Awake()
        {
            ValidateRequiredFields();

            if (button != null)
            {
                button.onClick.AddListener(HandleClick);
            }

            if (selectionGlow != null)
            {
                selectionGlow.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }

            // Complete tweens to ensure cleanup callbacks execute
            scaleTween?.Kill(true);
            glowTween?.Kill(true);
        }

        private void ValidateRequiredFields()
        {
            #if UNITY_EDITOR
            if (bodyIcon == null) Debug.LogWarning($"DraftCard '{name}': bodyIcon not assigned", this);
            if (weaponIcon == null) Debug.LogWarning($"DraftCard '{name}': weaponIcon not assigned", this);
            if (abilityIcon == null) Debug.LogWarning($"DraftCard '{name}': abilityIcon not assigned", this);
            if (effectIcon == null) Debug.LogWarning($"DraftCard '{name}': effectIcon not assigned", this);
            if (amountText == null) Debug.LogWarning($"DraftCard '{name}': amountText not assigned", this);
            if (nameText == null) Debug.LogWarning($"DraftCard '{name}': nameText not assigned", this);
            if (hpText == null) Debug.LogWarning($"DraftCard '{name}': hpText not assigned", this);
            if (dmgText == null) Debug.LogWarning($"DraftCard '{name}': dmgText not assigned", this);
            if (cardBackground == null) Debug.LogWarning($"DraftCard '{name}': cardBackground not assigned", this);
            if (selectionGlow == null) Debug.LogWarning($"DraftCard '{name}': selectionGlow not assigned", this);
            if (button == null) Debug.LogWarning($"DraftCard '{name}': button not assigned", this);
            #endif
        }

        /// <summary>
        /// Populates the card with troop combination data.
        /// </summary>
        public void SetCombination(ICombination combination)
        {
            if (combination == null)
            {
                Debug.LogError("DraftCard: Cannot set null combination!");
                return;
            }

            if (!combination.IsValid())
            {
                Debug.LogWarning($"DraftCard: Invalid combination - missing modules!");
            }

            currentCombination = combination;

            // Set module icons
            if (bodyIcon != null && combination.body != null)
            {
                bodyIcon.sprite = combination.body.icon;
                bodyIcon.color = Color.white;
            }

            if (weaponIcon != null && combination.weapon != null)
            {
                weaponIcon.sprite = combination.weapon.icon;
                weaponIcon.color = Color.white;
            }

            if (abilityIcon != null && combination.ability != null)
            {
                abilityIcon.sprite = combination.ability.icon;
                abilityIcon.color = Color.white;
            }

            // Effect icon gets tinted with element color for visual distinction
            if (effectIcon != null && combination.effect != null)
            {
                effectIcon.sprite = combination.effect.icon;
                effectIcon.color = combination.effect.tintColor;
            }

            // Set text elements using SetText to reduce allocations
            if (amountText != null)
            {
                amountText.SetText("×{0}", combination.amount);
            }

            if (nameText != null)
            {
                nameText.text = combination.DisplayName;
            }

            if (hpText != null)
            {
                var intHp = Mathf.RoundToInt(combination.GetFinalHP());
                hpText.SetText(intHp.ToString());
            }

            if (dmgText != null)
            {
                var intDmg = Mathf.RoundToInt(combination.GetFinalDamage());
                dmgText.SetText(intDmg.ToString());
            }

            // Tint card background based on element color (cached for performance)
            if (cardBackground != null && combination.effect != null)
            {
                cardBackground.color = GetCachedTint(combination.effect.tintColor);
            }
        }

        /// <summary>
        /// Enables or disables card interaction.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        /// <summary>
        /// Shows selection glow effect.
        /// </summary>
        public void ShowSelected()
        {
            if (selectionGlow == null) return;

            isSelected = true;
            selectionGlow.gameObject.SetActive(true);

            // Fade in glow
            glowTween?.Kill();
            selectionGlow.color = new Color(1f, 1f, 1f, 0f);
            glowTween = selectionGlow.DOFade(0.6f, 0.3f).SetEase(Ease.OutQuad);

            // Disable interaction after selection
            SetInteractable(false);
        }

        /// <summary>
        /// Hides selection glow effect.
        /// </summary>
        public void HideSelected()
        {
            if (selectionGlow == null) return;

            isSelected = false;

            glowTween?.Kill();
            glowTween = selectionGlow.DOFade(0f, 0.2f)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    // Null check to prevent issues if card destroyed mid-animation
                    if (selectionGlow != null)
                        selectionGlow.gameObject.SetActive(false);
                })
                .SetAutoKill(true);
        }

        /// <summary>
        /// Resets card to default state.
        /// </summary>
        public void ResetCard()
        {
            HideSelected();
            SetInteractable(true);
            transform.localScale = Vector3.one;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button == null || !button.interactable || isSelected) return;

            // Scale up with bounce
            scaleTween?.Kill();
            scaleTween = transform.DOScale(hoverScale, hoverDuration).SetEase(Ease.OutBack);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isSelected) return;

            // Scale back down
            scaleTween?.Kill();
            scaleTween = transform.DOScale(1f, hoverDuration).SetEase(Ease.OutQuad);
        }

        private void HandleClick()
        {
            if (currentCombination == null)
            {
                Debug.LogWarning("DraftCard: Cannot click - no combination set!");
                return;
            }

            OnCardClicked?.Invoke(this);
        }

        public ICombination GetCombination()
        {
            return currentCombination;
        }

        /// <summary>
        /// Caches and returns a tinted color for card background based on effect color.
        /// Reduces redundant Color.Lerp calculations for performance.
        /// </summary>
        private Color GetCachedTint(Color effectColor)
        {
            if (!tintCache.TryGetValue(effectColor, out var tint))
            {
                var tintColor = effectColor;
                tintColor.a = 0.15f;
                tint = Color.Lerp(Color.white, tintColor, 0.3f);
                tintCache[effectColor] = tint;
            }
            return tint;
        }
    }
}
