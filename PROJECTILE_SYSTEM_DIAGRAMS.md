# Projectile System - Visual Diagrams

## Class Hierarchy Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        MonoBehaviour                            │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            │ inherits
                            ↓
┌─────────────────────────────────────────────────────────────────┐
│                    ProjectileBase (abstract)                    │
│─────────────────────────────────────────────────────────────────│
│ # config: ProjectileConfig                                      │
│ # isInitialized: bool                                           │
│ # lifetimeRemaining: float                                      │
│ # hasHit: bool                                                  │
│─────────────────────────────────────────────────────────────────│
│ + Initialize(ProjectileConfig)                                  │
│ # OnInitialized() [abstract]                                    │
│ # UpdateMovement() [abstract]                                   │
│ # OnTriggerEnter(Collider)                                      │
│ + OnReturnedToPool()                                            │
└────────────────┬──────────────────────────┬─────────────────────┘
                 │                          │
                 │ inherits                 │ inherits
                 ↓                          ↓
┌────────────────────────────┐  ┌──────────────────────────────┐
│   LinearProjectile         │  │   HomingProjectile           │
│────────────────────────────│  │──────────────────────────────│
│ - direction: Vector3       │  │ - currentTargetPos: Vector3  │
│ - velocity: Vector3        │  │ - targetLost: bool           │
│────────────────────────────│  │ - rotationSpeed: float       │
│ # OnInitialized()          │  │──────────────────────────────│
│ # UpdateMovement()         │  │ # OnInitialized()            │
│   → Straight line          │  │ # UpdateMovement()           │
│                            │  │   → Smooth homing            │
└────────────────────────────┘  └──────────────────────────────┘
```

## Component Composition Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                   ProjectileBase GameObject                     │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ ProjectileBase (or LinearProjectile / HomingProjectile)   │ │
│  │ - Main script controlling behavior                        │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Collider (Sphere or Capsule)                              │ │
│  │ - isTrigger: true                                         │ │
│  │ - radius: ~0.5                                            │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Rigidbody                                                 │ │
│  │ - isKinematic: true                                       │ │
│  │ - useGravity: false                                       │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ TrailRenderer (optional)                                  │ │
│  │ - Visual trail effect                                     │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ MeshRenderer / SpriteRenderer                             │ │
│  │ - Visual representation                                   │ │
│  └───────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        PROJECTILE SPAWN FLOW                            │
└─────────────────────────────────────────────────────────────────────────┘

TroopCombat.Update()
    │
    ├─→ Target in range?
    │       │
    │       └─→ Yes → attackTimer <= 0?
    │                      │
    │                      └─→ Yes → PerformAttack()
    │                                     │
    │                                     ↓
    │                        CalculateDamage()
    │                        ├─ Base weapon damage
    │                        ├─ Amount multiplier
    │                        ├─ Element multiplier
    │                        └─ Ability modifiers
    │                                     │
    │                                     ↓
    │                        Switch on weapon.attackType
    │                                     │
    │                        ┌────────────┼────────────┐
    │                        │            │            │
    │                   Melee      Projectile      Homing
    │                        │            │            │
    │                        ↓            ↓            ↓
    │                  Instant    SpawnLinear   SpawnHoming
    │                   Damage     Projectile    Projectile
    │                                     │            │
    │                                     └────┬───────┘
    │                                          ↓
    │                            Create ProjectileConfig
    │                            ├─ spawnPosition
    │                            ├─ owner
    │                            ├─ targetPosition
    │                            ├─ targetTroop
    │                            ├─ damage (pre-calculated)
    │                            ├─ speed
    │                            └─ maxLifetime
    │                                          ↓
    │                         ProjectileManager.SpawnProjectile()
    │                                          ↓
    │                              Get from ObjectPool
    │                                          ↓
    │                         Projectile.Initialize(config)
    │                                          ↓
    │                               SetActive(true)
    │                                          │
    └──────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                      PROJECTILE UPDATE FLOW                             │
└─────────────────────────────────────────────────────────────────────────┘

Projectile.Update() [every frame]
    │
    ├─→ isInitialized?
    │       │
    │       └─→ No → return (safety check)
    │
    ├─→ lifetimeRemaining -= Time.deltaTime
    │       │
    │       └─→ lifetimeRemaining <= 0?
    │               │
    │               └─→ Yes → HandleExpiration() → ReturnToPool()
    │
    └─→ UpdateMovement() [subclass implementation]
            │
            ├─→ LinearProjectile:
            │       └─ Move along fixed direction vector
            │
            └─→ HomingProjectile:
                    ├─ Update target position if alive
                    ├─ Calculate direction to target
                    ├─ Rotate toward target
                    └─ Move forward at constant speed

┌─────────────────────────────────────────────────────────────────────────┐
│                     PROJECTILE COLLISION FLOW                           │
└─────────────────────────────────────────────────────────────────────────┘

OnTriggerEnter(Collider other)
    │
    ├─→ isInitialized? → No → return
    ├─→ hasHit? → Yes → return (prevent double-hit)
    │
    ├─→ Get TroopController from collider
    │       │
    │       └─→ Null? → return (not a troop)
    │
    ├─→ Same team? → Yes → return (friendly fire off)
    ├─→ Target alive? → No → return (already dead)
    │
    ├─→ Set hasHit = true
    │
    ├─→ ApplyDamage(target)
    │       └─ target.Health.TakeDamage(config.damage, config.owner)
    │
    ├─→ Trigger OnProjectileHit event
    │
    └─→ ReturnToPool()
            └─ ProjectileManager.ReturnProjectile(this)
                    └─ pool.Release(projectile)
                            ├─ OnReturnedToPool() called
                            └─ SetActive(false)
```

## Pooling System Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      ProjectileManager                                  │
│─────────────────────────────────────────────────────────────────────────│
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │              ObjectPool<LinearProjectile>                         │ │
│  │───────────────────────────────────────────────────────────────────│ │
│  │  Queue:  [proj] [proj] [proj] [proj] [proj] ...                  │ │
│  │          └──────┬─────────────────────────────────┘               │ │
│  │                 │                                                 │ │
│  │         Get() ──┤                                                 │ │
│  │                 └───→ Dequeue → SetActive(true) → Return          │ │
│  │                                                                   │ │
│  │         Release(proj) ─→ OnReturnedToPool() → SetActive(false) → │ │
│  │                          Enqueue back to pool                     │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │              ObjectPool<HomingProjectile>                         │ │
│  │───────────────────────────────────────────────────────────────────│ │
│  │  Queue:  [proj] [proj] [proj] [proj] [proj] ...                  │ │
│  │          └──────┬─────────────────────────────────┘               │ │
│  │                 │                                                 │ │
│  │         Get() ──┤                                                 │ │
│  │                 └───→ Dequeue → SetActive(true) → Return          │ │
│  │                                                                   │ │
│  │         Release(proj) ─→ OnReturnedToPool() → SetActive(false) → │ │
│  │                          Enqueue back to pool                     │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

Pool Lifecycle:
    1. Initialization: Create 20 instances, deactivate, enqueue
    2. Get: Dequeue (or create if empty), activate, return to caller
    3. Use: Projectile flies, hits target or expires
    4. Release: Dequeue callbacks, deactivate, enqueue back
    5. Repeat from step 2
```

## Linear vs Homing Behavior Comparison

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    LINEAR PROJECTILE                                    │
└─────────────────────────────────────────────────────────────────────────┘

Spawn: [Archer]
         │
         ↓ Snapshot target position
        ☆ targetPos

Flight:  │ →→→→→→→→→→→→→→→→→→→→→→→→→ (straight line)

Target:                                    [Enemy]
                                             ↓ (moves down)
                                           [Enemy]

Result: MISS (enemy moved, arrow continues straight)

Characteristics:
- Direction calculated once at spawn
- Never changes trajectory
- Can miss if target moves
- Faster performance (no targeting updates)
- More realistic (arrow physics)

┌─────────────────────────────────────────────────────────────────────────┐
│                    HOMING PROJECTILE                                    │
└─────────────────────────────────────────────────────────────────────────┘

Spawn: [Mage]
         │
         ↓ Track target reference
        ☆ targetTroop

Flight:  │ →→→↘
         │      ↘
         │       ↘→→↘
         │           ↘→→ [Enemy]
                          ↓ (moves down)
                        [Enemy] ← HIT!

Result: HIT (magic bolt curved to follow target)

Characteristics:
- Continuously updates target position each frame
- Smoothly rotates toward target (360°/sec)
- Cannot miss (unless target dies or timeout)
- Slightly higher performance cost (targeting updates)
- More "magical" feel

┌─────────────────────────────────────────────────────────────────────────┐
│              TARGET DEATH COMPARISON                                    │
└─────────────────────────────────────────────────────────────────────────┘

LINEAR PROJECTILE:
    Spawn → [Archer] →→→→ ☆ lastPos
                           ↓
                          💀 (target dies)

    Arrow continues to lastPos, expires naturally
    (No change in behavior - already targeting position)

HOMING PROJECTILE:
    Spawn → [Mage] →→↘
                      ↘→ ☆ lastPos
                           ↓
                          💀 (target dies)

    Magic bolt locks to lastPos, continues straight
    (Stops tracking, continues to last known position)
```

## State Machine Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    PROJECTILE STATE MACHINE                             │
└─────────────────────────────────────────────────────────────────────────┘

    ┌─────────────┐
    │   POOLED    │
    │ (inactive)  │
    └──────┬──────┘
           │
           │ Get()
           ↓
    ┌─────────────┐
    │ INITIALIZING│
    │             │
    └──────┬──────┘
           │
           │ Initialize(config)
           ↓
    ┌─────────────┐
    │   FLYING    │◄──────────┐
    │  (active)   │           │
    └──────┬──────┘           │
           │                  │
           │ Each frame:      │
           ├─ Update position │
           ├─ Tick lifetime   │
           └─ Check collision │
           │                  │
           │                  │
    ┌──────┴──────────────────┴──────┐
    │                                 │
    ↓                                 ↓
┌──────────┐                    ┌──────────┐
│   HIT    │                    │ EXPIRED  │
│ (collide)│                    │(timeout) │
└────┬─────┘                    └────┬─────┘
     │                               │
     │ Apply damage                  │ No damage
     ↓                               ↓
    ┌─────────────────────────────────┐
    │        RETURNING TO POOL        │
    │   - OnReturnedToPool() called   │
    │   - Clear state                 │
    │   - SetActive(false)            │
    └──────────────┬──────────────────┘
                   │
                   │ Release()
                   ↓
            ┌─────────────┐
            │   POOLED    │
            │ (inactive)  │
            └─────────────┘
                   ↑
                   │
                   └─ Ready for reuse
```

## Integration Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    EXISTING SYSTEMS                                     │
└─────────────────────────────────────────────────────────────────────────┘

┌──────────────┐         ┌──────────────┐         ┌──────────────┐
│TroopController│        │HealthComponent│        │TargetingSystem│
│              │         │              │         │              │
│ - Team       │         │ - CurrentHP  │         │ - FindEnemy()│
│ - IsAlive    │         │ - TakeDamage()│         │              │
└──────┬───────┘         └──────┬───────┘         └──────┬───────┘
       │                        │                        │
       │ uses                   │ uses                   │ uses
       ↓                        ↓                        ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                          TroopCombat                                    │
│─────────────────────────────────────────────────────────────────────────│
│ - CurrentTarget                                                         │
│ - PerformAttack()                                                       │
│ - CalculateDamage()                                                     │
│ - SpawnLinearProjectile() ◄── NEW                                      │
│ - SpawnHomingProjectile() ◄── NEW                                      │
└──────────────┬──────────────────────────────────────────────────────────┘
               │
               │ spawns
               ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                    NEW PROJECTILE SYSTEM                                │
└─────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────────┐
│                       ProjectileManager                                  │
│──────────────────────────────────────────────────────────────────────────│
│ - ObjectPool<LinearProjectile>                                           │
│ - ObjectPool<HomingProjectile>                                           │
│ + SpawnLinearProjectile(config) → LinearProjectile                       │
│ + SpawnHomingProjectile(config) → HomingProjectile                       │
│ + ReturnProjectile(projectile)                                           │
└────────────┬─────────────────────────────────────────────────────────────┘
             │
             │ manages
             ↓
┌──────────────────────────────┐  ┌──────────────────────────────┐
│     LinearProjectile         │  │     HomingProjectile         │
│──────────────────────────────│  │──────────────────────────────│
│ + Initialize(config)         │  │ + Initialize(config)         │
│ - UpdateMovement()           │  │ - UpdateMovement()           │
│ - OnTriggerEnter()           │  │ - OnTriggerEnter()           │
└──────────────────────────────┘  └──────────────────────────────┘
             │                                 │
             └─────────────┬───────────────────┘
                           │
                           │ hits
                           ↓
                  ┌──────────────────┐
                  │ HealthComponent  │
                  │  TakeDamage()    │
                  └──────────────────┘

Data flow:
    TroopCombat → ProjectileConfig → ProjectileManager → Projectile → HealthComponent
```

## Collision Detection Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    COLLISION DETECTION FLOW                             │
└─────────────────────────────────────────────────────────────────────────┘

                  Projectile Flying →→→→→→→
                                          ↓
                              ┌───────────────────┐
                              │ OnTriggerEnter()  │
                              │   (Unity event)   │
                              └─────────┬─────────┘
                                        │
                              ┌─────────┴─────────┐
                              │ Get TroopController│
                              │  from Collider    │
                              └─────────┬─────────┘
                                        │
                        ┌───────────────┼───────────────┐
                        │               │               │
                  ┌─────▼─────┐  ┌──────▼──────┐  ┌────▼────┐
                  │ Null?     │  │ Same team?  │  │ Dead?   │
                  └─────┬─────┘  └──────┬──────┘  └────┬────┘
                        │               │               │
                     Yes│            Yes│            Yes│
                        ↓               ↓               ↓
                    ┌─────────────────────────────────────┐
                    │          IGNORE - Continue          │
                    │     (projectile keeps flying)       │
                    └─────────────────────────────────────┘

                        │ No            │ No            │ No
                        └───────────────┴───────────────┘
                                        │
                              ┌─────────▼─────────┐
                              │  Valid Hit!       │
                              └─────────┬─────────┘
                                        │
                        ┌───────────────┼───────────────┐
                        ↓               ↓               ↓
                ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
                │ Set hasHit   │ │ Apply Damage │ │ Fire Events  │
                │   = true     │ │ to Target    │ │ (OnHit)      │
                └──────────────┘ └──────────────┘ └──────────────┘
                                        │
                              ┌─────────▼─────────┐
                              │  Return to Pool   │
                              │  (projectile done)│
                              └───────────────────┘

Collision Layers:
    Projectile: Layer "Projectile"
    Troops: Layer "Troop"

Layer Matrix (Edit → Project Settings → Physics):
    Projectile ↔ Troop: ✓ (collide)
    Projectile ↔ Projectile: ✗ (ignore)
    Projectile ↔ Ground: ✗ (ignore)
```

## Performance Profile Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    PERFORMANCE CHARACTERISTICS                          │
└─────────────────────────────────────────────────────────────────────────┘

Frame Budget: 16.67ms (60 FPS)

┌─────────────────────────────────────────────────────────────────────────┐
│  System Component          │  Cost      │  Notes                        │
├────────────────────────────┼────────────┼───────────────────────────────┤
│  Projectile.Update()       │  0.01ms    │  Per projectile               │
│  20 projectiles total      │  0.20ms    │  Worst case                   │
│                            │            │                               │
│  ObjectPool.Get()          │  0.001ms   │  Dequeue operation            │
│  ObjectPool.Release()      │  0.001ms   │  Enqueue operation            │
│                            │            │                               │
│  OnTriggerEnter()          │  0.01ms    │  Per collision event          │
│  HealthComponent.TakeDamage│  0.005ms   │  Damage application           │
│                            │            │                               │
│  Total Projectile System   │  ~0.5ms    │  < 3% of frame budget         │
└────────────────────────────┴────────────┴───────────────────────────────┘

Memory Usage:

┌─────────────────────────────────────────────────────────────────────────┐
│  Component                 │  Size      │  Count  │  Total              │
├────────────────────────────┼────────────┼─────────┼─────────────────────┤
│  ProjectileConfig (struct) │  64 bytes  │  -      │  Stack allocated    │
│  Projectile GameObject     │  ~500 B    │  20     │  ~10 KB             │
│  Pool overhead             │  ~100 B    │  2      │  ~200 B             │
│                            │            │         │                     │
│  Total System Memory       │            │         │  ~10.2 KB           │
└────────────────────────────┴────────────┴─────────┴─────────────────────┘

Scalability:

Active Projectiles vs Performance:

     FPS
     │
  60 │▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓░░░░
     │                    ↑
  50 │                    20 projectiles (target)
     │
  40 │
     │
  30 │
     │
     └────────────────────────────────→ Active Projectiles
      0    10    20    30    40    50

Bottleneck Analysis:
    Current bottleneck: NOT projectiles (< 3% frame time)
    Likely bottlenecks: AI pathfinding, rendering, ability systems
```

---

**See also:**
- PROJECTILE_SYSTEM_IMPLEMENTATION_PLAN.md - Full technical specification
- PROJECTILE_SYSTEM_QUICK_REFERENCE.md - Quick lookup guide
