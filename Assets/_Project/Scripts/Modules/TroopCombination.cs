using UnityEngine;

namespace AdaptiveDraftArena.Modules
{
    [CreateAssetMenu(fileName = "Combo_", menuName = "AdaptiveDraftArena/Modules/TroopCombination")]
    public class TroopCombination : ScriptableObject, ICombination
    {
        [Header("Module Composition")]
        [SerializeField] private BodyModule _body;
        [SerializeField] private WeaponModule _weapon;
        [SerializeField] private AbilityModule _ability;
        [SerializeField] private EffectModule _effect;

        [Header("Amount")]
        [Tooltip("Number of troops to spawn (1, 2, 3, or 5)")]
        [SerializeField] private int _amount = 1;

        [Header("Metadata")]
        [SerializeField] private bool _isAIGenerated;
        [SerializeField] private int _generationRound;

        [TextArea(2, 4)]
        public string counterReasoning;

        // Interface properties
        public BodyModule body => _body;
        public WeaponModule weapon => _weapon;
        public AbilityModule ability => _ability;
        public EffectModule effect => _effect;
        public int amount => _amount;
        public bool isAIGenerated => _isAIGenerated;

        // Computed display name (null-safe)
        public string DisplayName =>
            $"{(effect != null ? effect.displayName : "Unknown")} " +
            $"{(body != null ? body.displayName : "Unknown")} ×{amount}";

        // Validate that all modules are assigned
        public bool IsValid()
        {
            return body != null && weapon != null && ability != null && effect != null &&
                   (amount == 1 || amount == 2 || amount == 3 || amount == 5);
        }

        // Calculate final stats with amount multiplier applied (null-safe)
        public float GetFinalHP()
        {
            if (body == null) return 0f;
            return body.baseHP * TroopStats.GetStatMultiplier(amount);
        }

        public float GetFinalDamage()
        {
            if (weapon == null) return 0f;
            return weapon.baseDamage * TroopStats.GetStatMultiplier(amount);
        }

        public float GetFinalSpeed()
        {
            if (body == null) return 0f;
            return body.movementSpeed;
        }

        public float GetAbilityEffectiveness()
        {
            return TroopStats.GetAbilityMultiplier(amount);
        }
    }
}
