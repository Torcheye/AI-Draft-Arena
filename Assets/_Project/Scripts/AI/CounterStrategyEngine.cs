using System.Collections.Generic;
using System.Linq;
using AdaptiveDraftArena.Core;
using AdaptiveDraftArena.Modules;
using UnityEngine;

namespace AdaptiveDraftArena.AI
{
    /// <summary>
    /// Scores troop combinations and selects counters to player strategy.
    /// Phase 1: Simple element-based scoring only.
    /// </summary>
    public class CounterStrategyEngine
    {
        private GameConfig config;

        public CounterStrategyEngine(GameConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// Generates a counter combination based on player profile.
        /// Phase 1: Picks from available combos using element advantage only.
        /// Phase 2: Builds dynamic combos OR picks from pool (whichever scores higher).
        /// </summary>
        public TroopCombination GenerateCounter(
            PlayerProfile profile,
            List<TroopCombination> availableCombos)
        {
            if (availableCombos == null || availableCombos.Count == 0)
            {
                Debug.LogError("[CounterStrategyEngine] No available combos to choose from!");
                return null;
            }

            // Phase 2: Try building a dynamic combo
            TroopCombination dynamicCombo = null;
            int dynamicScore = 0;

            if (CanBuildDynamicCombo())
            {
                dynamicCombo = BuildDynamicCounter(profile);
                if (dynamicCombo != null)
                {
                    dynamicScore = ScoreCombo(dynamicCombo, profile);
                    Debug.Log($"[CounterStrategyEngine] Dynamic combo scored {dynamicScore}: {dynamicCombo.DisplayName}");
                }
            }

            // Score all available combos from pool
            var scoredCombos = new List<(TroopCombination combo, int score)>();

            foreach (var combo in availableCombos)
            {
                // Validate combo before scoring
                if (combo == null || combo.effect == null)
                {
                    Debug.LogWarning("[CounterStrategyEngine] Invalid combo found, skipping...");
                    continue;
                }

                int score = ScoreCombo(combo, profile);
                scoredCombos.Add((combo, score));
            }

            // Ensure we have at least one valid combo
            if (scoredCombos.Count == 0)
            {
                // If no valid combos from pool, use dynamic if available
                if (dynamicCombo != null)
                {
                    Debug.Log($"[CounterStrategyEngine] Using dynamic combo (pool empty)");
                    return dynamicCombo;
                }

                Debug.LogError("[CounterStrategyEngine] No valid combos after filtering!");
                return null;
            }

            // Find best combo from pool manually (no LINQ allocations)
            var bestPoolCombo = scoredCombos[0];
            for (int i = 1; i < scoredCombos.Count; i++)
            {
                if (scoredCombos[i].score > bestPoolCombo.score)
                {
                    bestPoolCombo = scoredCombos[i];
                }
            }

            // Compare dynamic vs pool and pick best
            if (dynamicCombo != null && dynamicScore > bestPoolCombo.score)
            {
                Debug.Log($"[CounterStrategyEngine] Selected DYNAMIC combo {dynamicCombo.DisplayName} (Score: {dynamicScore})");
                return dynamicCombo;
            }
            else
            {
                Debug.Log($"[CounterStrategyEngine] Selected POOL combo {bestPoolCombo.combo.DisplayName} (Score: {bestPoolCombo.score})");
                return bestPoolCombo.combo;
            }
        }

        /// <summary>
        /// Checks if we have all module pools available to build dynamic combos.
        /// DISABLED FOR PHASE 2: ScriptableObject.CreateInstance causes memory leaks.
        /// TODO Phase 3: Implement proper pooling or use plain C# runtime class instead.
        /// </summary>
        private bool CanBuildDynamicCombo()
        {
            // DISABLED: ScriptableObject.CreateInstance creates persistent memory that's never freed
            // This causes memory leaks in long play sessions
            // Re-enable when we have proper object pooling or runtime combo class
            return false;

            // Original logic (keep for reference):
            // return config.Bodies.Count > 0 &&
            //        config.Weapons.Count > 0 &&
            //        config.Abilities.Count > 0 &&
            //        config.Effects.Count > 0;
        }

        /// <summary>
        /// Builds a new counter combination dynamically by selecting strategic modules.
        /// Phase 2 feature: AI creates NEW combos instead of just picking from pool.
        /// NOTE: Currently disabled due to ScriptableObject memory leak. Enable in Phase 3 with proper pooling.
        /// </summary>
        private TroopCombination BuildDynamicCounter(PlayerProfile profile)
        {
            // Pick strategic modules
            var counterElement = PickCounterElement(profile);
            var counterBody = PickCounterBody(profile);
            var counterWeapon = PickCounterWeapon(profile, counterBody);
            var counterAbility = PickCounterAbility(profile, counterBody);
            var amount = PickCounterAmount(profile);

            // Validate all modules and amount
            if (counterElement == null || counterBody == null || counterWeapon == null ||
                counterAbility == null || !IsValidAmount(amount))
            {
                Debug.LogWarning("[CounterStrategyEngine] Failed to build valid dynamic combo");
                return null;
            }

            // Create combination
            var combo = ScriptableObject.CreateInstance<TroopCombination>();
            combo.body = counterBody;
            combo.weapon = counterWeapon;
            combo.ability = counterAbility;
            combo.effect = counterElement;
            combo.amount = amount;
            combo.isAIGenerated = true;

            return combo;
        }

        /// <summary>
        /// Validates that amount is one of the allowed values (1, 2, 3, 5).
        /// </summary>
        private bool IsValidAmount(int amount)
        {
            return amount == 1 || amount == 2 || amount == 3 || amount == 5;
        }

        private EffectModule PickCounterElement(PlayerProfile profile)
        {
            string counterElementId = profile.GetCounterElement();

            // Find matching effect
            foreach (var effect in config.Effects)
            {
                if (effect.moduleId == counterElementId)
                    return effect;
            }

            // Fallback: return first available
            return config.Effects.Count > 0 ? config.Effects[0] : null;
        }

        private BodyModule PickCounterBody(PlayerProfile profile)
        {
            // If player uses melee, pick ranged body
            if (profile.PrefersMelee())
            {
                foreach (var body in config.Bodies)
                {
                    if (body.attackRange >= 3.0f)
                        return body;
                }
            }

            // If player uses ranged, pick fast melee
            if (profile.PrefersRanged())
            {
                foreach (var body in config.Bodies)
                {
                    if (body.attackRange < 2.0f && body.movementSpeed >= 2.0f)
                        return body;
                }
            }

            // Fallback: pick tank
            foreach (var body in config.Bodies)
            {
                if (body.role == TroopRole.Tank)
                    return body;
            }

            // Ultimate fallback: first body
            return config.Bodies.Count > 0 ? config.Bodies[0] : null;
        }

        private WeaponModule PickCounterWeapon(PlayerProfile profile, BodyModule body)
        {
            // If player builds tanky, pick high DPS weapon
            if (profile.avgHP > 12.0f)
            {
                WeaponModule bestDPS = null;
                float maxDPS = 0f;

                foreach (var weapon in config.Weapons)
                {
                    float dps = weapon.baseDamage / weapon.attackCooldown;
                    if (dps > maxDPS)
                    {
                        maxDPS = dps;
                        bestDPS = weapon;
                    }
                }

                if (bestDPS != null)
                    return bestDPS;
            }

            // Fallback: random weapon
            int randomIndex = UnityEngine.Random.Range(0, config.Weapons.Count);
            return config.Weapons[randomIndex];
        }

        private AbilityModule PickCounterAbility(PlayerProfile profile, BodyModule body)
        {
            // Pick synergistic ability based on body role
            if (body.role == TroopRole.Tank)
            {
                // Look for defensive ability
                foreach (var ability in config.Abilities)
                {
                    if (ability.category == AbilityCategory.Defensive)
                        return ability;
                }
            }

            if (body.role == TroopRole.DPS)
            {
                // Look for offensive ability
                foreach (var ability in config.Abilities)
                {
                    if (ability.category == AbilityCategory.Offensive)
                        return ability;
                }
            }

            // Fallback: random ability
            int randomIndex = UnityEngine.Random.Range(0, config.Abilities.Count);
            return config.Abilities[randomIndex];
        }

        private int PickCounterAmount(PlayerProfile profile)
        {
            // Counter swarm with elite, counter elite with swarm
            if (profile.PrefersSwarm())
            {
                // Elite: 1 or 2
                return UnityEngine.Random.Range(0, 2) == 0 ? 1 : 2;
            }
            else
            {
                // Swarm: 3 or 5
                return UnityEngine.Random.Range(0, 2) == 0 ? 3 : 5;
            }
        }

        /// <summary>
        /// Scores a combo against player profile.
        /// Phase 1: Element counter only (+50 points).
        /// Phase 2: Multi-layer scoring (Element 30, Range 25, Stats 20, Amount 15, Ability 10).
        /// </summary>
        private int ScoreCombo(TroopCombination combo, PlayerProfile profile)
        {
            int score = 0;

            // Layer 1: Element Advantage (30 points)
            if (CountersElement(combo, profile))
                score += 30;

            // Layer 2: Range Advantage (25 points)
            if (CountersRange(combo, profile))
                score += 25;

            // Layer 3: Stat Counter (20 points)
            if (CountersStats(combo, profile))
                score += 20;

            // Layer 4: Amount Counter (15 points)
            if (CountersAmount(combo, profile))
                score += 15;

            // Layer 5: Ability Synergy (10 points)
            if (HasAbilitySynergy(combo))
                score += 10;

            return score;
        }

        /// <summary>
        /// Returns true if combo has the element advantage against player's most-used element.
        /// Fire < Water < Nature < Fire
        /// </summary>
        private bool CountersElement(TroopCombination combo, PlayerProfile profile)
        {
            if (combo.effect == null || string.IsNullOrEmpty(profile.mostUsedElement))
                return false;

            string playerElement = profile.mostUsedElement;
            string comboElement = combo.effect.moduleId;

            // Fire < Water < Nature < Fire
            if (playerElement == ElementIds.FIRE && comboElement == ElementIds.WATER) return true;
            if (playerElement == ElementIds.WATER && comboElement == ElementIds.NATURE) return true;
            if (playerElement == ElementIds.NATURE && comboElement == ElementIds.FIRE) return true;

            return false;
        }

        /// <summary>
        /// Returns true if combo has range advantage against player's preferred range.
        /// Melee → Counter with Ranged, Ranged → Counter with Fast Melee
        /// </summary>
        private bool CountersRange(TroopCombination combo, PlayerProfile profile)
        {
            if (combo.body == null)
                return false;

            float playerRange = profile.avgRange;
            float comboRange = combo.body.attackRange;

            // If player uses melee (range < 2), counter with ranged (range >= 3)
            if (playerRange < 2.0f && comboRange >= 3.0f)
                return true;

            // If player uses ranged (range >= 3), counter with fast melee (high speed)
            if (playerRange >= 3.0f && comboRange < 2.0f && combo.body.movementSpeed >= 2.0f)
                return true;

            return false;
        }

        /// <summary>
        /// Returns true if combo counters player's stat distribution.
        /// High HP → Counter with DPS, High Damage + Low HP → Counter with Tank
        /// </summary>
        private bool CountersStats(TroopCombination combo, PlayerProfile profile)
        {
            if (combo.weapon == null || combo.body == null)
                return false;

            // If player builds tanky (high HP), counter with sustained DPS
            if (profile.avgHP > 12.0f)
            {
                float comboDPS = combo.weapon.baseDamage / combo.weapon.attackCooldown;
                if (comboDPS > 3.0f) return true; // High DPS counters tanks
            }

            // If player builds glass cannon (high DMG, low HP), counter with tank
            if (profile.avgDamage > 4.0f && profile.avgHP < 10.0f)
            {
                if (combo.GetFinalHP() * combo.amount > 15.0f) return true; // Tank outlasts glass cannon
            }

            return false;
        }

        /// <summary>
        /// Returns true if combo counters player's amount preference.
        /// Swarm → Counter with Elite/AOE, Elite → Counter with Swarm
        /// </summary>
        private bool CountersAmount(TroopCombination combo, PlayerProfile profile)
        {
            bool playerUsesSwarm = profile.PrefersSwarm(); // avgAmount >= 3

            // Counter swarm with AOE or elite
            if (playerUsesSwarm)
            {
                // AOE ability counters swarm
                if (combo.ability != null && combo.ability.category == AbilityCategory.Offensive)
                    return true;

                // Elite (amount 1-2) with high stats counters swarm
                if (combo.amount <= 2 && combo.GetFinalHP() * combo.amount > 12.0f)
                    return true;
            }

            // Counter elite with swarm
            if (!playerUsesSwarm && combo.amount >= 3)
                return true;

            return false;
        }

        /// <summary>
        /// Returns true if combo has good synergy between modules.
        /// Tank + Regeneration, DPS + Berserk, Swarm + Shield Aura, etc.
        /// </summary>
        private bool HasAbilitySynergy(TroopCombination combo)
        {
            if (combo.ability == null || combo.body == null)
                return false;

            // Tank + Defensive ability = good synergy
            if (combo.body.role == TroopRole.Tank &&
                combo.ability.category == AbilityCategory.Defensive)
                return true;

            // DPS + Offensive ability = good synergy
            if (combo.body.role == TroopRole.DPS &&
                combo.ability.category == AbilityCategory.Offensive)
                return true;

            // Swarm + Utility/Control = good synergy
            if (combo.amount >= 3 &&
                (combo.ability.category == AbilityCategory.Utility ||
                 combo.ability.category == AbilityCategory.Control))
                return true;

            return false;
        }
    }
}
