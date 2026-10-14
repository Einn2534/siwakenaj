using UnityEngine;
using UnityEngine.SceneManagement;

// The moving targets share one right-to-left lane, separate from the collected icons above.
public sealed class GameplayLaneView : MonoBehaviour
{
    private const float LaneHeight = 240f;
    private static readonly Color RailColor = new Color32(169, 219, 216, 190);
    private RectTransform _root;

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

    public static GameplayLaneView EnsureInstalled(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != "Main") return null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            CarSpawner spawner = root.GetComponentInChildren<CarSpawner>(true);
            if (spawner == null) continue;
            return spawner.GetComponent<GameplayLaneView>() ?? spawner.gameObject.AddComponent<GameplayLaneView>();
        }
        return null;
    }

    private void Awake()
    {
        CarSpawner spawner = GetComponent<CarSpawner>();
        RectTransform zone = spawner != null ? spawner.PlayZone : null;
        Canvas parentCanvas = zone != null ? zone.GetComponentInParent<Canvas>() : null;
        if (parentCanvas == null) return;

        GameObject lane = new("GameplayLane", typeof(RectTransform));
        lane.layer = zone.gameObject.layer;
        _root = (RectTransform)lane.transform;
        _root.SetParent(zone, false);
        // Anchors follow the actual spawn center and miss boundary at every aspect ratio.
        _root.anchorMin = new Vector2(MissBoundaryGeometry.InsetRatio, 0.5f);
        _root.anchorMax = new Vector2(1f, 0.5f);
        _root.sizeDelta = new Vector2(0f, LaneHeight);
        _root.anchoredPosition = Vector2.zero;
        // Inherit the background canvas (0), keeping Powa (1), targets (20),
        // and the red miss boundary (21) in front of every part of the lane.

        CreateStrip("LaneTint", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(0f, LaneHeight), Vector2.zero, new Color32(27, 43, 53, 175));
        foreach (float edge in new[] { 0f, 1f })
        {
            Vector2 anchor = new(0f, edge);
            Vector2 end = new(1f, edge);
            CreateStrip("RailGlow", anchor, end, new Vector2(0f, 12f), Vector2.zero,
                new Color32(135, 213, 208, 35));
            CreateStrip("Rail", anchor, end, new Vector2(0f, 3f), Vector2.zero, RailColor);
        }

        Vector2 entrance = new(1f, 0.5f);
        CreateStrip("EntranceGlow", entrance, entrance, new Vector2(20f, LaneHeight - 8f),
            new Vector2(-12f, 0f), new Color32(147, 230, 218, 45));
        CreateStrip("Entrance", entrance, entrance, new Vector2(4f, LaneHeight - 8f),
            new Vector2(-12f, 0f), new Color32(182, 241, 228, 220));

    }

    private void OnEnable()
    {
        if (_root != null) _root.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (_root != null) _root.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_root != null) Destroy(_root.gameObject);
    }

    private RectTransform CreateStrip(string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 size, Vector2 position, Color color)
    {
        GameObject strip = new(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        strip.layer = _root.gameObject.layer;
        RectTransform rect = (RectTransform)strip.transform;
        rect.SetParent(_root, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        UnityEngine.UI.Image image = strip.GetComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }
}
