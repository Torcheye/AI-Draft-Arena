using AdaptiveDraftArena.Combat;
using AdaptiveDraftArena.Modules;

namespace AdaptiveDraftArena.Abilities
{
    /// <summary>
    /// Interface for all ability behaviors.
    /// Abilities modify troop behavior through hooks in combat/damage flow.
    /// </summary>
    public interface IAbilityEffect
    {
        /// <summary>
        /// Initialize the ability with module data and troop owner.
        /// Called once when troop is spawned.
        /// </summary>
        void Initialize(AbilityModule module, TroopController owner);

        /// <summary>
        /// Called every frame while troop is alive.
        /// Use for passive effects like regeneration, auras, etc.
        /// </summary>
        void Update(float deltaTime);

        /// <summary>
        /// Modify outgoing damage before it's applied to target.
        /// Return modified damage value.
        /// </summary>
        float ModifyOutgoingDamage(float baseDamage, TroopController target);

        /// <summary>
        /// Modify incoming damage before it's applied to this troop.
        /// Return modified damage value (can return 0 to negate).
        /// </summary>
        float ModifyIncomingDamage(float incomingDamage, TroopController attacker);

        /// <summary>
        /// Called when this troop attacks another troop.
        /// Use for on-hit effects like slow, lifesteal, etc.
        /// </summary>
        void OnAttack(TroopController target, float damageDealt);

        /// <summary>
        /// Called when this troop takes damage.
        /// Use for revenge effects like thorns, etc.
        /// </summary>
        void OnTakeDamage(float damage, TroopController attacker);

        /// <summary>
        /// Called when this troop kills an enemy.
        /// </summary>
        void OnKill(TroopController victim);

        /// <summary>
        /// Called when this troop dies.
        /// Use for death effects, cleanup, etc.
        /// </summary>
        void OnDeath();

        /// <summary>
        /// Cleanup resources, unsubscribe from events, etc.
        /// </summary>
        void Cleanup();
    }
}
