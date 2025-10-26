# Projectile System - Comprehensive Implementation Plan

## Document Version
Version: 1.0
Date: 2025-10-26
Status: Ready for Implementation

---

## 1. Executive Summary

This document provides a complete, implementation-ready design for a unified projectile system supporting both linear (Bow) and homing (Staff) projectiles. The system integrates with the existing modular troop combat architecture, uses object pooling for performance, and handles all edge cases including target death, out-of-bounds projectiles, and collision detection.

### Key Design Decisions (Confirmed)
- **Architecture**: Unified base class with specialized Linear and Homing implementations
- **Spawn Position**: `transform.position + Vector3.up * 1f` (slightly elevated from troop)
- **Target Death**: Projectile continues to last known position and expires naturally
- **Collision**: Trigger-based detection (`isTrigger = true`)
- **Lifetime**: 5-second maximum lifetime
- **Pooling**: Use existing `ObjectPool<T>` utility
- **Damage**: Pre-calculated damage stored in projectile, applied on hit
- **VFX**: Simple disappear (no particle effects initially)

---

## 2. Complete Architecture Overview

### 2.1 Component Hierarchy

```
ProjectileBase (abstract MonoBehaviour)
├─ LinearProjectile (concrete)
└─ HomingProjectile (concrete)

ProjectileManager (MonoBehaviour singleton)
├─ Manages pools for each projectile type
└─ Handles projectile spawning/recycling

TroopCombat (existing - modified)
└─ Calls ProjectileManager to spawn projectiles
```

### 2.2 Data Flow

```
TroopCombat.PerformAttack()
    ↓
Calculate final damage with all modifiers
    ↓
ProjectileManager.SpawnProjectile(type, config)
    ↓
Get projectile from pool
    ↓
ProjectileBase.Initialize(config)
    ↓
Update() moves projectile each frame
    ↓
OnTriggerEnter() detects collision
    ↓
ApplyDamage() to target
    ↓
ReturnToPool() - recycle projectile
```

### 2.3 Integration Points

**Existing Systems:**
- `TroopCombat` - Modified to spawn projectiles instead of instant damage
- `HealthComponent` - Receives damage from projectiles
- `ObjectPool<T>` - Manages projectile instances
- `WeaponModule` - Stores projectile prefab references

**New Systems:**
- `ProjectileBase` - Abstract base for all projectiles
- `LinearProjectile` - Straight-line projectile implementation
- `HomingProjectile` - Homing projectile implementation
- `ProjectileManager` - Centralized projectile spawning/pooling
- `ProjectileConfig` - Data structure for projectile initialization

---

## 3. Detailed Class Designs

### 3.1 ProjectileConfig (Data Structure)

**Purpose**: Immutable data structure passed to projectiles during initialization

**File**: `Assets\_Project\Scripts\Combat\Projectile\ProjectileConfig.cs`

```csharp
namespace AdaptiveDraftArena.Combat
{
    /// <summary>
    /// Immutable configuration data for projectile initialization.
    /// Passed from TroopCombat to ProjectileManager to Projectile instance.
    /// </summary>
    public struct ProjectileConfig
    {
        // Origin data
        public Vector3 spawnPosition;
        public GameObject owner;          // The troop that fired this projectile
        public Team ownerTeam;

        // Target data
        public Vector3 targetPosition;    // Initial target position (for linear)
        public TroopController targetTroop; // Target troop reference (for homing, can be null)

        // Damage
        public float damage;              // Pre-calculated final damage

        // Movement
        public float speed;               // Units per second
        public float homingStrength;      // 0 = no homing, 1 = perfect homing (for future use)

        // Lifetime
        public float maxLifetime;         // Maximum time before auto-destroy

        // Visual
        public EffectModule effect;       // For visual tint/effects
    }
}
```

**Validation Rules:**
- `spawnPosition` - Must be valid world position
- `owner` - Must not be null
- `damage` - Must be > 0
- `speed` - Must be > 0
- `maxLifetime` - Must be > 0 (default: 5 seconds)

---

### 3.2 ProjectileBase (Abstract Base Class)

**Purpose**: Abstract base providing common projectile functionality

**File**: `Assets\_Project\Scripts\Combat\Projectile\ProjectileBase.cs`

```csharp
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
```

**Key Properties:**
- `config` - Immutable configuration data
- `isInitialized` - Safety flag to prevent Update before initialization
- `hasHit` - Prevents double-hit if OnTriggerEnter fires multiple times
- `lifetimeRemaining` - Countdown timer for auto-expiration

**Key Methods:**
- `Initialize(config)` - Set up projectile state from config
- `OnInitialized()` - Abstract hook for subclass setup
- `UpdateMovement()` - Abstract hook for subclass movement logic
- `OnTriggerEnter(other)` - Collision detection and damage application
- `ReturnToPool()` - Cleanup and return to pool

**Edge Cases Handled:**
- Same-team collision (ignored)
- Dead target collision (ignored)
- Double-hit prevention (`hasHit` flag)
- Lifetime expiration without hitting
- Null target handling
- Uninitialized state protection

---

### 3.3 LinearProjectile (Concrete Implementation)

**Purpose**: Straight-line projectile (used by Bow)

**File**: `Assets\_Project\Scripts\Combat\Projectile\LinearProjectile.cs`

```csharp
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
```

**Behavior:**
- Travels in straight line from spawn position toward initial target position
- Does not track target if it moves
- Maintains constant speed
- Can miss if target moves out of the way
- Expires after 5 seconds if no hit

**When Used:**
- Bow weapon (`AttackType.Projectile`)
- Any future straight-line projectile weapons

---

### 3.4 HomingProjectile (Concrete Implementation)

**Purpose**: Homing projectile that tracks moving targets (used by Staff)

**File**: `Assets\_Project\Scripts\Combat\Projectile\HomingProjectile.cs`

```csharp
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
```

**Behavior:**
- Continuously updates target position while target is alive
- Smoothly rotates toward target (not instant snap)
- If target dies, continues to last known position
- Always moves at constant speed
- Cannot miss unless lifetime expires

**Key Parameters:**
- `rotationSpeed` - How fast projectile can turn (360°/sec = 1 second for 180° turn)

**When Used:**
- Staff weapon (`AttackType.Homing`)
- Any future homing projectile weapons

---

### 3.5 ProjectileManager (Singleton)

**Purpose**: Centralized projectile pooling and spawning

**File**: `Assets\_Project\Scripts\Combat\Projectile\ProjectileManager.cs`

```csharp
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
```

**Key Responsibilities:**
- Singleton instance management
- Pool initialization for each projectile type
- Spawning projectiles from pools
- Returning projectiles to pools
- Pool statistics for debugging

**Inspector Setup Required:**
- Assign `linearProjectilePrefab` (drag StaffProjectile.prefab modified as linear)
- Assign `homingProjectilePrefab` (drag StaffProjectile.prefab modified as homing)
- Optionally assign `projectileContainer` (or auto-created)

---

### 3.6 TroopCombat (Modifications)

**File**: `Assets\_Project\Scripts\Combat\TroopCombat.cs` (modify existing)

**Changes Required:**

```csharp
// In PerformAttack() method, replace TODO sections:

private void PerformAttack()
{
    if (CurrentTarget == null || !CurrentTarget.IsAlive) return;

    // Trigger attack event for visual feedback
    OnAttack?.Invoke();

    // Calculate damage
    var damage = CalculateDamage();

    // Apply damage based on weapon type
    switch (weapon.attackType)
    {
        case AttackType.Melee:
            ApplyMeleeDamage(damage);
            break;

        case AttackType.Projectile:
            SpawnLinearProjectile(damage);
            break;

        case AttackType.Homing:
            SpawnHomingProjectile(damage);
            break;

        case AttackType.AOE:
            ApplyAOEDamage(damage);
            break;
    }
}

// NEW METHOD: Spawn linear projectile (Bow)
private void SpawnLinearProjectile(float damage)
{
    if (ProjectileManager.Instance == null)
    {
        Debug.LogError("ProjectileManager not found! Applying damage directly.");
        CurrentTarget.Health.TakeDamage(damage, owner.gameObject);
        return;
    }

    var config = new ProjectileConfig
    {
        spawnPosition = transform.position + Vector3.up * 1f,
        owner = owner.gameObject,
        ownerTeam = owner.Team,
        targetPosition = CurrentTarget.transform.position,
        targetTroop = CurrentTarget,
        damage = damage,
        speed = weapon.projectileSpeed,
        maxLifetime = 5f,
        effect = effect
    };

    ProjectileManager.Instance.SpawnLinearProjectile(config);

    // TODO: Play bow shoot sound
    Debug.Log($"{owner.name} shoots arrow at {CurrentTarget.name} for {damage} damage");
}

// NEW METHOD: Spawn homing projectile (Staff)
private void SpawnHomingProjectile(float damage)
{
    if (ProjectileManager.Instance == null)
    {
        Debug.LogError("ProjectileManager not found! Applying damage directly.");
        CurrentTarget.Health.TakeDamage(damage, owner.gameObject);
        return;
    }

    var config = new ProjectileConfig
    {
        spawnPosition = transform.position + Vector3.up * 1f,
        owner = owner.gameObject,
        ownerTeam = owner.Team,
        targetPosition = CurrentTarget.transform.position,
        targetTroop = CurrentTarget,
        damage = damage,
        speed = weapon.projectileSpeed,
        maxLifetime = 5f,
        effect = effect
    };

    ProjectileManager.Instance.SpawnHomingProjectile(config);

    // TODO: Play staff cast sound
    Debug.Log($"{owner.name} casts homing spell at {CurrentTarget.name} for {damage} damage");
}
```

**Fallback Behavior:**
- If `ProjectileManager.Instance` is null, apply damage directly
- Ensures combat still works even if projectile system fails

---

## 4. Step-by-Step Implementation Sequence

### Phase 1: Core Data Structures (30 minutes)

**Step 1.1**: Create folder structure
- Create `Assets\_Project\Scripts\Combat\Projectile\` folder

**Step 1.2**: Implement `ProjectileConfig.cs`
- Create file
- Copy struct definition from section 3.1
- Validate compilation

**Step 1.3**: Verify namespace consistency
- Ensure `using AdaptiveDraftArena.Combat;` resolves
- Check against existing combat scripts

**Verification:**
- [ ] ProjectileConfig.cs compiles without errors
- [ ] Can create ProjectileConfig in test code

---

### Phase 2: Base Projectile Class (1 hour)

**Step 2.1**: Implement `ProjectileBase.cs`
- Create file
- Copy abstract class from section 3.2
- Add all protected/public methods
- Add XML documentation comments

**Step 2.2**: Test component requirements
- Create temporary test GameObject in scene
- Add ProjectileBase (will fail - abstract)
- Verify Collider and Rigidbody auto-added

**Step 2.3**: Create test concrete implementation
- Create minimal test class extending ProjectileBase
- Override abstract methods with empty implementations
- Verify component attachment and physics settings

**Verification:**
- [ ] ProjectileBase.cs compiles
- [ ] Test concrete class can be added to GameObject
- [ ] Collider.isTrigger = true automatically
- [ ] Rigidbody.isKinematic = true automatically

---

### Phase 3: Concrete Projectile Implementations (1 hour)

**Step 3.1**: Implement `LinearProjectile.cs`
- Create file
- Copy class from section 3.3
- Test direction calculation logic
- Test velocity calculation

**Step 3.2**: Implement `HomingProjectile.cs`
- Create file
- Copy class from section 3.4
- Test rotation logic
- Test target tracking

**Step 3.3**: Create prefabs
- Duplicate `StaffProjectile.prefab` twice:
  - `LinearProjectile.prefab` - Add LinearProjectile component
  - `HomingProjectile.prefab` - Add HomingProjectile component
- Configure colliders (sphere or capsule, radius ~0.5)
- Verify Rigidbody settings

**Step 3.4**: Test projectile movement
- Create test scene
- Spawn projectiles manually via Inspector
- Call Initialize() with test config
- Observe movement in Play mode

**Verification:**
- [ ] LinearProjectile moves in straight line
- [ ] HomingProjectile curves toward target
- [ ] Both respect lifetime and self-destruct
- [ ] Prefabs correctly configured

---

### Phase 4: Projectile Manager (45 minutes)

**Step 4.1**: Implement `ProjectileManager.cs`
- Create file
- Copy singleton class from section 3.5
- Implement pool initialization
- Implement spawn methods

**Step 4.2**: Create ProjectileManager GameObject
- Create empty GameObject in main game scene
- Name it "ProjectileManager"
- Add ProjectileManager component
- Assign prefab references in Inspector

**Step 4.3**: Test pooling
- Call SpawnLinearProjectile() in test code
- Verify projectile retrieved from pool
- Verify projectile initialized correctly
- Verify projectile returns to pool after expiration

**Verification:**
- [ ] ProjectileManager singleton works
- [ ] Pools initialize correctly
- [ ] Can spawn projectiles
- [ ] Projectiles return to pool
- [ ] Pool size increases/decreases correctly

---

### Phase 5: TroopCombat Integration (1 hour)

**Step 5.1**: Modify `TroopCombat.cs`
- Add SpawnLinearProjectile() method
- Add SpawnHomingProjectile() method
- Replace TODO comments in PerformAttack()
- Add ProjectileManager null checks

**Step 5.2**: Update WeaponModule assets
- Open Staff weapon ScriptableObject
- Set attackType to `AttackType.Homing`
- Verify projectileSpeed is set (default 10)
- Open Bow weapon ScriptableObject
- Set attackType to `AttackType.Projectile`
- Verify projectileSpeed is set

**Step 5.3**: Test in-game combat
- Start match
- Draft troop with Bow
- Draft troop with Staff
- Observe projectiles spawning during combat
- Verify damage applied on hit

**Verification:**
- [ ] Bow troops spawn linear projectiles
- [ ] Staff troops spawn homing projectiles
- [ ] Projectiles deal correct damage
- [ ] No errors in console
- [ ] Combat feels responsive

---

### Phase 6: Edge Case Testing (1 hour)

**Test Case 6.1**: Target dies before projectile hits
- Spawn archer targeting low-HP troop
- Kill target with another troop before arrow arrives
- **Expected**: Arrow continues to last position, expires naturally
- **Pass Criteria**: No null reference errors, arrow disappears

**Test Case 6.2**: Projectile spawner dies before projectile hits
- Spawn archer, fire arrow
- Kill archer before arrow lands
- **Expected**: Arrow still applies damage (owner reference kept)
- **Pass Criteria**: Target takes damage, no errors

**Test Case 6.3**: Multiple projectiles hit same target simultaneously
- Spawn 3 archers targeting same troop
- All fire at once
- **Expected**: All projectiles hit, damage stacks
- **Pass Criteria**: Target takes 3× damage, all projectiles return to pool

**Test Case 6.4**: Projectile flies out of map bounds
- Spawn archer at map edge facing outward
- Fire arrow off map
- **Expected**: Projectile expires after 5 seconds
- **Pass Criteria**: No orphaned projectiles, memory clean

**Test Case 6.5**: Same-team collision
- Spawn 2 archers on same team, aligned
- Front archer shoots through back archer
- **Expected**: Projectile ignores friendly collider
- **Pass Criteria**: Projectile passes through, no damage

**Test Case 6.6**: Rapid fire stress test
- Spawn 5 archers firing continuously at targets
- Observe for 30 seconds
- **Expected**: Smooth performance, pool grows if needed
- **Pass Criteria**: No lag, no errors, pool recycles

**Test Case 6.7**: Homing projectile perfect tracking
- Spawn staff user targeting fast-moving scout
- **Expected**: Magic bolt curves and hits moving target
- **Pass Criteria**: Bolt follows target, hits consistently

**Test Case 6.8**: Pool exhaustion
- Force spawn 100 projectiles simultaneously
- **Expected**: Pool creates new instances beyond initial size
- **Pass Criteria**: All projectiles spawn, no crashes

**Verification:**
- [ ] All test cases pass
- [ ] No console errors
- [ ] No memory leaks
- [ ] Performance acceptable

---

### Phase 7: Polish & Optimization (30 minutes)

**Step 7.1**: Add debug visualization
- Add optional Gizmo drawing in ProjectileBase
- Show projectile path prediction
- Show target position indicator
- Toggleable via inspector flag

**Step 7.2**: Performance profiling
- Use Unity Profiler during combat
- Check projectile Update() overhead
- Verify pool size is appropriate
- Adjust initial pool size if needed

**Step 7.3**: Code cleanup
- Remove debug logs
- Add final XML comments
- Ensure consistent formatting
- Remove test code

**Verification:**
- [ ] Code is clean and documented
- [ ] No debug logs in production code
- [ ] Performance is acceptable
- [ ] Gizmos available for debugging

---

## 5. Complete Edge Case Handling

### 5.1 Target Death During Flight

**Scenario**: Projectile is in flight when target dies

**Handling:**
- **Linear Projectile**: Already fires at position snapshot, unaffected
- **Homing Projectile**:
  - Update loop checks `targetTroop.IsAlive`
  - If false, sets `targetLost = true`
  - Locks `currentTargetPosition` to last known position
  - Continues to that position and expires

**Code Location**: `HomingProjectile.UpdateMovement()`

**Test**: Kill target while projectile is mid-flight

---

### 5.2 Owner Death Before Projectile Hits

**Scenario**: Troop that fired projectile dies before projectile lands

**Handling:**
- Projectile stores owner as GameObject reference
- GameObject persists briefly after death (0.5s destroy delay)
- If GameObject is destroyed, Unity handles null gracefully
- HealthComponent.TakeDamage() accepts null attacker

**Code Location**: `ProjectileBase.ApplyDamage()`

**Test**: Kill archer immediately after firing

---

### 5.3 Same-Team Collision

**Scenario**: Projectile collides with friendly unit

**Handling:**
- `OnTriggerEnter()` checks `hitTroop.Team == config.ownerTeam`
- If same team, early return (no damage, no hit registered)
- Projectile continues flight

**Code Location**: `ProjectileBase.OnTriggerEnter()`

**Test**: Fire through aligned friendly troops

---

### 5.4 Double Hit Prevention

**Scenario**: OnTriggerEnter fires multiple times in one frame

**Handling:**
- `hasHit` bool flag set on first hit
- Subsequent OnTriggerEnter calls early return
- Flag reset on pool return

**Code Location**: `ProjectileBase.OnTriggerEnter()`, `Initialize()`

**Test**: Fast-moving projectile hitting large collider

---

### 5.5 Lifetime Expiration

**Scenario**: Projectile flies for 5 seconds without hitting

**Handling:**
- `lifetimeRemaining` decrements in Update()
- When <= 0, calls `HandleExpiration()`
- Returns to pool cleanly

**Code Location**: `ProjectileBase.Update()`

**Test**: Fire at empty space, wait 5 seconds

---

### 5.6 Out of Bounds Projectiles

**Scenario**: Projectile flies off the map

**Handling:**
- Lifetime timer handles this automatically
- No special boundary detection needed
- Expires after 5 seconds regardless of position

**Code Location**: `ProjectileBase.Update()`

**Test**: Fire projectile off map edge

---

### 5.7 Null Target at Spawn

**Scenario**: Target becomes null between attack decision and spawn

**Handling:**
- TroopCombat checks `CurrentTarget != null` before calling spawn
- If target becomes null, attack aborted (no projectile spawned)
- If projectile spawns with null target:
  - LinearProjectile uses targetPosition (always valid)
  - HomingProjectile checks null before accessing targetTroop

**Code Location**: `TroopCombat.PerformAttack()`, `HomingProjectile.UpdateMovement()`

**Test**: Kill target in same frame as attack

---

### 5.8 Invalid Spawn Position

**Scenario**: Spawn position is inside geometry or invalid

**Handling:**
- Spawn position calculated as `transform.position + Vector3.up * 1f`
- Always valid unless troop itself is in invalid state
- If inside collider, trigger detection still works (isTrigger = true)

**Code Location**: `TroopCombat.SpawnLinearProjectile/SpawnHomingProjectile()`

**Test**: Spawn troop in extreme positions

---

### 5.9 Zero Distance to Target

**Scenario**: Target is directly on top of shooter

**Handling:**
- LinearProjectile: direction becomes zero vector
  - Falls back to `transform.forward`
- HomingProjectile: distance check in UpdateMovement()
  - If distance < 0.1f, snaps to target position

**Code Location**: `LinearProjectile.OnInitialized()`, `HomingProjectile.UpdateMovement()`

**Test**: Spawn troops at identical positions

---

### 5.10 Pool Exhaustion

**Scenario**: More projectiles needed than pool size

**Handling:**
- ObjectPool.Get() automatically creates new instance if pool empty
- New instance initialized normally
- Pool grows dynamically

**Code Location**: `ObjectPool<T>.Get()`

**Test**: Spawn 100 troops with rapid-fire weapons

---

### 5.11 ProjectileManager Missing

**Scenario**: ProjectileManager not in scene

**Handling:**
- TroopCombat checks `ProjectileManager.Instance == null`
- If null, logs error and applies damage directly (fallback)
- Combat still functions, just no visual projectiles

**Code Location**: `TroopCombat.SpawnLinearProjectile/SpawnHomingProjectile()`

**Test**: Remove ProjectileManager from scene

---

### 5.12 Scene Transition During Flight

**Scenario**: Scene changes while projectiles are active

**Handling:**
- ProjectileManager destroyed with scene
- Pooled projectiles destroyed with scene
- Active projectiles destroyed with scene
- No cleanup needed (Unity handles)

**Code Location**: N/A (Unity automatic)

**Test**: Load new scene during combat

---

## 6. Integration Points with Existing Systems

### 6.1 TroopCombat Integration

**Current State:**
- `TroopCombat.PerformAttack()` has TODO comments for projectiles
- Damage calculation already complete
- Target validation already handled

**Changes Required:**
- Add two new private methods (SpawnLinearProjectile, SpawnHomingProjectile)
- Replace TODO sections in switch statement
- Add ProjectileManager null check fallback

**No Breaking Changes:**
- Melee and AOE attacks unchanged
- Damage calculation unchanged
- Target tracking unchanged

**File Modified:** `TroopCombat.cs`

---

### 6.2 HealthComponent Integration

**Current State:**
- `TakeDamage(float, GameObject)` accepts attacker GameObject
- Already handles null attacker gracefully
- Ability system hooks already in place

**Changes Required:**
- None! Projectiles call existing TakeDamage() method

**No Breaking Changes:**
- All existing damage sources still work
- Ability modifiers still apply

**File Modified:** None

---

### 6.3 WeaponModule Integration

**Current State:**
- `AttackType` enum already has Projectile and Homing
- `projectilePrefab` field exists but unused
- `projectileSpeed` field exists but unused

**Changes Required:**
- Assign projectile prefabs to Bow and Staff ScriptableObjects
- Set projectileSpeed values (default 10)
- Set attackType correctly

**No Breaking Changes:**
- Existing weapons still work
- New fields are optional

**Files Modified:**
- `Weapon_Bow.asset` (ScriptableObject)
- `Weapon_Staff.asset` (ScriptableObject)

---

### 6.4 ObjectPool Integration

**Current State:**
- `ObjectPool<T>` utility exists and is tested
- Generic implementation works with any Component

**Changes Required:**
- None! Just instantiate with projectile types

**No Breaking Changes:**
- ObjectPool interface unchanged

**File Modified:** None (new usage only)

---

### 6.5 Team System Integration

**Current State:**
- `Team` enum exists (Player, AI)
- TroopController has Team property
- Targeting system uses Team for enemy detection

**Changes Required:**
- ProjectileConfig stores Team for collision filtering

**No Breaking Changes:**
- Team enum unchanged
- Team logic unchanged

**File Modified:** None (new usage only)

---

### 6.6 Ability System Integration

**Current State:**
- AbilityExecutor modifies damage via HealthComponent hooks
- Damage modifiers applied before TakeDamage() called

**Changes Required:**
- None! Projectiles pass pre-calculated damage

**How It Works:**
1. TroopCombat.CalculateDamage() calls AbilityExecutor.ModifyOutgoingDamage()
2. Final damage value passed to projectile via config
3. Projectile applies damage via HealthComponent.TakeDamage()
4. HealthComponent calls AbilityExecutor.ModifyIncomingDamage()
5. All ability modifiers applied correctly

**No Breaking Changes:**
- Ability system unchanged
- Damage flow unchanged

**File Modified:** None

---

### 6.7 Visual System Integration

**Current State:**
- TroopVisuals handles troop appearance
- TroopAnimationController handles attack animations
- VFX prefabs exist in `Assets\_Project\Prefabs\VFX\`

**Future Integration (Not in Initial Implementation):**
- Attack animation can trigger projectile spawn via AnimationEvent
- Projectile hit can spawn impact VFX
- Trail renderers can be added to projectile prefabs

**Changes Required (Future):**
- Add AnimationEvent to attack animations
- Create impact VFX prefabs
- Add spawn/hit sounds

**No Breaking Changes:**
- Visual system unchanged
- Can add projectile visuals incrementally

**File Modified:** None (future enhancement)

---

## 7. Testing Strategy

### 7.1 Unit Tests (Manual)

**Test 1: ProjectileConfig Creation**
```csharp
var config = new ProjectileConfig
{
    spawnPosition = Vector3.zero,
    owner = testGameObject,
    ownerTeam = Team.Player,
    targetPosition = Vector3.forward * 10,
    targetTroop = testTroop,
    damage = 5f,
    speed = 10f,
    maxLifetime = 5f,
    effect = testEffect
};
// Verify all fields set correctly
```

**Test 2: Linear Projectile Movement**
```csharp
// Spawn linear projectile
// Wait 1 second
// Assert position moved ~10 units (speed * time)
// Assert rotation unchanged
```

**Test 3: Homing Projectile Tracking**
```csharp
// Spawn homing projectile
// Move target sideways during flight
// Assert projectile curved toward target
// Assert projectile hit target
```

**Test 4: Pool Recycling**
```csharp
// Spawn 10 projectiles
// Wait for all to expire
// Check pool size returned to initial
```

---

### 7.2 Integration Tests (In-Game)

**Test 5: Bow Combat**
- Draft 2 archers vs 2 knights
- Observe arrows fired
- Verify damage applied
- Verify knights win (archers too fragile)

**Test 6: Staff Combat**
- Draft 2 staff users vs 2 scouts
- Observe magic bolts track moving scouts
- Verify hits land despite movement
- Verify damage applied

**Test 7: Mixed Combat**
- Draft 1 archer + 1 staff user vs 2 knights
- Observe both projectile types
- Verify both deal damage
- Verify no interference

**Test 8: Target Priority**
- Draft 3 archers vs 5 scouts
- Observe archers retarget after kills
- Verify no projectiles fired at dead targets
- Verify new projectiles fire at alive targets

---

### 7.3 Stress Tests

**Test 9: Projectile Spam**
```
- 5 archers vs 5 archers
- Continuous firing for 30 seconds
- Monitor performance
- Verify no memory leaks
- Verify pool recycling
```

**Test 10: Rapid Target Death**
```
- 5 archers vs 1 low-HP target
- All fire simultaneously
- Target dies from first hit
- Verify remaining projectiles handle gracefully
```

---

### 7.4 Edge Case Tests

See Section 5 for detailed edge case test scenarios.

---

## 8. Performance Considerations

### 8.1 Expected Performance Characteristics

**Projectile Count:**
- Typical match: 2-4 ranged troops per side
- Max active projectiles: ~20 simultaneously (worst case)
- Pool size: 20 initial, grows if needed

**Update Overhead:**
- 20 projectiles × ~0.01ms each = ~0.2ms per frame
- Negligible compared to other game systems

**Memory Footprint:**
- Per projectile: ~500 bytes (mostly Unity components)
- 20 pooled: ~10KB total
- Negligible impact

---

### 8.2 Optimization Strategies Employed

**1. Object Pooling**
- Eliminates allocation/deallocation overhead
- Reuses GameObjects and Components
- Pre-allocated pool reduces runtime cost

**2. Squared Distance Checks**
- HomingProjectile checks `distance < 0.1f` using magnitude
- Could optimize to squared distance if needed
- Current cost negligible

**3. Trigger-Based Collision**
- isTrigger = true avoids physics simulation
- Collision callbacks only when overlapping
- No continuous collision detection overhead

**4. Manual Movement**
- Rigidbody.isKinematic = true
- Manual position updates via transform
- Avoids physics engine overhead

**5. Early Returns**
- Multiple early return checks in OnTriggerEnter
- Minimizes processing for invalid collisions
- ~50% of collisions filtered early

**6. Cached Components**
- Collider, Rigidbody, TrailRenderer cached in Awake()
- No GetComponent() calls per frame
- Significant performance win

---

### 8.3 Profiling Recommendations

**Profiling Checkpoints:**
1. After Phase 5 (integration) - Baseline measurement
2. After Phase 6 (edge case testing) - Stress test measurement
3. Before final merge - Production measurement

**Metrics to Track:**
- Update() time per projectile (target: < 0.01ms)
- Pool Get/Release time (target: < 0.001ms)
- OnTriggerEnter time (target: < 0.01ms)
- Total active projectiles (target: < 50)

**Unity Profiler Markers:**
```csharp
// Add to ProjectileBase.Update()
UnityEngine.Profiling.Profiler.BeginSample("ProjectileUpdate");
// ... update logic
UnityEngine.Profiling.Profiler.EndSample();
```

**Performance Budget:**
- Total projectile system: < 0.5ms per frame
- Acceptable for PC target platform

---

### 8.4 Mobile/Console Considerations

**Not Applicable:**
- Game targets PC platform only
- No mobile optimization needed

**If Targeting Mobile (Future):**
- Reduce initial pool size to 10
- Add max active projectile cap
- Consider simpler visual effects
- Profile on target device

---

## 9. Future Extensibility

### 9.1 Planned Extension Points

**New Projectile Types:**
- Bouncing projectile (bounces off walls)
- Splitting projectile (splits into multiple on hit)
- Piercing projectile (penetrates multiple targets)

**Implementation:**
- Create new class extending ProjectileBase
- Override OnInitialized() and UpdateMovement()
- Add to ProjectileManager pools
- Add new AttackType enum value

---

### 9.2 Visual Effects Integration

**Impact VFX:**
```csharp
// In ProjectileBase.ApplyDamage()
if (impactVFXPrefab != null)
{
    var vfx = Instantiate(impactVFXPrefab, transform.position, Quaternion.identity);
    Destroy(vfx, 2f);
}
```

**Trail Customization:**
- TrailRenderer already cached
- Can customize color via config.effect.tintColor
- Can enable/disable per weapon type

---

### 9.3 Audio Integration

**Spawn Sound:**
```csharp
// In ProjectileManager spawn methods
if (spawnSound != null)
{
    AudioSource.PlayClipAtPoint(spawnSound, config.spawnPosition);
}
```

**Hit Sound:**
```csharp
// In ProjectileBase.ApplyDamage()
if (hitSound != null)
{
    AudioSource.PlayClipAtPoint(hitSound, transform.position);
}
```

---

### 9.4 Ability Interactions

**Projectile-Specific Abilities:**
- "Multishot" - Fire 3 projectiles in spread
- "Ricochet" - Projectile bounces to nearby enemy
- "Explosive Shot" - AOE damage on hit

**Implementation Approach:**
- Ability modifies ProjectileConfig before spawn
- Ability can spawn multiple projectiles
- Projectile can have OnHit callback for ability effects

---

### 9.5 Analytics/Telemetry

**Metrics to Track:**
- Projectiles fired per match
- Hit rate (hits / total fired)
- Average flight time
- Pool high water mark

**Implementation:**
```csharp
// In ProjectileManager
private static int projectilesFired;
private static int projectilesHit;

public static float GetHitRate()
{
    return projectilesFired > 0 ? (float)projectilesHit / projectilesFired : 0f;
}
```

---

## 10. Implementation Checklist

### Phase 1: Core Data Structures
- [ ] Create Projectile folder
- [ ] Implement ProjectileConfig.cs
- [ ] Verify compilation
- [ ] Test namespace resolution

### Phase 2: Base Projectile Class
- [ ] Implement ProjectileBase.cs
- [ ] Add all methods and properties
- [ ] Test component requirements
- [ ] Create test concrete implementation
- [ ] Verify physics settings

### Phase 3: Concrete Implementations
- [ ] Implement LinearProjectile.cs
- [ ] Implement HomingProjectile.cs
- [ ] Create LinearProjectile.prefab
- [ ] Create HomingProjectile.prefab
- [ ] Configure colliders and rigidbodies
- [ ] Test movement in isolation

### Phase 4: Projectile Manager
- [ ] Implement ProjectileManager.cs
- [ ] Create ProjectileManager GameObject in scene
- [ ] Assign prefab references
- [ ] Test pool initialization
- [ ] Test spawn methods
- [ ] Test return to pool

### Phase 5: TroopCombat Integration
- [ ] Add SpawnLinearProjectile() method
- [ ] Add SpawnHomingProjectile() method
- [ ] Update PerformAttack() switch statement
- [ ] Update Bow WeaponModule settings
- [ ] Update Staff WeaponModule settings
- [ ] Test in-game combat

### Phase 6: Edge Case Testing
- [ ] Test target death during flight
- [ ] Test owner death before hit
- [ ] Test same-team collision
- [ ] Test double-hit prevention
- [ ] Test lifetime expiration
- [ ] Test out-of-bounds
- [ ] Test null target
- [ ] Test zero distance
- [ ] Test pool exhaustion
- [ ] Test ProjectileManager missing
- [ ] Test rapid fire

### Phase 7: Polish & Optimization
- [ ] Add debug visualization (optional)
- [ ] Profile performance
- [ ] Remove debug logs
- [ ] Add XML documentation
- [ ] Code cleanup
- [ ] Final testing

### Final Verification
- [ ] No console errors during combat
- [ ] All projectile types work correctly
- [ ] Damage applies correctly
- [ ] Pool recycles properly
- [ ] Performance acceptable
- [ ] Code is clean and documented

---

## 11. Risk Assessment & Mitigation

### Risk 1: Collision Detection Unreliable

**Probability**: Low
**Impact**: High

**Symptoms:**
- Projectiles pass through targets
- Double-hits occur
- Collisions missed at high speed

**Mitigation:**
- Use trigger colliders (avoids physics timing issues)
- Ensure target colliders are large enough (>0.5 radius)
- Use `hasHit` flag to prevent double-hits
- Test with various projectile speeds
- Consider Continuous Dynamic collision detection if needed

**Fallback:**
- Increase projectile collider size
- Reduce projectile speed
- Add raycast-based collision as backup

---

### Risk 2: Pool Memory Leak

**Probability**: Low
**Impact**: Medium

**Symptoms:**
- Pool size grows indefinitely
- Memory usage increases over time
- Projectiles not returned to pool

**Mitigation:**
- Verify OnReturnedToPool() called in all code paths
- Add lifetime failsafe (always return after maxLifetime)
- Monitor pool size in inspector
- Add pool high-water-mark logging

**Fallback:**
- Add max pool size cap
- Force destroy oldest projectiles if cap exceeded
- Add manual pool cleanup method

---

### Risk 3: Performance Degradation

**Probability**: Low
**Impact**: Medium

**Symptoms:**
- Frame rate drops with many projectiles
- Update() overhead too high
- Pool allocations cause spikes

**Mitigation:**
- Profile early and often
- Use object pooling (already planned)
- Limit max active projectiles if needed
- Optimize UpdateMovement() logic

**Fallback:**
- Reduce max active projectiles (cap at 20)
- Increase attack cooldowns for ranged weapons
- Simplify homing logic (less frequent updates)

---

### Risk 4: Integration Breaks Existing Combat

**Probability**: Low
**Impact**: High

**Symptoms:**
- Melee attacks stop working
- Damage calculation broken
- Abilities no longer apply

**Mitigation:**
- Minimal changes to TroopCombat
- Fallback to instant damage if ProjectileManager missing
- Test all weapon types after integration
- Keep switch statement structure intact

**Fallback:**
- Revert TroopCombat changes
- Implement projectiles as separate system
- Use AnimationEvent to trigger spawn instead

---

### Risk 5: Target Tracking Edge Cases

**Probability**: Medium
**Impact**: Low

**Symptoms:**
- Homing projectiles stutter
- Projectiles target wrong enemy
- Null reference errors

**Mitigation:**
- Comprehensive null checks in HomingProjectile
- targetLost flag prevents re-acquiring target
- Test target death scenarios extensively
- Log warnings for invalid states

**Fallback:**
- Fall back to linear movement if target lost
- Add max rotation speed limit
- Simplify homing logic

---

## 12. Appendix: Code File Locations

### New Files to Create

```
Assets\_Project\Scripts\Combat\Projectile\
├── ProjectileConfig.cs          (130 lines)
├── ProjectileBase.cs            (250 lines)
├── LinearProjectile.cs          (40 lines)
├── HomingProjectile.cs          (80 lines)
└── ProjectileManager.cs         (150 lines)

Assets\_Project\Prefabs\VFX\
├── LinearProjectile.prefab      (duplicate StaffProjectile.prefab)
└── HomingProjectile.prefab      (duplicate StaffProjectile.prefab)

Assets\_Project\Scenes\
└── (Main Game Scene)
    └── ProjectileManager GameObject (add to hierarchy)
```

### Files to Modify

```
Assets\_Project\Scripts\Combat\
└── TroopCombat.cs
    - Add SpawnLinearProjectile() method    (~30 lines)
    - Add SpawnHomingProjectile() method    (~30 lines)
    - Modify PerformAttack() switch         (~5 lines)

Assets\_Project\Data\Weapons\
├── Weapon_Bow.asset
│   - Set attackType = Projectile
│   - Set projectileSpeed = 10
│   - Set projectilePrefab = LinearProjectile.prefab
└── Weapon_Staff.asset
    - Set attackType = Homing
    - Set projectileSpeed = 10
    - Set projectilePrefab = HomingProjectile.prefab
```

### Total New Code

- **Lines of Code**: ~650 (excluding comments)
- **Files Created**: 5 scripts + 2 prefabs + 1 GameObject
- **Files Modified**: 1 script + 2 assets
- **Estimated Time**: 5-6 hours (including testing)

---

## 13. Glossary

**Linear Projectile**: Projectile that travels in a straight line from spawn position toward initial target position. Does not track moving targets. Can miss.

**Homing Projectile**: Projectile that continuously adjusts trajectory to follow target. Cannot miss unless target dies or lifetime expires.

**Object Pool**: Performance optimization pattern that reuses GameObjects instead of destroying and creating new ones.

**Pre-calculated Damage**: Final damage value computed before projectile spawn, including all modifiers (abilities, elements, etc). Stored in projectile and applied on hit.

**Trigger Collider**: Unity collider with isTrigger=true that generates OnTriggerEnter callbacks without physics simulation.

**Lifetime**: Maximum duration a projectile exists before self-destructing. Set to 5 seconds to handle missed shots and out-of-bounds cases.

**Target Death Behavior**: How projectile reacts when target dies mid-flight. Linear projectiles unaffected (already targeting position). Homing projectiles continue to last known position.

**Pool Recycling**: Process of returning used projectile to pool for reuse instead of destroying it.

**Owner**: The TroopController that fired the projectile. Stored as GameObject reference for damage attribution.

**Config**: Immutable ProjectileConfig struct containing all initialization parameters for a projectile.

---

## 14. Final Notes

### Implementation Priority

This system is **P0 - Critical** for completing the projectile weapons (Bow, Staff) as designed in the Game Design Document.

### Dependencies

- Requires existing TroopCombat system (✓ exists)
- Requires existing HealthComponent system (✓ exists)
- Requires existing ObjectPool utility (✓ exists)
- Requires WeaponModule infrastructure (✓ exists)

### Breaking Changes

**None**. This system is purely additive. Existing melee and AOE weapons continue working unchanged.

### Rollback Plan

If projectile system causes critical issues:
1. Comment out projectile spawn calls in TroopCombat
2. Restore original instant-damage TODO code
3. System degrades gracefully to instant hits
4. Debug and fix issues offline
5. Re-enable when stable

### Post-Implementation

After successful implementation:
- Update Game Design Document status
- Create developer documentation
- Record demo video showing projectile system
- Add to feature showcase for stakeholders

### Questions for Clarification

If any aspect of this design is unclear:
1. Re-read relevant section carefully
2. Check code examples in section 3
3. Review edge case handling in section 5
4. Consult integration points in section 6
5. If still unclear, flag for discussion before implementation

---

## Document Approval

**Status**: ✅ Ready for Implementation
**Reviewer**: (To be assigned)
**Date**: 2025-10-26

**Implementation Start**: Upon approval
**Expected Completion**: 5-6 hours of focused work
**Next Step**: Begin Phase 1 (Core Data Structures)

---

**End of Implementation Plan**
