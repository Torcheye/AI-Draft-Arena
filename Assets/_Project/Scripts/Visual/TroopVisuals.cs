using UnityEngine;
using AdaptiveDraftArena.Modules;

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

        private GameObject currentAuraEffect;
        private ICombination combination;
        private Renderer[] bodyRenderers;
        private Renderer[] weaponRenderers;
        private MaterialPropertyBlock propertyBlock;
        private Color[][] cachedOriginalColors;
        private Coroutine flashCoroutine;
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

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
                bodyModel.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                bodyRenderers = bodyModel.GetComponentsInChildren<Renderer>();

                // Create weapon socket at specified position
                var socketObj = new GameObject("WeaponSocket");
                socketObj.transform.SetParent(transform);
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
                    weaponModel.transform.localPosition = combination.weapon.modelOffset;
                    weaponModel.transform.localRotation = Quaternion.Euler(combination.weapon.modelRotation);
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

        public void PlayHitEffect()
        {
            if (bodyRenderers == null || bodyRenderers.Length == 0)
                return;

            if (flashCoroutine != null)
                StopCoroutine(flashCoroutine);

            flashCoroutine = StartCoroutine(FlashRoutine());
        }

        private System.Collections.IEnumerator FlashRoutine()
        {
            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            // Store original colors and apply white
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                var materials = bodyRenderers[i].materials;
                for (int j = 0; j < materials.Length; j++)
                {
                    cachedOriginalColors[i][j] = materials[j].color;
                    materials[j].color = Color.white;
                }
            }

            yield return new WaitForSeconds(0.1f);

            // Restore original colors using MaterialPropertyBlock
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                bodyRenderers[i].GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(ColorProperty, cachedOriginalColors[i][0]);
                bodyRenderers[i].SetPropertyBlock(propertyBlock);
            }

            flashCoroutine = null;
        }

        private void OnDisable()
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
            }
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
