using UnityEngine;

namespace StartupLife.Presentation
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class MobileSafeArea : MonoBehaviour
    {
        Rect previousArea;
        Vector2Int previousSize;
        void OnEnable() => Apply(Screen.safeArea, Screen.width, Screen.height);
        void Update()
        {
            if (previousArea != Screen.safeArea || previousSize != new Vector2Int(Screen.width, Screen.height))
                Apply(Screen.safeArea, Screen.width, Screen.height);
        }
        public void Apply(Rect area, int width, int height)
        {
            if (width <= 0 || height <= 0) return;
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(area.xMin / width, area.yMin / height);
            rect.anchorMax = new Vector2(area.xMax / width, area.yMax / height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            previousArea = area;
            previousSize = new Vector2Int(width, height);
        }
    }
}
