using UnityEngine;
using UnityEngine.EventSystems;

namespace Shooter
{
    /// <summary>
    /// Unified input for desktop (keyboard + mouse) and mobile (touch).
    /// Mobile: left half = floating move-joystick, right half = look-drag,
    /// on-screen FIRE button feeds <see cref="FireButtonHeld"/>, RELOAD button
    /// calls <see cref="QueueReload"/>. Exposes Move / Look / Fire / Reload.
    /// </summary>
    public class GameInput : MonoBehaviour
    {
        public static GameInput Instance { get; private set; }

        [Header("Tuning")]
        public float mouseLookSpeed = 2.0f;
        public float touchLookSpeed = 0.12f;
        [Range(0.05f, 0.4f)] public float joystickRadiusFraction = 0.15f;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool Fire { get; private set; }
        public bool Reload { get; private set; }   // true for one frame

        /// <summary>Set by the on-screen Fire button (mobile).</summary>
        public bool FireButtonHeld { get; set; }

        bool _reloadQueued;
        int _moveFinger = -1;
        Vector2 _moveStart;
        int _lookFinger = -1;

        void Awake() => Instance = this;

        /// <summary>Called by the on-screen Reload button (mobile).</summary>
        public void QueueReload() => _reloadQueued = true;

        void Update()
        {
            if (Input.touchSupported && Input.touchCount > 0)
                ReadTouch();
            else
                ReadDesktop();

            Reload = _reloadQueued || Input.GetKeyDown(KeyCode.R);
            _reloadQueued = false;
        }

        void ReadDesktop()
        {
            Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (Move.sqrMagnitude > 1f) Move = Move.normalized;
            Look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * mouseLookSpeed;
            Fire = Input.GetMouseButton(0) || FireButtonHeld;
        }

        void ReadTouch()
        {
            float halfW = Screen.width * 0.5f;
            float radius = Screen.height * joystickRadiusFraction;
            Vector2 move = Vector2.zero;
            Vector2 look = Vector2.zero;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);

                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId))
                {
                    if (t.fingerId == _moveFinger) _moveFinger = -1;
                    if (t.fingerId == _lookFinger) _lookFinger = -1;
                    continue;
                }

                bool ending = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;

                if (t.position.x < halfW)
                {
                    if (_moveFinger == -1 && t.phase == TouchPhase.Began)
                    {
                        _moveFinger = t.fingerId;
                        _moveStart = t.position;
                    }
                    if (t.fingerId == _moveFinger)
                    {
                        if (ending) _moveFinger = -1;
                        else
                        {
                            Vector2 d = (t.position - _moveStart) / radius;
                            if (d.sqrMagnitude > 1f) d = d.normalized;
                            move = d;
                        }
                    }
                }
                else
                {
                    if (_lookFinger == -1 && t.phase == TouchPhase.Began)
                        _lookFinger = t.fingerId;
                    if (t.fingerId == _lookFinger)
                    {
                        if (ending) _lookFinger = -1;
                        else look += t.deltaPosition * touchLookSpeed;
                    }
                }
            }

            Move = move;
            Look = look;
            Fire = FireButtonHeld;
        }
    }
}
