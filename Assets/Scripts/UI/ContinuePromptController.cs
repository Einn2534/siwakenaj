using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ContinuePromptController : MonoBehaviour
{
    private const float ReferenceWidth = 1080f;
    private const float ReferenceHeight = 1920f;

    private static readonly Color OverlayColor = new(0.075f, 0.045f, 0.10f, 0.72f);
    private static readonly Color PaperColor = new(1f, 0.955f, 0.84f, 1f);
    private static readonly Color ShadowColor = new(0.055f, 0.025f, 0.07f, 0.65f);
    private static readonly Color TextColor = new(0.24f, 0.14f, 0.12f, 1f);
    private static readonly Color MutedTextColor = new(0.43f, 0.32f, 0.27f, 1f);
    private static readonly Color PurpleColor = new(0.39f, 0.23f, 0.49f, 1f);
    private static readonly Color GoldColor = new(0.73f, 0.51f, 0.24f, 1f);

    private GameObject _root;
    private Button _continueButton;
    private Button _giveUpButton;
    private TMP_Text _scoreText;
    private TMP_Text _statusText;
    private TMP_FontAsset _font;
    private Action _continueRequested;
    private Action _giveUpRequested;

    public static ContinuePromptController EnsureInstalled()
    {
        ContinuePromptController controller = FindAnyObjectByType<ContinuePromptController>(FindObjectsInactive.Include);
        if (controller != null)
        {
            return controller;
        }

        GameObject controllerObject = new("ContinuePromptController");
        return controllerObject.AddComponent<ContinuePromptController>();
    }

    private void Awake()
    {
        EventSystemInputModuleUtility.EnsureCompatibleEventSystem();
        BuildInterface();
        Hide();
    }

    private void OnDestroy()
    {
        ClearListeners();
    }

    public void Show(int score, Action continueRequested, Action giveUpRequested)
    {
        _continueRequested = continueRequested;
        _giveUpRequested = giveUpRequested;

        if (_scoreText != null)
        {
            _scoreText.text = score.ToString("N0");
        }

        SetStatusText("広告を最後まで見て、続きから再開");
        SetButtonsInteractable(true);

        if (_root != null)
        {
            _root.SetActive(true);
        }

        if (EventSystem.current != null && _continueButton != null)
        {
            EventSystem.current.SetSelectedGameObject(_continueButton.gameObject);
        }
    }

    public void Hide()
    {
        if (_root != null)
        {
            _root.SetActive(false);
        }

        _continueRequested = null;
        _giveUpRequested = null;
    }

    public void ShowAdWaiting()
    {
        SetStatusText("\u5e83\u544a\u3092\u8868\u793a\u4e2d...");
        SetButtonsInteractable(false);
    }

    public void ShowAdUnavailable()
    {
        SetStatusText("\u5e83\u544a\u3092\u5b8c\u4e86\u3067\u304d\u307e\u305b\u3093\u3067\u3057\u305f");
        SetButtonsInteractable(false);
    }

    private void HandleContinuePressed()
    {
        _continueRequested?.Invoke();
    }

    private void HandleGiveUpPressed()
    {
        _giveUpRequested?.Invoke();
    }

    private void BuildInterface()
    {
        if (_root != null)
        {
            return;
        }

        Canvas canvas = CreateCanvas();
        canvas.transform.SetParent(transform, false);
        _font = Resources.Load<TMP_FontAsset>("Fonts/NotoSansCJKjp-Regular SDF");
        RectTransform overlay = CreatePanel("ContinueOverlay", canvas.transform, OverlayColor);
        Stretch(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        overlay.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
        _root = overlay.gameObject;

        RectTransform safeArea = CreateUiObject("SafeArea", overlay);
        Stretch(safeArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        safeArea.gameObject.AddComponent<SafeAreaFitter>();

        Sprite wood = Resources.Load<Sprite>("UI/Tutorial/hud_wood_panel");
        Sprite paper = Resources.Load<Sprite>("UI/Tutorial/speech_panel");
        Sprite primary = Resources.Load<Sprite>("UI/Pause/ui_pause_primary_button");
        Sprite secondary = Resources.Load<Sprite>("UI/Pause/ui_pause_secondary_button");
        Sprite powa = Resources.Load<Sprite>("UI/Pause/ui_pause_powa");
        Sprite star = Resources.Load<Sprite>("UI/Tutorial/star_filled");

        RectTransform shadow = CreateDecoration("DialogShadow", safeArea, wood, ShadowColor);
        SetAnchored(shadow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -38f), new Vector2(856f, 996f));

        RectTransform dialog = CreateDecoration("Dialog", safeArea, wood, Color.white);
        SetAnchored(dialog, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(840f, 980f));
        RectTransform parchment = CreateDecoration("Parchment", dialog, paper, PaperColor);
        Stretch(parchment, Vector2.zero, Vector2.one, new Vector2(22f, 22f), new Vector2(-22f, -22f));

        RectTransform character = CreateDecoration("Powa", dialog, powa, Color.white, true);
        PlaceFromTop(character, 0f, 0f, 250f, 250f);
        character.GetComponent<UnityEngine.UI.Image>().preserveAspect = true;
        RectTransform leftStar = CreateDecoration("LeftStar", dialog, star, Color.white, true);
        PlaceFromTop(leftStar, -176f, 20f, 48f, 48f);
        leftStar.localRotation = Quaternion.Euler(0f, 0f, 16f);
        RectTransform rightStar = CreateDecoration("RightStar", dialog, star, Color.white, true);
        PlaceFromTop(rightStar, 174f, 65f, 34f, 34f);
        rightStar.localRotation = Quaternion.Euler(0f, 0f, -12f);

        TMP_Text titleText = CreateText("Title", dialog, "CONTINUE", 25f, 25f, FontStyles.Bold, PurpleColor, TextAlignmentOptions.Center);
        titleText.characterSpacing = 7f;
        PlaceFromTop(titleText.rectTransform, 0f, 155f, 580f, 44f);
        TMP_Text messageText = CreateText("Message", dialog, "まだ、つづけられるよ！", 49f, 40f, FontStyles.Bold, TextColor, TextAlignmentOptions.Center);
        PlaceFromTop(messageText.rectTransform, 0f, 219f, 710f, 84f);
        TMP_Text encouragement = CreateText("Encouragement", dialog, "ポワと一緒に、もうひとがんばり。", 28f, 26f, FontStyles.Normal, MutedTextColor, TextAlignmentOptions.Center);
        PlaceFromTop(encouragement.rectTransform, 0f, 287f, 680f, 48f);

        RectTransform scoreWell = CreateDecoration("ScoreWell", dialog, paper, new Color(0.93f, 0.84f, 0.69f, 1f));
        PlaceFromTop(scoreWell, 0f, 419f, 600f, 170f);
        scoreWell.GetComponent<UnityEngine.UI.Image>().pixelsPerUnitMultiplier = 3f;
        TMP_Text scoreLabel = CreateText("ScoreLabel", dialog, "今回のスコア", 26f, 26f, FontStyles.Normal, MutedTextColor, TextAlignmentOptions.Center);
        PlaceFromTop(scoreLabel.rectTransform, 0f, 371f, 500f, 42f);
        _scoreText = CreateText("Score", dialog, "0", 78f, 40f, FontStyles.Bold, PurpleColor, TextAlignmentOptions.Center);
        _scoreText.textWrappingMode = TextWrappingModes.NoWrap;
        PlaceFromTop(_scoreText.rectTransform, 0f, 446f, 510f, 100f);

        _statusText = CreateText("Status", dialog, string.Empty, 29f, 26f, FontStyles.Normal, MutedTextColor, TextAlignmentOptions.Center);
        PlaceFromTop(_statusText.rectTransform, 0f, 557f, 692f, 70f);

        RectTransform buttonShadow = CreateDecoration("ContinueButtonShadow", dialog, primary, ShadowColor, true);
        PlaceFromTop(buttonShadow, 0f, 688f, 684f, 150f);
        _continueButton = CreateButton("ContinueButton", dialog, "広告を見て復活", primary, Color.white, TextColor, new Vector2(0f, -677f), new Vector2(684f, 150f));
        TMP_Text rewardLabel = _continueButton.GetComponentInChildren<TMP_Text>();
        rewardLabel.fontStyle = FontStyles.Bold;
        rewardLabel.rectTransform.offsetMin = new Vector2(122f, 16f);
        rewardLabel.rectTransform.offsetMax = new Vector2(-32f, -16f);

        RectTransform adBadge = CreateDecoration("AdBadge", _continueButton.transform, paper, new Color(0.40f, 0.26f, 0.36f, 1f));
        SetAnchored(adBadge, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(91f, 2f), new Vector2(72f, 54f));
        adBadge.GetComponent<UnityEngine.UI.Image>().pixelsPerUnitMultiplier = 3f;
        TMP_Text adLabel = CreateText("AdLabel", adBadge, "AD", 24f, 24f, FontStyles.Bold, PaperColor, TextAlignmentOptions.Center);
        Stretch(adLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TMP_Text limitText = CreateText("ContinueLimit", dialog, "復活できるのは1プレイに1回", 25f, 24f, FontStyles.Normal, MutedTextColor, TextAlignmentOptions.Center);
        PlaceFromTop(limitText.rectTransform, 0f, 789f, 620f, 42f);
        _giveUpButton = CreateButton("GiveUpButton", dialog, "あきらめる", secondary, new Color(1f, 0.96f, 0.89f, 1f), TextColor, new Vector2(0f, -877f), new Vector2(520f, 104f));

        CreateRule(dialog, -217f);
        CreateRule(dialog, 217f);
        _continueButton.onClick.AddListener(HandleContinuePressed);
        _giveUpButton.onClick.AddListener(HandleGiveUpPressed);
    }

    private static void CreateRule(Transform parent, float x)
    {
        RectTransform rule = CreatePanel("TitleRule", parent, GoldColor);
        PlaceFromTop(rule, x, 155f, 100f, 2f);
    }

    private static RectTransform CreateDecoration(string name, Transform parent, Sprite sprite, Color color, bool simple = false)
    {
        RectTransform rect = CreatePanel(name, parent, color);
        UnityEngine.UI.Image image = rect.GetComponent<UnityEngine.UI.Image>();
        image.sprite = sprite;
        image.type = !simple && sprite != null && sprite.border.sqrMagnitude > 0f
            ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
        return rect;
    }

    private static void PlaceFromTop(RectTransform rect, float x, float y, float width, float height)
    {
        SetAnchored(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(x, -y), new Vector2(width, height));
    }

    private void ClearListeners()
    {
        if (_continueButton != null)
        {
            _continueButton.onClick.RemoveListener(HandleContinuePressed);
        }

        if (_giveUpButton != null)
        {
            _giveUpButton.onClick.RemoveListener(HandleGiveUpPressed);
        }
    }

    private void SetStatusText(string value)
    {
        if (_statusText != null)
        {
            _statusText.text = value;
        }
    }

    private void SetButtonsInteractable(bool isInteractable)
    {
        if (_continueButton != null)
        {
            _continueButton.interactable = isInteractable;
        }

        if (_giveUpButton != null)
        {
            _giveUpButton.interactable = isInteractable;
        }
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new("ContinuePromptCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 130;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private Button CreateButton(string name, Transform parent, string label, Sprite sprite, Color backgroundColor, Color textColor, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreatePanel(name, parent, backgroundColor, typeof(Button));
        SetAnchored(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), position, size);

        Button button = rect.GetComponent<Button>();
        Image image = rect.GetComponent<Image>();
        image.sprite = sprite;
        // The baked capsule highlights cross the sprite's slice borders.
        // Scale the complete artwork so short buttons do not show seams.
        image.type = Image.Type.Simple;
        image.raycastTarget = true;
        button.targetGraphic = image;
        ApplyButtonColors(button);

        TMP_Text labelText = CreateText("Label", rect, label, 42f, 28f, FontStyles.Normal, textColor, TextAlignmentOptions.Center);
        Stretch((RectTransform)labelText.transform, Vector2.zero, Vector2.one, new Vector2(36f, 14f), new Vector2(-36f, -14f));
        labelText.textWrappingMode = TextWrappingModes.NoWrap;
        return button;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color, params Type[] extraComponents)
    {
        Type[] components = new Type[extraComponents.Length + 1];
        components[0] = typeof(Image);
        for (int i = 0; i < extraComponents.Length; i += 1)
        {
            components[i + 1] = extraComponents[i];
        }

        RectTransform rect = CreateUiObject(name, parent, components);
        Image image = rect.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private TMP_Text CreateText(string name, Transform parent, string value, float fontSizeMax, float fontSizeMin, FontStyles fontStyle, Color color, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateUiObject(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = _font != null ? _font : TMP_Settings.defaultFontAsset;
        text.fontSize = fontSizeMax;
        text.fontSizeMax = fontSizeMax;
        text.fontSizeMin = fontSizeMin;
        text.enableAutoSizing = true;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.richText = true;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private static RectTransform CreateUiObject(string name, Transform parent, params Type[] components)
    {
        Type[] allComponents = new Type[components.Length + 2];
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

    private static void ApplyButtonColors(Button button)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.selectedColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.82f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.42f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
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
}
