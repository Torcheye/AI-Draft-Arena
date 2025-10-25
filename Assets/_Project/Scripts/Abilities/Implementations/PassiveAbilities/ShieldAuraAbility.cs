using System.Collections.Generic;
using UnityEngine;
using AdaptiveDraftArena.Combat;
using AdaptiveDraftArena.Modules;

namespace AdaptiveDraftArena.Abilities
{
    /// <summary>
    /// Passive ability: +2 HP to this unit and allies within radius
    /// </summary>
    public class ShieldAuraAbility : IAbilityEffect
    {
        private TroopController owner;
        private float hpBonus = 2f;
        private float radius = 3f;
        private List<TroopController> buffedTroops = new List<TroopController>();

        public void Initialize(AbilityModule module, TroopController troopOwner)
        {
            owner = troopOwner;

            // Read parameters
            var parameters = module.GetParameters();
            if (parameters.ContainsKey("hpBonus"))
            {
                hpBonus = parameters["hpBonus"];
            }
            if (parameters.ContainsKey("radius"))
            {
                radius = parameters["radius"];
            }

            // Apply bonus to self immediately
            owner.Health.ModifyMaxHP(owner.Health.MaxHP + hpBonus);
        }

        public void Update(float deltaTime)
        {
            // Find nearby allies and apply HP bonus
            var allies = TargetingSystem.GetAliveTroops(owner.Team);

            // Remove buff from troops that left radius
            for (int i = buffedTroops.Count - 1; i >= 0; i--)
            {
                var troop = buffedTroops[i];
                if (troop == null || troop == owner || !troop.IsAlive)
                {
                    buffedTroops.RemoveAt(i);
                    continue;
                }

                var distance = Vector2.Distance(owner.transform.position, troop.transform.position);
                if (distance > radius)
                {
                    // Remove buff
                    troop.Health.ModifyMaxHP(troop.Health.MaxHP - hpBonus);
                    buffedTroops.RemoveAt(i);
                }
            }

            // Add buff to troops that entered radius
            foreach (var ally in allies)
            {
                if (ally == owner || buffedTroops.Contains(ally)) continue;

                var distance = Vector2.Distance(owner.transform.position, ally.transform.position);
                if (distance <= radius)
                {
                    // Apply buff
                    ally.Health.ModifyMaxHP(ally.Health.MaxHP + hpBonus);
                    buffedTroops.Add(ally);
                }
            }
        }

        public float ModifyOutgoingDamage(float baseDamage, TroopController target) => baseDamage;
        public float ModifyIncomingDamage(float incomingDamage, TroopController attacker) => incomingDamage;
        public void OnAttack(TroopController target, float damageDealt) { }
        public void OnTakeDamage(float damage, TroopController attacker) { }
        public void OnKill(TroopController victim) { }

        public void OnDeath()
        {
            // Remove all buffs when aura source dies
            foreach (var troop in buffedTroops)
            {
                if (troop != null && troop.IsAlive)
                {
                    troop.Health.ModifyMaxHP(troop.Health.MaxHP - hpBonus);
                }
            }
            buffedTroops.Clear();
        }

        public void Cleanup()
        {
            OnDeath();
        }
    }
}
