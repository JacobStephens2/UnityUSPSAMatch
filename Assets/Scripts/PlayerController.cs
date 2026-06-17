using UnityEngine;

namespace Shooter
{
    /// <summary>
    /// First-person shooter rig. Frozen before the buzzer and after the stage
    /// ends; in between it walks, looks, fires, and reloads via the gun.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Refs")]
        public Transform cameraPivot;
        public GameInput input;
        public Gun gun;

        [Header("Movement")]
        public float moveSpeed = 6f;
        public float gravity = -20f;

        [Header("Look")]
        public float lookSensitivity = 2.0f;
        public float minPitch = -80f;
        public float maxPitch = 80f;

        CharacterController _cc;
        float _pitch;
        float _verticalVel;
        bool _frozen = true;   // start frozen until the buzzer

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (input == null) input = FindAnyObjectByType<GameInput>();
            if (gun == null) gun = GetComponentInChildren<Gun>();
        }

        public void Freeze(bool frozen)
        {
            _frozen = frozen;
            Cursor.lockState = frozen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = frozen;
        }

        void Update()
        {
            if (_frozen || input == null) return;

            // Look
            Vector2 look = input.Look * lookSensitivity;
            transform.Rotate(Vector3.up, look.x, Space.Self);
            _pitch = Mathf.Clamp(_pitch - look.y, minPitch, maxPitch);
            if (cameraPivot != null)
                cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

            // Move
            Vector2 m = input.Move;
            Vector3 wish = transform.right * m.x + transform.forward * m.y;
            if (wish.sqrMagnitude > 1f) wish.Normalize();
            if (_cc.isGrounded) _verticalVel = -2f;
            _verticalVel += gravity * Time.deltaTime;
            _cc.Move((wish * moveSpeed + Vector3.up * _verticalVel) * Time.deltaTime);

            // Shoot / reload
            if (gun != null)
            {
                gun.SetFiring(input.Fire);
                if (input.Reload) gun.Reload();
            }
        }
    }
}
