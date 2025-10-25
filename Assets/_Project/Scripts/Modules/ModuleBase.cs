using UnityEngine;

namespace AdaptiveDraftArena.Modules
{
    public abstract class ModuleBase : ScriptableObject
    {
        [Header("Identification")]
        public string moduleId;
        public string displayName;

        [Header("Visuals")]
        public Sprite icon;

        [Header("Description")]
        [TextArea(3, 6)]
        public string description;
    }
}
