using UnityEngine;

namespace Shooter
{
    /// <summary>
    /// Steel popper: one hit knocks it down for points. Topples backward on the
    /// pivot it sits on, then stops being shootable. The collider lives on a
    /// child plate; this component is found via GetComponentInParent.
    /// </summary>
    public class SteelTarget : MonoBehaviour
    {
        public int points = 5;
        public float toppleTime = 0.35f;

        public bool Down { get; private set; }

        Quaternion _start, _end;
        float _t = -1f;
        Collider[] _colliders;

        void Awake()
        {
            _colliders = GetComponentsInChildren<Collider>();
            _start = transform.localRotation;
            _end = _start * Quaternion.AngleAxis(82f, Vector3.right); // tip backward
        }

        public void Hit()
        {
            if (Down) return;
            Down = true;
            _t = 0f;
            foreach (var c in _colliders) c.enabled = false;
            if (MatchManager.Instance != null) MatchManager.Instance.PlayDing(transform.position);
        }

        void Update()
        {
            if (_t < 0f) return;
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / toppleTime);
            transform.localRotation = Quaternion.Slerp(_start, _end, k);
            if (k >= 1f) _t = -1f;
        }
    }
}
