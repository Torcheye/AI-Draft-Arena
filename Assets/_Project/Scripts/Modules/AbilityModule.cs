using System.Collections.Generic;
using UnityEngine;

namespace AdaptiveDraftArena.Modules
{
    public enum AbilityCategory
    {
        Offensive,
        Defensive,
        Utility,
        Control
    }

    public enum AbilityTrigger
    {
        Passive,
        OnHit,
        OnTakeDamage,
        Conditional
    }

    [CreateAssetMenu(fileName = "Ability_", menuName = "AdaptiveDraftArena/Modules/Ability")]
    public class AbilityModule : ModuleBase
    {
        [Header("Classification")]
        public AbilityCategory category;
        public AbilityTrigger trigger;

        [Header("Implementation")]
        public string abilityClassName;

        [Header("Parameters")]
        [SerializeField] private List<AbilityParameter> parametersList = new List<AbilityParameter>();

        [Header("Visuals")]
        public GameObject vfxPrefab;
        public Sprite statusIcon;

        public Dictionary<string, float> GetParameters()
        {
            var dict = new Dictionary<string, float>();
            foreach (var param in parametersList)
            {
                dict[param.key] = param.value;
            }
            return dict;
        }
    }

    [System.Serializable]
    public class AbilityParameter
    {
        public string key;
        public float value;
    }
}
