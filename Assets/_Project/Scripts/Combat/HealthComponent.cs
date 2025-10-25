using System;
using UnityEngine;
using AdaptiveDraftArena.Abilities;

namespace AdaptiveDraftArena.Combat
{
    public class HealthComponent : MonoBehaviour
    {
        public float MaxHP { get; private set; }
        public float CurrentHP { get; private set; }
        public bool IsAlive => CurrentHP > 0;
        public float HealthPercent => MaxHP > 0 ? CurrentHP / MaxHP : 0f;

        // Events
        public event Action OnDeath;
        public event Action<float, GameObject> OnTakeDamage; // damage amount, attacker
        public event Action<float> OnHeal; // heal amount
        public event Action<float> OnHealthChanged; // new HP value

        private TroopController owner;

        public void Initialize(float maxHP)
        {
            MaxHP = Mathf.Max(1f, maxHP);
            CurrentHP = MaxHP;
            OnHealthChanged?.Invoke(CurrentHP);

            owner = GetComponent<TroopController>();
        }

        public void TakeDamage(float damage, GameObject attacker = null)
        {
            if (!IsAlive || damage <= 0) return;

            // Allow abilities to modify incoming damage (Dodge, LastStand, etc.)
            if (owner != null && owner.AbilityExecutor != null && attacker != null)
            {
                var attackerTroop = attacker.GetComponent<TroopController>();
                if (attackerTroop != null)
                {
                    damage = owner.AbilityExecutor.ModifyIncomingDamage(damage, attackerTroop);
                }
            }

            var actualDamage = Mathf.Min(damage, CurrentHP);
            CurrentHP -= actualDamage;

            OnTakeDamage?.Invoke(actualDamage, attacker);
            OnHealthChanged?.Invoke(CurrentHP);

            // Notify ability system
            if (owner != null && owner.AbilityExecutor != null && attacker != null)
            {
                var attackerTroop = attacker.GetComponent<TroopController>();
                if (attackerTroop != null)
                {
                    owner.AbilityExecutor.OnDamageTaken(actualDamage, attackerTroop);
                }
            }

            if (CurrentHP <= 0)
            {
                CurrentHP = 0;
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0) return;

            var actualHeal = Mathf.Min(amount, MaxHP - CurrentHP);
            CurrentHP += actualHeal;

            OnHeal?.Invoke(actualHeal);
            OnHealthChanged?.Invoke(CurrentHP);
        }

        public void SetHP(float newHP)
        {
            CurrentHP = Mathf.Clamp(newHP, 0, MaxHP);
            OnHealthChanged?.Invoke(CurrentHP);

            if (CurrentHP <= 0 && IsAlive)
            {
                Die();
            }
        }

        public void ModifyMaxHP(float newMaxHP)
        {
            MaxHP = Mathf.Max(1f, newMaxHP);
            CurrentHP = Mathf.Min(CurrentHP, MaxHP);
            OnHealthChanged?.Invoke(CurrentHP);
        }

        private void Die()
        {
            OnDeath?.Invoke();
        }
    }
}
