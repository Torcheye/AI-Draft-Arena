using UnityEngine;
using AdaptiveDraftArena.Combat;

namespace AdaptiveDraftArena.Abilities
{
    public enum StatusType
    {
        Slow,
        Stun,
        Root,
        Poison,
        SpeedBuff,
        DamageBuff,
        Shield
    }

    public class StatusEffect
    {
        public StatusType Type { get; set; }
        public float Duration { get; set; }
        public float Value { get; set; }
        public TroopController Source { get; set; }

        public bool IsExpired => Duration <= 0;

        public StatusEffect(StatusType type, float duration, float value, TroopController source = null)
        {
            Type = type;
            Duration = duration;
            Value = value;
            Source = source;
        }

        public void Update(float deltaTime)
        {
            Duration -= deltaTime;
        }
    }
}
