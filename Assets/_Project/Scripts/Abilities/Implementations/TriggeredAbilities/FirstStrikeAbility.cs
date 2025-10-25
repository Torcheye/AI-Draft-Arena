using UnityEngine;
using AdaptiveDraftArena.Combat;
using AdaptiveDraftArena.Modules;

namespace AdaptiveDraftArena.Abilities
{
    /// <summary>
    /// Triggered ability: Deal 2× damage on first attack only
    /// </summary>
    public class FirstStrikeAbility : IAbilityEffect
    {
        private TroopController owner;
        private float damageMultiplier = 2f;
        private bool hasTriggered;

        public void Initialize(AbilityModule module, TroopController troopOwner)
        {
            owner = troopOwner;

            // Read parameters
            var parameters = module.GetParameters();
            if (parameters.ContainsKey("damageMultiplier"))
            {
                damageMultiplier = parameters["damageMultiplier"];
            }

            hasTriggered = false;
        }

        public void Update(float deltaTime) { }

        public float ModifyOutgoingDamage(float baseDamage, TroopController target)
        {
            if (!hasTriggered)
            {
                Debug.Log($"{owner.name} FIRST STRIKE! {damageMultiplier}× damage");
                return baseDamage * damageMultiplier;
            }
            return baseDamage;
        }

        public void OnAttack(TroopController target, float damageDealt)
        {
            // Mark as triggered after first attack
            if (!hasTriggered)
            {
                hasTriggered = true;
            }
        }

        public float ModifyIncomingDamage(float incomingDamage, TroopController attacker) => incomingDamage;
        public void OnTakeDamage(float damage, TroopController attacker) { }
        public void OnKill(TroopController victim) { }
        public void OnDeath() { }
        public void Cleanup() { }
    }
}
