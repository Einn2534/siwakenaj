using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LaneInputController))]
public class LaneButtonVisualController : MonoBehaviour
{
    private const string LaneLabelName = "LaneLabel";
    private const float IconInsetRatio = 0.72f;
    private const float IconCenterY = 0.62f;
    private const float LabelHeight = 58f;
    private const float LabelBottomInset = 6f;
    private const float LabelFontSize = 30f;
    private const float LabelFontSizeMin = 18f;

    private static readonly Color LabelColor = new(1f, 0.96f, 0.72f, 1f);
    private static readonly Color LabelOutlineColor = new(0.02f, 0.05f, 0.10f, 1f);

    [SerializeField]
    private CarVisualDatabase _visualDatabase;

    [SerializeField]
    private Graphic _laneAImage;

    [SerializeField]
    private Graphic _laneBImage;

    [SerializeField]
    private Graphic _laneCImage;

    private LaneInputController _laneInputController;

    private void Awake()
    {
        CacheReferences();
        ApplyVisuals();
    }

    private void OnEnable()
    {
        ApplyVisuals();
    }

    private void Start()
    {
        ApplyVisuals();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall -= ApplyVisualsInEditor;
        UnityEditor.EditorApplication.delayCall += ApplyVisualsInEditor;
    }

    private void ApplyVisualsInEditor()
    {
        UnityEditor.EditorApplication.delayCall -= ApplyVisualsInEditor;
        if (this == null)
        {
            return;
        }

        CacheReferences();
        ApplyVisuals();
    }
#endif

    public void ApplyVisuals()
    {
        CacheReferences();
        if (_visualDatabase == null || _laneInputController == null)
        {
            return;
        }

        ApplyLane(_laneAImage, _laneInputController.LaneAType);
        ApplyLane(_laneBImage, _laneInputController.LaneBType);
        ApplyLane(_laneCImage, _laneInputController.LaneCType);
    }

    private void CacheReferences()
    {
        if (_visualDatabase == null)
        {
            _visualDatabase = CarVisualDatabase.LoadDefault();
        }

        if (_laneInputController == null)
        {
            _laneInputController = GetComponent<LaneInputController>();
        }
    }

    private void ApplyLane(Graphic graphic, CarType laneType)
    {
        ApplyGraphic(graphic, laneType);
        FitGraphicForLabel(graphic);
        ApplyLabel(graphic, GetLaneLabel(laneType));
    }

    private void ApplyGraphic(Graphic graphic, CarType laneType)
    {
        if (graphic == null)
        {
            return;
        }

        Sprite sprite = _visualDatabase.GetIconSprite(laneType);
        if (sprite == null)
        {
            return;
        }

        if (graphic is Image image)
        {
            image.sprite = sprite;
            image.preserveAspect = true;
            return;
        }

        if (graphic is RawImage rawImage && sprite.texture != null)
        {
            Rect rect = sprite.textureRect;
            Texture texture = sprite.texture;
            rawImage.texture = texture;
            rawImage.uvRect = new Rect(
                rect.x / texture.width,
                rect.y / texture.height,
                rect.width / texture.width,
                rect.height / texture.height);
            FitRawImageToSprite(rawImage, rect);
        }
    }

    private static void FitRawImageToSprite(RawImage rawImage, Rect spriteRect)
    {
        RectTransform rectTransform = rawImage.rectTransform;
        RectTransform parent = rectTransform.parent as RectTransform;
        if (parent == null || spriteRect.width <= 0f || spriteRect.height <= 0f)
        {
            return;
        }

        Rect parentRect = parent.rect;
        float maxWidth = parentRect.width * IconInsetRatio;
        float maxHeight = parentRect.height * IconInsetRatio;
        if (maxWidth <= 0f || maxHeight <= 0f)
        {
            return;
        }

        float aspect = spriteRect.width / spriteRect.height;
        float width = maxWidth;
        float height = width / aspect;
        if (height > maxHeight)
        {
            height = maxHeight;
            width = height * aspect;
        }

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0f, parentRect.height * (IconCenterY - 0.5f));
        rectTransform.sizeDelta = new Vector2(width, height);
    }

    private static void FitGraphicForLabel(Graphic graphic)
    {
        if (graphic == null || graphic is RawImage)
        {
            return;
        }

        RectTransform rectTransform = graphic.rectTransform;
        RectTransform parent = ResolveButtonRect(graphic);
        if (rectTransform == null || parent == null)
        {
            return;
        }

        Rect parentRect = parent.rect;
        float maxWidth = parentRect.width * IconInsetRatio;
        float maxHeight = parentRect.height * IconInsetRatio;
        if (maxWidth <= 0f || maxHeight <= 0f)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(0.5f, IconCenterY);
        rectTransform.anchorMax = new Vector2(0.5f, IconCenterY);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(maxWidth, maxHeight);
    }

    private static void ApplyLabel(Graphic graphic, string label)
    {
        RectTransform buttonRect = ResolveButtonRect(graphic);
        if (buttonRect == null)
        {
            return;
        }

        TMP_Text labelText = ResolveLabel(buttonRect);
        labelText.text = label;
        labelText.gameObject.SetActive(true);
        labelText.transform.SetAsLastSibling();
    }

    private static TMP_Text ResolveLabel(RectTransform buttonRect)
    {
        Transform existing = buttonRect.Find(LaneLabelName);
        if (existing != null && existing.TryGetComponent(out TMP_Text existingText))
        {
            return existingText;
        }

        GameObject labelObject = new(LaneLabelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.layer = buttonRect.gameObject.layer;
        labelObject.transform.SetParent(buttonRect, false);

        RectTransform rectTransform = (RectTransform)labelObject.transform;
        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(1f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = new Vector2(0f, LabelBottomInset);
        rectTransform.sizeDelta = new Vector2(0f, LabelHeight);

        TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = LabelFontSize;
        text.fontSizeMax = LabelFontSize;
        text.fontSizeMin = LabelFontSizeMin;
        text.enableAutoSizing = true;
        text.alignment = TextAlignmentOptions.Center;
        text.color = LabelColor;
        text.outlineColor = LabelOutlineColor;
        text.outlineWidth = 0.18f;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static RectTransform ResolveButtonRect(Graphic graphic)
    {
        if (graphic == null)
        {
            return null;
        }

        Button button = graphic.GetComponentInParent<Button>();
        return button != null ? button.transform as RectTransform : graphic.rectTransform.parent as RectTransform;
    }

    private static string GetLaneLabel(CarType laneType)
    {
        return laneType switch
        {
            CarType.LightTruck => "\u30c8\u30e9\u30c3\u30af",
            CarType.CompactCar => "\u5c0f\u578b",
            CarType.SportsCar => "\u30b9\u30dd\u30fc\u30c4",
            _ => string.Empty
        };
    }
}
