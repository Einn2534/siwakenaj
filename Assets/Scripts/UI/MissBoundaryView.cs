using UnityEngine;
using UnityEngine.SceneManagement;

// Shares the live PlayZone geometry with CarController, including during resized captures.
public sealed class MissBoundaryView : MonoBehaviour
{
    private const float FlashSeconds = 0.2f;
    private const float FenceHeight = 240f;
    private const float LineWidth = 6f;
    private const float CapWidth = 30f;
    private static readonly Color OutlineColor = new Color32(48, 28, 40, 255);
    private static readonly Color IdleColor = new Color32(206, 81, 91, 255);
    private static readonly Color WarningColor = new Color32(255, 113, 111, 255);
    private static readonly Color FlashColor = new Color32(255, 224, 205, 255);

    private CarSpawner _spawner;
    private GameFlowController _flow;
    private RectTransform _root;
    private readonly UnityEngine.UI.Image[] _lines = new UnityEngine.UI.Image[3];
    private readonly UnityEngine.UI.Image[] _glows = new UnityEngine.UI.Image[6];
    private float _flashStartedAt = float.NegativeInfinity;

    public float Proximity { get; private set; }
    public float FlashStrength => Mathf.Clamp01(1f - (Time.time - _flashStartedAt) / FlashSeconds);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSceneHook() => SceneManager.sceneLoaded -= OnSceneLoaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallBootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureInstalled(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureInstalled(scene);

    public static MissBoundaryView EnsureInstalled(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != "Main") return null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            CarSpawner spawner = root.GetComponentInChildren<CarSpawner>(true);
            if (spawner == null) continue;
            return spawner.GetComponent<MissBoundaryView>() ?? spawner.gameObject.AddComponent<MissBoundaryView>();
        }
        return null;
    }

    private void Awake()
    {
        _spawner = GetComponent<CarSpawner>();
        _flow = FindAnyObjectByType<GameFlowController>();
        BuildInterface();
        RefreshLayout();
        RefreshAppearance();
    }

    private void OnEnable()
    {
        if (_spawner != null) _spawner.CarMissed += OnCarMissed;
        Canvas.willRenderCanvases += RefreshLayout;
        if (_root != null) _root.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (_spawner != null) _spawner.CarMissed -= OnCarMissed;
        Canvas.willRenderCanvases -= RefreshLayout;
        if (_root != null) _root.gameObject.SetActive(false);
        _flashStartedAt = float.NegativeInfinity;
        Proximity = 0f;
    }

    private void OnDestroy()
    {
        if (_root != null) Destroy(_root.gameObject);
    }

    private void LateUpdate()
    {
        RefreshLayout();
        if (Time.deltaTime <= 0f || (_flow != null && _flow.IsPaused())) return;
        RefreshAppearance();
    }

    public void RefreshLayout()
    {
        if (_root == null || _spawner == null || !_spawner.TryGetPlayZoneWorldRect(out Rect zone)) return;
        _root.position = new Vector3(
            MissBoundaryGeometry.GetBoundaryX(zone.xMin, zone.width), zone.center.y, _root.position.z);
    }

    private void OnCarMissed(CarController car)
    {
        // Start at the event time: the frame's elapsed time precedes this miss.
        _flashStartedAt = Time.time;
        RefreshAppearance();
    }

    private void RefreshAppearance()
    {
        Proximity = 0f;
        if (_spawner != null && _spawner.TryGetPlayZoneWorldRect(out Rect zone))
        {
            CarController car = _spawner.GetActiveCar();
            if (car != null)
                Proximity = MissBoundaryGeometry.GetProximity(car.GetMissPointX(), car.MissBoundaryX, zone.width);
        }

        float flash = FlashStrength;
        Color lineColor = Color.Lerp(Color.Lerp(IdleColor, WarningColor, Proximity), FlashColor, flash);
        foreach (UnityEngine.UI.Image line in _lines)
            if (line != null) line.color = lineColor;
        for (int i = 0; i < _glows.Length; i++)
        {
            if (_glows[i] == null) continue;
            Color glow = WarningColor;
            glow.a = (i < 3 ? 0.14f : 0.3f) * Mathf.Max(Proximity, flash);
            _glows[i].color = glow;
        }
    }

    private void BuildInterface()
    {
        Canvas parentCanvas = _spawner != null && _spawner.PlayZone != null
            ? _spawner.PlayZone.GetComponentInParent<Canvas>() : null;
        if (parentCanvas == null || _root != null) return;
        GameObject rootObject = new("MissBoundary", typeof(RectTransform), typeof(Canvas));
        rootObject.layer = parentCanvas.gameObject.layer;
        _root = (RectTransform)rootObject.transform;
        _root.SetParent(parentCanvas.transform, false);
        _root.anchorMin = _root.anchorMax = _root.pivot = new Vector2(0.5f, 0.5f);
        _root.sizeDelta = new Vector2(CapWidth, FenceHeight);
        Canvas canvas = rootObject.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingLayerID = parentCanvas.sortingLayerID;
        canvas.sortingOrder = 21; // Cars: 20, progress HUD: 40, modal canvases: 90+.

        for (int layer = 0; layer < 4; layer++)
        {
            float expansion = layer == 0 ? 26f : layer == 1 ? 14f : layer == 2 ? 6f : 0f;
            for (int part = 0; part < 3; part++)
            {
                Vector2 size = part == 0 ? new Vector2(LineWidth, FenceHeight)
                    : new Vector2(CapWidth, LineWidth);
                Vector2 position = part == 0 ? Vector2.zero
                    : new Vector2(0f, (part == 1 ? 1f : -1f) * (FenceHeight - LineWidth) * 0.5f);
                UnityEngine.UI.Image image = CreateBar($"Fence_{layer}_{part}", size + Vector2.one * expansion, position);
                if (layer < 2) _glows[layer * 3 + part] = image;
                else if (layer == 2) image.color = OutlineColor;
                else _lines[part] = image;
            }
        }
    }

    private UnityEngine.UI.Image CreateBar(string name, Vector2 size, Vector2 position)
    {
        GameObject bar = new(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        bar.layer = _root.gameObject.layer;
        RectTransform rect = (RectTransform)bar.transform;
        rect.SetParent(_root, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        UnityEngine.UI.Image image = bar.GetComponent<UnityEngine.UI.Image>();
        image.raycastTarget = false;
        return image;
    }
}
