using UnityEngine;
using AdaptiveDraftArena.Modules;

namespace AdaptiveDraftArena.Visual
{
    public class TroopVisuals : MonoBehaviour
    {
        [Header("Sprite Renderers")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer weaponRenderer;

        [Header("Particle Effects")]
        [SerializeField] private Transform particleAnchor;

        private GameObject currentAuraEffect;
        private TroopCombination combination;

        private void Awake()
        {
            // Auto-setup if renderers not assigned
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponent<SpriteRenderer>();
                if (bodyRenderer == null)
                {
                    bodyRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            if (weaponRenderer == null)
            {
                // Create weapon renderer as child
                var weaponObj = new GameObject("Weapon");
                weaponObj.transform.SetParent(transform);
                weaponObj.transform.localPosition = Vector3.zero;
                weaponRenderer = weaponObj.AddComponent<SpriteRenderer>();
                weaponRenderer.sortingOrder = bodyRenderer.sortingOrder + 1;
            }

            if (particleAnchor == null)
            {
                particleAnchor = transform;
            }
        }

        public void Compose(TroopCombination troopCombination)
        {
            combination = troopCombination;

            if (combination == null)
            {
                Debug.LogError("TroopVisuals.Compose called with null combination!");
                return;
            }

            // Set body sprite
            if (combination.body != null && combination.body.bodySprite != null)
            {
                bodyRenderer.sprite = combination.body.bodySprite;
            }

            // Set weapon sprite
            if (combination.weapon != null && combination.weapon.weaponSprite != null)
            {
                weaponRenderer.sprite = combination.weapon.weaponSprite;

                // Position weapon at anchor point
                if (combination.body != null)
                {
                    weaponRenderer.transform.localPosition = combination.body.weaponAnchorPoint;
                }

                // Apply weapon offset
                weaponRenderer.transform.localPosition += (Vector3)combination.weapon.spriteOffset;
            }

            // Apply element color tint
            if (combination.effect != null)
            {
                bodyRenderer.color = combination.effect.tintColor;

                // Spawn aura particle effect
                if (combination.effect.auraPrefab != null)
                {
                    currentAuraEffect = Instantiate(combination.effect.auraPrefab, particleAnchor);
                    currentAuraEffect.transform.localPosition = Vector3.zero;
                }
            }

            // Apply size scaling
            if (combination.body != null)
            {
                transform.localScale = Vector3.one * combination.body.size;
            }
        }

        public void PlayHitEffect()
        {
            // Flash white briefly
            if (bodyRenderer != null)
            {
                StartCoroutine(FlashRoutine());
            }
        }

        private System.Collections.IEnumerator FlashRoutine()
        {
            var originalColor = bodyRenderer.color;
            bodyRenderer.color = Color.white;

            yield return new WaitForSeconds(0.1f);

            bodyRenderer.color = originalColor;
        }

        private void OnDestroy()
        {
            if (currentAuraEffect != null)
            {
                Destroy(currentAuraEffect);
            }
        }
    }
}
