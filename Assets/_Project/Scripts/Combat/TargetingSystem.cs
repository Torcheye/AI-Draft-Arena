using System.Collections.Generic;
using System.Linq;
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
            var minDistance = float.MaxValue;
            var myPosition = (Vector2)transform.position;

            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive)
                    continue;

                var distance = Vector2.Distance(myPosition, enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = enemy;
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

            return troopsByTeam[team].Where(t => t != null && t.IsAlive).ToList();
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
            foreach (var troop in troopsByTeam[team])
            {
                if (troop != null && troop.IsAlive && troop.Health != null)
                {
                    total += troop.Health.CurrentHP;
                }
            }
            return total;
        }
    }
}
