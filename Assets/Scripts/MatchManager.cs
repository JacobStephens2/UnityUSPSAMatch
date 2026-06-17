using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Shooter
{
    /// <summary>
    /// Runs one USPSA-style stage: MAKE READY → STANDBY → buzzer, a running
    /// timer while you engage every target, then a hit-factor result.
    /// Comstock scoring, Minor power factor (A=5, C=3, D=1; steel=5;
    /// miss = no-shoot = -10).
    /// </summary>
    public class MatchManager : MonoBehaviour
    {
        public static MatchManager Instance { get; private set; }

        enum State { Pre, Running, Complete }

        [Header("Refs")]
        public PlayerController player;
        public Gun gun;

        [Header("HUD")]
        public Text timeText;
        public Text ammoText;
        public Text statusText;
        public Text remainingText;
        public GameObject resultsPanel;
        public Text resultsText;

        State _state = State.Pre;
        float _startTime, _finalTime;
        PaperTarget[] _papers;
        SteelTarget[] _steels;
        int _scoringPaperCount;

        AudioSource _audio;
        AudioSource _music;

        void Awake() => Instance = this;

        void Start()
        {
            _papers = Object.FindObjectsByType<PaperTarget>(FindObjectsSortMode.None);
            _steels = Object.FindObjectsByType<SteelTarget>(FindObjectsSortMode.None);
            _scoringPaperCount = 0;
            foreach (var p in _papers) if (!p.isNoShoot) _scoringPaperCount++;

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;

            _music = gameObject.AddComponent<AudioSource>();
            _music.clip = ProcAudio.Music;
            _music.loop = true;
            _music.volume = 0.20f;
            _music.spatialBlend = 0f;
            _music.Play();

            if (resultsPanel != null) resultsPanel.SetActive(false);
            if (player != null) player.Freeze(true);
            if (gun != null) gun.Active = false;

            StartCoroutine(StartSequence());
        }

        IEnumerator StartSequence()
        {
            SetStatus("MAKE READY");
            yield return new WaitForSeconds(1.4f);
            SetStatus("STANDBY");
            yield return new WaitForSeconds(Random.Range(1.2f, 2.8f));

            _audio.PlayOneShot(ProcAudio.Buzzer);
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
            if (_state == State.Running)
            {
                float elapsed = Time.time - _startTime;
                if (timeText != null) timeText.text = elapsed.ToString("0.00");
                if (ammoText != null)
                    ammoText.text = gun != null && gun.Reloading ? "RELOAD"
                                  : (gun != null ? gun.Ammo + "/" + gun.MagCapacity : "");
                if (remainingText != null) remainingText.text = "TARGETS  " + RemainingCount();

                if (AllNeutralized()) Complete();
            }
        }

        int RemainingCount()
        {
            int n = 0;
            foreach (var p in _papers) if (!p.isNoShoot && !p.Neutralized) n++;
            foreach (var s in _steels) if (!s.Down) n++;
            return n;
        }

        bool AllNeutralized()
        {
            foreach (var p in _papers) if (!p.isNoShoot && !p.Neutralized) return false;
            foreach (var s in _steels) if (!s.Down) return false;
            return true;
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

            int penalties = (misses + noShoot) * 10;
            int rawPoints = scoringPts + steelPts;
            int points = rawPoints - penalties;
            float hf = _finalTime > 0f ? Mathf.Max(0, points) / _finalTime : 0f;

            SetStatus("");
            if (timeText != null) timeText.text = _finalTime.ToString("0.00");
            if (resultsText != null)
            {
                resultsText.text =
                    "TIME   " + _finalTime.ToString("0.00") + " s\n" +
                    "POINTS   " + points + "  (" + rawPoints + " - " + penalties + ")\n" +
                    "A " + a + "    C " + c + "    D " + d + "    steel " + steelDown + "\n" +
                    "misses " + misses + "    no-shoots " + noShoot + "\n" +
                    "shots fired   " + (gun != null ? gun.ShotsFired : 0) + "\n\n" +
                    "HIT FACTOR   " + hf.ToString("0.0000");
            }
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

        /// <summary>Hooked to the Restart button.</summary>
        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
