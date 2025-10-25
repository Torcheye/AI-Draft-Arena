using UnityEngine;

namespace AdaptiveDraftArena.Battle
{
    /// <summary>
    /// Defines visual, adjustable spawn bounds for troops using BoxCollider components.
    /// Provides designer-friendly spawn zone configuration via scene editing.
    /// </summary>
    public class BattlefieldBounds : MonoBehaviour
    {
        [Header("Spawn Areas")]
        [SerializeField] private BoxCollider playerSpawnArea;
        [SerializeField] private BoxCollider aiSpawnArea;

        [Header("Auto-Detection (Optional)")]
        [SerializeField] private string playerAreaPath = "spawning_areas/spawning_area_player";
        [SerializeField] private string aiAreaPath = "spawning_areas/spawning_area_ai";

        // Cached bounds for performance
        private Bounds playerBoundsCache;
        private Bounds aiBoundsCache;
        private bool boundsCached;

        private void Awake()
        {
            CacheBounds();
        }

        private void OnValidate()
        {
            // Skip auto-detection if both areas are already assigned
            if (playerSpawnArea != null && aiSpawnArea != null)
            {
                ValidateSpawnArea(playerSpawnArea, "Player");
                ValidateSpawnArea(aiSpawnArea, "AI");
                return;
            }

            // Auto-detect spawn areas if not assigned
            if (playerSpawnArea == null && !string.IsNullOrEmpty(playerAreaPath))
            {
                var playerArea = transform.Find(playerAreaPath);
                if (playerArea != null)
                {
                    playerSpawnArea = playerArea.GetComponent<BoxCollider>();
                    if (playerSpawnArea == null)
                    {
                        Debug.LogWarning($"BattlefieldBounds: {playerAreaPath} found but missing BoxCollider component!");
                    }
                }
            }

            if (aiSpawnArea == null && !string.IsNullOrEmpty(aiAreaPath))
            {
                var aiArea = transform.Find(aiAreaPath);
                if (aiArea != null)
                {
                    aiSpawnArea = aiArea.GetComponent<BoxCollider>();
                    if (aiSpawnArea == null)
                    {
                        Debug.LogWarning($"BattlefieldBounds: {aiAreaPath} found but missing BoxCollider component!");
                    }
                }
            }

            // Validate spawn areas have reasonable sizes
            ValidateSpawnArea(playerSpawnArea, "Player");
            ValidateSpawnArea(aiSpawnArea, "AI");
        }

        private void ValidateSpawnArea(BoxCollider area, string areaName)
        {
            if (area == null) return;

            var size = area.size;
            if (size.x < 1f || size.z < 1f)
            {
                Debug.LogWarning($"BattlefieldBounds: {areaName} spawn area is very small (Size: {size}). Consider increasing size to at least 1x1 units.");
            }

            if (!area.isTrigger)
            {
                Debug.LogWarning($"BattlefieldBounds: {areaName} spawn area should be marked as 'Is Trigger'.");
            }
        }

        private void CacheBounds()
        {
            if (playerSpawnArea != null)
            {
                playerBoundsCache = playerSpawnArea.bounds;
            }

            if (aiSpawnArea != null)
            {
                aiBoundsCache = aiSpawnArea.bounds;
            }

            boundsCached = true;
        }

        /// <summary>
        /// Gets the world-space bounds for player spawn area.
        /// </summary>
        public Bounds GetPlayerSpawnBounds()
        {
            if (!boundsCached)
            {
                CacheBounds();
            }

            if (playerSpawnArea == null)
            {
                Debug.LogError("BattlefieldBounds: Player spawn area not assigned!");
                return new Bounds(Vector3.zero, Vector3.one);
            }

            return playerBoundsCache;
        }

        /// <summary>
        /// Gets the world-space bounds for AI spawn area.
        /// </summary>
        public Bounds GetAISpawnBounds()
        {
            if (!boundsCached)
            {
                CacheBounds();
            }

            if (aiSpawnArea == null)
            {
                Debug.LogError("BattlefieldBounds: AI spawn area not assigned!");
                return new Bounds(Vector3.zero, Vector3.one);
            }

            return aiBoundsCache;
        }

        /// <summary>
        /// Gets a random position within the specified bounds at the given ground level.
        /// </summary>
        /// <param name="bounds">The bounds to spawn within</param>
        /// <param name="groundLevel">The Y position for the spawn point</param>
        /// <returns>A random position within the bounds</returns>
        public Vector3 GetRandomPositionInBounds(Bounds bounds, float groundLevel)
        {
            var x = Random.Range(bounds.min.x, bounds.max.x);
            var z = Random.Range(bounds.min.z, bounds.max.z);
            return new Vector3(x, groundLevel, z);
        }

        #if UNITY_EDITOR
        private static GUIStyle playerLabelStyle;
        private static GUIStyle aiLabelStyle;
        #endif

        private void OnDrawGizmos()
        {
            #if UNITY_EDITOR
            // Initialize cached styles to avoid GC allocations
            if (playerLabelStyle == null)
            {
                playerLabelStyle = new GUIStyle() { normal = new GUIStyleState() { textColor = Color.green } };
                aiLabelStyle = new GUIStyle() { normal = new GUIStyleState() { textColor = Color.red } };
            }
            #endif

            // Draw player spawn area in green
            if (playerSpawnArea != null)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                Gizmos.DrawWireCube(playerSpawnArea.bounds.center, playerSpawnArea.bounds.size);

                #if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    playerSpawnArea.bounds.center + Vector3.up * (playerSpawnArea.bounds.size.y / 2f + 0.5f),
                    "Player Spawn",
                    playerLabelStyle
                );
                #endif
            }

            // Draw AI spawn area in red
            if (aiSpawnArea != null)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                Gizmos.DrawWireCube(aiSpawnArea.bounds.center, aiSpawnArea.bounds.size);

                #if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    aiSpawnArea.bounds.center + Vector3.up * (aiSpawnArea.bounds.size.y / 2f + 0.5f),
                    "AI Spawn",
                    aiLabelStyle
                );
                #endif
            }
        }
    }
}
