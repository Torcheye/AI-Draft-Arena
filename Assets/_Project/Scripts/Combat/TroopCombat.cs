using System;
using UnityEngine;
using AdaptiveDraftArena.Modules;

namespace AdaptiveDraftArena.Combat
{
    public class TroopCombat : MonoBehaviour
    {
        public TroopController CurrentTarget { get; private set; }
        public bool IsInRange { get; private set; }

        // Events
        public event Action OnAttack;

        private TroopController owner;
        private TargetingSystem targeting;
        private WeaponModule weapon;
        private EffectModule effect;
        private float attackRange;

        private float attackTimer;
        private float retargetCooldown;

        private const float RetargetInterval = 0.5f;

        public void Initialize(TroopController troopController)
        {
            owner = troopController;
            weapon = owner.Combination.weapon;
            effect = owner.Combination.effect;
            attackRange = owner.Combination.body.attackRange;
            targeting = GetComponent<TargetingSystem>();

            attackTimer = 0f;
        }

        private void Update()
        {
            if (!owner.IsAlive) return;

            var myPosition = transform.position; // Cache transform.position

            // Decrement retarget cooldown unconditionally
            retargetCooldown -= Time.deltaTime;

            // Find or update target (with cooldown to reduce O(n²) complexity)
            if (CurrentTarget == null || !CurrentTarget.IsAlive)
            {
                if (retargetCooldown <= 0f)
                {
                    CurrentTarget = targeting.FindClosestEnemy();
                    retargetCooldown = RetargetInterval;
                }

                // If still no target after retry, stop and return
                if (CurrentTarget == null)
                {
                    owner.Movement.Stop();
                    IsInRange = false;
                    return;
                }
            }

            // Check if in range (using squared distance for performance)
            var sqrDistance = myPosition.SqrDistanceXZ(CurrentTarget.transform.position);
            IsInRange = sqrDistance <= attackRange * attackRange;

            if (IsInRange)
            {
                // In range - attack
                owner.Movement.Stop();

                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0)
                {
                    PerformAttack();
                    attackTimer = weapon.attackCooldown;
                }
            }
            else
            {
                // Out of range - move closer
                owner.Movement.MoveToward(CurrentTarget.transform.position);
            }
        }

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
                    // TODO: Spawn projectile
                    Debug.Log($"{owner.name} shoots projectile at {CurrentTarget.name} for {damage} damage");
                    CurrentTarget.Health.TakeDamage(damage, owner.gameObject);
                    break;

                case AttackType.Homing:
                    // TODO: Spawn homing projectile
                    Debug.Log($"{owner.name} casts homing spell at {CurrentTarget.name} for {damage} damage");
                    CurrentTarget.Health.TakeDamage(damage, owner.gameObject);
                    break;

                case AttackType.AOE:
                    ApplyAOEDamage(damage);
                    break;
            }

            // TODO: Trigger ability OnAttack hooks
        }

        private void ApplyMeleeDamage(float damage)
        {
            CurrentTarget.Health.TakeDamage(damage, owner.gameObject);
            Debug.Log($"{owner.name} melee attacks {CurrentTarget.name} for {damage} damage");

            // Notify ability system of attack
            owner.AbilityExecutor?.OnAttackPerformed(CurrentTarget, damage);
        }

        private void ApplyAOEDamage(float damage)
        {
            var enemyTeam = owner.Team == Team.Player ? Team.AI : Team.Player;
            var enemies = TargetingSystem.GetAliveTroops(enemyTeam);
            var myPosition = transform.position;
            var sqrAoeRadius = weapon.aoeRadius * weapon.aoeRadius;

            foreach (var enemy in enemies)
            {
                var sqrDist = myPosition.SqrDistanceXZ(enemy.transform.position);
                if (sqrDist <= sqrAoeRadius)
                {
                    enemy.Health.TakeDamage(damage, owner.gameObject);
                }
            }

            Debug.Log($"{owner.name} AOE attacks for {damage} damage in {weapon.aoeRadius} radius");
        }

        private float CalculateDamage()
        {
            // Base damage from weapon
            var baseDamage = (float)weapon.baseDamage;

            // Apply amount multiplier
            var statMultiplier = TroopStats.GetStatMultiplier(owner.Combination.amount);
            baseDamage *= statMultiplier;

            // Apply element multiplier
            var elementMultiplier = TroopStats.CalculateElementMultiplier(effect, CurrentTarget.Combination.effect);
            baseDamage *= elementMultiplier;

            // Apply ability modifiers (Berserk, FirstStrike, etc.)
            if (owner.AbilityExecutor != null)
            {
                baseDamage = owner.AbilityExecutor.ModifyOutgoingDamage(baseDamage, CurrentTarget);
            }

            return baseDamage;
        }
    }
}
