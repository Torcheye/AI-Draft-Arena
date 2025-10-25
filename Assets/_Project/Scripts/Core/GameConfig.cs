using System;
using UnityEngine;

namespace AdaptiveDraftArena.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "AdaptiveDraftArena/Config/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Match Settings")]
        public int maxRounds = 7;
        public int winsRequired = 4;

        [Header("Draft Settings")]
        public float draftDuration = 15f;
        public int draftOptionsCount = 3;

        [Header("Battle Settings")]
        public float battleDuration = 30f;
        public int maxTroopsPerSide = 4;
        public Vector3 battlefieldSize = new Vector3(20f, 0f, 12f); // Width, Y(locked to groundLevel), Depth
        public float groundLevel = 0f;

        [Header("Battlefield")]
        public GameObject battlefieldPrefab;

        [Header("Spawn Zones (Deprecated - Use Battlefield Prefab)")]
        [Obsolete("Use battlefieldPrefab instead")]
        public Vector3 playerSpawnCenter = new Vector3(3.5f, 0f, 6f);
        [Obsolete("Use battlefieldPrefab instead")]
        public Vector3 playerSpawnSize = new Vector3(3f, 0f, 6f);

        [Obsolete("Use battlefieldPrefab instead")]
        public Vector3 aiSpawnCenter = new Vector3(16.5f, 0f, 6f);
        [Obsolete("Use battlefieldPrefab instead")]
        public Vector3 aiSpawnSize = new Vector3(3f, 0f, 6f);

        [Header("Amount Multipliers")]
        public float[] statMultipliers = { 1.0f, 0.8f, 0.6f, 0.4f };
        public float[] abilityMultipliers = { 1.0f, 1.0f, 0.5f, 0.0f };

        [Header("Element Modifiers")]
        public float advantageMultiplier = 1.5f;
        public float disadvantageMultiplier = 0.75f;

        [Header("AI Settings")]
        public float aiGenerationTimeout = 10f;
        public bool useMockAI = false;

        [Header("Visuals")]
        public float hitFlashDuration = 0.1f;
        public float deathFadeDuration = 0.5f;
        public float screenShakeIntensity = 0.2f;

        /// <summary>
        /// Validates that the battlefield prefab has the required BattlefieldBounds component.
        /// </summary>
        /// <returns>True if battlefield prefab is valid, false otherwise</returns>
        public bool ValidateBattlefieldPrefab()
        {
            if (battlefieldPrefab == null)
            {
                return false;
            }
            return battlefieldPrefab.GetComponent<Battle.BattlefieldBounds>() != null;
        }

        private void OnValidate()
        {
            // Check if battlefield prefab is assigned
            if (battlefieldPrefab != null && !ValidateBattlefieldPrefab())
            {
                Debug.LogWarning("GameConfig: Battlefield prefab is assigned but missing BattlefieldBounds component!");
            }

            #pragma warning disable CS0618 // Suppress obsolete warnings for deprecated fields
            // Ensure Y components are locked to ground level (for deprecated fields)
            playerSpawnCenter.y = groundLevel;
            aiSpawnCenter.y = groundLevel;
            playerSpawnSize.y = 0f;
            aiSpawnSize.y = 0f;
            battlefieldSize.y = 0f;

            // Ensure spawn sizes are positive
            playerSpawnSize.x = Mathf.Max(0.1f, playerSpawnSize.x);
            playerSpawnSize.z = Mathf.Max(0.1f, playerSpawnSize.z);
            aiSpawnSize.x = Mathf.Max(0.1f, aiSpawnSize.x);
            aiSpawnSize.z = Mathf.Max(0.1f, aiSpawnSize.z);
            #pragma warning restore CS0618

            // Ensure battlefield dimensions are valid
            battlefieldSize.x = Mathf.Max(1f, battlefieldSize.x);
            battlefieldSize.z = Mathf.Max(1f, battlefieldSize.z);
        }
    }
}
