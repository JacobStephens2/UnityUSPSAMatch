using System;
using UnityEngine;

namespace Shooter
{
    /// <summary>Generic hit-point container used by both the player and enemies.</summary>
    public class Health : MonoBehaviour
    {
        public float maxHealth = 100f;
        public float CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;

        /// <summary>Raised with the new current-health value whenever it changes.</summary>
        public event Action<float> Changed;
        /// <summary>Raised once when health first reaches zero.</summary>
        public event Action Died;

        void Awake()
        {
            CurrentHealth = maxHealth;
        }

        /// <summary>
        /// Sets max and current health together. Use this when configuring a
        /// runtime-added Health, because Awake() already captured CurrentHealth
        /// from the default maxHealth before any field assignment takes effect.
        /// </summary>
        public void Init(float max)
        {
            maxHealth = max;
            CurrentHealth = max;
            Changed?.Invoke(CurrentHealth);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            Changed?.Invoke(CurrentHealth);
            if (CurrentHealth <= 0f) Died?.Invoke();
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            Changed?.Invoke(CurrentHealth);
        }
    }
}
