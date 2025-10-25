using UnityEngine;
using AdaptiveDraftArena.Modules;

namespace AdaptiveDraftArena.Combat
{
    public class TroopCombat : MonoBehaviour
    {
        public TroopController CurrentTarget { get; private set; }
        public bool IsInRange { get; private set; }

        private TroopController owner;
        private TargetingSystem targeting;
        private WeaponModule weapon;
        private EffectModule effect;
        private float attackRange;

        private float attackTimer;

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

            // Find or update target
            if (CurrentTarget == null || !CurrentTarget.IsAlive)
            {
                CurrentTarget = targeting.FindClosestEnemy();
            }

            if (CurrentTarget == null)
            {
                owner.Movement.Stop();
                IsInRange = false;
                return;
            }

            // Check if in range
            var distance = Vector2.Distance(transform.position, CurrentTarget.transform.position);
            IsInRange = distance <= attackRange;

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

            foreach (var enemy in enemies)
            {
                var dist = Vector2.Distance(transform.position, enemy.transform.position);
                if (dist <= weapon.aoeRadius)
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
