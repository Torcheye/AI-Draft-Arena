using System.Collections.Generic;
using System.Linq;
using AdaptiveDraftArena.Modules;

namespace AdaptiveDraftArena.AI
{
    /// <summary>
    /// Stores analyzed player behavior patterns for counter-generation.
    /// Phase 1: Simple element tracking and stat averages.
    /// </summary>
    public class PlayerProfile
    {
        // Element tracking
        public Dictionary<string, int> elementUsage = new Dictionary<string, int>();
        public string mostUsedElement;

        // Tactical tracking
        public float avgAmount;
        public float avgHP;
        public float avgDamage;

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
        /// Returns true if player prefers swarm tactics (amount >= 3).
        /// </summary>
        public bool PrefersSwarm()
        {
            return avgAmount >= 3f;
        }

        /// <summary>
        /// Returns true if player prefers melee combat (range < 2.5).
        /// Phase 1: Not used yet (avgRange not tracked).
        /// </summary>
        public bool PrefersMelee()
        {
            // Will implement in Phase 2 when we track avgRange
            return false;
        }
    }
}
