using System.Collections.Generic;
using UnityEngine;
using AdaptiveDraftArena.Core;
using AdaptiveDraftArena.Modules;

namespace AdaptiveDraftArena.Combat
{
    public class TroopSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject troopPrefab;

        [Header("Configuration")]
        private GameConfig config;

        private void Awake()
        {
            if (GameManager.Instance != null)
            {
                config = GameManager.Instance.Config;
            }
        }

        public List<TroopController> SpawnTroops(TroopCombination combination, Team team, Transform parent = null)
        {
            if (combination == null)
            {
                Debug.LogError("TroopSpawner: Cannot spawn null combination!");
                return new List<TroopController>();
            }

            if (!combination.IsValid())
            {
                Debug.LogError($"TroopSpawner: Invalid combination {combination.name}!");
                return new List<TroopController>();
            }

            var spawnedTroops = new List<TroopController>();
            var spawnZone = team == Team.Player ? config.playerSpawnZone : config.aiSpawnZone;

            // Spawn the specified amount of troops
            for (int i = 0; i < combination.amount; i++)
            {
                var spawnPos = GetRandomPositionInZone(spawnZone);
                var troop = SpawnSingleTroop(combination, team, spawnPos, parent);

                if (troop != null)
                {
                    spawnedTroops.Add(troop);
                }
            }

            Debug.Log($"Spawned {spawnedTroops.Count} troops: {combination.DisplayName} for {team}");

            return spawnedTroops;
        }

        private TroopController SpawnSingleTroop(TroopCombination combination, Team team, Vector2 position, Transform parent)
        {
            // Instantiate from prefab
            var troopObj = Instantiate(troopPrefab, position, Quaternion.identity, parent);

            // Get TroopController component
            var troopController = troopObj.GetComponent<TroopController>();
            if (troopController == null)
            {
                Debug.LogError("TroopSpawner: Troop prefab missing TroopController component!");
                Destroy(troopObj);
                return null;
            }

            // Initialize the troop
            troopController.Initialize(combination, team, position);

            return troopController;
        }

        private Vector2 GetRandomPositionInZone(Rect zone)
        {
            var x = Random.Range(zone.xMin, zone.xMax);
            var y = Random.Range(zone.yMin, zone.yMax);
            return new Vector2(x, y);
        }

        public void SetTroopPrefab(GameObject prefab)
        {
            troopPrefab = prefab;
        }
    }
}
