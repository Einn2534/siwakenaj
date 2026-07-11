using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class HowToOverlayController : MonoBehaviour
{
    private const string MainSceneName = "Main";

    private static readonly Color TutorialButtonColor = new(0.96f, 0.98f, 1f, 1f);
    private static readonly Color TutorialTextColor = new(0.08f, 0.105f, 0.13f, 1f);

    [SerializeField, FormerlySerializedAs("overlayPanel")]
    private GameObject _overlayPanel;

    [SerializeField, FormerlySerializedAs("closeButton")]
    private Button _closeButton;

    [SerializeField]
    private Button[] _extraCloseButtons;

    private Button _tutorialReplayButton;

    private void Start()
    {
        AddCloseListener(_closeButton);

        if (_extraCloseButtons != null)
        {
            foreach (Button closeButton in _extraCloseButtons)
            {
                AddCloseListener(closeButton);
            }
        }

        RefreshInstructionText();
        EnsureTutorialReplayButton();

        if (!SaveService.GetHowToShown())
        {
            ShowOverlay();
        }
        else
        {
            SetOverlayActive(false);
        }
    }

    private void OnDestroy()
    {
        RemoveCloseListener(_closeButton);
        RemoveTutorialReplayListener();

        if (_extraCloseButtons == null)
        {
            return;
        }

        foreach (Button closeButton in _extraCloseButtons)
        {
            RemoveCloseListener(closeButton);
        }
    }

    public void ReplayTutorial()
    {
        SetOverlayActive(false);
        SaveService.SetHowToShown(true);
        SaveService.Save();
        StageSelectionService.SelectStage(TutorialLaunchService.TutorialStageNumber);
        TutorialLaunchService.RequestReplay();
        SceneManager.LoadScene(MainSceneName);
    }

    public void ShowOverlay()
    {
        SetOverlayActive(true);
    }

    public void CloseOverlay()
    {
        SetOverlayActive(false);
        SaveService.SetHowToShown(true);
        SaveService.Save();
    }

    private void SetOverlayActive(bool isActive)
    {
        if (_overlayPanel != null)
        {
            _overlayPanel.SetActive(isActive);
        }
    }

    private void RefreshInstructionText()
    {
        if (_overlayPanel == null)
        {
            return;
        }

        TMP_Text[] texts = _overlayPanel.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text != null && text.text == "\u8eca\u3092\u898b\u3066\u3001\u540c\u3058\u30ec\u30fc\u30f3\u306e\u30dc\u30bf\u30f3\u3092\u30bf\u30c3\u30d7")
            {
                text.text = "\u6765\u305f\u8eca\u3068\u540c\u3058\u30dc\u30bf\u30f3\u3092\u62bc\u305d\u3046";
            }
        }
    }

    private void EnsureTutorialReplayButton()
    {
        if (_overlayPanel == null)
        {
            return;
        }

        RectTransform panelRect = _overlayPanel.transform as RectTransform;
        if (panelRect == null)
        {
            return;
        }

        Transform existing = panelRect.Find("TutorialReplayButton");
        if (existing != null && existing.TryGetComponent(out Button existingButton))
        {
            ArrangeCloseButtonForReplay(panelRect);
            _tutorialReplayButton = existingButton;
            _tutorialReplayButton.onClick.RemoveListener(ReplayTutorial);
            _tutorialReplayButton.onClick.AddListener(ReplayTutorial);
            return;
        }

        ArrangeCloseButtonForReplay(panelRect);
        RectTransform buttonRect = CreatePanel("TutorialReplayButton", panelRect, TutorialButtonColor, typeof(Button));
        SetAnchored(buttonRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(210f, 34f), new Vector2(360f, 136f));

        _tutorialReplayButton = buttonRect.GetComponent<Button>();
        _tutorialReplayButton.targetGraphic = buttonRect.GetComponent<Image>();
        ApplyButtonColors(_tutorialReplayButton);
        _tutorialReplayButton.onClick.AddListener(ReplayTutorial);

        TMP_Text label = CreateText("Label", buttonRect, "TUTORIAL", 42f, 24f, FontStyles.Bold, TextAlignmentOptions.Center, TutorialTextColor);
        Stretch((RectTransform)label.transform, Vector2.zero, Vector2.one, new Vector2(34f, 12f), new Vector2(-34f, -12f));
        label.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private void ArrangeCloseButtonForReplay(RectTransform panelRect)
    {
        if (_closeButton == null || _closeButton.transform is not RectTransform closeRect || panelRect == null)
        {
            return;
        }

        if (!closeRect.IsChildOf(panelRect))
        {
            return;
        }

        SetAnchored(closeRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-210f, 34f), new Vector2(360f, 136f));
    }

    private void RemoveTutorialReplayListener()
    {
        if (_tutorialReplayButton != null)
        {
            _tutorialReplayButton.onClick.RemoveListener(ReplayTutorial);
        }
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color, params System.Type[] extraComponents)
    {
        System.Type[] components = new System.Type[extraComponents.Length + 1];
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

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string value,
        float fontSizeMax,
        float fontSizeMin,
        FontStyles fontStyle,
        TextAlignmentOptions alignment,
        Color color)
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
        text.textWrappingMode = TextWrappingModes.Normal;
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
        gameObject.layer = parent != null ? parent.gameObject.layer : LayerMask.NameToLayer("UI");
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

    private void AddCloseListener(Button closeButton)
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseOverlay);
        }
    }

    private void RemoveCloseListener(Button closeButton)
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseOverlay);
        }
    }
}
