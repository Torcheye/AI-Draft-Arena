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

            // Track element usage
            foreach (var pick in state.PlayerPickHistory)
            {
                if (pick.effect == null) continue;

                string element = pick.effect.moduleId;
                if (!profile.elementUsage.ContainsKey(element))
                    profile.elementUsage[element] = 0;
                profile.elementUsage[element]++;
            }

            // Find most used element
            if (profile.elementUsage.Count > 0)
            {
                profile.mostUsedElement = profile.elementUsage
                    .OrderByDescending(kvp => kvp.Value)
                    .First().Key;
            }
            else
            {
                profile.mostUsedElement = ElementIds.WATER; // Default fallback
                Debug.LogWarning("[PlayerAnalyzer] No element usage data, defaulting to WATER");
            }

            // Calculate stat averages (defensive against empty history)
            if (state.PlayerPickHistory.Count > 0)
            {
                profile.avgAmount = state.PlayerPickHistory.Average(p => (float)p.amount);
                profile.avgHP = state.PlayerPickHistory.Average(p => p.GetFinalHP() * p.amount);
                profile.avgDamage = state.PlayerPickHistory.Average(p => p.GetFinalDamage() * p.amount);
            }
            else
            {
                // Default balanced values if no history
                profile.avgAmount = 2f;
                profile.avgHP = 10f;
                profile.avgDamage = 3f;
            }

            // Store last 3 picks
            profile.recentPicks = state.PlayerPickHistory
                .Skip(System.Math.Max(0, state.PlayerPickHistory.Count - 3))
                .ToList();

            Debug.Log($"[PlayerAnalyzer] Profile: Element={profile.mostUsedElement}, AvgAmount={profile.avgAmount:F1}, AvgHP={profile.avgHP:F1}, AvgDMG={profile.avgDamage:F1}");

            return profile;
        }
    }
}
