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

        // Cached parameters dictionary to prevent GC allocations
        private Dictionary<string, float> cachedParameters;

        public IReadOnlyDictionary<string, float> GetParameters()
        {
            if (cachedParameters == null)
            {
                cachedParameters = new Dictionary<string, float>();
                foreach (var param in parametersList)
                {
                    cachedParameters[param.key] = param.value;
                }
            }
            return cachedParameters;
        }

        // Provide direct access to parameters list for iteration without dictionary lookup
        public IReadOnlyList<AbilityParameter> Parameters => parametersList;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Invalidate cache when parameters change in editor
            cachedParameters = null;
        }
#endif
    }

    [System.Serializable]
    public class AbilityParameter
    {
        public string key;
        public float value;
    }
}
