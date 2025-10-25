using UnityEngine;
using AdaptiveDraftArena.Combat;
using AdaptiveDraftArena.Modules;

namespace AdaptiveDraftArena.Abilities
{
    /// <summary>
    /// Control ability: Attacks reduce target movement speed by 30% for 2 seconds
    /// </summary>
    public class SlowAbility : IAbilityEffect
    {
        private TroopController owner;
        private float speedReduction = 0.3f;
        private float duration = 2f;

        public void Initialize(AbilityModule module, TroopController troopOwner)
        {
            owner = troopOwner;

            // Read parameters
            var parameters = module.GetParameters();
            if (parameters.ContainsKey("speedReduction"))
            {
                speedReduction = parameters["speedReduction"];
            }
            if (parameters.ContainsKey("duration"))
            {
                duration = parameters["duration"];
            }
        }

        public void Update(float deltaTime) { }

        public void OnAttack(TroopController target, float damageDealt)
        {
            // Apply slow status effect to target
            var slowEffect = new StatusEffect(StatusType.Slow, duration, speedReduction, owner);

            if (target.AbilityExecutor != null)
            {
                target.AbilityExecutor.ApplyStatusEffect(slowEffect);
                Debug.Log($"{owner.name} slowed {target.name} by {speedReduction * 100}%");
            }
        }

        public float ModifyOutgoingDamage(float baseDamage, TroopController target) => baseDamage;
        public float ModifyIncomingDamage(float incomingDamage, TroopController attacker) => incomingDamage;
        public void OnTakeDamage(float damage, TroopController attacker) { }
        public void OnKill(TroopController victim) { }
        public void OnDeath() { }
        public void Cleanup() { }
    }
}
