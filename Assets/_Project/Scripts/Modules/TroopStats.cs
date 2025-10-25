namespace AdaptiveDraftArena.Modules
{
    public static class TroopStats
    {
        // Amount multiplier mappings
        private static readonly float[] StatMultipliers = { 1.0f, 0.8f, 0.6f, 0.4f };
        private static readonly float[] AbilityMultipliers = { 1.0f, 1.0f, 0.5f, 0.0f };

        public static float GetStatMultiplier(int amount)
        {
            return amount switch
            {
                1 => StatMultipliers[0],
                2 => StatMultipliers[1],
                3 => StatMultipliers[2],
                5 => StatMultipliers[3],
                _ => 1.0f
            };
        }

        public static float GetAbilityMultiplier(int amount)
        {
            return amount switch
            {
                1 => AbilityMultipliers[0],
                2 => AbilityMultipliers[1],
                3 => AbilityMultipliers[2],
                5 => AbilityMultipliers[3],
                _ => 1.0f
            };
        }

        public static float CalculateElementMultiplier(EffectModule attacker, EffectModule defender)
        {
            if (attacker.strongVsElement == defender.moduleId)
                return attacker.advantageMultiplier;
            if (attacker.weakVsElement == defender.moduleId)
                return attacker.disadvantageMultiplier;
            return 1.0f;
        }
    }
}
