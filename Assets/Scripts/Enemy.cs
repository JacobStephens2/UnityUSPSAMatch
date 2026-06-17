using System;
using UnityEngine;

namespace Shooter
{
    /// <summary>
    /// Walks toward the player across the flat arena and deals contact damage
    /// in intervals. Dies when its <see cref="Health"/> is depleted, awarding
    /// score and notifying the spawner.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        public float moveSpeed = 2.6f;
        public float attackRange = 1.6f;
        public float attackDamage = 8f;
        public float attackInterval = 1.2f;
        public int scoreValue = 10;

        [HideInInspector] public Transform target;
        public event Action<Enemy> Removed;

        Health _health;
        Health _targetHealth;
        Renderer _renderer;
        MaterialPropertyBlock _mpb;
        Color _baseColor = new Color(0.85f, 0.18f, 0.18f);
        float _flashUntil;
        float _nextAttack;
        float _groundY;

        void Awake()
        {
            _health = GetComponent<Health>();
            _renderer = GetComponentInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
            _health.Died += OnDied;
            _health.Changed += _ => Flash();
        }

        void Start()
        {
            _groundY = transform.position.y;
            if (target == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) target = p.transform;
            }
            if (target != null) _targetHealth = target.GetComponent<Health>();
            ApplyColor(_baseColor);
        }

        void Update()
        {
            if (target == null || _health.IsDead) return;

            Vector3 to = target.position - transform.position;
            to.y = 0f;
            float dist = to.magnitude;

            if (dist > attackRange)
            {
                Vector3 step = to.normalized * moveSpeed * Time.deltaTime;
                Vector3 pos = transform.position + step;
                pos.y = _groundY;                       // stay on the ground plane
                transform.position = pos;
                if (to.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
            }
            else if (Time.time >= _nextAttack && _targetHealth != null && !_targetHealth.IsDead)
            {
                _nextAttack = Time.time + attackInterval;
                _targetHealth.TakeDamage(attackDamage);
            }

            if (_flashUntil > 0f && Time.time >= _flashUntil)
            {
                _flashUntil = 0f;
                ApplyColor(_baseColor);
            }
        }

        void Flash()
        {
            _flashUntil = Time.time + 0.06f;
            ApplyColor(Color.white);
        }

        void ApplyColor(Color c)
        {
            if (_renderer == null) return;
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor("_Color", c);          // Built-in Standard
            _mpb.SetColor("_BaseColor", c);      // URP fallback
            _renderer.SetPropertyBlock(_mpb);
        }

        void OnDied()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.AddKill(scoreValue);
            Removed?.Invoke(this);
            DeathPuff();
            Destroy(gameObject);
        }

        void DeathPuff()
        {
            // Cheap, asset-free death flourish: a sphere that scales down and fades.
            var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.transform.position = transform.position + Vector3.up * 0.5f;
            puff.transform.localScale = Vector3.one * 1.2f;
            var col = puff.GetComponent<Collider>();
            if (col != null) Destroy(col);
            var r = puff.GetComponent<Renderer>();
            if (r != null)
            {
                var b = new MaterialPropertyBlock();
                b.SetColor("_Color", new Color(1f, 0.7f, 0.2f));
                b.SetColor("_BaseColor", new Color(1f, 0.7f, 0.2f));
                r.SetPropertyBlock(b);
            }
            puff.AddComponent<DeathPuff>();
        }

        void OnDestroy()
        {
            if (_health != null) _health.Died -= OnDied;
        }
    }

    /// <summary>Tiny self-contained shrink-and-die effect for the death puff.</summary>
    public class DeathPuff : MonoBehaviour
    {
        float _life = 0.35f;
        float _t;
        Vector3 _start;

        void Start() => _start = transform.localScale;

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / _life);
            transform.localScale = Vector3.Lerp(_start, Vector3.zero, k);
            if (_t >= _life) Destroy(gameObject);
        }
    }
}
