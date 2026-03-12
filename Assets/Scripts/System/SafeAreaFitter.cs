using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform _targetRect;
    private Rect _lastSafeArea = Rect.zero;
    private Vector2Int _lastScreenSize = Vector2Int.zero;

    private void Awake()
    {
        _targetRect = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void Update()
    {
        if (IsSafeAreaChanged())
        {
            ApplySafeArea();
        }
    }

    private void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        _targetRect.anchorMin = anchorMin;
        _targetRect.anchorMax = anchorMax;
        _targetRect.offsetMin = Vector2.zero;
        _targetRect.offsetMax = Vector2.zero;

        _lastSafeArea = safeArea;
        _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }

    private bool IsSafeAreaChanged()
    {
        if (_lastScreenSize.x != Screen.width || _lastScreenSize.y != Screen.height)
        {
            return true;
        }

        return _lastSafeArea != Screen.safeArea;
    }
}
