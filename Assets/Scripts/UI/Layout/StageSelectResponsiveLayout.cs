using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class StageSelectResponsiveLayout : MonoBehaviour
{
    private const int CompactMaxScreenWidth = 720;
    private const int CompactMaxScreenHeight = 1280;
    private const float CompactTallAspect = 1.66f;

    private const float CompactHeaderTopInset = 62f;
    private const float CompactHeaderHeight = 94f;
    private const float CompactHeaderFontSize = 62f;
    private const float CompactHeaderFontSizeMin = 42f;
    private const float CompactHeaderFontSizeMax = 64f;
    private const float CompactScrollTopInset = 190f;
    private const float CompactScrollBottomInset = 214f;
    private const float CompactButtonInset = 44f;
    private const float CompactButtonSize = 156f;
    private const float CompactCardWidthRatio = 0.90f;
    private const float CompactCardHeightRatio = 0.82f;
    private const float CompactCardMinWidth = 560f;
    private const float CompactCardMaxWidth = 720f;
    private const float CompactCardSpacing = 28f;

    [SerializeField]
    private RectTransform _safeAreaRect;

    [SerializeField]
    private RectTransform _headerRect;

    [SerializeField]
    private TMP_Text _headerText;

    [SerializeField]
    private RectTransform _scrollViewRect;

    [SerializeField]
    private RectTransform _backButtonRect;

    [SerializeField]
    private LayoutElement _backButtonLayout;

    [SerializeField]
    private RectTransform _playButtonRect;

    [SerializeField]
    private LayoutElement _playButtonLayout;

    [SerializeField]
    private HorizontalLayoutGroup _contentLayoutGroup;

    [SerializeField]
    private PreferredSizeByParentWidth[] _cardSizers;

    [SerializeField]
    private SwipeSnapController _swipeSnapController;

    private bool _defaultsCached;
    private bool _applyQueued;
    private HeaderTextState _defaultHeaderTextState;
    private RectTransformState _defaultHeaderRectState;
    private RectTransformState _defaultBackButtonRectState;
    private LayoutElementState _defaultBackButtonLayoutState;
    private RectTransformState _defaultPlayButtonRectState;
    private LayoutElementState _defaultPlayButtonLayoutState;
    private Vector2 _defaultScrollOffsetMin;
    private Vector2 _defaultScrollOffsetMax;
    private float _defaultContentSpacing;
    private CardLayoutState[] _defaultCardStates;
    private CardLayoutState _fallbackCardState;
    private bool _hasFallbackCardState;
    private Vector2Int _lastAppliedScreenSize = new Vector2Int(-1, -1);
    private Rect _lastAppliedSafeArea = Rect.zero;
    private Vector2 _lastAppliedSafeAreaRectSize = new Vector2(-1f, -1f);
    private bool _lastAppliedCompact;

    private void OnEnable()
    {
        ResolveReferences();
        CacheDefaultsIfNeeded();
        QueueApply();
    }

    private void Start()
    {
        QueueApply();
    }

    private void OnRectTransformDimensionsChange()
    {
        QueueApply();
    }

    private void LateUpdate()
    {
        ResolveReferences();
        CacheDefaultsIfNeeded();

        if (HasEnvironmentChanged())
        {
            QueueApply();
        }

        if (!_applyQueued)
        {
            return;
        }

        ApplyLayout();
        _applyQueued = false;
    }

    private void QueueApply()
    {
        _applyQueued = true;
    }

    private void ApplyLayout()
    {
        bool isCompact = IsCompactScreen();

        if (isCompact)
        {
            ApplyCompactLayout();
        }
        else
        {
            ApplyRegularLayout();
        }

        Canvas.ForceUpdateCanvases();
        RefreshCards();
        RebuildLayout();
        RecenterSelectedCard();

        _lastAppliedCompact = isCompact;
        _lastAppliedScreenSize = new Vector2Int(Screen.width, Screen.height);
        _lastAppliedSafeArea = Screen.safeArea;
        _lastAppliedSafeAreaRectSize = _safeAreaRect != null
            ? _safeAreaRect.rect.size
            : Vector2.zero;
    }

    private void ApplyCompactLayout()
    {
        ApplyHeaderLayout(
            CompactHeaderTopInset,
            CompactHeaderHeight,
            true,
            CompactHeaderFontSize,
            CompactHeaderFontSizeMin,
            CompactHeaderFontSizeMax);
        ApplyScrollInsets(CompactScrollTopInset, CompactScrollBottomInset);
        ApplyButtonLayout(_backButtonRect, _backButtonLayout, new Vector2(CompactButtonInset, CompactButtonInset), CompactButtonSize);
        ApplyButtonLayout(_playButtonRect, _playButtonLayout, new Vector2(-CompactButtonInset, CompactButtonInset), CompactButtonSize);

        if (_contentLayoutGroup != null)
        {
            _contentLayoutGroup.spacing = CompactCardSpacing;
        }

        if (_cardSizers == null)
        {
            return;
        }

        foreach (PreferredSizeByParentWidth cardSizer in _cardSizers)
        {
            if (cardSizer == null)
            {
                continue;
            }

            cardSizer.widthRatio = CompactCardWidthRatio;
            cardSizer.heightRatio = CompactCardHeightRatio;
            cardSizer.minWidth = CompactCardMinWidth;
            cardSizer.maxWidth = CompactCardMaxWidth;
        }
    }

    private void ApplyRegularLayout()
    {
        if (_headerRect != null)
        {
            _headerRect.anchoredPosition = _defaultHeaderRectState.AnchoredPosition;
            _headerRect.sizeDelta = _defaultHeaderRectState.SizeDelta;
        }

        if (_headerText != null)
        {
            _headerText.enableAutoSizing = _defaultHeaderTextState.EnableAutoSizing;
            _headerText.fontSize = _defaultHeaderTextState.FontSize;
            _headerText.fontSizeMin = _defaultHeaderTextState.FontSizeMin;
            _headerText.fontSizeMax = _defaultHeaderTextState.FontSizeMax;
        }

        if (_scrollViewRect != null)
        {
            _scrollViewRect.offsetMin = _defaultScrollOffsetMin;
            _scrollViewRect.offsetMax = _defaultScrollOffsetMax;
        }

        RestoreButtonLayout(_backButtonRect, _backButtonLayout, _defaultBackButtonRectState, _defaultBackButtonLayoutState);
        RestoreButtonLayout(_playButtonRect, _playButtonLayout, _defaultPlayButtonRectState, _defaultPlayButtonLayoutState);

        if (_contentLayoutGroup != null)
        {
            _contentLayoutGroup.spacing = _defaultContentSpacing;
        }

        if (_cardSizers == null)
        {
            return;
        }

        for (int i = 0; i < _cardSizers.Length; i += 1)
        {
            PreferredSizeByParentWidth cardSizer = _cardSizers[i];
            if (cardSizer == null)
            {
                continue;
            }

            CardLayoutState state = GetDefaultCardState(i, cardSizer);
            cardSizer.widthRatio = state.WidthRatio;
            cardSizer.heightRatio = state.HeightRatio;
            cardSizer.minWidth = state.MinWidth;
            cardSizer.maxWidth = state.MaxWidth;
        }
    }

    private void ApplyHeaderLayout(
        float topInset,
        float height,
        bool enableAutoSizing,
        float fontSize,
        float fontSizeMin,
        float fontSizeMax)
    {
        if (_headerRect != null)
        {
            _headerRect.anchoredPosition = new Vector2(_defaultHeaderRectState.AnchoredPosition.x, -topInset);
            float headerWidth = _defaultHeaderRectState.SizeDelta.x;
            if (enableAutoSizing && _safeAreaRect != null)
            {
                headerWidth = Mathf.Min(Mathf.Max(_safeAreaRect.rect.width - 160f, 620f), 920f);
            }

            _headerRect.sizeDelta = new Vector2(headerWidth, height);
        }

        if (_headerText != null)
        {
            _headerText.enableAutoSizing = enableAutoSizing;
            _headerText.fontSize = fontSize;
            _headerText.fontSizeMin = fontSizeMin;
            _headerText.fontSizeMax = fontSizeMax;
            _headerText.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }

    private void ApplyScrollInsets(float topInset, float bottomInset)
    {
        if (_scrollViewRect == null)
        {
            return;
        }

        _scrollViewRect.offsetMin = new Vector2(_defaultScrollOffsetMin.x, bottomInset);
        _scrollViewRect.offsetMax = new Vector2(_defaultScrollOffsetMax.x, -topInset);
    }

    private static void ApplyButtonLayout(
        RectTransform rectTransform,
        LayoutElement layoutElement,
        Vector2 anchoredPosition,
        float size)
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(size, size);
        }

        if (layoutElement == null)
        {
            return;
        }

        layoutElement.minWidth = size;
        layoutElement.minHeight = size;
        layoutElement.preferredWidth = size;
        layoutElement.preferredHeight = size;
    }

    private static void RestoreButtonLayout(
        RectTransform rectTransform,
        LayoutElement layoutElement,
        RectTransformState rectState,
        LayoutElementState layoutState)
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = rectState.AnchoredPosition;
            rectTransform.sizeDelta = rectState.SizeDelta;
        }

        if (layoutElement == null)
        {
            return;
        }

        layoutElement.minWidth = layoutState.MinWidth;
        layoutElement.minHeight = layoutState.MinHeight;
        layoutElement.preferredWidth = layoutState.PreferredWidth;
        layoutElement.preferredHeight = layoutState.PreferredHeight;
        layoutElement.flexibleWidth = layoutState.FlexibleWidth;
        layoutElement.flexibleHeight = layoutState.FlexibleHeight;
    }

    private void RefreshCards()
    {
        if (_cardSizers == null)
        {
            return;
        }

        foreach (PreferredSizeByParentWidth cardSizer in _cardSizers)
        {
            cardSizer?.Refresh();
        }
    }

    private void RebuildLayout()
    {
        Canvas.ForceUpdateCanvases();

        if (_safeAreaRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_safeAreaRect);
        }

        if (_scrollViewRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollViewRect);
        }

        if (_contentLayoutGroup != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentLayoutGroup.transform as RectTransform);
        }
    }

    private void RecenterSelectedCard()
    {
        if (_swipeSnapController == null)
        {
            return;
        }

        _swipeSnapController.Refresh();
    }

    private bool HasEnvironmentChanged()
    {
        if (_lastAppliedScreenSize.x != Screen.width || _lastAppliedScreenSize.y != Screen.height)
        {
            return true;
        }

        if (_lastAppliedSafeArea != Screen.safeArea)
        {
            return true;
        }

        if (_safeAreaRect == null)
        {
            return false;
        }

        Vector2 currentSafeAreaRectSize = _safeAreaRect.rect.size;
        return !Mathf.Approximately(_lastAppliedSafeAreaRectSize.x, currentSafeAreaRectSize.x)
            || !Mathf.Approximately(_lastAppliedSafeAreaRectSize.y, currentSafeAreaRectSize.y)
            || _lastAppliedCompact != IsCompactScreen();
    }

    private bool IsCompactScreen()
    {
        int shortSide = Mathf.Min(Screen.width, Screen.height);
        int longSide = Mathf.Max(Screen.width, Screen.height);
        float aspect = shortSide > 0 ? longSide / (float)shortSide : 0f;
        return (shortSide <= CompactMaxScreenWidth && longSide <= CompactMaxScreenHeight)
            || aspect >= CompactTallAspect;
    }

    private void CacheDefaultsIfNeeded()
    {
        if (_defaultsCached)
        {
            return;
        }

        ResolveReferences();

        _defaultHeaderRectState = CaptureRectTransformState(_headerRect);
        _defaultHeaderTextState = CaptureHeaderTextState(_headerText);
        _defaultBackButtonRectState = CaptureRectTransformState(_backButtonRect);
        _defaultBackButtonLayoutState = CaptureLayoutElementState(_backButtonLayout);
        _defaultPlayButtonRectState = CaptureRectTransformState(_playButtonRect);
        _defaultPlayButtonLayoutState = CaptureLayoutElementState(_playButtonLayout);
        _defaultScrollOffsetMin = _scrollViewRect != null ? _scrollViewRect.offsetMin : Vector2.zero;
        _defaultScrollOffsetMax = _scrollViewRect != null ? _scrollViewRect.offsetMax : Vector2.zero;
        _defaultContentSpacing = _contentLayoutGroup != null ? _contentLayoutGroup.spacing : 0f;

        if (_cardSizers != null)
        {
            _defaultCardStates = new CardLayoutState[_cardSizers.Length];
            for (int i = 0; i < _cardSizers.Length; i += 1)
            {
                PreferredSizeByParentWidth cardSizer = _cardSizers[i];
                if (cardSizer == null)
                {
                    continue;
                }

                _defaultCardStates[i] = new CardLayoutState
                {
                    WidthRatio = cardSizer.widthRatio,
                    HeightRatio = cardSizer.heightRatio,
                    MinWidth = cardSizer.minWidth,
                    MaxWidth = cardSizer.maxWidth,
                };

                if (_hasFallbackCardState)
                {
                    continue;
                }

                _fallbackCardState = _defaultCardStates[i];
                _hasFallbackCardState = true;
            }
        }

        _defaultsCached = true;
    }

    private void ResolveReferences()
    {
        if (_safeAreaRect == null)
        {
            _safeAreaRect = transform as RectTransform;
        }

        if (_headerRect == null)
        {
            Transform headerTransform = transform.Find(" HeaderText");
            _headerRect = headerTransform as RectTransform;
        }

        if (_headerText == null && _headerRect != null)
        {
            _headerText = _headerRect.GetComponent<TMP_Text>();
        }

        if (_scrollViewRect == null)
        {
            Transform scrollViewTransform = transform.Find("Scroll View");
            _scrollViewRect = scrollViewTransform as RectTransform;
        }

        if (_backButtonRect == null)
        {
            Transform backButtonTransform = transform.Find("BackButton");
            _backButtonRect = backButtonTransform as RectTransform;
        }

        if (_backButtonLayout == null && _backButtonRect != null)
        {
            _backButtonLayout = _backButtonRect.GetComponent<LayoutElement>();
        }

        if (_playButtonRect == null)
        {
            Transform playButtonTransform = transform.Find("PlayButton");
            _playButtonRect = playButtonTransform as RectTransform;
        }

        if (_playButtonLayout == null && _playButtonRect != null)
        {
            _playButtonLayout = _playButtonRect.GetComponent<LayoutElement>();
        }

        if (_contentLayoutGroup == null && _scrollViewRect != null)
        {
            _contentLayoutGroup = _scrollViewRect.GetComponentInChildren<HorizontalLayoutGroup>(true);
        }

        if (_contentLayoutGroup != null)
        {
            PreferredSizeByParentWidth[] contentCardSizers = _contentLayoutGroup.GetComponentsInChildren<PreferredSizeByParentWidth>(true);
            if (_cardSizers == null || _cardSizers.Length != contentCardSizers.Length)
            {
                _cardSizers = contentCardSizers;
            }
        }

        if (_swipeSnapController == null && _scrollViewRect != null)
        {
            _swipeSnapController = _scrollViewRect.GetComponent<SwipeSnapController>();
        }
    }

    private CardLayoutState GetDefaultCardState(int index, PreferredSizeByParentWidth cardSizer)
    {
        if (_defaultCardStates != null
            && index >= 0
            && index < _defaultCardStates.Length
            && _defaultCardStates[index].MaxWidth > 0f)
        {
            return _defaultCardStates[index];
        }

        if (_hasFallbackCardState)
        {
            return _fallbackCardState;
        }

        return new CardLayoutState
        {
            WidthRatio = cardSizer.widthRatio,
            HeightRatio = cardSizer.heightRatio,
            MinWidth = cardSizer.minWidth,
            MaxWidth = cardSizer.maxWidth,
        };
    }

    private static RectTransformState CaptureRectTransformState(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return default;
        }

        return new RectTransformState
        {
            AnchoredPosition = rectTransform.anchoredPosition,
            SizeDelta = rectTransform.sizeDelta,
        };
    }

    private static LayoutElementState CaptureLayoutElementState(LayoutElement layoutElement)
    {
        if (layoutElement == null)
        {
            return default;
        }

        return new LayoutElementState
        {
            MinWidth = layoutElement.minWidth,
            MinHeight = layoutElement.minHeight,
            PreferredWidth = layoutElement.preferredWidth,
            PreferredHeight = layoutElement.preferredHeight,
            FlexibleWidth = layoutElement.flexibleWidth,
            FlexibleHeight = layoutElement.flexibleHeight,
        };
    }

    private static HeaderTextState CaptureHeaderTextState(TMP_Text headerText)
    {
        if (headerText == null)
        {
            return default;
        }

        return new HeaderTextState
        {
            EnableAutoSizing = headerText.enableAutoSizing,
            FontSize = headerText.fontSize,
            FontSizeMin = headerText.fontSizeMin,
            FontSizeMax = headerText.fontSizeMax,
        };
    }

    private struct RectTransformState
    {
        public Vector2 AnchoredPosition;
        public Vector2 SizeDelta;
    }

    private struct LayoutElementState
    {
        public float MinWidth;
        public float MinHeight;
        public float PreferredWidth;
        public float PreferredHeight;
        public float FlexibleWidth;
        public float FlexibleHeight;
    }

    private struct HeaderTextState
    {
        public bool EnableAutoSizing;
        public float FontSize;
        public float FontSizeMin;
        public float FontSizeMax;
    }

    private struct CardLayoutState
    {
        public float WidthRatio;
        public float HeightRatio;
        public float MinWidth;
        public float MaxWidth;
    }
}
