using AdaptiveDraftArena.Modules;
using UnityEngine;

namespace AdaptiveDraftArena.Combat
{
    /// <summary>
    /// Immutable configuration data for projectile initialization.
    /// Passed from TroopCombat to ProjectileManager to Projectile instance.
    /// </summary>
    public struct ProjectileConfig
    {
        // Origin data
        public Vector3 spawnPosition;
        public GameObject owner;          // The troop that fired this projectile
        public Team ownerTeam;

        // Target data
        public Vector3 targetPosition;    // Initial target position (for linear)
        public TroopController targetTroop; // Target troop reference (for homing, can be null)

        // Damage
        public float damage;              // Pre-calculated final damage

        // Movement
        public float speed;               // Units per second
        public float homingStrength;      // 0 = no homing, 1 = perfect homing (for future use)

        // Lifetime
        public float maxLifetime;         // Maximum time before auto-destroy

        // Visual
        public EffectModule effect;       // For visual tint/effects
    }
}
