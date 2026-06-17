using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Shooter
{
    /// <summary>
    /// Runs one USPSA-style stage: MAKE READY → STANDBY → buzzer, a running
    /// timer while you engage every target, then a hit-factor result. Stage 2
    /// adds live outlaws that shoot back, so the player can also be downed.
    /// Comstock scoring, Minor power factor (A=5, C=3, D=1; steel=5;
    /// miss = no-shoot = -10).
    /// </summary>
    public class MatchManager : MonoBehaviour
    {
        public static MatchManager Instance { get; private set; }

        enum State { Pre, Running, Complete }

        [Header("Stage")]
        public int stageNumber = 1;
        public string nextScene = "";

        [Header("Refs")]
        public PlayerController player;
        public Gun gun;
        public PlayerHealth playerHealth;

        [Header("HUD")]
        public Text timeText;
        public Text ammoText;
        public Text statusText;
        public Text remainingText;
        public Text healthText;
        public Image healthFill;
        public Image damageFlash;
        public GameObject resultsPanel;
        public Text resultsTitle;
        public Text resultsText;
        public Text resultsButtonText;

        State _state = State.Pre;
        bool _failed;

        /// <summary>True only between the buzzer and stage end (enemies hold fire until then).</summary>
        public bool IsRunning => _state == State.Running;
        float _startTime, _finalTime;
        PaperTarget[] _papers;
        SteelTarget[] _steels;
        Enemy[] _enemies;
        float _flashAlpha;

        AudioSource _audio;
        AudioSource _music;

        void Awake() => Instance = this;

        void Start()
        {
            _papers = Object.FindObjectsByType<PaperTarget>(FindObjectsSortMode.None);
            _steels = Object.FindObjectsByType<SteelTarget>(FindObjectsSortMode.None);
            _enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;

            _music = gameObject.AddComponent<AudioSource>();
            _music.clip = ProcAudio.RandomMusic; // coin-flip: spaghetti-western or blues boogie
            _music.loop = true;
            _music.volume = 0.28f;
            _music.spatialBlend = 0f;
            _music.Play();

            if (resultsPanel != null) resultsPanel.SetActive(false);
            if (player != null) player.Freeze(true);
            if (gun != null) gun.Active = false;

            bool showHealth = _enemies.Length > 0;
            if (healthText != null) healthText.gameObject.SetActive(showHealth);
            if (healthFill != null) healthFill.transform.parent.gameObject.SetActive(showHealth);
            if (damageFlash != null)
            {
                var c = damageFlash.color; c.a = 0f; damageFlash.color = c;
            }

            StartCoroutine(StartSequence());
        }

        IEnumerator StartSequence()
        {
            SetStatus(stageNumber >= 2 ? "STAGE 2  —  MAKE READY" : "MAKE READY");
            yield return new WaitForSeconds(1.4f);
            SetStatus("STANDBY");
            yield return new WaitForSeconds(Random.Range(1.2f, 2.8f));

            _audio.PlayOneShot(ProcAudio.Buzzer, 0.55f);
            _startTime = Time.time;
            _state = State.Running;
            if (gun != null) gun.Active = true;
            if (player != null) player.Freeze(false);
            SetStatus("GO!");
            yield return new WaitForSeconds(0.8f);
            SetStatus("");
        }

        void Update()
        {
            // Damage vignette fade (always, so it clears after the stage too).
            if (_flashAlpha > 0f)
            {
                _flashAlpha = Mathf.Max(0f, _flashAlpha - Time.deltaTime * 1.6f);
                if (damageFlash != null)
                {
                    var c = damageFlash.color; c.a = _flashAlpha; damageFlash.color = c;
                }
            }

            if (_state != State.Running) return;

            float elapsed = Time.time - _startTime;
            if (timeText != null) timeText.text = elapsed.ToString("0.00");
            if (ammoText != null)
                ammoText.text = gun != null && gun.Reloading ? "RELOAD"
                              : (gun != null ? gun.Ammo + "/" + gun.MagCapacity : "");
            if (remainingText != null) remainingText.text = "TARGETS  " + RemainingCount();
            UpdateHealthHud();

            if (AllNeutralized()) Complete();
        }

        void UpdateHealthHud()
        {
            if (playerHealth == null) return;
            float f = playerHealth.Fraction;
            if (healthFill != null)
            {
                // Fill is a left-anchored child of the bar; stretch it to f of the width.
                healthFill.rectTransform.anchorMax = new Vector2(f, 1f);
                healthFill.color = Color.Lerp(new Color(0.85f, 0.2f, 0.16f), new Color(0.3f, 0.8f, 0.35f), f);
            }
            if (healthText != null) healthText.text = "HP " + playerHealth.Health;
        }

        int RemainingCount()
        {
            int n = 0;
            foreach (var p in _papers) if (!p.isNoShoot && !p.Neutralized) n++;
            foreach (var s in _steels) if (!s.Down) n++;
            foreach (var e in _enemies) if (!e.Dead) n++;
            return n;
        }

        bool AllNeutralized()
        {
            foreach (var p in _papers) if (!p.isNoShoot && !p.Neutralized) return false;
            foreach (var s in _steels) if (!s.Down) return false;
            foreach (var e in _enemies) if (!e.Dead) return false;
            return true;
        }

        public void OnPlayerHit()
        {
            _flashAlpha = 0.55f;
            if (_audio != null) _audio.PlayOneShot(ProcAudio.Hurt, 0.7f);
        }

        public void OnPlayerDead()
        {
            if (_state == State.Complete) return;
            Fail();
        }

        void Fail()
        {
            _state = State.Complete;
            _failed = true;
            _finalTime = Time.time - _startTime;
            if (gun != null) gun.Active = false;
            if (player != null) player.Freeze(true);

            SetStatus("");
            if (resultsTitle != null) { resultsTitle.text = "YOU WERE DOWNED"; resultsTitle.color = new Color(0.9f, 0.3f, 0.25f); }
            if (resultsText != null)
                resultsText.text =
                    "The outlaw got you at " + _finalTime.ToString("0.00") + " s.\n\n" +
                    "Take cover behind the barriers,\nput him down, then clear the stage.";
            if (resultsButtonText != null) resultsButtonText.text = "RETRY";
            if (resultsPanel != null) resultsPanel.SetActive(true);
        }

        void Complete()
        {
            _state = State.Complete;
            _finalTime = Time.time - _startTime;
            if (gun != null) gun.Active = false;
            if (player != null) player.Freeze(true);

            int a = 0, c = 0, d = 0, scoringPts = 0, misses = 0, noShoot = 0;
            foreach (var p in _papers)
            {
                scoringPts += p.ScorePoints();
                misses += p.MissCount;
                noShoot += p.NoShootHits;
                foreach (int v in p.BestTwo())
                {
                    if (v == 5) a++;
                    else if (v == 3) c++;
                    else if (v == 1) d++;
                }
            }
            int steelDown = 0;
            foreach (var s in _steels) if (s.Down) steelDown++;
            int steelPts = steelDown * 5;

            int outlaws = 0;
            foreach (var e in _enemies) if (e.Dead) outlaws++;
            int outlawPts = outlaws * 15; // bonus for downing each outlaw

            int penalties = (misses + noShoot) * 10;
            int rawPoints = scoringPts + steelPts + outlawPts;
            int points = rawPoints - penalties;
            float hf = _finalTime > 0f ? Mathf.Max(0, points) / _finalTime : 0f;

            bool finalStage = stageNumber >= 2;
            SetStatus("");
            if (timeText != null) timeText.text = _finalTime.ToString("0.00");
            if (resultsTitle != null)
            {
                resultsTitle.text = finalStage ? "OUTLAWS DOWN!" : "STAGE COMPLETE";
                resultsTitle.color = new Color(0.4f, 0.9f, 0.5f);
            }
            if (resultsText != null)
            {
                string successLine = finalStage
                    ? "Nice shooting, hombre — the bay's clear and\nthe outlaws are down. You cleared Stage 2!\n\n"
                    : "";
                string outlawLine = _enemies.Length > 0 ? "outlaws " + outlaws + " / " + _enemies.Length + "\n" : "";
                resultsText.text =
                    successLine +
                    "TIME   " + _finalTime.ToString("0.00") + " s\n" +
                    "POINTS   " + points + "  (" + rawPoints + " - " + penalties + ")\n" +
                    "A " + a + "    C " + c + "    D " + d + "    steel " + steelDown + "\n" +
                    outlawLine +
                    "misses " + misses + "    no-shoots " + noShoot + "\n" +
                    "shots fired   " + (gun != null ? gun.ShotsFired : 0) + "\n\n" +
                    "HIT FACTOR   " + hf.ToString("0.0000");
            }
            if (resultsButtonText != null)
                resultsButtonText.text = (stageNumber == 1 && !string.IsNullOrEmpty(nextScene)) ? "NEXT STAGE" : "RUN AGAIN";
            if (resultsPanel != null) resultsPanel.SetActive(true);
        }

        void SetStatus(string s)
        {
            if (statusText != null) statusText.text = s;
        }

        public void PlayDing(Vector3 pos)
        {
            AudioSource.PlayClipAtPoint(ProcAudio.SteelDing, pos, 0.8f);
        }

        /// <summary>Hooked to the results-panel button. Next stage, run again, or retry.</summary>
        public void OnResultsButton()
        {
            Time.timeScale = 1f;
            if (!_failed && stageNumber == 1 && !string.IsNullOrEmpty(nextScene))
                SceneManager.LoadScene(nextScene);
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Back-compat alias.</summary>
        public void Restart() => OnResultsButton();
    }
}
