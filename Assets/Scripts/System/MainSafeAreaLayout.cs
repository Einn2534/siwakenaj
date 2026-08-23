using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps the existing Main layout intact while moving its fixed top and bottom
/// zones clear of device cutouts and gesture areas.
/// </summary>
public sealed class MainSafeAreaLayout : MonoBehaviour
{
    private RectTransform _canvasRect;
    private RectTransform _topZone;
    private RectTransform _buttonZone;
    private Vector2 _topZoneBasePosition;
    private Vector2 _buttonZoneBasePosition;
    private Rect _lastSafeArea = new(float.MinValue, float.MinValue, 0f, 0f);
    private Vector2Int _lastScreenSize;
    private bool _isCached;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallForCurrentScene()
    {
        EnsureInstalled(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstalled(scene);
    }

    public static MainSafeAreaLayout EnsureInstalled(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || !string.Equals(scene.name, "Main", StringComparison.Ordinal))
        {
            return null;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            MainSafeAreaLayout existing = root.GetComponentInChildren<MainSafeAreaLayout>(true);
            if (existing != null)
            {
                existing.ApplyNow();
                return existing;
            }
        }

        Transform canvasTransform = FindTransform(scene, "Canvas");
        Transform topZone = FindTransform(scene, "TopZone");
        Transform buttonZone = FindTransform(scene, "ButtonZone");
        if (canvasTransform is not RectTransform || topZone is not RectTransform || buttonZone is not RectTransform)
        {
            return null;
        }

        MainSafeAreaLayout layout = canvasTransform.gameObject.AddComponent<MainSafeAreaLayout>();
        layout.ApplyNow();
        return layout;
    }

    public void ApplyNow()
    {
        if (!CacheLayout())
        {
            return;
        }

        Rect safeArea = SafeAreaFitter.CurrentSafeArea;
        Vector2Int screenSize = SafeAreaFitter.CurrentScreenSize;
        float canvasHeight = Mathf.Max(1f, _canvasRect.rect.height);
        float unitsPerScreenPixel = canvasHeight / Mathf.Max(1, screenSize.y);
        float topInset = Mathf.Max(0f, screenSize.y - safeArea.yMax) * unitsPerScreenPixel;
        float bottomInset = Mathf.Max(0f, safeArea.yMin) * unitsPerScreenPixel;

        _topZone.anchoredPosition = _topZoneBasePosition + Vector2.down * topInset;
        _buttonZone.anchoredPosition = _buttonZoneBasePosition + Vector2.up * bottomInset;
        _lastSafeArea = safeArea;
        _lastScreenSize = screenSize;
    }

    private void Awake()
    {
        ApplyNow();
    }

    private void Update()
    {
        if (_lastSafeArea != SafeAreaFitter.CurrentSafeArea || _lastScreenSize != SafeAreaFitter.CurrentScreenSize)
        {
            ApplyNow();
        }
    }

    private bool CacheLayout()
    {
        if (_isCached)
        {
            return _canvasRect != null && _topZone != null && _buttonZone != null;
        }

        _canvasRect = transform as RectTransform;
        _topZone = FindChild(transform, "TopZone") as RectTransform;
        _buttonZone = FindChild(transform, "ButtonZone") as RectTransform;
        if (_canvasRect == null || _topZone == null || _buttonZone == null)
        {
            return false;
        }

        _topZoneBasePosition = _topZone.anchoredPosition;
        _buttonZoneBasePosition = _buttonZone.anchoredPosition;
        _isCached = true;
        return true;
    }

    private static Transform FindTransform(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform result = FindChild(root.transform, objectName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static Transform FindChild(Transform parent, string objectName)
    {
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (string.Equals(child.name, objectName, StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }
}
