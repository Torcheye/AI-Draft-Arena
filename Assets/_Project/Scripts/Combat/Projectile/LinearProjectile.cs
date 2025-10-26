using UnityEngine;

namespace AdaptiveDraftArena.Combat
{
    /// <summary>
    /// Linear projectile that travels in a straight line toward initial target position.
    /// Does not track moving targets - fires at where target was at spawn time.
    /// </summary>
    public class LinearProjectile : ProjectileBase
    {
        private Vector3 direction;
        private Vector3 velocity;

        protected override void OnInitialized()
        {
            // Calculate direction to target position (snapshot at spawn time)
            direction = (config.targetPosition - config.spawnPosition).normalized;

            // If direction is invalid (e.g., zero vector), fire forward
            if (direction == Vector3.zero)
            {
                direction = transform.forward;
            }

            // Calculate velocity
            velocity = direction * config.speed;
        }

        protected override void UpdateMovement()
        {
            // Simple linear movement
            transform.position += velocity * Time.deltaTime;

            // Keep rotation aligned with direction
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
