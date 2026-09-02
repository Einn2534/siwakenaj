using UnityEngine;

namespace Siwakenja.Experiments.InputUIComparison
{
    [RequireComponent(typeof(RectTransform))]
    internal sealed class ExperimentSafeArea : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreen;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (_lastSafeArea != Screen.safeArea || _lastScreen.x != Screen.width || _lastScreen.y != Screen.height)
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            Rect safe = Screen.safeArea;
            float width = Mathf.Max(1f, Screen.width);
            float height = Mathf.Max(1f, Screen.height);
            _rectTransform.anchorMin = new Vector2(safe.xMin / width, safe.yMin / height);
            _rectTransform.anchorMax = new Vector2(safe.xMax / width, safe.yMax / height);
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
            _lastSafeArea = safe;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
