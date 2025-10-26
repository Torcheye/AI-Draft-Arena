using UnityEngine;
using DG.Tweening;
using AdaptiveDraftArena.Modules;
using AdaptiveDraftArena.Combat;

namespace AdaptiveDraftArena.Visual
{
    public class TroopVisuals : MonoBehaviour
    {
        [Header("3D Models")]
        [SerializeField] private GameObject bodyModel;
        [SerializeField] private GameObject weaponModel;
        [SerializeField] private Transform weaponSocket;

        [Header("Particle Effects")]
        [SerializeField] private Transform particleAnchor;

        [Header("Health Bar")]
        [SerializeField] HealthBarUI healthBarUI;

        private GameObject currentAuraEffect;
        private ICombination combination;
        private Renderer[] bodyRenderers;
        private Renderer[] weaponRenderers;
        private MaterialPropertyBlock propertyBlock;
        private Color[][] cachedOriginalColors;
        private Sequence hitEffectSequence;
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        // Visual enhancement components
        private TroopAnimationController animationController;
        private TroopController cachedTroopController;

        public void Compose(ICombination troopCombination)
        {
            combination = troopCombination;

            if (combination == null)
            {
                Debug.LogError("TroopVisuals.Compose called with null combination!");
                return;
            }

            // Clean up existing models
            if (bodyModel != null) Destroy(bodyModel);
            if (weaponModel != null) Destroy(weaponModel);
            if (weaponSocket != null) Destroy(weaponSocket.gameObject);

            // Instantiate body model
            if (combination.body != null && combination.body.bodyModelPrefab != null)
            {
                bodyModel = Instantiate(combination.body.bodyModelPrefab, transform);
                bodyModel.transform.localPosition = Vector3.zero;
                bodyModel.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                bodyRenderers = bodyModel.GetComponentsInChildren<Renderer>();

                // Create weapon socket at specified position
                var socketObj = new GameObject("WeaponSocket");
                socketObj.transform.SetParent(bodyModel.transform);
                socketObj.transform.localPosition = combination.body.weaponSocketPosition;
                socketObj.transform.localRotation = Quaternion.identity;
                weaponSocket = socketObj.transform;
            }
            else
            {
                // Fallback: create simple cube placeholder
                bodyModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bodyModel.transform.SetParent(transform);
                bodyModel.transform.localPosition = Vector3.zero;
                bodyRenderers = bodyModel.GetComponentsInChildren<Renderer>();

                // Create weapon socket at default position
                var socketObj = new GameObject("WeaponSocket");
                socketObj.transform.SetParent(bodyModel.transform);
                socketObj.transform.localPosition = new Vector3(0.5f, 0.5f, 0f);
                weaponSocket = socketObj.transform;
            }

            // Instantiate weapon model
            if (combination.weapon != null && weaponSocket != null)
            {
                if (combination.weapon.weaponModelPrefab != null)
                {
                    weaponModel = Instantiate(combination.weapon.weaponModelPrefab, weaponSocket);
                    //weaponModel.transform.localPosition = combination.weapon.modelOffset;
                    //weaponModel.transform.localRotation = Quaternion.Euler(combination.weapon.modelRotation);
                    weaponRenderers = weaponModel.GetComponentsInChildren<Renderer>();
                }
                else
                {
                    // Fallback: create small cube for weapon
                    weaponModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    weaponModel.transform.SetParent(weaponSocket);
                    weaponModel.transform.localPosition = Vector3.zero;
                    weaponModel.transform.localScale = Vector3.one * 0.3f;
                    weaponRenderers = weaponModel.GetComponentsInChildren<Renderer>();
                }
            }

            // Apply element color tint to all renderers
            if (combination.effect != null)
            {
                ApplyTint(combination.effect.tintColor);

                // Spawn aura particle effect
                if (combination.effect.auraPrefab != null)
                {
                    if (particleAnchor == null) particleAnchor = transform;
                    currentAuraEffect = Instantiate(combination.effect.auraPrefab, particleAnchor);
                    currentAuraEffect.transform.localPosition = Vector3.zero;
                }
            }

            // Apply size scaling
            if (combination.body != null)
            {
                transform.localScale = Vector3.one * combination.body.size;
            }

            // Cache original colors for flash effect
            CacheOriginalColors();

            // Initialize visual enhancements
            InitializeVisualEnhancements();
            
            healthBarUI.Initialize(transform, GetComponent<HealthComponent>());
        }

        private void ApplyTint(Color tint)
        {
            if (bodyRenderers == null || bodyRenderers.Length == 0)
                return;

            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            foreach (var renderer in bodyRenderers)
            {
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(ColorProperty, tint);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void CacheOriginalColors()
        {
            if (bodyRenderers == null || bodyRenderers.Length == 0)
                return;

            cachedOriginalColors = new Color[bodyRenderers.Length][];
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                var materials = bodyRenderers[i].materials;
                cachedOriginalColors[i] = new Color[materials.Length];
            }
        }

        private void InitializeVisualEnhancements()
        {
            cachedTroopController = GetComponent<TroopController>();
            if (cachedTroopController == null) return;

            // Initialize animation controller
            animationController = gameObject.AddComponent<TroopAnimationController>();
            animationController.Initialize(
                bodyModel?.transform,
                weaponModel?.transform,
                cachedTroopController.Movement
            );
            
            // Subscribe to events
            if (cachedTroopController != null)
            {
                if (cachedTroopController.Combat != null)
                {
                    cachedTroopController.Combat.OnAttack += HandleAttack;
                }

                if (cachedTroopController.Health != null)
                {
                    cachedTroopController.Health.OnTakeDamage += HandleDamage;
                }
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            if (cachedTroopController != null)
            {
                if (cachedTroopController.Combat != null)
                {
                    cachedTroopController.Combat.OnAttack -= HandleAttack;
                }

                if (cachedTroopController.Health != null)
                {
                    cachedTroopController.Health.OnTakeDamage -= HandleDamage;
                }
            }

            CleanupVisualEffects();
        }

        private void HandleAttack()
        {
            if (animationController != null)
            {
                animationController.PlayAttackAnimation();
            }
        }

        private void HandleDamage(float damage, GameObject attacker)
        {
            PlayHitEffect();
        }

        public void PlayHitEffect()
        {
            if (bodyRenderers == null || bodyRenderers.Length == 0)
                return;

            // Kill any existing hit effect sequence
            hitEffectSequence?.Kill();

            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            // Create enhanced hit effect sequence
            hitEffectSequence = DOTween.Sequence();

            // // Flash white using MaterialPropertyBlock (0.05s)
            // hitEffectSequence.AppendCallback(() =>
            // {
            //     foreach (var renderer in bodyRenderers)
            //     {
            //         renderer.GetPropertyBlock(propertyBlock);
            //         propertyBlock.SetColor(ColorProperty, Color.white);
            //         renderer.SetPropertyBlock(propertyBlock);
            //     }
            // });

            // Scale punch effect (10% increase)
            if (bodyModel != null)
            {
                var originalScale = bodyModel.transform.localScale;
                hitEffectSequence.Join(
                    bodyModel.transform.DOPunchScale(originalScale * 0.1f, 0.15f, 1, 0.5f)
                );
            }

            // Wait for flash duration
            //hitEffectSequence.AppendInterval(0.05f);

            // Restore original tint color
            // hitEffectSequence.AppendCallback(() =>
            // {
            //     if (combination?.effect != null)
            //     {
            //         ApplyTint(combination.effect.tintColor);
            //     }
            // });
        }

        private void OnDestroy()
        {
            if (currentAuraEffect != null)
            {
                Destroy(currentAuraEffect);
            }
        }

        private void CleanupVisualEffects()
        {
            hitEffectSequence?.Kill();
        }
    }
}
