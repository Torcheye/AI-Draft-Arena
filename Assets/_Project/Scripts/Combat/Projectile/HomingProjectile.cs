using UnityEngine;

namespace AdaptiveDraftArena.Combat
{
    /// <summary>
    /// Homing projectile that continuously tracks target position.
    /// If target dies, continues to last known position.
    /// </summary>
    public class HomingProjectile : ProjectileBase
    {
        [Header("Homing Settings")]
        [SerializeField] private float rotationSpeed = 360f; // Degrees per second

        private Vector3 currentTargetPosition;
        private bool targetLost;

        protected override void OnInitialized()
        {
            // Initialize target position
            currentTargetPosition = config.targetPosition;
            targetLost = false;
        }

        protected override void UpdateMovement()
        {
            // Update target position if target still alive
            if (!targetLost && config.targetTroop != null && config.targetTroop.IsAlive)
            {
                currentTargetPosition = config.targetTroop.transform.position;
            }
            else if (!targetLost)
            {
                // Target died - lock to last known position
                targetLost = true;
            }

            // Calculate direction to current target position
            var toTarget = (currentTargetPosition - transform.position);
            var distance = toTarget.magnitude;

            // If very close to target position, just move directly to it
            if (distance < 0.1f)
            {
                transform.position = currentTargetPosition;
                return;
            }

            var directionToTarget = toTarget.normalized;

            // Smoothly rotate toward target
            var targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            // Move forward at constant speed
            var velocity = transform.forward * config.speed;
            transform.position += velocity * Time.deltaTime;
        }

        public override void OnReturnedToPool()
        {
            base.OnReturnedToPool();

            // Reset homing state
            targetLost = false;
            currentTargetPosition = Vector3.zero;
        }
    }
}
