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

            // Score all available combos
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
                Debug.LogError("[CounterStrategyEngine] No valid combos after filtering!");
                return null;
            }

            // Find best combo manually (no LINQ allocations)
            var bestCombo = scoredCombos[0];
            for (int i = 1; i < scoredCombos.Count; i++)
            {
                if (scoredCombos[i].score > bestCombo.score)
                {
                    bestCombo = scoredCombos[i];
                }
            }

            Debug.Log($"[CounterStrategyEngine] Selected {bestCombo.combo.DisplayName} (Score: {bestCombo.score})");

            return bestCombo.combo;
        }

        /// <summary>
        /// Scores a combo against player profile.
        /// Phase 1: Element counter only (+50 points).
        /// </summary>
        private int ScoreCombo(TroopCombination combo, PlayerProfile profile)
        {
            int score = 0;

            // Phase 1: Simple element counter
            string counterElement = profile.GetCounterElement();
            if (combo.effect != null && combo.effect.moduleId == counterElement)
            {
                score += 50; // Big bonus for element advantage
            }

            return score;
        }
    }
}
