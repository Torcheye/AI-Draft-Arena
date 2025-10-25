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

        private static readonly List<TroopController> EmptyList = new List<TroopController>(0);

        private GameConfig GetConfig()
        {
            if (config == null && GameManager.Instance != null)
            {
                config = GameManager.Instance.Config;
            }
            return config;
        }

        public List<TroopController> SpawnTroops(TroopCombination combination, Team team, Transform parent = null)
        {
            if (combination == null)
            {
                Debug.LogError("TroopSpawner: Cannot spawn null combination!");
                return EmptyList;
            }

            if (!combination.IsValid())
            {
                Debug.LogError($"TroopSpawner: Invalid combination {combination.name}!");
                return EmptyList;
            }

            var cfg = GetConfig();
            if (cfg == null)
            {
                Debug.LogError("TroopSpawner: Config is null! Cannot spawn troops.");
                return EmptyList;
            }

            var spawnedTroops = new List<TroopController>(combination.amount);
            var spawnCenter = team == Team.Player ? cfg.playerSpawnCenter : cfg.aiSpawnCenter;
            var spawnSize = team == Team.Player ? cfg.playerSpawnSize : cfg.aiSpawnSize;

            // Spawn the specified amount of troops
            for (int i = 0; i < combination.amount; i++)
            {
                var spawnPos = GetRandomPositionInZone(spawnCenter, spawnSize, cfg.groundLevel);
                var troop = SpawnSingleTroop(combination, team, spawnPos, parent);

                if (troop != null)
                {
                    spawnedTroops.Add(troop);
                }
            }

            Debug.Log($"Spawned {spawnedTroops.Count} troops: {combination.DisplayName} for {team}");

            return spawnedTroops;
        }

        private TroopController SpawnSingleTroop(TroopCombination combination, Team team, Vector3 position, Transform parent)
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

        private Vector3 GetRandomPositionInZone(Vector3 center, Vector3 size, float groundLevel)
        {
            var x = Random.Range(center.x - size.x / 2f, center.x + size.x / 2f);
            var z = Random.Range(center.z - size.z / 2f, center.z + size.z / 2f);
            return new Vector3(x, groundLevel, z);
        }

        public void SetTroopPrefab(GameObject prefab)
        {
            troopPrefab = prefab;
        }
    }
}
