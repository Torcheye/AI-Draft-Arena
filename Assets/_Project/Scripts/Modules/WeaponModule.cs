using UnityEngine;

namespace AdaptiveDraftArena.Modules
{
    public enum AttackType
    {
        Melee,
        Projectile,
        Homing,
        AOE
    }

    [CreateAssetMenu(fileName = "Weapon_", menuName = "AdaptiveDraftArena/Modules/Weapon")]
    public class WeaponModule : ModuleBase
    {
        [Header("Stats")]
        public int baseDamage = 3;
        public float attackCooldown = 0.8f;
        public AttackType attackType = AttackType.Melee;

        [Header("Projectile Settings")]
        public GameObject projectilePrefab;
        public float projectileSpeed = 10f;

        [Header("AOE Settings")]
        public float aoeRadius = 0f;

        [Header("Visuals - 3D")]
        public GameObject weaponModelPrefab; // 3D model for gameplay
        public Vector3 modelOffset; // 3D offset from weapon socket
        public Vector3 modelRotation; // 3D rotation from weapon socket

        [Header("Visuals - UI")]
        public Sprite weaponSprite; // Icon for draft/UI screens

        [Header("Animation")]
        public AnimationClip attackAnimation;
    }
}
