using System.Collections.Generic;
using System.Linq;
using AdaptiveDraftArena.Match;
using UnityEngine;

namespace AdaptiveDraftArena.AI
{
    /// <summary>
    /// Analyzes player picks and creates a PlayerProfile.
    /// Phase 1: Simple element tracking and stat averages.
    /// </summary>
    public class PlayerAnalyzer
    {
        /// <summary>
        /// Analyzes player pick history and returns a profile.
        /// Phase 1: Element tracking and stat averages.
        /// Phase 2: Module tracking (body, weapon, ability) and range/speed.
        /// </summary>
        public PlayerProfile AnalyzePlayer(MatchState state)
        {
            var profile = new PlayerProfile();

            // Handle no history (Round 1)
            if (state.PlayerPickHistory.Count == 0)
            {
                Debug.Log("[PlayerAnalyzer] No pick history yet, returning default profile");
                return profile;
            }

            // Track element, body, weapon, ability usage
            foreach (var pick in state.PlayerPickHistory)
            {
                // Element tracking (Phase 1)
                if (pick.effect != null)
                {
                    string element = pick.effect.moduleId;
                    if (!profile.elementUsage.ContainsKey(element))
                        profile.elementUsage[element] = 0;
                    profile.elementUsage[element]++;
                }

                // Body tracking (Phase 2)
                if (pick.body != null)
                {
                    string body = pick.body.moduleId;
                    if (!profile.bodyUsage.ContainsKey(body))
                        profile.bodyUsage[body] = 0;
                    profile.bodyUsage[body]++;
                }

                // Weapon tracking (Phase 2)
                if (pick.weapon != null)
                {
                    string weapon = pick.weapon.moduleId;
                    if (!profile.weaponUsage.ContainsKey(weapon))
                        profile.weaponUsage[weapon] = 0;
                    profile.weaponUsage[weapon]++;
                }

                // Ability tracking (Phase 2)
                if (pick.ability != null)
                {
                    string ability = pick.ability.moduleId;
                    if (!profile.abilityUsage.ContainsKey(ability))
                        profile.abilityUsage[ability] = 0;
                    profile.abilityUsage[ability]++;
                }
            }

            // Find most used element (manual max-finding to avoid LINQ allocations)
            if (profile.elementUsage.Count > 0)
            {
                var maxEntry = default(KeyValuePair<string, int>);
                var first = true;

                foreach (var entry in profile.elementUsage)
                {
                    if (first || entry.Value > maxEntry.Value)
                    {
                        maxEntry = entry;
                        first = false;
                    }
                }

                profile.mostUsedElement = maxEntry.Key;
            }
            else
            {
                profile.mostUsedElement = ElementIds.WATER; // Default fallback
                Debug.LogWarning("[PlayerAnalyzer] No element usage data, defaulting to WATER");
            }

            // Calculate stat averages (single-pass to avoid LINQ allocations)
            if (state.PlayerPickHistory.Count > 0)
            {
                float totalAmount = 0f;
                float totalHP = 0f;
                float totalDamage = 0f;
                float totalRange = 0f;
                float totalSpeed = 0f;
                int count = state.PlayerPickHistory.Count;

                foreach (var pick in state.PlayerPickHistory)
                {
                    totalAmount += pick.amount;
                    totalHP += pick.GetFinalHP() * pick.amount;
                    totalDamage += pick.GetFinalDamage() * pick.amount;
                    totalRange += pick.body != null ? pick.body.attackRange : 0f;
                    totalSpeed += pick.body != null ? pick.body.movementSpeed : 0f;
                }

                profile.avgAmount = totalAmount / count;
                profile.avgHP = totalHP / count;
                profile.avgDamage = totalDamage / count;
                profile.avgRange = totalRange / count;
                profile.avgSpeed = totalSpeed / count;
            }
            else
            {
                // Default balanced values if no history
                profile.avgAmount = 2f;
                profile.avgHP = 10f;
                profile.avgDamage = 3f;
                profile.avgRange = 2f;
                profile.avgSpeed = 1.5f;
            }

            // Store last 3 picks (manual slice to avoid LINQ allocations)
            profile.recentPicks.Clear();
            int startIndex = System.Math.Max(0, state.PlayerPickHistory.Count - 3);
            for (int i = startIndex; i < state.PlayerPickHistory.Count; i++)
            {
                profile.recentPicks.Add(state.PlayerPickHistory[i]);
            }

            Debug.Log($"[PlayerAnalyzer] Profile: Element={profile.mostUsedElement}, AvgAmount={profile.avgAmount:F1}, AvgHP={profile.avgHP:F1}, AvgDMG={profile.avgDamage:F1}, AvgRange={profile.avgRange:F1}, AvgSpeed={profile.avgSpeed:F1}");

            return profile;
        }
    }
}
