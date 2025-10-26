using System;
using UnityEngine;

namespace AdaptiveDraftArena.Combat
{
    /// <summary>
    /// Abstract base class for all projectile types.
    /// Handles initialization, lifetime tracking, damage application, and pooling.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public abstract class ProjectileBase : MonoBehaviour
    {
        // Configuration (set during Initialize)
        protected ProjectileConfig config;

        // State
        protected bool isInitialized;
        protected float lifetimeRemaining;
        protected bool hasHit; // Prevent double-hit

        // Cached components
        protected Collider projectileCollider;
        protected Rigidbody rb;
        protected TrailRenderer trail; // Optional, can be null

        // Events (for future extensibility)
        public event Action<ProjectileBase> OnProjectileHit;
        public event Action<ProjectileBase> OnProjectileExpired;

        protected virtual void Awake()
        {
            // Cache components
            projectileCollider = GetComponent<Collider>();
            rb = GetComponent<Rigidbody>();
            trail = GetComponent<TrailRenderer>();

            // Configure physics
            projectileCollider.isTrigger = true;
            rb.isKinematic = true; // We control movement manually
            rb.useGravity = false;
        }

        /// <summary>
        /// Initialize projectile with configuration.
        /// Called immediately after retrieving from pool.
        /// </summary>
        public virtual void Initialize(ProjectileConfig projectileConfig)
        {
            config = projectileConfig;
            isInitialized = true;
            hasHit = false;
            lifetimeRemaining = config.maxLifetime;

            // Set position and rotation
            transform.position = config.spawnPosition;

            // Calculate initial facing direction
            var direction = (config.targetPosition - config.spawnPosition).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            // Reset trail if present
            if (trail != null)
            {
                trail.Clear();
            }

            // Subclass-specific initialization
            OnInitialized();
        }

        /// <summary>
        /// Override for subclass-specific initialization logic.
        /// </summary>
        protected abstract void OnInitialized();

        protected virtual void Update()
        {
            if (!isInitialized) return;

            // Tick lifetime
            lifetimeRemaining -= Time.deltaTime;
            if (lifetimeRemaining <= 0)
            {
                HandleExpiration();
                return;
            }

            // Subclass-specific movement
            UpdateMovement();
        }

        /// <summary>
        /// Override for subclass-specific movement logic.
        /// Called every frame while projectile is active.
        /// </summary>
        protected abstract void UpdateMovement();

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (!isInitialized || hasHit) return;

            // Check if we hit a valid target
            var hitTroop = other.GetComponent<TroopController>();
            if (hitTroop == null) return;

            // Ignore if same team
            if (hitTroop.Team == config.ownerTeam) return;

            // Ignore if target is already dead
            if (!hitTroop.IsAlive) return;

            // Mark as hit to prevent double-damage
            hasHit = true;

            // Apply damage
            ApplyDamage(hitTroop);

            // Notify listeners
            OnProjectileHit?.Invoke(this);

            // Return to pool
            ReturnToPool();
        }

        /// <summary>
        /// Apply pre-calculated damage to target.
        /// </summary>
        private void ApplyDamage(TroopController target)
        {
            if (target == null || !target.IsAlive) return;

            target.Health.TakeDamage(config.damage, config.owner);

            // TODO: Spawn hit VFX at impact point
            // TODO: Play hit sound
        }

        /// <summary>
        /// Handle projectile expiration (lifetime ended without hitting).
        /// </summary>
        private void HandleExpiration()
        {
            OnProjectileExpired?.Invoke(this);
            ReturnToPool();
        }

        /// <summary>
        /// Return this projectile to the pool for reuse.
        /// </summary>
        private void ReturnToPool()
        {
            // Cleanup
            isInitialized = false;
            hasHit = false;

            // Notify manager to return to pool
            ProjectileManager.Instance.ReturnProjectile(this);
        }

        /// <summary>
        /// Called when projectile is returned to pool.
        /// Override for custom cleanup logic.
        /// </summary>
        public virtual void OnReturnedToPool()
        {
            // Clear trail
            if (trail != null)
            {
                trail.Clear();
            }

            // Clear events
            OnProjectileHit = null;
            OnProjectileExpired = null;
        }
    }
}
