using UnityEngine;

namespace Shooter
{
    /// <summary>
    /// First-person character: walks with a CharacterController, looks with
    /// yaw on the body and pitch on the camera, and drives the attached Gun.
    /// Reads all input through <see cref="GameInput"/> so it works on
    /// desktop and Android touch alike.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Refs")]
        public Transform cameraPivot;   // child holding the Camera (pitched)
        public GameInput input;
        public Gun gun;

        [Header("Movement")]
        public float moveSpeed = 6f;
        public float gravity = -20f;
        public float jumpHeight = 1.2f;

        [Header("Look")]
        public float lookSensitivity = 2.0f;
        public float minPitch = -80f;
        public float maxPitch = 80f;

        CharacterController _cc;
        float _pitch;
        float _verticalVel;
        bool _frozen;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (input == null) input = FindAnyObjectByType<GameInput>();
            if (gun == null) gun = GetComponentInChildren<Gun>();
        }

        void Start()
        {
            // Lock the cursor for desktop play; harmless on device.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void Freeze(bool frozen)
        {
            _frozen = frozen;
            if (frozen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        void Update()
        {
            if (_frozen || input == null) return;

            // --- Look ---
            Vector2 look = input.Look * lookSensitivity;
            transform.Rotate(Vector3.up, look.x, Space.Self);
            _pitch = Mathf.Clamp(_pitch - look.y, minPitch, maxPitch);
            if (cameraPivot != null)
                cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

            // --- Move ---
            Vector2 m = input.Move;
            Vector3 wish = (transform.right * m.x + transform.forward * m.y);
            if (wish.sqrMagnitude > 1f) wish.Normalize();

            if (_cc.isGrounded)
            {
                _verticalVel = -2f;
                if (input.Jump)
                    _verticalVel = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            _verticalVel += gravity * Time.deltaTime;

            Vector3 velocity = wish * moveSpeed + Vector3.up * _verticalVel;
            _cc.Move(velocity * Time.deltaTime);

            // --- Shoot ---
            if (gun != null) gun.SetFiring(input.Fire);
        }
    }
}
