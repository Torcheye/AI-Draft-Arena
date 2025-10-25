using System;
using UnityEngine;
using AdaptiveDraftArena.Combat;
using AdaptiveDraftArena.Modules;

namespace AdaptiveDraftArena.Abilities
{
    /// <summary>
    /// Component attached to troops that executes ability logic.
    /// Uses reflection to instantiate the correct IAbilityEffect implementation.
    /// </summary>
    public class AbilityExecutor : MonoBehaviour
    {
        private IAbilityEffect abilityEffect;
        private AbilityModule abilityModule;
        private TroopController owner;
        private StatusEffectManager statusEffectManager;

        public bool HasAbility => abilityEffect != null;

        private void Awake()
        {
            statusEffectManager = GetComponent<StatusEffectManager>();
            if (statusEffectManager == null)
            {
                statusEffectManager = gameObject.AddComponent<StatusEffectManager>();
            }
        }

        public void Initialize(AbilityModule module, TroopController troopController)
        {
            abilityModule = module;
            owner = troopController;
            statusEffectManager.Initialize(owner);

            // Check if ability class name is specified
            if (module == null || string.IsNullOrEmpty(module.abilityClassName))
            {
                Debug.Log($"{owner.name} has no ability");
                return;
            }

            // Try to instantiate ability via reflection
            try
            {
                var abilityTypeName = $"AdaptiveDraftArena.Abilities.{module.abilityClassName}";
                var abilityType = Type.GetType(abilityTypeName);

                if (abilityType == null)
                {
                    Debug.LogWarning($"Ability class not found: {abilityTypeName}");
                    return;
                }

                if (!typeof(IAbilityEffect).IsAssignableFrom(abilityType))
                {
                    Debug.LogError($"Class {abilityTypeName} does not implement IAbilityEffect!");
                    return;
                }

                abilityEffect = (IAbilityEffect)Activator.CreateInstance(abilityType);
                abilityEffect.Initialize(module, owner);

                Debug.Log($"{owner.name} ability initialized: {module.displayName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create ability {module.abilityClassName}: {ex.Message}");
            }
        }

        private void Update()
        {
            if (!owner.IsAlive || abilityEffect == null) return;

            abilityEffect.Update(Time.deltaTime);
        }

        // Hooks for combat system to call

        public float ModifyOutgoingDamage(float baseDamage, TroopController target)
        {
            if (abilityEffect == null) return baseDamage;
            return abilityEffect.ModifyOutgoingDamage(baseDamage, target);
        }

        public float ModifyIncomingDamage(float damage, TroopController attacker)
        {
            if (abilityEffect == null) return damage;
            return abilityEffect.ModifyIncomingDamage(damage, attacker);
        }

        public void OnAttackPerformed(TroopController target, float damageDealt)
        {
            abilityEffect?.OnAttack(target, damageDealt);
        }

        public void OnDamageTaken(float damage, TroopController attacker)
        {
            abilityEffect?.OnTakeDamage(damage, attacker);
        }

        public void OnKilledEnemy(TroopController victim)
        {
            abilityEffect?.OnKill(victim);
        }

        public void OnOwnerDeath()
        {
            abilityEffect?.OnDeath();
        }

        // Status effect helpers

        public void ApplyStatusEffect(StatusEffect effect)
        {
            statusEffectManager.ApplyStatusEffect(effect);
        }

        public bool IsStunned()
        {
            return statusEffectManager.IsStunned();
        }

        public bool IsRooted()
        {
            return statusEffectManager.IsRooted();
        }

        private void OnDestroy()
        {
            abilityEffect?.Cleanup();
        }
    }
}
