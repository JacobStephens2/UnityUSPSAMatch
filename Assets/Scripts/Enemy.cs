using UnityEngine;

namespace Shooter
{
    /// <summary>
    /// A wild-west outlaw (built from primitives by the SceneBuilder): strafes
    /// back and forth as a moving target, turns to face the player, and fires
    /// his revolver back when he has a clear line of sight. Takes several hits
    /// to put down; topples over when killed and stops being a threat.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        [Header("Health")]
        public int maxHits = 5;

        [Header("Movement")]
        public float patrolHalfWidth = 4f;
        public float moveSpeed = 2.4f;

        [Header("Fire")]
        public float minInterval = 1.6f;
        public float maxInterval = 2.9f;
        [Range(0f, 1f)] public float hitChance = 0.5f;
        public int damage = 16;

        [Header("Refs (assigned by SceneBuilder)")]
        public Transform muzzle;
        public Light muzzleFlash;
        public LineRenderer tracer;
        public Renderer[] tintRenderers;

        public bool Dead { get; private set; }

        Transform _player;
        PlayerHealth _playerHealth;
        float _centerX;
        float _offset;
        float _dir = 1f;
        float _pauseUntil;
        float _nextFire;
        float _flashOff, _tracerOff, _flinchOff;
        int _hits;
        AudioSource _sfx;
        Color[] _baseColors;

        // Topple-on-death
        float _toppleT = -1f;
        Quaternion _toppleStart, _toppleEnd;

        void Start()
        {
            _centerX = transform.position.x;
            var p = GameObject.FindWithTag("Player");
            if (p != null)
            {
                _player = p.transform;
                _playerHealth = p.GetComponent<PlayerHealth>();
            }
            _nextFire = Time.time + Random.Range(minInterval, maxInterval) + 1.0f;
            _dir = Random.value < 0.5f ? -1f : 1f;

            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _sfx.spatialBlend = 0.55f;
            _sfx.maxDistance = 70f;
            _sfx.rolloffMode = AudioRolloffMode.Linear;

            if (muzzleFlash != null) muzzleFlash.enabled = false;
            if (tracer != null) tracer.enabled = false;

            if (tintRenderers != null)
            {
                _baseColors = new Color[tintRenderers.Length];
                for (int i = 0; i < tintRenderers.Length; i++)
                    if (tintRenderers[i] != null) _baseColors[i] = tintRenderers[i].material.color;
            }
        }

        void Update()
        {
            if (muzzleFlash != null && muzzleFlash.enabled && Time.time >= _flashOff) muzzleFlash.enabled = false;
            if (tracer != null && tracer.enabled && Time.time >= _tracerOff) tracer.enabled = false;
            if (_flinchOff > 0f && Time.time >= _flinchOff) { RestoreTint(); _flinchOff = -1f; }

            if (Dead) { Topple(); return; }

            // Hold until the buzzer (GO): the outlaw stands and faces you, but
            // can't move or shoot during MAKE READY / STANDBY.
            if (MatchManager.Instance != null && !MatchManager.Instance.IsRunning)
            {
                FacePlayer();
                _nextFire = Time.time + Random.Range(0.8f, 1.6f); // delay first shot past GO
                return;
            }

            FacePlayer();
            Patrol();

            if (_player != null && Time.time >= _nextFire)
            {
                if (HasLineOfSight()) Fire();
                else _nextFire = Time.time + 0.35f; // hold fire behind cover, re-check soon
            }
        }

        void FacePlayer()
        {
            if (_player == null) return;
            Vector3 to = _player.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.01f) return;
            var want = Quaternion.LookRotation(to);
            transform.rotation = Quaternion.Slerp(transform.rotation, want, 7f * Time.deltaTime);
        }

        void Patrol()
        {
            if (Time.time < _pauseUntil) return;
            _offset += _dir * moveSpeed * Time.deltaTime;
            if (Mathf.Abs(_offset) >= patrolHalfWidth)
            {
                _offset = Mathf.Clamp(_offset, -patrolHalfWidth, patrolHalfWidth);
                _dir = -_dir;
            }
            else if (Random.value < 0.004f) // occasional juke / pause
            {
                if (Random.value < 0.5f) _dir = -_dir;
                else _pauseUntil = Time.time + Random.Range(0.3f, 0.8f);
            }
            var pos = transform.position;
            pos.x = _centerX + _offset;
            transform.position = pos;
        }

        bool HasLineOfSight()
        {
            if (_player == null || muzzle == null) return false;
            Vector3 from = muzzle.position;
            Vector3 target = _player.position + Vector3.up * 0.5f;
            Vector3 dir = target - from;
            float dist = dir.magnitude;
            dir /= dist;
            // First thing the ray reaches: clear shot only if it's the player.
            if (Physics.Raycast(from + dir * 0.3f, dir, out RaycastHit hit, dist,
                                ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.collider.GetComponentInParent<PlayerHealth>() != null
                    || hit.collider.GetComponentInParent<PlayerController>() != null;
            }
            return true;
        }

        void Fire()
        {
            _nextFire = Time.time + Random.Range(minInterval, maxInterval);

            Vector3 from = muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.3f;
            Vector3 to = _player != null ? _player.position + Vector3.up * 0.5f : from + transform.forward * 10f;

            if (muzzleFlash != null) { muzzleFlash.enabled = true; _flashOff = Time.time + 0.05f; }
            if (tracer != null)
            {
                tracer.positionCount = 2;
                tracer.SetPosition(0, from);
                tracer.SetPosition(1, to);
                tracer.enabled = true;
                _tracerOff = Time.time + 0.05f;
            }
            if (_sfx != null)
            {
                _sfx.pitch = Random.Range(0.78f, 0.9f); // deeper revolver
                _sfx.PlayOneShot(ProcAudio.Gunshot, 0.7f);
            }

            // Harder to hit while moving.
            bool moving = Time.time >= _pauseUntil;
            float acc = hitChance * (moving ? 0.72f : 1f);
            if (_playerHealth != null && !_playerHealth.IsDead && Random.value < acc)
                _playerHealth.Damage(damage);
        }

        /// <summary>Called by the player's gun when a shot connects.</summary>
        public void TakeHit(Vector3 point)
        {
            if (Dead) return;
            _hits++;
            SetTint(new Color(0.95f, 0.2f, 0.15f));
            _flinchOff = Time.time + 0.08f;
            if (_sfx != null)
            {
                _sfx.pitch = Random.Range(0.95f, 1.1f);
                _sfx.PlayOneShot(ProcAudio.PaperHit, 0.7f);
            }
            if (_hits >= maxHits) Die();
        }

        void Die()
        {
            Dead = true;
            if (muzzleFlash != null) muzzleFlash.enabled = false;
            if (tracer != null) tracer.enabled = false;
            _toppleStart = transform.rotation;
            _toppleEnd = transform.rotation * Quaternion.AngleAxis(-82f, Vector3.right); // fall backward
            _toppleT = 0f;
            if (_sfx != null) { _sfx.pitch = 1f; _sfx.PlayOneShot(ProcAudio.EnemyDown, 0.9f); }
        }

        void Topple()
        {
            if (_toppleT < 0f) return;
            _toppleT += Time.deltaTime;
            float k = Mathf.Clamp01(_toppleT / 0.45f);
            transform.rotation = Quaternion.Slerp(_toppleStart, _toppleEnd, k);
            if (k >= 1f) _toppleT = -1f;
        }

        void SetTint(Color c)
        {
            if (tintRenderers == null) return;
            foreach (var r in tintRenderers) if (r != null) r.material.color = c;
        }

        void RestoreTint()
        {
            if (tintRenderers == null || _baseColors == null) return;
            for (int i = 0; i < tintRenderers.Length; i++)
                if (tintRenderers[i] != null) tintRenderers[i].material.color = _baseColors[i];
        }
    }
}
