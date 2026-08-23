using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GimmickHudController : MonoBehaviour
{
    private const float ReferenceWidth = 1080f;
    private const float ReferenceHeight = 1920f;
    private const float MessageDurationSeconds = 1.35f;
    private const float RepairButtonBottomOffset = 406f;

    private static readonly Color InkColor = new(43f / 255f, 37f / 255f, 48f / 255f, 1f);
    private static readonly Color PaperColor = new(1f, 247f / 255f, 222f / 255f, 0.96f);
    private static readonly Color RepairColor = new(235f / 255f, 87f / 255f, 71f / 255f, 1f);
    private static readonly Color ComboColor = new(76f / 255f, 182f / 255f, 214f / 255f, 0.96f);
    private static readonly Color FeverColor = new(1f, 197f / 255f, 66f / 255f, 1f);

    private readonly HashSet<CarModifier> _shownModifierHints = new();

    private RectTransform _repairRoot;
    private UnityEngine.UI.Button _repairButton;
    private RepairButtonPointerDownForwarder _repairForwarder;
    private RectTransform _comboRoot;
    private UnityEngine.UI.Image _comboPanelImage;
    private UnityEngine.UI.Image _comboFillImage;
    private TMP_Text _comboText;
    private RectTransform _messageRoot;
    private UnityEngine.UI.Image _messageImage;
    private TMP_Text _messageText;
    private Coroutine _messageRoutine;
    private int _feverThreshold;

    public static GimmickHudController EnsureInstalled()
    {
        GimmickHudController existing = FindAnyObjectByType<GimmickHudController>(FindObjectsInactive.Include);
        if (existing != null)
        {
            return existing;
        }

        GameObject controllerObject = new("GimmickHudController");
        return controllerObject.AddComponent<GimmickHudController>();
    }

    public void Initialize(StageDefinition stageDefinition, LaneInputController laneInputController)
    {
        BuildInterface();
        StageDefinition safeStage = stageDefinition ?? StageDefinition.CreateFallback(1);
        _feverThreshold = Mathf.Max(0, safeStage.FeverComboThreshold);

        bool hasRepairCars = safeStage.BrokenChance > 0f;
        _repairRoot.gameObject.SetActive(hasRepairCars);
        _repairButton.interactable = hasRepairCars;
        _repairForwarder.Configure(laneInputController, _repairButton);

        _comboRoot.gameObject.SetActive(_feverThreshold > 0);
        _shownModifierHints.Clear();
        HideMessage();
        UpdateState(null);
    }

    public void SetGameplayActive(bool isActive)
    {
        if (_repairButton != null)
        {
            _repairButton.interactable = isActive && _repairRoot.gameObject.activeSelf;
        }
    }

    public void UpdateState(ScoreState state)
    {
        if (_comboRoot == null || _feverThreshold <= 0)
        {
            return;
        }

        int combo = state != null ? state.ComboCount : 0;
        bool isFever = state != null && state.IsFeverActive;
        _comboText.text = isFever
            ? $"FEVER  {combo} COMBO  x2"
            : $"COMBO  {combo}/{_feverThreshold}";
        _comboFillImage.fillAmount = isFever
            ? 1f
            : Mathf.Clamp01(combo / (float)Mathf.Max(1, _feverThreshold));
        _comboFillImage.color = isFever ? FeverColor : ComboColor;
        _comboPanelImage.color = isFever ? new Color(1f, 0.84f, 0.38f, 0.96f) : PaperColor;
    }

    public void ShowModifierHint(CarModifier modifier)
    {
        if (modifier == CarModifier.Normal || !_shownModifierHints.Add(modifier))
        {
            return;
        }

        switch (modifier)
        {
            case CarModifier.Express:
                ShowMessage("急送車！  「!」は得点×2", FeverColor);
                break;
            case CarModifier.Covered:
                ShowMessage("覆面車！  「?」が消えてから仕分け", ComboColor);
                break;
            case CarModifier.Broken:
                ShowMessage("故障車！  「X」は整備へ送れ", RepairColor);
                break;
        }
    }

    public void ShowRushWarning()
    {
        ShowMessage("まもなく大名行列！", RepairColor, 0.9f);
    }

    public void ShowRushStarted()
    {
        ShowMessage("大名行列！  3台連続", FeverColor, 0.9f);
    }

    public void StopEffects()
    {
        if (_messageRoutine != null)
        {
            StopCoroutine(_messageRoutine);
            _messageRoutine = null;
        }

        HideMessage();
    }

    private void ShowMessage(string message, Color accentColor, float duration = MessageDurationSeconds)
    {
        BuildInterface();
        if (_messageRoutine != null)
        {
            StopCoroutine(_messageRoutine);
        }

        _messageRoutine = StartCoroutine(ShowMessageRoutine(message, accentColor, duration));
    }

    private IEnumerator ShowMessageRoutine(string message, Color accentColor, float duration)
    {
        _messageText.text = message;
        _messageImage.color = accentColor;
        _messageRoot.localScale = Vector3.one * 0.88f;
        _messageRoot.gameObject.SetActive(true);

        float introElapsed = 0f;
        while (introElapsed < 0.14f)
        {
            introElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(introElapsed / 0.14f);
            _messageRoot.localScale = Vector3.one * Mathf.Lerp(0.88f, 1f, t);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, duration));
        HideMessage();
        _messageRoutine = null;
    }

    private void HideMessage()
    {
        if (_messageRoot != null)
        {
            _messageRoot.gameObject.SetActive(false);
            _messageRoot.localScale = Vector3.one;
        }
    }

    private void BuildInterface()
    {
        if (_repairRoot != null)
        {
            return;
        }

        GameObject canvasObject = new(
            "GimmickHudCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler),
            typeof(UnityEngine.UI.GraphicRaycaster));
        canvasObject.layer = LayerMask.NameToLayer("UI");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        Camera gameplayCamera = Camera.main;
        canvas.renderMode = gameplayCamera != null
            ? RenderMode.ScreenSpaceCamera
            : RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = gameplayCamera;
        canvas.planeDistance = 90f;
        canvas.sortingOrder = 42;

        UnityEngine.UI.CanvasScaler scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform safeRoot = CreateUiObject("SafeAreaRoot", canvas.transform);
        Stretch(safeRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        safeRoot.gameObject.AddComponent<SafeAreaFitter>();

        BuildRepairButton(safeRoot);
        BuildComboPanel(safeRoot);
        BuildMessagePanel(safeRoot);
    }

    private void BuildRepairButton(RectTransform parent)
    {
        _repairRoot = CreatePanel("RepairButton", parent, RepairColor, true);
        SetAnchored(
            _repairRoot,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, RepairButtonBottomOffset),
            new Vector2(470f, 112f));

        _repairButton = _repairRoot.gameObject.AddComponent<UnityEngine.UI.Button>();
        _repairButton.targetGraphic = _repairRoot.GetComponent<UnityEngine.UI.Image>();
        UnityEngine.UI.ColorBlock colors = _repairButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.94f, 0.88f, 1f);
        colors.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
        colors.colorMultiplier = 1f;
        _repairButton.colors = colors;

        TMP_Text label = CreateText("Label", _repairRoot, "X  整備へ送る", 44f, 30f, TextAlignmentOptions.Center, Color.white);
        Stretch((RectTransform)label.transform, Vector2.zero, Vector2.one, new Vector2(20f, 10f), new Vector2(-20f, -10f));
        AddOutline(_repairRoot.gameObject, InkColor, new Vector2(7f, 7f));

        _repairForwarder = _repairRoot.gameObject.AddComponent<RepairButtonPointerDownForwarder>();
    }

    private void BuildComboPanel(RectTransform parent)
    {
        _comboRoot = CreatePanel("ComboPanel", parent, PaperColor, false);
        _comboPanelImage = _comboRoot.GetComponent<UnityEngine.UI.Image>();
        SetAnchored(
            _comboRoot,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -192f),
            new Vector2(510f, 82f));
        AddOutline(_comboRoot.gameObject, InkColor, new Vector2(5f, 5f));

        _comboText = CreateText("ComboText", _comboRoot, "COMBO  0/8", 31f, 23f, TextAlignmentOptions.Center, InkColor);
        SetAnchored((RectTransform)_comboText.transform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), new Vector2(-30f, -8f));

        RectTransform track = CreatePanel("ComboTrack", _comboRoot, new Color(0.16f, 0.14f, 0.19f, 0.28f), false);
        SetAnchored(track, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 11f), new Vector2(-34f, 15f));

        RectTransform fill = CreatePanel("ComboFill", track, ComboColor, false);
        Stretch(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        _comboFillImage = fill.GetComponent<UnityEngine.UI.Image>();
        _comboFillImage.type = UnityEngine.UI.Image.Type.Filled;
        _comboFillImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        _comboFillImage.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
        _comboFillImage.fillAmount = 0f;
    }

    private void BuildMessagePanel(RectTransform parent)
    {
        _messageRoot = CreatePanel("GimmickMessage", parent, FeverColor, false);
        _messageImage = _messageRoot.GetComponent<UnityEngine.UI.Image>();
        SetAnchored(
            _messageRoot,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 238f),
            new Vector2(830f, 112f));
        AddOutline(_messageRoot.gameObject, InkColor, new Vector2(8f, 8f));

        _messageText = CreateText("MessageText", _messageRoot, string.Empty, 43f, 28f, TextAlignmentOptions.Center, InkColor);
        Stretch((RectTransform)_messageText.transform, Vector2.zero, Vector2.one, new Vector2(24f, 12f), new Vector2(-24f, -12f));
        _messageRoot.gameObject.SetActive(false);
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color, bool raycastTarget)
    {
        RectTransform rect = CreateUiObject(name, parent, typeof(UnityEngine.UI.Image));
        UnityEngine.UI.Image image = rect.GetComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return rect;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float fontSizeMax, float fontSizeMin, TextAlignmentOptions alignment, Color color)
    {
        RectTransform rect = CreateUiObject(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = Resources.Load<TMP_FontAsset>("UI/Tutorial/YomiyasuWide-Bold SDF") ?? TMP_Settings.defaultFontAsset;
        text.fontSize = fontSizeMax;
        text.fontSizeMax = fontSizeMax;
        text.fontSizeMin = fontSizeMin;
        text.enableAutoSizing = true;
        text.fontStyle = FontStyles.Normal;
        text.alignment = alignment;
        text.color = color;
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
        gameObject.transform.SetParent(parent, false);
        return (RectTransform)gameObject.transform;
    }

    private static void SetAnchored(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
    }

    private static void AddOutline(GameObject target, Color color, Vector2 distance)
    {
        UnityEngine.UI.Outline outline = target.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }
}
