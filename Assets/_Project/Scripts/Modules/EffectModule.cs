using UnityEngine;

namespace AdaptiveDraftArena.Modules
{
    public enum ElementType
    {
        Fire,
        Water,
        Nature
    }

    [CreateAssetMenu(fileName = "Effect_", menuName = "AdaptiveDraftArena/Modules/Effect")]
    public class EffectModule : ModuleBase
    {
        [Header("Element")]
        public ElementType elementType;
        public Color tintColor = Color.white;

        [Header("Damage Modifiers")]
        public string strongVsElement;
        public string weakVsElement;
        public float advantageMultiplier = 1.5f;
        public float disadvantageMultiplier = 0.75f;

        [Header("Visuals")]
        public GameObject auraPrefab;
    }
}
