using UnityEngine;

namespace Shooter
{
    /// <summary>
    /// Hitscan pistol with a magazine and reload. Only fires while
    /// <see cref="Active"/> (set by the match once the buzzer sounds). Raycasts
    /// from the camera centre and reports impacts to paper / steel targets.
    /// </summary>
    public class Gun : MonoBehaviour
    {
        [Header("Aim")]
        public Camera aimCamera;

        [Header("Ballistics")]
        public float range = 120f;
        public float fireRate = 7f;            // shots per second (semi-auto, hold to bump-fire)
        public LayerMask hitMask = ~0;

        [Header("Magazine")]
        public int magCapacity = 10;
        public float reloadTime = 1.3f;

        [Header("Feedback")]
        public Light muzzleFlash;
        public LineRenderer tracer;
        public float tracerTime = 0.04f;

        public bool Active { get; set; }
        public int Ammo { get; private set; }
        public bool Reloading { get; private set; }
        public int MagCapacity => magCapacity;
        public int ShotsFired { get; private set; }

        bool _firing;
        float _nextShot;
        float _reloadDoneAt;
        float _tracerOffAt;
        float _flashOffAt;
        AudioSource _sfx;

        void Awake()
        {
            if (aimCamera == null) aimCamera = GetComponentInChildren<Camera>();
            if (aimCamera == null) aimCamera = Camera.main;
            Ammo = magCapacity;
            if (tracer != null) tracer.enabled = false;
            if (muzzleFlash != null) muzzleFlash.enabled = false;

            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _sfx.spatialBlend = 0f; // 2D — always audible
        }

        public void SetFiring(bool firing) => _firing = firing;

        public void Reload()
        {
            if (Reloading || Ammo >= magCapacity) return;
            Reloading = true;
            _reloadDoneAt = Time.time + reloadTime;
        }

        void Update()
        {
            if (Reloading && Time.time >= _reloadDoneAt)
            {
                Reloading = false;
                Ammo = magCapacity;
            }

            if (Active && _firing && !Reloading && Ammo > 0 && Time.time >= _nextShot)
            {
                _nextShot = Time.time + 1f / Mathf.Max(0.01f, fireRate);
                Shoot();
            }

            if (tracer != null && tracer.enabled && Time.time >= _tracerOffAt) tracer.enabled = false;
            if (muzzleFlash != null && muzzleFlash.enabled && Time.time >= _flashOffAt) muzzleFlash.enabled = false;
        }

        void Shoot()
        {
            Ammo--;
            ShotsFired++;
            if (_sfx != null)
            {
                _sfx.pitch = Random.Range(0.94f, 1.07f);
                _sfx.PlayOneShot(ProcAudio.Gunshot, 0.45f);
            }
            if (aimCamera == null) return;

            Vector3 origin = aimCamera.transform.position;
            Vector3 dir = aimCamera.transform.forward;
            Vector3 end = origin + dir * range;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                var paper = hit.collider.GetComponentInParent<PaperTarget>();
                if (paper != null)
                {
                    paper.RegisterHit(hit.point);
                }
                else
                {
                    var steel = hit.collider.GetComponentInParent<SteelTarget>();
                    if (steel != null) steel.Hit();
                }
            }

            ShowTracer(origin, end);
            ShowMuzzleFlash();
        }

        void ShowTracer(Vector3 from, Vector3 to)
        {
            if (tracer == null) return;
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
