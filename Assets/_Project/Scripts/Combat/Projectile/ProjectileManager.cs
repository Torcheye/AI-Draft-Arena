using UnityEngine;

namespace AdaptiveDraftArena.Combat
{
    /// <summary>
    /// Centralized manager for projectile pooling and spawning.
    /// Singleton pattern for global access from TroopCombat.
    /// </summary>
    public class ProjectileManager : MonoBehaviour
    {
        public static ProjectileManager Instance { get; private set; }

        [Header("Projectile Prefabs")]
        [SerializeField] private LinearProjectile linearProjectilePrefab;
        [SerializeField] private HomingProjectile homingProjectilePrefab;

        [Header("Pool Settings")]
        [SerializeField] private int initialPoolSize = 20;
        [SerializeField] private Transform projectileContainer; // Parent for pooled projectiles

        private ObjectPool<LinearProjectile> linearPool;
        private ObjectPool<HomingProjectile> homingPool;

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Create container if not assigned
            if (projectileContainer == null)
            {
                var container = new GameObject("ProjectilePool");
                projectileContainer = container.transform;
                projectileContainer.SetParent(transform);
            }

            // Initialize pools
            InitializePools();
        }

        private void InitializePools()
        {
            if (linearProjectilePrefab != null)
            {
                linearPool = new ObjectPool<LinearProjectile>(
                    prefab: linearProjectilePrefab,
                    parent: projectileContainer,
                    onGet: null,
                    onRelease: (proj) => proj.OnReturnedToPool(),
                    initialSize: initialPoolSize
                );
            }

            if (homingProjectilePrefab != null)
            {
                homingPool = new ObjectPool<HomingProjectile>(
                    prefab: homingProjectilePrefab,
                    parent: projectileContainer,
                    onGet: null,
                    onRelease: (proj) => proj.OnReturnedToPool(),
                    initialSize: initialPoolSize
                );
            }
        }

        /// <summary>
        /// Spawn a linear projectile (Bow).
        /// </summary>
        public LinearProjectile SpawnLinearProjectile(ProjectileConfig config)
        {
            if (linearPool == null)
            {
                Debug.LogError("Linear projectile pool not initialized!");
                return null;
            }

            var projectile = linearPool.Get();
            projectile.Initialize(config);
            return projectile;
        }

        /// <summary>
        /// Spawn a homing projectile (Staff).
        /// </summary>
        public HomingProjectile SpawnHomingProjectile(ProjectileConfig config)
        {
            if (homingPool == null)
            {
                Debug.LogError("Homing projectile pool not initialized!");
                return null;
            }

            var projectile = homingPool.Get();
            projectile.Initialize(config);
            return projectile;
        }

        /// <summary>
        /// Return a projectile to its pool.
        /// Called by ProjectileBase when hit or expired.
        /// </summary>
        public void ReturnProjectile(ProjectileBase projectile)
        {
            if (projectile == null) return;

            if (projectile is LinearProjectile linear)
            {
                linearPool?.Release(linear);
            }
            else if (projectile is HomingProjectile homing)
            {
                homingPool?.Release(homing);
            }
        }

        /// <summary>
        /// Get pool statistics for debugging/monitoring.
        /// </summary>
        public void GetPoolStats(out int linearPooled, out int homingPooled)
        {
            linearPooled = linearPool?.PoolSize ?? 0;
            homingPooled = homingPool?.PoolSize ?? 0;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
