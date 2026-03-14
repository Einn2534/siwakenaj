using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LaneInputController))]
public class LaneButtonVisualController : MonoBehaviour
{
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
        }
    }
}
