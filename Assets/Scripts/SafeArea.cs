using UnityEngine;

namespace Shooter
{
    /// <summary>
    /// Fits this RectTransform to the device's safe area (Screen.safeArea), so
    /// HUD elements anchored to the corners stay clear of notches, camera
    /// cutouts, and rounded display corners. Re-applies if the area changes
    /// (rotation, resize).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        RectTransform _rt;
        Rect _lastArea;
        Vector2Int _lastRes;

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
            Apply();
        }

        void Update()
        {
            if (Screen.safeArea != _lastArea ||
                Screen.width != _lastRes.x || Screen.height != _lastRes.y)
                Apply();
        }

        void Apply()
        {
            _lastArea = Screen.safeArea;
            _lastRes = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Vector2 min = Screen.safeArea.position;
            Vector2 max = min + Screen.safeArea.size;
            min.x /= Screen.width; min.y /= Screen.height;
            max.x /= Screen.width; max.y /= Screen.height;
            if (float.IsNaN(min.x) || float.IsNaN(max.x)) return;

            _rt.anchorMin = min;
            _rt.anchorMax = max;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
