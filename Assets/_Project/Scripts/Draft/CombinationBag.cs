using System.Collections.Generic;
using AdaptiveDraftArena.Modules;
using UnityEngine;

namespace AdaptiveDraftArena.Draft
{
    /// <summary>
    /// Implements 7-bag randomization algorithm for draft options.
    /// Inspired by Tetris' bag randomization - ensures no combo appears too frequently across rounds.
    ///
    /// Algorithm:
    /// 1. Pool: All available combos (base + AI-generated)
    /// 2. Bag: Shuffled subset of pool (excludes recent picks to avoid immediate repeats)
    /// 3. Draw: Remove from bag, add to recent picks
    /// 4. Refill: When bag empty, create new shuffled bag from pool (minus recent picks)
    /// 5. Result: Minimum 7 rounds between seeing the same combo (if pool > 7)
    /// </summary>
    public class CombinationBag
    {
        private List<ICombination> pool;            // Full pool of available combos
        private HashSet<ICombination> poolSet;      // O(1) lookup for pool membership
        private List<ICombination> currentBag;      // Current bag being drawn from
        private Queue<ICombination> recentPicksQueue;    // FIFO queue of recent picks
        private HashSet<ICombination> recentPicksSet;    // O(1) lookup for recent picks
        private int recentMemory;                   // How many recent picks to exclude (default 6)

        public CombinationBag(List<ICombination> initialPool, int recentMemory = 6)
        {
            this.recentMemory = recentMemory;
            pool = new List<ICombination>(initialPool);
            poolSet = new HashSet<ICombination>(initialPool);
            currentBag = new List<ICombination>();
            recentPicksQueue = new Queue<ICombination>();
            recentPicksSet = new HashSet<ICombination>();
            RefillBag();
        }

        /// <summary>
        /// Updates the pool with new AI-generated combos.
        /// New combos are immediately added to both pool and current bag.
        /// Uses HashSet for O(1) duplicate checking.
        /// </summary>
        public void AddToPool(List<ICombination> newCombos)
        {
            int addedCount = 0;
            foreach (var combo in newCombos)
            {
                if (poolSet.Add(combo)) // O(1) check + add
                {
                    pool.Add(combo);
                    currentBag.Add(combo); // Also add to current bag for immediate availability
                    addedCount++;
                }
            }

            Debug.Log($"[CombinationBag] Added {addedCount} new combos to pool (total pool: {pool.Count}, current bag: {currentBag.Count})");
        }

        /// <summary>
        /// Draws N unique combinations from the bag.
        /// Refills bag when empty (shuffle pool and create new bag).
        /// </summary>
        public List<ICombination> Draw(int count)
        {
            var drawn = new List<ICombination>();

            for (int i = 0; i < count; i++)
            {
                // Refill if bag is empty
                if (currentBag.Count == 0)
                {
                    RefillBag();
                }

                // If still empty (pool too small), break
                if (currentBag.Count == 0)
                {
                    Debug.LogWarning($"[CombinationBag] Pool exhausted, only drew {drawn.Count}/{count} combos");
                    break;
                }

                // Draw random from bag (Fisher-Yates style)
                int randomIndex = Random.Range(0, currentBag.Count);
                var combo = currentBag[randomIndex];
                currentBag.RemoveAt(randomIndex);

                drawn.Add(combo);

                // Add to recent picks (maintain both queue and set)
                recentPicksQueue.Enqueue(combo);
                recentPicksSet.Add(combo);

                // Trim recent picks to memory size
                while (recentPicksQueue.Count > recentMemory)
                {
                    var removed = recentPicksQueue.Dequeue();
                    recentPicksSet.Remove(removed);
                }
            }

            return drawn;
        }

        /// <summary>
        /// Refills the bag by shuffling the pool (excluding recent picks).
        /// Uses Fisher-Yates shuffle for uniform randomness.
        /// Uses HashSet for O(1) lookup instead of O(n) Contains.
        /// </summary>
        private void RefillBag()
        {
            currentBag.Clear();

            // Add all pool combos except recent picks (O(1) lookup with HashSet)
            foreach (var combo in pool)
            {
                if (!recentPicksSet.Contains(combo))
                {
                    currentBag.Add(combo);
                }
            }

            // Fisher-Yates shuffle for uniform distribution
            for (int i = currentBag.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (currentBag[i], currentBag[j]) = (currentBag[j], currentBag[i]);
            }

            Debug.Log($"[CombinationBag] Refilled bag with {currentBag.Count} combos (pool: {pool.Count}, recent: {recentPicksQueue.Count})");
        }
    }
}
