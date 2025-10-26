using UnityEngine;
using AdaptiveDraftArena.Combat;
using AdaptiveDraftArena.Match;
using AdaptiveDraftArena.Modules;
using Cysharp.Threading.Tasks;

namespace AdaptiveDraftArena.Core
{
    /// <summary>
    /// Simple test controller to spawn troops and test combat
    /// </summary>
    public class BattleTestController : MonoBehaviour
    {
        [Header("Test Combinations")]
        [SerializeField] private TroopCombination playerCombination;
        [SerializeField] private TroopCombination aiCombination;

        [Header("Components")]
        [SerializeField] private TroopSpawner troopSpawner;

        [Header("Test Settings")]
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private KeyCode respawnKey = KeyCode.Space;

        private void Start()
        {
            // if (spawnOnStart)
            // {
            //     SpawnTestTroops();
            // }
            
            var matchController = GetComponent<MatchController>();
            matchController.StartMatchAsync().Forget();
        }

        private void Update()
        {
            // Press Space to respawn troops for testing
            // if (Input.GetKeyDown(respawnKey))
            // {
            //     CleanupExistingTroops();
            //     SpawnTestTroops();
            // }
        }

        public void SpawnTestTroops()
        {
            if (troopSpawner == null)
            {
                Debug.LogError("BattleTestController: TroopSpawner not assigned!");
                return;
            }

            if (playerCombination == null || aiCombination == null)
            {
                Debug.LogError("BattleTestController: Combinations not assigned!");
                return;
            }

            Debug.Log("=== Spawning Test Troops ===");

            // Spawn player troops
            troopSpawner.SpawnTroops(playerCombination, Team.Player);

            // Spawn AI troops
            troopSpawner.SpawnTroops(aiCombination, Team.AI);

            Debug.Log("Test troops spawned! Watch them fight!");
        }

        private void CleanupExistingTroops()
        {
            TargetingSystem.ClearAll();

            var allTroops = FindObjectsByType<TroopController>(FindObjectsSortMode.None);
            foreach (var troop in allTroops)
            {
                Destroy(troop.gameObject);
            }

            Debug.Log("Cleaned up existing troops");
        }

        private void OnGUI()
        {
            // Simple debug UI
            GUI.Label(new Rect(10, 10, 400, 30), $"Press {respawnKey} to respawn troops");

            var playerTroops = TargetingSystem.GetAliveTroops(Team.Player);
            var aiTroops = TargetingSystem.GetAliveTroops(Team.AI);

            GUI.Label(new Rect(10, 40, 400, 30), $"Player Troops: {playerTroops.Count}");
            GUI.Label(new Rect(10, 70, 400, 30), $"AI Troops: {aiTroops.Count}");

            if (playerTroops.Count == 0 && aiTroops.Count > 0)
            {
                GUI.Label(new Rect(10, 100, 400, 30), "AI WINS!");
            }
            else if (aiTroops.Count == 0 && playerTroops.Count > 0)
            {
                GUI.Label(new Rect(10, 100, 400, 30), "PLAYER WINS!");
            }
        }
    }
}
