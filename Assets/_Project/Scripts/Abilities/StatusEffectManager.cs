using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AdaptiveDraftArena.Combat;

namespace AdaptiveDraftArena.Abilities
{
    public class StatusEffectManager : MonoBehaviour
    {
        private List<StatusEffect> activeEffects = new List<StatusEffect>();
        private TroopController owner;

        public void Initialize(TroopController troopController)
        {
            owner = troopController;
        }

        private void Update()
        {
            // Update all active effects
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                activeEffects[i].Update(Time.deltaTime);

                if (activeEffects[i].IsExpired)
                {
                    OnEffectRemoved(activeEffects[i]);
                    activeEffects.RemoveAt(i);
                }
            }
        }

        public void ApplyStatusEffect(StatusEffect effect)
        {
            // Check if effect already exists
            var existing = activeEffects.Find(e => e.Type == effect.Type && e.Source == effect.Source);

            if (existing != null)
            {
                // Refresh duration or stack
                existing.Duration = Mathf.Max(existing.Duration, effect.Duration);
                existing.Value = Mathf.Max(existing.Value, effect.Value);
            }
            else
            {
                activeEffects.Add(effect);
                OnEffectApplied(effect);
            }
        }

        public bool HasEffect(StatusType type)
        {
            return activeEffects.Any(e => e.Type == type);
        }

        public float GetMovementSpeedMultiplier()
        {
            var multiplier = 1f;

            foreach (var effect in activeEffects)
            {
                if (effect.Type == StatusType.Slow)
                {
                    multiplier *= (1f - effect.Value); // e.g., 0.7 for 30% slow
                }
                else if (effect.Type == StatusType.SpeedBuff)
                {
                    multiplier *= (1f + effect.Value); // e.g., 1.5 for 50% boost
                }
            }

            return Mathf.Max(0.1f, multiplier); // Minimum 10% speed
        }

        public bool IsStunned()
        {
            return HasEffect(StatusType.Stun);
        }

        public bool IsRooted()
        {
            return HasEffect(StatusType.Root);
        }

        private void OnEffectApplied(StatusEffect effect)
        {
            // Update movement speed if slow/speed buff
            if (effect.Type == StatusType.Slow || effect.Type == StatusType.SpeedBuff)
            {
                owner.Movement.SetSpeedModifier(GetMovementSpeedMultiplier());
            }

            // TODO: Show visual indicator (icon above troop)
            Debug.Log($"{owner.name} gained {effect.Type} for {effect.Duration}s");
        }

        private void OnEffectRemoved(StatusEffect effect)
        {
            // Update movement speed when slow/speed buff expires
            if (effect.Type == StatusType.Slow || effect.Type == StatusType.SpeedBuff)
            {
                owner.Movement.SetSpeedModifier(GetMovementSpeedMultiplier());
            }

            Debug.Log($"{owner.name} lost {effect.Type}");
        }

        public void ClearAllEffects()
        {
            activeEffects.Clear();
            owner.Movement.SetSpeedModifier(1f);
        }
    }
}
