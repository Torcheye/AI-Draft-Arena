using UnityEngine;

namespace AdaptiveDraftArena.Modules
{
    public enum TroopRole
    {
        Tank,
        DPS,
        Support,
        Flanker
    }

    [CreateAssetMenu(fileName = "Body_", menuName = "AdaptiveDraftArena/Modules/Body")]
    public class BodyModule : ModuleBase
    {
        [Header("Stats")]
        public float baseHP = 8f;
        public float movementSpeed = 1.5f;
        public float attackRange = 1.5f;
        public float size = 1f;

        [Header("Visuals - 3D")]
        public GameObject bodyModelPrefab; // 3D model for gameplay
        public Vector3 weaponSocketPosition; // 3D position for weapon attachment

        [Header("Visuals - UI")]
        public Sprite bodySprite; // Icon for draft/UI screens

        [Header("Role")]
        public TroopRole role = TroopRole.DPS;
    }
}
