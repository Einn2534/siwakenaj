using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LaneInputController))]
public class LaneButtonVisualController : MonoBehaviour
{
    private const float RawImageInsetRatio = 0.9f;

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

        ApplyGraphic(_laneAImage, _laneInputController.LaneAType);
        ApplyGraphic(_laneBImage, _laneInputController.LaneBType);
        ApplyGraphic(_laneCImage, _laneInputController.LaneCType);
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
        float maxWidth = parentRect.width * RawImageInsetRatio;
        float maxHeight = parentRect.height * RawImageInsetRatio;
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
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(width, height);
    }
}
