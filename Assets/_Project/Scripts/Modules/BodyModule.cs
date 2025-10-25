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

        [Header("Visuals")]
        public Sprite bodySprite;
        public Vector2 weaponAnchorPoint;

        [Header("Role")]
        public TroopRole role = TroopRole.DPS;
    }
}
