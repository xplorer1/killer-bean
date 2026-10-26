using UnityEngine;

namespace BeanBlaster.UI
{
    /// <summary>Fits a UI panel inside the device safe area, including display cutouts.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform panel;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            panel = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea || Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
            {
                Apply();
            }
        }

        /// <summary>Applies the current screen safe area to this panel's anchors.</summary>
        public void Apply()
        {
            if (panel == null)
            {
                panel = GetComponent<RectTransform>();
            }

            Rect safeArea = Screen.safeArea;
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            Rect anchors = CalculateAnchors(safeArea, screenSize);
            panel.anchorMin = anchors.min;
            panel.anchorMax = anchors.max;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }

        /// <summary>Converts a pixel safe area into normalized canvas anchors.</summary>
        public static Rect CalculateAnchors(Rect safeArea, Vector2 screenSize)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            Vector2 minimum = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
            Vector2 maximum = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);
            minimum = new Vector2(Mathf.Clamp01(minimum.x), Mathf.Clamp01(minimum.y));
            maximum = new Vector2(Mathf.Clamp01(maximum.x), Mathf.Clamp01(maximum.y));
            return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
        }
    }
}
