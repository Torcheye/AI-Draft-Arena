using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AdaptiveDraftArena.Combat;

namespace AdaptiveDraftArena.Visual
{
    // World-space health bar UI that follows troops and displays health percentage
    public class HealthBarUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image fillImage;

        [Header("Settings")]
        [SerializeField] private Gradient healthColorGradient;

        private Transform troopTransform;
        private HealthComponent health;
        private Camera mainCamera;

        private void Awake()
        {
            mainCamera = Camera.main;

            // Setup default gradient if not assigned
            if (healthColorGradient == null || healthColorGradient.colorKeys.Length == 0)
            {
                healthColorGradient = new Gradient();
                var colorKeys = new GradientColorKey[3];
                colorKeys[0] = new GradientColorKey(Color.red, 0f);
                colorKeys[1] = new GradientColorKey(Color.yellow, 0.5f);
                colorKeys[2] = new GradientColorKey(Color.green, 1f);

                var alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0] = new GradientAlphaKey(1f, 0f);
                alphaKeys[1] = new GradientAlphaKey(1f, 1f);

                healthColorGradient.SetKeys(colorKeys, alphaKeys);
            }
        }

        public void Initialize(Transform troop, HealthComponent troopHealth)
        {
            troopTransform = troop;
            health = troopHealth;

            if (health != null)
            {
                health.OnHealthChanged += UpdateHealthBar;
                UpdateHealthBar(health.CurrentHP);
            }
        }

        private void UpdateHealthBar(float currentHP)
        {
            if (health == null) return;

            var healthPercent = health.HealthPercent;

            // Update fill amount
            if (fillImage != null)
            {
                fillImage.fillAmount = healthPercent;
                fillImage.color = healthColorGradient.Evaluate(healthPercent);
            }
        }

        private void LateUpdate()
        {
            if (troopTransform == null)
            {
                Destroy(gameObject);
                return;
            }

            // Ensure camera reference is valid
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            // Billboard effect - always face camera
            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnHealthChanged -= UpdateHealthBar;
            }
        }
    }
}
