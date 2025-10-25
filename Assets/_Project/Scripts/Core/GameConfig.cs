using UnityEngine;

namespace AdaptiveDraftArena.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "AdaptiveDraftArena/Config/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Match Settings")]
        public int maxRounds = 7;
        public int winsRequired = 4;

        [Header("Draft Settings")]
        public float draftDuration = 15f;
        public int draftOptionsCount = 3;

        [Header("Battle Settings")]
        public float battleDuration = 30f;
        public int maxTroopsPerSide = 4;
        public Vector2 battlefieldSize = new Vector2(20f, 12f);

        [Header("Spawn Zones")]
        public Rect playerSpawnZone = new Rect(2f, 3f, 3f, 6f);
        public Rect aiSpawnZone = new Rect(15f, 3f, 3f, 6f);

        [Header("Amount Multipliers")]
        public float[] statMultipliers = { 1.0f, 0.8f, 0.6f, 0.4f };
        public float[] abilityMultipliers = { 1.0f, 1.0f, 0.5f, 0.0f };

        [Header("Element Modifiers")]
        public float advantageMultiplier = 1.5f;
        public float disadvantageMultiplier = 0.75f;

        [Header("AI Settings")]
        public float aiGenerationTimeout = 10f;
        public bool useMockAI = false;

        [Header("Visuals")]
        public float hitFlashDuration = 0.1f;
        public float deathFadeDuration = 0.5f;
        public float screenShakeIntensity = 0.2f;
    }
}
