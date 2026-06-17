using System.Collections.Generic;
using UnityEngine;

namespace Shooter
{
    /// <summary>
    /// A USPSA-style cardboard target. Scoring targets need two hits; only the
    /// best two count (A=5, C=3, D=1 — Minor power factor). No-shoot targets
    /// must NOT be hit; each hit is a penalty. Zone is derived analytically from
    /// where the bullet lands on the board's local face.
    /// </summary>
    public class PaperTarget : MonoBehaviour
    {
        public const int RequiredHits = 2;

        [Tooltip("White penalty target — hitting it costs points.")]
        public bool isNoShoot = false;

        // Zone half-extents in metres, measured on the board's local face
        // (board is ~0.46 x 0.76 m, so half-extents are 0.23 x 0.38).
        [Header("Zone size (local half-extents, metres)")]
        public Vector2 aZone = new Vector2(0.076f, 0.14f);
        public Vector2 cZone = new Vector2(0.15f, 0.305f);

        readonly List<int> _hits = new List<int>();   // points per hit, in order

        public int HitCount => _hits.Count;
        public bool Neutralized => isNoShoot || _hits.Count >= RequiredHits;

        /// <summary>Register a bullet impact at a world point on this target.</summary>
        public void RegisterHit(Vector3 worldPoint)
        {
            int pts = isNoShoot ? 0 : ZonePoints(worldPoint);
            _hits.Add(pts);
            SpawnHole(worldPoint);
        }

        int ZonePoints(Vector3 worldPoint)
        {
            Vector3 lp = transform.InverseTransformPoint(worldPoint);
            float x = Mathf.Abs(lp.x), y = Mathf.Abs(lp.y);
            if (x <= aZone.x && y <= aZone.y) return 5; // A
            if (x <= cZone.x && y <= cZone.y) return 3; // C
            return 1;                                   // D
        }

        /// <summary>Best two hit values (for the scored result), high to low.</summary>
        public List<int> BestTwo()
        {
            var copy = new List<int>(_hits);
            copy.Sort();            // ascending
            copy.Reverse();         // descending
            if (copy.Count > RequiredHits) copy.RemoveRange(RequiredHits, copy.Count - RequiredHits);
            return copy;
        }

        public int ScorePoints()
        {
            if (isNoShoot) return 0;
            int s = 0;
            foreach (int v in BestTwo()) s += v;
            return s;
        }

        /// <summary>Unfilled required hits (each is a -10 miss). Scoring targets only.</summary>
        public int MissCount => isNoShoot ? 0 : Mathf.Max(0, RequiredHits - _hits.Count);

        /// <summary>Hits on a no-shoot, capped at the required-hit count.</summary>
        public int NoShootHits => isNoShoot ? Mathf.Min(RequiredHits, _hits.Count) : 0;

        void SpawnHole(Vector3 worldPoint)
        {
            // A small cube renders from every angle (no quad-facing pitfalls).
            var hole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var col = hole.GetComponent<Collider>();
            if (col != null) Destroy(col);
            hole.transform.SetParent(transform, true);
            hole.transform.position = worldPoint + transform.forward * 0.02f;
            hole.transform.rotation = transform.rotation;
            hole.transform.localScale = new Vector3(0.03f, 0.03f, 0.01f);
            var r = hole.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = new Color(0.05f, 0.05f, 0.05f);
            r.sharedMaterial = mat;
        }
    }
}
