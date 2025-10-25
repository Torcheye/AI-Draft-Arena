using UnityEngine;

namespace AdaptiveDraftArena.Modules
{
    [CreateAssetMenu(fileName = "Combo_", menuName = "AdaptiveDraftArena/Modules/TroopCombination")]
    public class TroopCombination : ScriptableObject
    {
        [Header("Module Composition")]
        public BodyModule body;
        public WeaponModule weapon;
        public AbilityModule ability;
        public EffectModule effect;

        [Header("Amount")]
        [Tooltip("Number of troops to spawn (1, 2, 3, or 5)")]
        public int amount = 1;

        [Header("Metadata")]
        public bool isAIGenerated;
        public int generationRound;

        [TextArea(2, 4)]
        public string counterReasoning;

        // Computed display name
        public string DisplayName => $"{effect.displayName} {body.displayName} ×{amount}";

        // Validate that all modules are assigned
        public bool IsValid()
        {
            return body != null && weapon != null && ability != null && effect != null &&
                   (amount == 1 || amount == 2 || amount == 3 || amount == 5);
        }

        // Calculate final stats with amount multiplier applied
        public float GetFinalHP()
        {
            return body.baseHP * TroopStats.GetStatMultiplier(amount);
        }

        public float GetFinalDamage()
        {
            return weapon.baseDamage * TroopStats.GetStatMultiplier(amount);
        }

        public float GetFinalSpeed()
        {
            return body.movementSpeed;
        }

        public float GetAbilityEffectiveness()
        {
            return TroopStats.GetAbilityMultiplier(amount);
        }
    }
}
