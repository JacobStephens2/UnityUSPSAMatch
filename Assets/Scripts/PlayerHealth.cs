using UnityEngine;

namespace Shooter
{
    /// <summary>
    /// Player hit points. Only matters on stages with enemies that shoot back
    /// (Stage 2). Reaching zero ends the stage as a failure.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        public int maxHealth = 100;

        public int Health { get; private set; }
        public bool IsDead => Health <= 0;
        public float Fraction => maxHealth > 0 ? (float)Health / maxHealth : 0f;

        void Awake() => Health = maxHealth;

        public void Damage(int amount)
        {
            if (IsDead) return;
            Health = Mathf.Max(0, Health - amount);
            if (MatchManager.Instance != null) MatchManager.Instance.OnPlayerHit();
            if (Health <= 0 && MatchManager.Instance != null) MatchManager.Instance.OnPlayerDead();
        }
    }
}
