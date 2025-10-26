using System.Collections.Generic;
using System.Linq;
using AdaptiveDraftArena.Modules;

namespace AdaptiveDraftArena.AI
{
    /// <summary>
    /// Stores analyzed player behavior patterns for counter-generation.
    /// Phase 1: Simple element tracking and stat averages.
    /// Phase 2: Multi-factor tracking (body, weapon, ability, range, speed).
    /// </summary>
    public class PlayerProfile
    {
        // Phase 1: Element tracking
        public Dictionary<string, int> elementUsage = new Dictionary<string, int>();
        public string mostUsedElement;

        // Phase 2: Module tracking
        public Dictionary<string, int> bodyUsage = new Dictionary<string, int>();
        public Dictionary<string, int> weaponUsage = new Dictionary<string, int>();
        public Dictionary<string, int> abilityUsage = new Dictionary<string, int>();

        // Phase 1: Tactical tracking
        public float avgAmount;
        public float avgHP;
        public float avgDamage;

        // Phase 2: Extended tactical tracking
        public float avgRange;
        public float avgSpeed;

        // Recent picks (last 3)
        public List<TroopCombination> recentPicks = new List<TroopCombination>();

        /// <summary>
        /// Returns the element that counters the player's most-used element.
        /// Fire → Water, Water → Nature, Nature → Fire
        /// </summary>
        public string GetCounterElement()
        {
            if (string.IsNullOrEmpty(mostUsedElement))
                return ElementIds.WATER; // Default

            switch (mostUsedElement)
            {
                case ElementIds.FIRE:
                    return ElementIds.WATER;
                case ElementIds.WATER:
                    return ElementIds.NATURE;
                case ElementIds.NATURE:
                    return ElementIds.FIRE;
                default:
                    return ElementIds.WATER; // Fallback
            }
        }

        /// <summary>
        /// Returns the most frequently used body module ID.
        /// </summary>
        public string GetMostUsedBody()
        {
            if (bodyUsage.Count == 0)
                return null;

            string maxKey = null;
            int maxValue = 0;
            var first = true;

            foreach (var entry in bodyUsage)
            {
                if (first || entry.Value > maxValue)
                {
                    maxKey = entry.Key;
                    maxValue = entry.Value;
                    first = false;
                }
            }

            return maxKey;
        }

        /// <summary>
        /// Returns the most frequently used weapon module ID.
        /// </summary>
        public string GetMostUsedWeapon()
        {
            if (weaponUsage.Count == 0)
                return null;

            string maxKey = null;
            int maxValue = 0;
            var first = true;

            foreach (var entry in weaponUsage)
            {
                if (first || entry.Value > maxValue)
                {
                    maxKey = entry.Key;
                    maxValue = entry.Value;
                    first = false;
                }
            }

            return maxKey;
        }

        /// <summary>
        /// Returns true if player prefers swarm tactics (amount >= 3).
        /// </summary>
        public bool PrefersSwarm()
        {
            return avgAmount >= 3f;
        }

        /// <summary>
        /// Returns true if player prefers melee combat (range < 2.5).
        /// </summary>
        public bool PrefersMelee()
        {
            return avgRange < 2.5f;
        }

        /// <summary>
        /// Returns true if player prefers ranged combat (range >= 3.0).
        /// </summary>
        public bool PrefersRanged()
        {
            return avgRange >= 3.0f;
        }

        /// <summary>
        /// Returns true if player prefers fast units (speed >= 2.0).
        /// </summary>
        public bool PrefersFastUnits()
        {
            return avgSpeed >= 2.0f;
        }
    }
}
