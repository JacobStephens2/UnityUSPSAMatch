using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Shooter
{
    /// <summary>
    /// Owns the run state: score, the player's health readout, and the
    /// game-over / restart flow. Wired up by the editor SceneBuilder.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Refs")]
        public PlayerController player;
        public Health playerHealth;

        [Header("HUD")]
        public Text healthText;
        public Text scoreText;
        public GameObject gameOverPanel;
        public Text finalScoreText;

        int _score;
        int _kills;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            if (player == null) player = FindAnyObjectByType<PlayerController>();
            if (playerHealth == null && player != null) playerHealth = player.GetComponent<Health>();

            if (playerHealth != null)
            {
                playerHealth.Changed += OnPlayerHealthChanged;
                playerHealth.Died += OnPlayerDied;
                OnPlayerHealthChanged(playerHealth.CurrentHealth);
            }
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            UpdateScore();
        }

        public void AddKill(int value)
        {
            _kills++;
            _score += value;
            UpdateScore();
        }

        void OnPlayerHealthChanged(float hp)
        {
            if (healthText != null) healthText.text = "HP " + Mathf.CeilToInt(hp);
        }

        void UpdateScore()
        {
            if (scoreText != null) scoreText.text = "SCORE " + _score;
        }

        void OnPlayerDied()
        {
            if (player != null) player.Freeze(true);
            if (finalScoreText != null)
                finalScoreText.text = "Score " + _score + "   Kills " + _kills;
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
        }

        /// <summary>Hooked to the Restart button.</summary>
        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.Changed -= OnPlayerHealthChanged;
                playerHealth.Died -= OnPlayerDied;
            }
        }
    }
}
