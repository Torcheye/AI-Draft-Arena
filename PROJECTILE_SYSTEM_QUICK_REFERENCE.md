# Projectile System - Quick Reference

## Overview
Unified projectile system supporting Linear (Bow) and Homing (Staff) projectiles with object pooling.

## Architecture at a Glance

```
ProjectileBase (abstract)
├── LinearProjectile  → Straight-line flight
└── HomingProjectile  → Tracks moving target

ProjectileManager (singleton) → Pools & spawns projectiles

TroopCombat → Calculates damage → Spawns projectile
```

## Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Spawn Position** | `transform.position + Vector3.up * 1f` | Elevated to avoid ground collision |
| **Target Death** | Continue to last position | Prevents null errors, looks natural |
| **Collision** | Trigger-based (`isTrigger = true`) | Better than physics simulation |
| **Lifetime** | 5 seconds max | Handles missed shots and OOB |
| **Damage** | Pre-calculated, stored in projectile | All modifiers applied before spawn |
| **Pooling** | Yes, using `ObjectPool<T>` | Performance optimization |
| **VFX** | Simple disappear | Can add later |

## File Structure

### New Files (5 scripts)
```
Assets\_Project\Scripts\Combat\Projectile\
├── ProjectileConfig.cs       → Data structure
├── ProjectileBase.cs         → Abstract base class
├── LinearProjectile.cs       → Bow projectile
├── HomingProjectile.cs       → Staff projectile
└── ProjectileManager.cs      → Pooling & spawning
```

### Modified Files (1 script)
```
Assets\_Project\Scripts\Combat\
└── TroopCombat.cs
    - Add SpawnLinearProjectile() method
    - Add SpawnHomingProjectile() method
    - Update PerformAttack() switch
```

### New Prefabs (2)
```
Assets\_Project\Prefabs\VFX\
├── LinearProjectile.prefab
└── HomingProjectile.prefab
```

## Implementation Sequence

1. **Phase 1** (30 min): ProjectileConfig.cs
2. **Phase 2** (1 hour): ProjectileBase.cs
3. **Phase 3** (1 hour): LinearProjectile.cs + HomingProjectile.cs + prefabs
4. **Phase 4** (45 min): ProjectileManager.cs
5. **Phase 5** (1 hour): TroopCombat integration
6. **Phase 6** (1 hour): Edge case testing
7. **Phase 7** (30 min): Polish & optimization

**Total Time**: 5-6 hours

## Core Classes Cheat Sheet

### ProjectileConfig (struct)
```csharp
public struct ProjectileConfig
{
    public Vector3 spawnPosition;
    public GameObject owner;
    public Team ownerTeam;
    public Vector3 targetPosition;
    public TroopController targetTroop; // Can be null
    public float damage;               // Pre-calculated
    public float speed;
    public float maxLifetime;          // Default: 5 seconds
    public EffectModule effect;
}
```

### ProjectileBase (abstract)
```csharp
public abstract class ProjectileBase : MonoBehaviour
{
    protected ProjectileConfig config;
    protected bool isInitialized;
    protected float lifetimeRemaining;
    protected bool hasHit;

    // Override these:
    protected abstract void OnInitialized();
    protected abstract void UpdateMovement();

    // Handles everything else:
    public virtual void Initialize(ProjectileConfig cfg);
    protected virtual void OnTriggerEnter(Collider other);
}
```

### LinearProjectile
```csharp
public class LinearProjectile : ProjectileBase
{
    // Flies straight to initial target position
    // Snapshots direction at spawn time
    // Cannot adjust trajectory
}
```

### HomingProjectile
```csharp
public class HomingProjectile : ProjectileBase
{
    [SerializeField] float rotationSpeed = 360f;

    // Continuously tracks target
    // If target dies, continues to last position
    // Smooth rotation with rotationSpeed limit
}
```

### ProjectileManager (singleton)
```csharp
public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance;

    public LinearProjectile SpawnLinearProjectile(ProjectileConfig cfg);
    public HomingProjectile SpawnHomingProjectile(ProjectileConfig cfg);
    public void ReturnProjectile(ProjectileBase projectile);
}
```

## TroopCombat Changes

### Before
```csharp
case AttackType.Projectile:
    // TODO: Spawn projectile
    Debug.Log($"{owner.name} shoots projectile...");
    CurrentTarget.Health.TakeDamage(damage, owner.gameObject);
    break;
```

### After
```csharp
case AttackType.Projectile:
    SpawnLinearProjectile(damage);
    break;

case AttackType.Homing:
    SpawnHomingProjectile(damage);
    break;
```

### New Methods
```csharp
private void SpawnLinearProjectile(float damage)
{
    if (ProjectileManager.Instance == null)
    {
        // Fallback: instant damage
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
}

// Similar for SpawnHomingProjectile()
```

## Edge Cases Handled

| Scenario | Solution |
|----------|----------|
| Target dies mid-flight | Homing projectiles continue to last position |
| Owner dies before hit | GameObject reference persists briefly |
| Same-team collision | Filtered in OnTriggerEnter |
| Double-hit | `hasHit` flag prevents multiple triggers |
| Lifetime expiration | Auto-returns to pool after 5 seconds |
| Out of bounds | Lifetime timer handles automatically |
| Null target at spawn | TroopCombat checks before spawn |
| Zero distance | Fallback to transform.forward |
| Pool exhaustion | ObjectPool auto-creates new instances |
| ProjectileManager missing | Fallback to instant damage |

## Testing Checklist

### Unit Tests
- [ ] ProjectileConfig creation
- [ ] Linear projectile movement
- [ ] Homing projectile tracking
- [ ] Pool recycling

### Integration Tests
- [ ] Bow combat (linear projectiles)
- [ ] Staff combat (homing projectiles)
- [ ] Mixed combat
- [ ] Target retargeting

### Edge Case Tests
- [ ] Target dies during flight
- [ ] Owner dies before hit
- [ ] Same-team collision ignored
- [ ] Multiple projectiles hit same target
- [ ] Projectile flies off map
- [ ] Rapid fire stress test
- [ ] Pool exhaustion handling

### Performance Tests
- [ ] Profile Update() time (<0.01ms per projectile)
- [ ] Monitor pool size
- [ ] Check memory leaks
- [ ] 20+ simultaneous projectiles smooth

## Inspector Setup

### ProjectileManager GameObject
1. Create empty GameObject in scene
2. Name it "ProjectileManager"
3. Add ProjectileManager component
4. Assign prefabs:
   - `linearProjectilePrefab` → LinearProjectile.prefab
   - `homingProjectilePrefab` → HomingProjectile.prefab
5. Set `initialPoolSize` → 20

### Linear/Homing Projectile Prefabs
1. Duplicate StaffProjectile.prefab
2. Add LinearProjectile or HomingProjectile component
3. Configure Collider:
   - Type: Sphere or Capsule
   - Radius: ~0.5
   - isTrigger: true (auto-set by script)
4. Configure Rigidbody:
   - isKinematic: true (auto-set by script)
   - useGravity: false (auto-set by script)
5. Optional: Add TrailRenderer

### Weapon ScriptableObjects
**Weapon_Bow.asset:**
- attackType = `Projectile`
- projectileSpeed = `10`
- projectilePrefab = `LinearProjectile.prefab` (optional)

**Weapon_Staff.asset:**
- attackType = `Homing`
- projectileSpeed = `10`
- projectilePrefab = `HomingProjectile.prefab` (optional)

## Performance Characteristics

- **Typical Load**: 2-4 ranged troops × 2 sides = 4-8 active projectiles
- **Worst Case**: 20 simultaneous projectiles
- **Update Overhead**: ~0.2ms total (0.01ms × 20)
- **Memory**: ~10KB (500 bytes × 20 pooled instances)
- **Pool Size**: 20 initial, grows dynamically if needed

## Common Issues & Solutions

### Issue: Projectiles pass through targets
**Solution:** Increase collider radius, ensure target has collider

### Issue: Double damage on hit
**Solution:** Already handled by `hasHit` flag

### Issue: Null reference errors
**Solution:** Check all OnTriggerEnter null guards are in place

### Issue: Projectiles never return to pool
**Solution:** Verify lifetime countdown in Update() is running

### Issue: Performance drops with many projectiles
**Solution:** Cap max active projectiles at 20, reduce spawn rate

### Issue: Homing projectiles don't track
**Solution:** Verify targetTroop reference is set in config

## Future Enhancements

### Visual Effects
- Impact VFX on hit
- Spawn sound/VFX
- Custom trail colors per element

### New Projectile Types
- Bouncing projectile
- Splitting projectile
- Piercing projectile
- AOE on impact

### Ability Interactions
- Multishot (fire 3 projectiles)
- Ricochet (bounce to nearby enemy)
- Explosive shot (AOE on hit)

## Rollback Plan

If critical issues arise:
1. Comment out projectile spawn calls in TroopCombat
2. Restore original instant-damage code
3. System degrades to instant hits (no visual projectiles)
4. Debug offline and re-enable when fixed

## Success Criteria

✅ System is ready when:
- [ ] Bow troops fire arrows that travel and hit
- [ ] Staff troops fire magic bolts that home to targets
- [ ] Damage applies correctly with all modifiers
- [ ] No console errors during combat
- [ ] Pool recycles properly (size stabilizes)
- [ ] Performance is smooth (>60 FPS with 20 projectiles)
- [ ] All edge case tests pass

---

**Full implementation details:** See PROJECTILE_SYSTEM_IMPLEMENTATION_PLAN.md

**Estimated implementation time:** 5-6 hours

**Status:** Ready for implementation
