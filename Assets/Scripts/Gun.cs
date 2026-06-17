using UnityEngine;

namespace Shooter
{
    /// <summary>
    /// Hitscan weapon. Raycasts from the camera centre, applies damage to any
    /// <see cref="Health"/> it hits, and draws a short-lived tracer + muzzle
    /// flash for feedback. No art assets required.
    /// </summary>
    public class Gun : MonoBehaviour
    {
        [Header("Aim")]
        public Camera aimCamera;

        [Header("Ballistics")]
        public float damage = 25f;
        public float range = 120f;
        public float fireRate = 8f;            // shots per second
        public LayerMask hitMask = ~0;

        [Header("Feedback")]
        public Light muzzleFlash;
        public LineRenderer tracer;
        public float tracerTime = 0.04f;

        bool _firing;
        float _nextShot;
        float _tracerOffAt;
        float _flashOffAt;

        void Awake()
        {
            if (aimCamera == null) aimCamera = GetComponentInChildren<Camera>();
            if (aimCamera == null) aimCamera = Camera.main;
            if (tracer != null) tracer.enabled = false;
            if (muzzleFlash != null) muzzleFlash.enabled = false;
        }

        public void SetFiring(bool firing) => _firing = firing;

        void Update()
        {
            if (_firing && Time.time >= _nextShot)
            {
                _nextShot = Time.time + 1f / Mathf.Max(0.01f, fireRate);
                Shoot();
            }

            if (tracer != null && tracer.enabled && Time.time >= _tracerOffAt)
                tracer.enabled = false;
            if (muzzleFlash != null && muzzleFlash.enabled && Time.time >= _flashOffAt)
                muzzleFlash.enabled = false;
        }

        void Shoot()
        {
            if (aimCamera == null) return;

            Vector3 origin = aimCamera.transform.position;
            Vector3 dir = aimCamera.transform.forward;
            Vector3 end = origin + dir * range;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                var hp = hit.collider.GetComponentInParent<Health>();
                if (hp != null) hp.TakeDamage(damage);
            }

            ShowTracer(origin, end);
            ShowMuzzleFlash();
        }

        void ShowTracer(Vector3 from, Vector3 to)
        {
            if (tracer == null) return;
            // Start the tracer a little ahead of the camera so it reads as a barrel shot.
            tracer.positionCount = 2;
            tracer.SetPosition(0, from + aimCamera.transform.forward * 0.4f - aimCamera.transform.up * 0.15f);
            tracer.SetPosition(1, to);
            tracer.enabled = true;
            _tracerOffAt = Time.time + tracerTime;
        }

        void ShowMuzzleFlash()
        {
            if (muzzleFlash == null) return;
            muzzleFlash.enabled = true;
            _flashOffAt = Time.time + tracerTime;
        }
    }
}
