using System.Threading;
using Cysharp.Threading.Tasks;
using AdaptiveDraftArena.Core;
using AdaptiveDraftArena.Match;
using AdaptiveDraftArena.Modules;
using UnityEngine;

namespace AdaptiveDraftArena.AI
{
    /// <summary>
    /// Main orchestrator for AI counter-generation system.
    /// Coordinates PlayerAnalyzer and CounterStrategyEngine.
    /// Phase 1: Simple flow - analyze, score, pick.
    /// </summary>
    public class AIGenerationOrchestrator : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GameConfig config;

        private PlayerAnalyzer analyzer;
        private CounterStrategyEngine engine;

        private void Awake()
        {
            // Validate config
            if (config == null)
            {
                Debug.LogError("[AIGenerationOrchestrator] GameConfig not assigned! Disabling component.");
                enabled = false;
                return;
            }

            // Initialize components
            analyzer = new PlayerAnalyzer();
            engine = new CounterStrategyEngine(config);

            Debug.Log("[AIGenerationOrchestrator] Initialized successfully");
        }

        /// <summary>
        /// Generates a counter combination based on player patterns.
        /// Phase 1: Uses base combinations pool only, no delay.
        /// </summary>
        public async UniTask<TroopCombination> GenerateCounterAsync(
            MatchState state,
            CancellationToken ct)
        {
            Debug.Log("[AIGenerationOrchestrator] Generating counter...");

            // Step 1: Analyze player patterns
            var profile = analyzer.AnalyzePlayer(state);

            // Step 2: Get available combinations
            var availableCombos = state.GetFullDraftPool();

            // Validate pool
            if (availableCombos.Count == 0)
            {
                Debug.LogError("[AIGenerationOrchestrator] Draft pool is empty! Cannot generate counter.");
                return null;
            }

            // Step 3: Generate counter
            var counter = engine.GenerateCounter(profile, availableCombos);

            // Phase 1: No delay, instant return
            // Phase 4 will add "thinking" delay here

            if (counter != null)
            {
                Debug.Log($"[AIGenerationOrchestrator] Generated: {counter.DisplayName}");
            }

            return counter;
        }
    }
}
