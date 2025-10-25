using UnityEngine;
using AdaptiveDraftArena.Combat;
using AdaptiveDraftArena.Modules;

namespace AdaptiveDraftArena.Abilities
{
    /// <summary>
    /// Triggered ability: +50% damage when below 50% HP
    /// </summary>
    public class BerserkAbility : IAbilityEffect
    {
        private TroopController owner;
        private float damageBonus = 0.5f;
        private float hpThreshold = 0.5f;
        private bool isActive;

        public void Initialize(AbilityModule module, TroopController troopOwner)
        {
            owner = troopOwner;

            // Read parameters
            var parameters = module.GetParameters();
            if (parameters.ContainsKey("damageBonus"))
            {
                damageBonus = parameters["damageBonus"];
            }
            if (parameters.ContainsKey("hpThreshold"))
            {
                hpThreshold = parameters["hpThreshold"];
            }

            isActive = false;
        }

        public void Update(float deltaTime)
        {
            // Check if HP dropped below threshold
            var hpPercent = owner.Health.HealthPercent;

            if (!isActive && hpPercent <= hpThreshold)
            {
                isActive = true;
                Debug.Log($"{owner.name} BERSERK activated! +{damageBonus * 100}% damage");
                // TODO: Spawn red aura VFX
            }
        }

        public float ModifyOutgoingDamage(float baseDamage, TroopController target)
        {
            return isActive ? baseDamage * (1f + damageBonus) : baseDamage;
        }

        public float ModifyIncomingDamage(float incomingDamage, TroopController attacker) => incomingDamage;
        public void OnAttack(TroopController target, float damageDealt) { }
        public void OnTakeDamage(float damage, TroopController attacker) { }
        public void OnKill(TroopController victim) { }
        public void OnDeath() { }
        public void Cleanup() { }
    }
}
