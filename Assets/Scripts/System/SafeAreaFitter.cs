using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform _targetRect;
    private Rect _lastSafeArea = Rect.zero;
    private Vector2Int _lastScreenSize = Vector2Int.zero;

#if UNITY_EDITOR
    private static bool s_HasEditorSimulation;
    private static Rect s_EditorSafeArea;
    private static Vector2Int s_EditorScreenSize;

    public static void SetEditorSimulation(Vector2Int screenSize, Rect safeArea)
    {
        s_EditorScreenSize = new Vector2Int(Mathf.Max(1, screenSize.x), Mathf.Max(1, screenSize.y));
        s_EditorSafeArea = safeArea;
        s_HasEditorSimulation = true;
    }

    public static void ClearEditorSimulation()
    {
        s_HasEditorSimulation = false;
        s_EditorSafeArea = Rect.zero;
        s_EditorScreenSize = Vector2Int.zero;
    }
#endif

    public static Rect CurrentSafeArea => GetCurrentSafeArea();
    public static Vector2Int CurrentScreenSize => GetCurrentScreenSize();

    private void Awake()
    {
        Refresh();
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
        _targetRect ??= GetComponent<RectTransform>();
        Rect safeArea = GetCurrentSafeArea();
        Vector2Int screenSize = GetCurrentScreenSize();
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= screenSize.x;
        anchorMin.y /= screenSize.y;
        anchorMax.x /= screenSize.x;
        anchorMax.y /= screenSize.y;

        _targetRect.anchorMin = anchorMin;
        _targetRect.anchorMax = anchorMax;
        _targetRect.offsetMin = Vector2.zero;
        _targetRect.offsetMax = Vector2.zero;

        _lastSafeArea = safeArea;
        _lastScreenSize = screenSize;
    }

    public void Refresh()
    {
        ApplySafeArea();
    }

    private bool IsSafeAreaChanged()
    {
        Vector2Int screenSize = GetCurrentScreenSize();
        if (_lastScreenSize != screenSize)
        {
            return true;
        }

        return _lastSafeArea != GetCurrentSafeArea();
    }

    private static Rect GetCurrentSafeArea()
    {
#if UNITY_EDITOR
        if (s_HasEditorSimulation)
        {
            return s_EditorSafeArea;
        }
#endif
        return Screen.safeArea;
    }

    private static Vector2Int GetCurrentScreenSize()
    {
#if UNITY_EDITOR
        if (s_HasEditorSimulation)
        {
            return s_EditorScreenSize;
        }
#endif
        return new Vector2Int(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
    }
}
