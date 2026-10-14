using TMPro;
using UnityEngine;

/// <summary>Offline purchase terms, with explicit links for refunds and support.</summary>
public sealed class PurchaseLegalPanel : MonoBehaviour
{
    public const string WebsiteUrl = "https://einn2534.github.io/siwakenja-privacy/purchase.html";
    public const string RefundUrl = "https://support.google.com/googleplay/answer/2479637?hl=ja";
    public const string SupportUrl = "mailto:kibundeseisakujo@gmail.com";
    private TMP_FontAsset _font;
    private GameObject _reader;
    private UnityEngine.UI.ScrollRect _scroll;

    public void Initialize(TMP_FontAsset font) => _font = font;

    public void Open()
    {
        if (_reader == null) Build();
        _reader.SetActive(true);
        _reader.transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);
        _scroll.StopMovement();
        _scroll.verticalNormalizedPosition = 1f;
    }

    public void Close() { if (_reader != null) _reader.SetActive(false); }
    private void OnDisable() => Close();

    private void Build()
    {
        var root = Rect("PurchaseLegalReader", transform);
        _reader = root.gameObject;
        Stretch(root, 0, 0, 0, 0);
        root.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.984f, 0.976f, 0.925f);
        var heading = Label("Heading", root, "購入条件・特商法表示", 36);
        heading.alignment = TextAlignmentOptions.Center;
        heading.rectTransform.anchorMin = new Vector2(0, 1);
        heading.rectTransform.anchorMax = Vector2.one;
        heading.rectTransform.offsetMin = new Vector2(24, -112);
        heading.rectTransform.offsetMax = new Vector2(-24, -24);

        var view = Rect("ScrollView", root);
        Stretch(view, 40, 40, 135, 365);
        _scroll = view.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        _scroll.horizontal = false;
        _scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        _scroll.scrollSensitivity = 45;
        var viewport = Rect("Viewport", view);
        Stretch(viewport, 0, 0, 0, 0);
        viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color = Color.white;
        viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
        var content = Rect("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = Vector2.zero;
        var layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 24);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        var terms = Resources.Load<TextAsset>("Legal/purchase-terms-ja");
        Label("LegalText", content, terms != null ? terms.text : "本文を読み込めませんでした。Webの購入条件をご確認ください。", 30);
        _scroll.content = content;
        _scroll.viewport = viewport;

        Button("WebsiteButton", root, "Webで購入条件を見る", 275, () => Application.OpenURL(WebsiteUrl));
        Button("RefundButton", root, "Google Playの返金案内", 190, () => Application.OpenURL(RefundUrl));
        Button("SupportButton", root, "お問い合わせ（メール）", 105, () => Application.OpenURL(SupportUrl));
        Button("CloseButton", root, "購入画面にもどる", 20, Close);
    }

    private void Button(string name, Transform parent, string caption, float bottom, UnityEngine.Events.UnityAction action)
    {
        var rect = Rect(name, parent);
        rect.anchorMin = new Vector2(0.06f, 0);
        rect.anchorMax = new Vector2(0.94f, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, bottom);
        rect.sizeDelta = new Vector2(0, 72);
        var face = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        face.color = new Color(0.34f, 0.20f, 0.46f);
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = face;
        button.onClick.AddListener(action);
        var label = Label("Label", rect, caption, 29);
        Stretch(label.rectTransform, 12, 12, 4, 4);
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 24;
        label.fontSizeMax = 29;
    }

    private TMP_Text Label(string name, Transform parent, string value, float size)
    {
        var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (_font != null) text.font = _font;
        text.text = value;
        text.fontSize = size;
        text.color = new Color(0.24f, 0.14f, 0.12f);
        text.richText = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = parent.gameObject.layer;
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
