using System.Collections.Generic;
using UnityEngine;

namespace AdaptiveDraftArena
{
    public static class Extensions
    {
        // Shuffle list in place
        public static void Shuffle<T>(this IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = Random.Range(0, n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        // Get random element from list
        public static T GetRandom<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0) return default;
            return list[Random.Range(0, list.Count)];
        }

        // Check if list is null or empty
        public static bool IsNullOrEmpty<T>(this IList<T> list)
        {
            return list == null || list.Count == 0;
        }

        // 3D Ground Plane Utilities (for Y-locked isometric gameplay)

        // Calculate distance on XZ plane (ignoring Y-axis)
        public static float DistanceXZ(this Vector3 from, Vector3 to)
        {
            var dx = to.x - from.x;
            var dz = to.z - from.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // Calculate squared distance on XZ plane (faster, use for comparisons)
        public static float SqrDistanceXZ(this Vector3 from, Vector3 to)
        {
            var dx = to.x - from.x;
            var dz = to.z - from.z;
            return dx * dx + dz * dz;
        }

        // Get direction on XZ plane (normalized, Y=0)
        public static Vector3 DirectionXZ(this Vector3 from, Vector3 to)
        {
            var direction = to - from;
            direction.y = 0f;

            // Handle zero-length case
            if (direction.sqrMagnitude < 0.001f)
                return Vector3.forward;

            return direction.normalized;
        }

        // Clamp position Y to ground level
        public static Vector3 ToGroundPosition(this Vector3 position, float groundLevel)
        {
            position.y = groundLevel;
            return position;
        }
    }
}
