namespace AdaptiveDraftArena.Modules
{
    /// <summary>
    /// Common interface for both static (ScriptableObject) and runtime (AI-generated) combos.
    /// Enables polymorphism between TroopCombination and RuntimeTroopCombination.
    /// </summary>
    public interface ICombination
    {
        // Module composition
        BodyModule body { get; }
        WeaponModule weapon { get; }
        AbilityModule ability { get; }
        EffectModule effect { get; }

        // Amount
        int amount { get; }

        // Metadata
        bool isAIGenerated { get; }
        string DisplayName { get; }

        // Validation
        bool IsValid();

        // Stat calculations
        float GetFinalHP();
        float GetFinalDamage();
        float GetFinalSpeed();
        float GetAbilityEffectiveness();
    }
}
