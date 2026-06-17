using UnityEngine;

namespace Shooter
{
    /// <summary>
    /// Spawns capsule enemies around the player at a cadence that ramps up over
    /// time. Enemies are built from primitives at runtime, so the project needs
    /// no imported prefabs or art.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        public Transform player;
        public float arenaRadius = 22f;

        [Header("Spawning")]
        public float startInterval = 3.0f;
        public float minInterval = 1.0f;
        public float rampSeconds = 120f;      // time to reach minInterval
        public int maxAlive = 8;
        public float minSpawnDistance = 14f;

        Material _enemyMaterial;
        int _alive;
        float _nextSpawn;
        float _elapsed;

        void Awake()
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            _enemyMaterial = new Material(shader);
            _enemyMaterial.color = new Color(0.85f, 0.18f, 0.18f);
        }

        void Start()
        {
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }
            _nextSpawn = Time.time + 1.0f;
        }

        void Update()
        {
            _elapsed += Time.deltaTime;
            if (Time.time < _nextSpawn || _alive >= maxAlive) return;

            float t = Mathf.Clamp01(_elapsed / rampSeconds);
            float interval = Mathf.Lerp(startInterval, minInterval, t);
            _nextSpawn = Time.time + interval;
            Spawn();
        }

        void Spawn()
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            float d = Random.Range(minSpawnDistance, arenaRadius);
            Vector3 center = player != null ? player.position : Vector3.zero;
            Vector3 pos = center + new Vector3(dir.x, 0f, dir.y) * d;
            pos.y = 1f; // capsule half-height above the ground plane

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Enemy";
            go.tag = "Enemy";
            go.transform.position = pos;

            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = _enemyMaterial;

            var health = go.AddComponent<Health>();
            health.Init(50f);   // 2 shots at 25 dmg; must set after AddComponent

            var enemy = go.AddComponent<Enemy>();
            enemy.target = player;
            enemy.Removed += OnEnemyRemoved;

            _alive++;
        }

        void OnEnemyRemoved(Enemy e)
        {
            e.Removed -= OnEnemyRemoved;
            _alive = Mathf.Max(0, _alive - 1);
        }
    }
}
