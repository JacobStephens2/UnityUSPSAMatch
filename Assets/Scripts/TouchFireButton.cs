using UnityEngine;
using UnityEngine.EventSystems;

namespace Shooter
{
    /// <summary>
    /// On-screen Fire button for mobile. Holds <see cref="GameInput.FireButtonHeld"/>
    /// true while pressed. Also works with a mouse click in the editor.
    /// </summary>
    public class TouchFireButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        void Set(bool held)
        {
            if (GameInput.Instance != null) GameInput.Instance.FireButtonHeld = held;
        }

        public void OnPointerDown(PointerEventData e) => Set(true);
        public void OnPointerUp(PointerEventData e) => Set(false);
        void OnDisable() => Set(false);
    }
}
