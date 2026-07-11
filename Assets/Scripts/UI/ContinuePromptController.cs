using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ContinuePromptController : MonoBehaviour
{
    private const float ReferenceWidth = 1080f;
    private const float ReferenceHeight = 1920f;

    private static readonly Color OverlayColor = new(0.015f, 0.018f, 0.024f, 0.78f);
    private static readonly Color PanelColor = new(0.98f, 0.99f, 1f, 0.98f);
    private static readonly Color ShadowColor = new(0f, 0f, 0f, 0.32f);
    private static readonly Color TextColor = new(0.08f, 0.105f, 0.13f, 1f);
    private static readonly Color MutedTextColor = new(0.42f, 0.47f, 0.54f, 1f);
    private static readonly Color RewardColor = new(1f, 0.78f, 0.26f, 1f);
    private static readonly Color GiveUpColor = new(0.96f, 0.98f, 1f, 1f);
    private static readonly Color DangerColor = new(1f, 0.46f, 0.43f, 1f);

    private GameObject _root;
    private Button _continueButton;
    private Button _giveUpButton;
    private TMP_Text _scoreText;
    private TMP_Text _statusText;
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
            _scoreText.text = $"SCORE {score:N0}";
        }

        SetStatusText("\u5e83\u544a\u3092\u6700\u5f8c\u307e\u3067\u898b\u308b\u3068\u5fa9\u6d3b");
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
        RectTransform overlay = CreatePanel("ContinueOverlay", canvas.transform, OverlayColor);
        Stretch(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        _root = overlay.gameObject;

        RectTransform shadow = CreatePanel("DialogShadow", overlay, ShadowColor);
        SetAnchored(shadow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(840f, 680f));
        shadow.GetComponent<Image>().raycastTarget = false;

        RectTransform dialog = CreatePanel("Dialog", overlay, PanelColor);
        SetAnchored(dialog, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(840f, 680f));

        TMP_Text titleText = CreateText("Title", dialog, "CONTINUE?", 74f, 42f, FontStyles.Bold, TextColor, TextAlignmentOptions.Center);
        SetAnchored((RectTransform)titleText.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(720f, 96f));

        TMP_Text messageText = CreateText("Message", dialog, "\u30b3\u30f3\u30c6\u30a3\u30cb\u30e5\u30fc\u3059\u308b\uff1f", 46f, 30f, FontStyles.Bold, DangerColor, TextAlignmentOptions.Center);
        SetAnchored((RectTransform)messageText.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -166f), new Vector2(720f, 74f));

        _scoreText = CreateText("Score", dialog, "SCORE 0", 42f, 28f, FontStyles.Bold, TextColor, TextAlignmentOptions.Center);
        SetAnchored((RectTransform)_scoreText.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -248f), new Vector2(620f, 64f));

        _statusText = CreateText("Status", dialog, string.Empty, 30f, 22f, FontStyles.Normal, MutedTextColor, TextAlignmentOptions.Center);
        SetAnchored((RectTransform)_statusText.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -318f), new Vector2(680f, 64f));

        _continueButton = CreateButton("ContinueButton", dialog, "\u5e83\u544a\u3067\u5fa9\u6d3b", RewardColor, TextColor, new Vector2(0f, -458f), new Vector2(620f, 118f));
        _giveUpButton = CreateButton("GiveUpButton", dialog, "\u3042\u304d\u3089\u3081\u308b", GiveUpColor, TextColor, new Vector2(0f, -596f), new Vector2(620f, 96f));

        _continueButton.onClick.AddListener(HandleContinuePressed);
        _giveUpButton.onClick.AddListener(HandleGiveUpPressed);
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

    private static Button CreateButton(string name, Transform parent, string label, Color backgroundColor, Color textColor, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreatePanel(name, parent, backgroundColor, typeof(Button));
        SetAnchored(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), position, size);

        Button button = rect.GetComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        ApplyButtonColors(button);

        TMP_Text labelText = CreateText("Label", rect, label, 42f, 28f, FontStyles.Bold, textColor, TextAlignmentOptions.Center);
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
        image.raycastTarget = true;
        return rect;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float fontSizeMax, float fontSizeMin, FontStyles fontStyle, Color color, TextAlignmentOptions alignment)
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
