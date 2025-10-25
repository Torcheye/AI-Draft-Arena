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
        private HashSet<TroopController> buffedTroops = new HashSet<TroopController>();

        // Performance optimizations
        private List<TroopController> cachedAllies;
        private float allyRefreshTimer;
        private float updateTimer;

        private const float AllyRefreshInterval = 0.5f;
        private const float UpdateInterval = 0.1f;

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
            // Throttle updates to 10 FPS for non-critical aura checks
            updateTimer += deltaTime;
            if (updateTimer < UpdateInterval) return;
            updateTimer = 0f;

            // Cache ally list with periodic refresh
            allyRefreshTimer -= deltaTime;
            if (allyRefreshTimer <= 0f || cachedAllies == null)
            {
                cachedAllies = TargetingSystem.GetAliveTroops(owner.Team);
                allyRefreshTimer = AllyRefreshInterval;
            }

            var ownerPos = owner.transform.position; // Cache position
            var sqrRadius = radius * radius;

            // Remove buff from troops that left radius (reverse iteration for safe removal)
            var troopsToRemove = new List<TroopController>();
            foreach (var troop in buffedTroops)
            {
                if (troop == null || troop == owner || !troop.IsAlive)
                {
                    troopsToRemove.Add(troop);
                    continue;
                }

                var sqrDistance = ownerPos.SqrDistanceXZ(troop.transform.position);
                if (sqrDistance > sqrRadius)
                {
                    // Remove buff with null safety
                    if (troop.Health != null)
                    {
                        troop.Health.ModifyMaxHP(troop.Health.MaxHP - hpBonus);
                    }
                    troopsToRemove.Add(troop);
                }
            }

            foreach (var troop in troopsToRemove)
            {
                buffedTroops.Remove(troop);
            }

            // Add buff to troops that entered radius
            foreach (var ally in cachedAllies)
            {
                if (ally == owner || buffedTroops.Contains(ally)) continue;

                var sqrDistance = ownerPos.SqrDistanceXZ(ally.transform.position);
                if (sqrDistance <= sqrRadius)
                {
                    // Apply buff with null safety
                    if (ally != null && ally.Health != null && ally.IsAlive)
                    {
                        ally.Health.ModifyMaxHP(ally.Health.MaxHP + hpBonus);
                        buffedTroops.Add(ally);
                    }
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
                if (troop != null && troop.Health != null && troop.IsAlive)
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
