using System.Collections.Generic;
using System.Linq;
using AdaptiveDraftArena.Match;
using AdaptiveDraftArena.Modules;
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
        /// Phase 3: Win/loss tracking for progressive difficulty.
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

            // Phase 3: Analyze round outcomes for win/loss tracking
            AnalyzeRoundOutcomes(state, profile);

            Debug.Log($"[PlayerAnalyzer] Profile: Element={profile.mostUsedElement}, AvgAmount={profile.avgAmount:F1}, AvgHP={profile.avgHP:F1}, AvgDMG={profile.avgDamage:F1}, AvgRange={profile.avgRange:F1}, AvgSpeed={profile.avgSpeed:F1}, Wins={profile.winningPicks.Count}, Losses={profile.losingPicks.Count}");

            return profile;
        }

        /// <summary>
        /// Analyzes round history to track winning picks, losing picks, and successful AI counters.
        /// Phase 3: Used for Mastery difficulty to learn from past rounds.
        /// FIXED: Uses round numbers to prevent index misalignment and duplicate analysis.
        /// </summary>
        private void AnalyzeRoundOutcomes(MatchState state, PlayerProfile profile)
        {
            // Need at least one completed round to analyze
            if (state.RoundHistory == null || state.RoundHistory.Count == 0)
            {
                Debug.Log("[PlayerAnalyzer] No round history yet for outcome analysis");
                return;
            }

            // Validate data integrity
            if (state.RoundHistory.Count > state.PlayerPickHistory.Count)
            {
                Debug.LogError($"[PlayerAnalyzer] Data integrity error: RoundHistory count ({state.RoundHistory.Count}) exceeds PlayerPickHistory count ({state.PlayerPickHistory.Count})!");
                return;
            }

            // Analyze each completed round
            for (int i = 0; i < state.RoundHistory.Count; i++)
            {
                var round = state.RoundHistory[i];

                // Skip if already analyzed (prevents double-counting if AnalyzePlayer called multiple times)
                if (profile.analyzedRounds.Contains(round.RoundNumber))
                    continue;

                // Mark as analyzed
                profile.analyzedRounds.Add(round.RoundNumber);

                // The pick for this round is at index (round.RoundNumber - 1)
                // RoundNumbers are 1-indexed, arrays are 0-indexed
                int pickIndex = round.RoundNumber - 1;

                if (pickIndex < 0 || pickIndex >= state.PlayerPickHistory.Count)
                {
                    Debug.LogWarning($"[PlayerAnalyzer] Invalid pickIndex {pickIndex} for round {round.RoundNumber}");
                    continue;
                }

                TroopCombination playerPick = state.PlayerPickHistory[pickIndex];
                TroopCombination aiPick = pickIndex < state.AIPickHistory.Count ? state.AIPickHistory[pickIndex] : null;

                if (playerPick == null)
                    continue; // Skip if we don't have player pick data

                // Categorize based on round outcome
                if (round.Winner == Team.Player)
                {
                    profile.winningPicks.Add(playerPick);
                    Debug.Log($"[PlayerAnalyzer] Round {round.RoundNumber}: Player won with {playerPick.DisplayName}");
                }
                else // AI won
                {
                    profile.losingPicks.Add(playerPick);

                    // Track successful AI counter
                    if (aiPick != null)
                    {
                        profile.successfulCounters.Add(aiPick);
                        Debug.Log($"[PlayerAnalyzer] Round {round.RoundNumber}: AI won with {aiPick.DisplayName} (counter to {playerPick.DisplayName})");
                    }
                }
            }

            Debug.Log($"[PlayerAnalyzer] Outcome analysis complete: {profile.winningPicks.Count} wins, {profile.losingPicks.Count} losses, {profile.successfulCounters.Count} successful counters");
        }
    }
}
