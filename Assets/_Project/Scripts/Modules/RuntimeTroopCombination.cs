namespace AdaptiveDraftArena.Modules
{
    /// <summary>
    /// Runtime-only troop combination created by AI.
    /// Unlike TroopCombination (ScriptableObject), this is a plain C# class that is GC-friendly.
    /// References existing ScriptableObject modules rather than duplicating them.
    /// </summary>
    public class RuntimeTroopCombination : ICombination
    {
        // Module composition (references to existing ScriptableObject modules)
        public BodyModule body { get; set; }
        public WeaponModule weapon { get; set; }
        public AbilityModule ability { get; set; }
        public EffectModule effect { get; set; }

        // Amount
        public int amount { get; set; }

        // Metadata
        public bool isAIGenerated { get; set; }
        public int generationRound { get; set; }
        public string counterReasoning { get; set; }

        // Computed display name (null-safe)
        public string DisplayName =>
            $"{(effect != null ? effect.displayName : "Unknown")} " +
            $"{(body != null ? body.displayName : "Unknown")} ×{amount}";

        // Default constructor
        public RuntimeTroopCombination()
        {
            isAIGenerated = true;
            amount = 1;
        }

        // Validation
        public bool IsValid()
        {
            return body != null && weapon != null && ability != null && effect != null &&
                   (amount == 1 || amount == 2 || amount == 3 || amount == 5);
        }

        // Stat calculations (same as TroopCombination)
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
