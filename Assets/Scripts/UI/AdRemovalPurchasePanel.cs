using TMPro;
using UnityEngine;

/// <summary>Installs the same purchase entry in every existing settings panel.</summary>
public sealed class AdRemovalPurchasePanel : MonoBehaviour
{
    private static readonly Color Paper = new(0.984f, 0.976f, 0.925f);
    private static readonly Color Purple = new(0.34f, 0.20f, 0.46f);
    private static readonly Color Ink = new(0.24f, 0.14f, 0.12f);
    private TMP_FontAsset _font;
    private AdRemovalPurchaseService _service;
    private GameObject _sheet;
    private UnityEngine.UI.ScrollRect _scroll;
    private PurchaseLegalPanel _legal;
    private TMP_Text _entryLabel, _message, _price, _buyLabel, _closeLabel;
    private UnityEngine.UI.Button _buy, _restore, _retry;
    private System.Action _onClosed;

    public static void Install(Transform settings, float bottom = 275f)
    {
        if (settings.GetComponent<AdRemovalPurchasePanel>() != null) return;
        var panel = settings.gameObject.AddComponent<AdRemovalPurchasePanel>();
        panel._font = Resources.Load<TMP_FontAsset>("Fonts/NotoSansCJKjp-Regular SDF")
            ?? settings.GetComponentInChildren<TMP_Text>(true)?.font;
        RectTransform entry = Rect("AdRemovalButton", settings);
        entry.anchorMin = new Vector2(0.08f, 0);
        entry.anchorMax = new Vector2(0.92f, 0);
        entry.pivot = new Vector2(0.5f, 0);
        entry.anchoredPosition = new Vector2(0, bottom);
        entry.sizeDelta = new Vector2(0, 80);
        UnityEngine.UI.Button button = panel.Button(entry, "広告を削除", out panel._entryLabel);
        button.onClick.AddListener(panel.Open);
        panel.Refresh();
    }

    private void OnEnable()
    {
        _service = AdRemovalPurchaseService.Instance;
        _service.Changed += Refresh;
        AdRemovalEntitlement.Changed += OnEntitlementChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (_service != null) _service.Changed -= Refresh;
        AdRemovalEntitlement.Changed -= OnEntitlementChanged;
        Close();
    }

    private void OnEntitlementChanged(bool owned) => Refresh();

    public void Open()
    {
        _onClosed = null;
        ShowSheet();
    }

    public void OpenFromTitle(System.Action onClosed)
    {
        _onClosed = onClosed;
        ShowSheet();
    }

    private void ShowSheet()
    {
        if (_sheet == null) BuildSheet();
        _closeLabel.text = _onClosed != null ? "タイトルにもどる" : "設定にもどる";
        _sheet.SetActive(true);
        _sheet.transform.SetAsLastSibling();
        Refresh();
        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);
        _scroll.StopMovement();
        _scroll.verticalNormalizedPosition = 1f;
    }

    public void Close()
    {
        if (_legal != null) _legal.Close();
        if (_sheet != null) _sheet.SetActive(false);
        System.Action onClosed = _onClosed;
        _onClosed = null;
        onClosed?.Invoke();
    }

    private void Refresh()
    {
        bool owned = AdRemovalEntitlement.IsOwned;
        if (_entryLabel != null) _entryLabel.text = owned ? "広告削除：購入済み" : "広告を削除";
        if (_service == null || _sheet == null) return;
        _message.text = _service.Message;
        _price.text = owned ? "購入済み・追加の購入は不要です" : string.IsNullOrWhiteSpace(_service.LocalizedPrice)
            ? "価格はストアから取得します" : "買い切り価格（税込）：" + _service.LocalizedPrice;
        _buyLabel.text = owned ? "購入済み" : _service.State == AdRemovalStoreState.Deferred
            ? "承認待ち" : string.IsNullOrWhiteSpace(_service.LocalizedPrice)
            ? "購入手続きへ" : "購入手続きへ（" + _service.LocalizedPrice + "）";
        _buy.interactable = _service.CanPurchase;
        _restore.interactable = _service.CanRestore;
        _retry.gameObject.SetActive(!_service.IsBusy && (_service.State == AdRemovalStoreState.Unavailable
            || _service.State == AdRemovalStoreState.Failed));
    }

    private void BuildSheet()
    {
        RectTransform sheet = Rect("AdRemovalPurchaseSheet", transform);
        _sheet = sheet.gameObject;
        // Pause settings are shorter than title settings. Use the same full
        // purchase dialog inside their unmasked modal, without moving its rows.
        sheet.anchorMin = new Vector2(0, 0.5f);
        sheet.anchorMax = new Vector2(1, 0.5f);
        sheet.sizeDelta = new Vector2(0, Mathf.Max(1400f, ((RectTransform)transform).rect.height));
        sheet.gameObject.AddComponent<UnityEngine.UI.Image>().color = Paper;

        TMP_Text heading = Label("Heading", sheet, "広告を削除", 42);
        heading.rectTransform.anchorMin = new Vector2(0, 1);
        heading.rectTransform.anchorMax = Vector2.one;
        heading.rectTransform.pivot = new Vector2(0.5f, 1);
        heading.rectTransform.anchoredPosition = new Vector2(0, -40);
        heading.rectTransform.sizeDelta = new Vector2(-80, 80);
        heading.alignment = TextAlignmentOptions.Center;

        RectTransform view = Rect("PurchaseScrollView", sheet);
        Stretch(view, 45, 45, 145, 190);
        _scroll = view.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        _scroll.horizontal = false;
        _scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        _scroll.scrollSensitivity = 45;
        RectTransform viewport = Rect("Viewport", view);
        Stretch(viewport, 0, 0, 0, 0);
        viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color = Paper;
        viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
        RectTransform body = Rect("Body", viewport);
        body.anchorMin = new Vector2(0, 1);
        body.anchorMax = Vector2.one;
        body.pivot = new Vector2(0.5f, 1);
        body.sizeDelta = Vector2.zero;
        var layout = body.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 24);
        layout.spacing = 20;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperCenter;
        body.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        _scroll.viewport = viewport;
        _scroll.content = body;

        TMP_Text description = Label("Description", body,
            "強制広告と復活広告を非表示にします。\n広告削除1件の買い切り・自動更新なし。\n復活は1プレイに1回まで。\n復活できないおしごと・モードもあります。", 32);
        _price = Label("Price", body, "", 32);
        _price.alignment = TextAlignmentOptions.Center;
        Height(_price.gameObject, 70);
        Label("PurchaseConditions", body,
            "Google Playで購入時にお支払い。\n請求日は選んだ支払方法に従います。\n決済完了を確認後、直ちに反映します。\n通信料はお客様の負担です。\n\nお客様都合の返金は原則できません。\nGoogle Playの返金制度や法令に基づく\n返金は除きます。未提供・不具合は\nお問い合わせください。\n\n18歳未満の方は保護者の同意が必要です。\n購入前に下の条件をご確認ください。", 29);
        _legal = sheet.gameObject.AddComponent<PurchaseLegalPanel>();
        _legal.Initialize(_font);
        var privacy = PrivacyPolicyPanel.CreateReader(sheet, _font);
        RowButton("LegalButton", body, "購入条件・特商法表示", 86, out _).onClick.AddListener(_legal.Open);
        RowButton("PrivacyButton", body, "プライバシーポリシー", 86, out _).onClick.AddListener(privacy.Open);
        _message = Label("Status", body, "", 29);
        _message.alignment = TextAlignmentOptions.Center;

        _buy = RowButton("PurchaseButton", body, "広告を削除", 110, out _buyLabel);
        _buy.onClick.AddListener(() => _service.Purchase());
        _restore = RowButton("RestoreButton", body, "購入を復元", 96, out _);
        _restore.onClick.AddListener(() => _service.Restore());
        _retry = RowButton("ReconnectButton", body, "ストアに接続", 96, out _);
        _retry.onClick.AddListener(() => _service.Initialize());
        var hint = Label("ScrollHint", sheet, "上下にスクロールしてご確認ください", 24);
        hint.alignment = TextAlignmentOptions.Center;
        hint.rectTransform.anchorMin = new Vector2(0, 0);
        hint.rectTransform.anchorMax = new Vector2(1, 0);
        hint.rectTransform.pivot = new Vector2(0.5f, 0);
        hint.rectTransform.anchoredPosition = new Vector2(0, 142);
        hint.rectTransform.sizeDelta = new Vector2(-80, 38);
        RectTransform closeRect = Rect("CloseButton", sheet);
        closeRect.anchorMin = new Vector2(0, 0);
        closeRect.anchorMax = new Vector2(1, 0);
        closeRect.pivot = new Vector2(0.5f, 0);
        closeRect.anchoredPosition = new Vector2(0, 35);
        closeRect.sizeDelta = new Vector2(-100, 96);
        UnityEngine.UI.Button close = Button(closeRect, "設定にもどる", out _closeLabel);
        close.onClick.AddListener(Close);
        Refresh();
    }

    private UnityEngine.UI.Button RowButton(string name, Transform parent, string caption, float height, out TMP_Text label)
    {
        RectTransform rect = Rect(name, parent);
        Height(rect.gameObject, height);
        return Button(rect, caption, out label);
    }

    private UnityEngine.UI.Button Button(RectTransform rect, string caption, out TMP_Text label)
    {
        var face = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        face.color = Purple;
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = face;
        label = Label("Label", rect, caption, 32);
        Stretch(label.rectTransform, 16, 16, 6, 6);
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 24;
        label.fontSizeMax = 32;
        return button;
    }

    private TMP_Text Label(string name, Transform parent, string value, float size)
    {
        var label = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (_font != null) label.font = _font;
        label.text = value;
        label.fontSize = size;
        label.color = Ink;
        label.raycastTarget = false;
        label.richText = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }

    private static void Height(GameObject gameObject, float height)
    {
        var element = gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
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
