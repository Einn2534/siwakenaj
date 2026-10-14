using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Offline policy reader, with an explicit link to the published copy.</summary>
public sealed class PrivacyPolicyPanel : MonoBehaviour
{
    public const string PolicyUrl = "https://einn2534.github.io/siwakenja-privacy/";
    private GameObject _reader;
    private ScrollRect _scroll;
    private TMP_FontAsset _font;
    private GameObject _adPrivacy;
    private string _closeCaption = "設定にもどる";

    public static PrivacyPolicyPanel CreateReader(Transform parent, TMP_FontAsset font)
    {
        var panel = parent.GetComponent<PrivacyPolicyPanel>();
        if (panel != null) return panel;
        panel = parent.gameObject.AddComponent<PrivacyPolicyPanel>();
        panel._font = font;
        panel._closeCaption = "購入画面にもどる";
        return panel;
    }

    public static void Install(Transform settings)
    {
        if (settings.GetComponent<PrivacyPolicyPanel>() != null) return;
        var panel = settings.gameObject.AddComponent<PrivacyPolicyPanel>();
        panel._font = settings.GetComponentInChildren<TMP_Text>(true)?.font;
        panel.AddButton("PrivacyPolicyButton", settings, "プライバシーポリシー", 175f, panel.Open);
    }

    public void Open()
    {
        if (_reader == null) BuildReader();
        bool privacyRequired = AdMobManager.Instance.PrivacyOptionsRequired;
        _adPrivacy.SetActive(privacyRequired);
        var viewRect = (RectTransform)_scroll.transform;
        viewRect.offsetMin = new Vector2(viewRect.offsetMin.x, privacyRequired ? 330 : 230);
        _reader.SetActive(true);
        _reader.transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);
        _scroll.StopMovement();
        _scroll.verticalNormalizedPosition = 1f;
    }

    public void Close()
    {
        if (_reader != null) _reader.SetActive(false);
    }

    private void OnDisable() => Close();

    private void BuildReader()
    {
        RectTransform root = Rect("PrivacyPolicyReader", transform);
        _reader = root.gameObject;
        Stretch(root, 0, 0, 0, 0);
        root.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.984f, 0.976f, 0.925f);
        var heading = Label("Heading", root, "プライバシーポリシー", 38);
        Stretch(heading.rectTransform, 32, 32, 32, 0);
        heading.rectTransform.anchorMin = new Vector2(0, 1);
        heading.rectTransform.offsetMin = new Vector2(32, -104);
        heading.alignment = TextAlignmentOptions.Center;

        RectTransform view = Rect("ScrollView", root);
        Stretch(view, 40, 40, 125, 230);
        _scroll = view.gameObject.AddComponent<ScrollRect>();
        _scroll.horizontal = false;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
        _scroll.scrollSensitivity = 45;
        RectTransform viewport = Rect("Viewport", view);
        Stretch(viewport, 0, 0, 0, 0);
        viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color = Color.white;
        viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
        RectTransform content = Rect("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = Vector2.zero;
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 24);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        TextAsset policy = Resources.Load<TextAsset>("Legal/privacy-policy-ja");
        var body = Label("PolicyText", content, policy != null ? policy.text : "本文を読み込めませんでした。下のボタンから公開ページをご確認ください。", 30);
        body.richText = false;
        body.textWrappingMode = TextWrappingModes.Normal;
        body.overflowMode = TextOverflowModes.Overflow;
        body.raycastTarget = false;
        _scroll.viewport = viewport;
        _scroll.content = content;
        AddButton("OpenPolicyWebsite", root, "Webで見る（通信が必要）", 130, () => Application.OpenURL(PolicyUrl));
        AddButton("ClosePolicy", root, _closeCaption, 30, Close);
        AddButton("AdPrivacyOptions", root, "広告のプライバシー設定", 230, () => AdMobManager.Instance.ShowPrivacyOptions());
        _adPrivacy = root.Find("AdPrivacyOptions").gameObject;
    }

    private void AddButton(string name, Transform parent, string caption, float bottom, UnityEngine.Events.UnityAction action)
    {
        var rect = Rect(name, parent);
        rect.anchorMin = new Vector2(0.08f, 0);
        rect.anchorMax = new Vector2(0.92f, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, bottom);
        rect.sizeDelta = new Vector2(0, 80);
        var face = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        face.color = new Color(0.34f, 0.20f, 0.46f);
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = face;
        button.onClick.AddListener(action);
        var label = Label("Label", rect, caption, 30);
        Stretch(label.rectTransform, 12, 12, 4, 4);
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }

    private TMP_Text Label(string name, Transform parent, string value, float size)
    {
        var label = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (_font != null) label.font = _font;
        label.text = value;
        label.fontSize = size;
        label.color = new Color(0.15f, 0.12f, 0.10f);
        return label;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }
}
