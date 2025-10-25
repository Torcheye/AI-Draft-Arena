using System.Collections.Generic;
using UnityEngine;

namespace AdaptiveDraftArena.Combat
{
    public class TargetingSystem : MonoBehaviour
    {
        private Team myTeam;
        private static Dictionary<Team, List<TroopController>> troopsByTeam = new Dictionary<Team, List<TroopController>>();

        public void Initialize(Team team)
        {
            myTeam = team;
        }

        public TroopController FindClosestEnemy()
        {
            var enemyTeam = myTeam == Team.Player ? Team.AI : Team.Player;

            if (!troopsByTeam.ContainsKey(enemyTeam))
                return null;

            var enemies = troopsByTeam[enemyTeam];
            if (enemies == null || enemies.Count == 0)
                return null;

            TroopController closest = null;
            var minSqrDistance = float.MaxValue;
            var myPosition = transform.position;

            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive)
                    continue;

                var sqrDistance = myPosition.SqrDistanceXZ(enemy.transform.position);
                if (sqrDistance < minSqrDistance)
                {
                    minSqrDistance = sqrDistance;
                    closest = enemy;

                    // Early exit if essentially at same position
                    if (sqrDistance < 0.001f)
                        break;
                }
            }

            return closest;
        }

        public static void RegisterTroop(TroopController troop)
        {
            if (!troopsByTeam.ContainsKey(troop.Team))
            {
                troopsByTeam[troop.Team] = new List<TroopController>();
            }

            if (!troopsByTeam[troop.Team].Contains(troop))
            {
                troopsByTeam[troop.Team].Add(troop);
            }
        }

        public static void UnregisterTroop(TroopController troop)
        {
            if (troopsByTeam.ContainsKey(troop.Team))
            {
                troopsByTeam[troop.Team].Remove(troop);
            }
        }

        public static void ClearAll()
        {
            troopsByTeam.Clear();
        }

        public static List<TroopController> GetAliveTroops(Team team)
        {
            if (!troopsByTeam.ContainsKey(team))
                return new List<TroopController>();

            var troops = troopsByTeam[team];
            var result = new List<TroopController>(troops.Count);

            for (var i = 0; i < troops.Count; i++)
            {
                if (troops[i] != null && troops[i].IsAlive)
                    result.Add(troops[i]);
            }

            return result;
        }

        public static int GetAliveCount(Team team)
        {
            if (!troopsByTeam.ContainsKey(team))
                return 0;

            var count = 0;
            var troops = troopsByTeam[team];
            for (var i = 0; i < troops.Count; i++)
            {
                if (troops[i] != null && troops[i].IsAlive)
                    count++;
            }
            return count;
        }

        public static float GetTotalHP(Team team)
        {
            if (!troopsByTeam.ContainsKey(team))
                return 0f;

            var total = 0f;
            var troops = troopsByTeam[team];
            for (var i = 0; i < troops.Count; i++)
            {
                if (troops[i] != null && troops[i].IsAlive && troops[i].Health != null)
                {
                    total += troops[i].Health.CurrentHP;
                }
            }
            return total;
        }
    }
}
