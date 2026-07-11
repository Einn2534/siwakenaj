using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class StageProgressHudController : MonoBehaviour
{
    private const string MainSceneName = "Main";
    private const float ReferenceWidth = 1080f;
    private const float ReferenceHeight = 1920f;

    private static readonly Color PanelColor = new(1f, 1f, 1f, 0f);
    private static readonly Color MissPanelColor = Color.white;
    private static readonly Color ProgressPanelColor = Color.white;
    private static readonly Color TrackColor = new(0f, 0f, 0f, 0.35f);
    private static readonly Color FillColor = new(111f / 255f, 212f / 255f, 141f / 255f, 1f);
    private static readonly Color TextColor = new(1f, 247f / 255f, 234f / 255f, 1f);
    private static readonly Color HighlightColor = new(1f, 0.82f, 0.24f, 1f);
    private static readonly bool AutoInstallProgressHud = true;

    private static StageProgressHudController _activeController;

    private ScoreManager _scoreManager;
    private TMP_Text _remainingText;
    private TMP_Text _missText;
    private TMP_Text _fractionText;
    private Image _progressFillImage;
    private Image _missPanelImage;
    private Image _progressPanelImage;
    private RectTransform _missPanel;
    private RectTransform _progressPanel;
    private Coroutine _missHighlightRoutine;
    private Coroutine _goalHighlightRoutine;
    private readonly Image[] _missOrbs = new Image[3];
    private Sprite _emptyOrbSprite;
    private Sprite _litOrbSprite;
    private bool _tutorialMode;
    private int _tutorialCompleted;
    private int _tutorialTotal = 6;
    private int _tutorialMisses;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        _activeController = null;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallBootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryInstall(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryInstall(scene);
    }

    private static void TryInstall(Scene scene)
    {
        if (!AutoInstallProgressHud)
        {
            return;
        }

        if (!scene.IsValid() || scene.name != MainSceneName)
        {
            return;
        }

        if (_activeController != null || FindAnyObjectByType<StageProgressHudController>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        GameObject controllerObject = new("StageProgressHudController");
        controllerObject.AddComponent<StageProgressHudController>();
    }

    private void Awake()
    {
        if (_activeController != null && _activeController != this)
        {
            Destroy(gameObject);
            return;
        }

        _activeController = this;
        ResolveScoreManager();
        BuildInterface();
        Refresh();
    }

    private void Update()
    {
        if (_scoreManager == null)
        {
            ResolveScoreManager();
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (_activeController == this)
        {
            _activeController = null;
        }
    }

    public void HighlightMiss()
    {
        RestartHighlight(ref _missHighlightRoutine, _missPanel, _missPanelImage, MissPanelColor);
    }

    public void HighlightGoal()
    {
        RestartHighlight(ref _goalHighlightRoutine, _progressPanel, _progressPanelImage, ProgressPanelColor);
    }

    public void SetTutorialMode(bool enabled)
    {
        _tutorialMode = enabled;
        Refresh();
    }

    public void SetTutorialProgress(int completed, int total, int misses)
    {
        _tutorialCompleted = Mathf.Max(0, completed);
        _tutorialTotal = Mathf.Max(1, total);
        _tutorialMisses = Mathf.Clamp(misses, 0, _missOrbs.Length);
        Refresh();
    }

    private void RestartHighlight(ref Coroutine routine, RectTransform target, Image image, Color baseColor)
    {
        if (target == null || image == null)
        {
            return;
        }

        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(HighlightRoutine(target, image, baseColor));
    }

    private IEnumerator HighlightRoutine(RectTransform target, Image image, Color baseColor)
    {
        const float duration = 1.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float wave = Mathf.PingPong(elapsed * 4.5f, 1f);
            image.color = Color.Lerp(baseColor, HighlightColor, wave);
            target.localScale = Vector3.one * Mathf.Lerp(1f, 1.06f, wave);
            yield return null;
        }

        image.color = baseColor;
        target.localScale = Vector3.one;
    }

    private void ResolveScoreManager()
    {
        _scoreManager = FindAnyObjectByType<ScoreManager>();
    }

    private void Refresh()
    {
        if (_tutorialMode)
        {
            ApplyValues(_tutorialCompleted, _tutorialTotal, _tutorialMisses, false, 0);
            return;
        }

        ScoreState state = _scoreManager != null ? _scoreManager.State : null;
        int remaining = _scoreManager != null ? _scoreManager.RemainingSuccessCount : 0;
        int missCount = state != null ? state.MissCount : 0;
        int missLimit = state != null ? state.MissLimit : 0;
        int score = state != null ? state.CurrentScore : 0;
        int targetScore = state != null ? state.TargetScore : 0;
        bool isEndless = state != null && state.IsEndless;
        float progress = isEndless ? 1f : targetScore > 0 ? Mathf.Clamp01(score / (float)targetScore) : 0f;

        int completed = isEndless ? score : Mathf.Max(0, targetScore - remaining);
        int total = isEndless ? Mathf.Max(1, score) : Mathf.Max(1, targetScore);
        ApplyValues(completed, total, missCount, isEndless, score);
    }

    private void ApplyValues(int completed, int total, int missCount, bool isEndless, int score)
    {
        int remaining = Mathf.Max(0, total - completed);
        if (_remainingText != null)
        {
            _remainingText.text = isEndless ? $"SCORE {score:N0}" : $"\u3042\u3068{remaining}\u53f0";
        }

        if (_missText != null)
        {
            _missText.text = "\u30df\u30b9";
        }

        if (_fractionText != null)
        {
            _fractionText.text = isEndless ? string.Empty : $"{completed}/{total}";
        }

        if (_progressFillImage != null)
        {
            _progressFillImage.fillAmount = isEndless ? 1f : Mathf.Clamp01(completed / (float)Mathf.Max(1, total));
        }

        for (int i = 0; i < _missOrbs.Length; i += 1)
        {
            if (_missOrbs[i] != null)
            {
                _missOrbs[i].sprite = i < missCount ? _litOrbSprite : _emptyOrbSprite;
            }
        }
    }

    private void BuildInterface()
    {
        Canvas canvas = CreateCanvas();
        RectTransform safeRoot = CreateUiObject("SafeAreaRoot", canvas.transform);
        Stretch(safeRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        safeRoot.gameObject.AddComponent<SafeAreaFitter>();

        RectTransform root = CreatePanel("StageProgressHud", safeRoot, PanelColor);
        SetAnchored(root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(-64f, 150f));

        _missPanel = CreatePanel("MissPanel", root, MissPanelColor);
        _missPanelImage = _missPanel.GetComponent<Image>();
        ConfigureWoodPanel(_missPanelImage);
        SetAnchored(_missPanel, new Vector2(0f, 0f), new Vector2(0.4f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(-13f, 0f));
        _missText = CreateText("MissText", _missPanel, "\u30df\u30b9", 38f, 28f, FontStyles.Normal, TextAlignmentOptions.Left);
        _missText.font = LoadFont("UI/Tutorial/DotGothic16-Regular SDF");
        SetAnchored((RectTransform)_missText.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(35f, 0f), new Vector2(96f, 70f));

        _emptyOrbSprite = Resources.Load<Sprite>("UI/Tutorial/miss_orb_empty");
        _litOrbSprite = Resources.Load<Sprite>("UI/Tutorial/miss_orb_lit");
        for (int i = 0; i < _missOrbs.Length; i += 1)
        {
            RectTransform orb = CreateUiObject($"MissOrb{i + 1}", _missPanel, typeof(Image));
            Image orbImage = orb.GetComponent<Image>();
            orbImage.sprite = _emptyOrbSprite;
            orbImage.preserveAspect = true;
            orbImage.raycastTarget = false;
            _missOrbs[i] = orbImage;
            SetAnchored(orb, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(150f + (i * 62f), 0f), new Vector2(48f, 48f));
        }

        _progressPanel = CreatePanel("ProgressPanel", root, ProgressPanelColor);
        _progressPanelImage = _progressPanel.GetComponent<Image>();
        ConfigureWoodPanel(_progressPanelImage);
        SetAnchored(_progressPanel, new Vector2(0.4f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(-13f, 0f));
        _remainingText = CreateText("RemainingText", _progressPanel, "\u3042\u30686\u53f0", 38f, 28f, FontStyles.Normal, TextAlignmentOptions.Left);
        _remainingText.font = LoadFont("UI/Tutorial/YomiyasuWide-Bold SDF");
        SetAnchored((RectTransform)_remainingText.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(35f, -19f), new Vector2(280f, 58f));

        _fractionText = CreateText("FractionText", _progressPanel, "0/6", 32f, 24f, FontStyles.Normal, TextAlignmentOptions.Right);
        _fractionText.font = LoadFont("UI/Tutorial/DotGothic16-Regular SDF");
        SetAnchored((RectTransform)_fractionText.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-35f, -19f), new Vector2(120f, 58f));

        RectTransform track = CreatePanel("ProgressTrack", _progressPanel, TrackColor);
        Stretch(track, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(35f, 19f), new Vector2(-35f, 48f));
        track.GetComponent<Image>().raycastTarget = false;

        RectTransform fill = CreatePanel("ProgressFill", track, FillColor);
        Stretch(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        _progressFillImage = fill.GetComponent<Image>();
        _progressFillImage.type = Image.Type.Filled;
        _progressFillImage.fillMethod = Image.FillMethod.Horizontal;
        _progressFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        _progressFillImage.raycastTarget = false;
    }

    private static void ConfigureWoodPanel(Image image)
    {
        image.sprite = Resources.Load<Sprite>("UI/Tutorial/hud_wood_panel");
        image.type = Image.Type.Sliced;
        image.color = Color.white;
    }

    private static TMP_FontAsset LoadFont(string path)
    {
        return Resources.Load<TMP_FontAsset>(path) ?? TMP_Settings.defaultFontAsset;
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new("StageProgressHudCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateUiObject(name, parent, typeof(Image));
        Image image = rect.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float fontSizeMax, float fontSizeMin, FontStyles fontStyle, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateUiObject(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSizeMax;
        text.fontSizeMax = fontSizeMax;
        text.fontSizeMin = fontSizeMin;
        text.enableAutoSizing = true;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = TextColor;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static RectTransform CreateUiObject(string name, Transform parent, params System.Type[] components)
    {
        System.Type[] allComponents = new System.Type[components.Length + 2];
        allComponents[0] = typeof(RectTransform);
        allComponents[1] = typeof(CanvasRenderer);
        for (int i = 0; i < components.Length; i += 1)
        {
            allComponents[i + 2] = components[i];
        }

        GameObject gameObject = new(name, allComponents);
        gameObject.layer = LayerMask.NameToLayer("UI");
        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }

        return (RectTransform)gameObject.transform;
    }

    private static void SetAnchored(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localScale = Vector3.one;
    }

    private static void AddShadow(GameObject gameObject, Color color, Vector2 distance)
    {
        Shadow shadow = gameObject.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = gameObject.AddComponent<Shadow>();
        }

        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }
}
