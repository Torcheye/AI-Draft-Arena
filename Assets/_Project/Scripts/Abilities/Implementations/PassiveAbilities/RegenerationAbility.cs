using UnityEngine;
using AdaptiveDraftArena.Combat;
using AdaptiveDraftArena.Modules;

namespace AdaptiveDraftArena.Abilities
{
    /// <summary>
    /// Passive ability: Heal 1 HP per second
    /// </summary>
    public class RegenerationAbility : IAbilityEffect
    {
        private TroopController owner;
        private float healPerSecond = 1f;
        private float healTimer;

        public void Initialize(AbilityModule module, TroopController troopOwner)
        {
            owner = troopOwner;

            // Read parameters if specified
            var parameters = module.GetParameters();
            if (parameters.ContainsKey("healPerSecond"))
            {
                healPerSecond = parameters["healPerSecond"];
            }

            healTimer = 0f;
        }

        public void Update(float deltaTime)
        {
            healTimer += deltaTime;

            // Heal every second
            if (healTimer >= 1f)
            {
                owner.Health.Heal(healPerSecond);
                healTimer = 0f;
            }
        }

        public float ModifyOutgoingDamage(float baseDamage, TroopController target) => baseDamage;
        public float ModifyIncomingDamage(float incomingDamage, TroopController attacker) => incomingDamage;
        public void OnAttack(TroopController target, float damageDealt) { }
        public void OnTakeDamage(float damage, TroopController attacker) { }
        public void OnKill(TroopController victim) { }
        public void OnDeath() { }
        public void Cleanup() { }
    }
}
