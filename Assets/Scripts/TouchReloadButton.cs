using UnityEngine;
using UnityEngine.EventSystems;

namespace Shooter
{
    /// <summary>On-screen Reload button (mobile). Queues a reload on press.</summary>
    public class TouchReloadButton : MonoBehaviour, IPointerDownHandler
    {
        public void OnPointerDown(PointerEventData e)
        {
            if (GameInput.Instance != null) GameInput.Instance.QueueReload();
        }
    }
}
