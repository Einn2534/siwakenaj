using TMPro;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CarVisualController : MonoBehaviour
{
    private const string CoveredSpriteResourcePath = "Gameplay/covered_vehicle";

    private static readonly Color ExpressColor = new(1f, 0.78f, 0.22f, 1f);
    private static readonly Color BrokenColor = new(1f, 0.38f, 0.42f, 1f);
    private static Sprite s_CoveredSprite;

    [SerializeField]
    private CarVisualDatabase _visualDatabase;

    [SerializeField]
    private SpriteRenderer _spriteRenderer;

    private TextMeshPro _modifierBadge;

    private void Awake()
    {
        CacheReferences();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferences();
    }
#endif

    public void Apply(CarType carType)
    {
        Apply(carType, CarModifier.Normal, true);
    }

    public void Apply(CarType carType, CarModifier modifier, bool isRevealed)
    {
        CacheReferences();
        if (_visualDatabase == null || _spriteRenderer == null)
        {
            return;
        }

        Sprite sprite = modifier == CarModifier.Covered && !isRevealed
            ? GetCoveredSprite()
            : _visualDatabase.GetBodySprite(carType);
        sprite ??= _visualDatabase.GetBodySprite(CarType.CompactCar);
        if (sprite != null)
        {
            _spriteRenderer.sprite = sprite;
        }

        _spriteRenderer.color = GetBodyColor(modifier, isRevealed);
        UpdateModifierBadge(modifier, isRevealed);
    }

    public void Reveal(CarType carType, CarModifier modifier)
    {
        Apply(carType, modifier, true);
    }

    private void CacheReferences()
    {
        if (_visualDatabase == null)
        {
            _visualDatabase = CarVisualDatabase.LoadDefault();
        }

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void UpdateModifierBadge(CarModifier modifier, bool isRevealed)
    {
        string badge = modifier switch
        {
            CarModifier.Express => "!",
            CarModifier.Covered when !isRevealed => "?",
            CarModifier.Broken => "X",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(badge))
        {
            if (_modifierBadge != null)
            {
                _modifierBadge.gameObject.SetActive(false);
            }

            return;
        }

        EnsureModifierBadge();
        _modifierBadge.gameObject.SetActive(true);
        _modifierBadge.text = badge;
        _modifierBadge.color = Color.white;
        _modifierBadge.renderer.sortingOrder = _spriteRenderer.sortingOrder + 1;
        if (modifier == CarModifier.Express && _spriteRenderer.sprite != null)
        {
            // World-space TMP size varies with the font. Match the marker to the car body.
            _modifierBadge.ForceMeshUpdate();
            Bounds textBounds = _modifierBadge.mesh.bounds;
            Bounds bodyBounds = _spriteRenderer.sprite.bounds;
            if (textBounds.size.y > 0f)
            {
                float markerHeight = bodyBounds.size.y * 0.45f;
                float markerScale = markerHeight / textBounds.size.y;
                _modifierBadge.transform.localScale = Vector3.one * markerScale;
                _modifierBadge.transform.localPosition = new Vector3(
                    bodyBounds.center.x - textBounds.center.x * markerScale,
                    bodyBounds.max.y + markerHeight * 0.75f - textBounds.center.y * markerScale,
                    -0.1f);
            }
        }
        else
        {
            _modifierBadge.transform.localScale = Vector3.one;
            _modifierBadge.transform.localPosition = new Vector3(0f, 3.5f, -0.1f);
        }
    }

    private static Sprite GetCoveredSprite()
    {
        s_CoveredSprite ??= Resources.Load<Sprite>(CoveredSpriteResourcePath);
        return s_CoveredSprite;
    }

    private void EnsureModifierBadge()
    {
        if (_modifierBadge != null)
        {
            return;
        }

        GameObject badgeObject = new("ModifierBadge", typeof(RectTransform), typeof(MeshRenderer), typeof(TextMeshPro));
        badgeObject.transform.SetParent(transform, false);
        badgeObject.transform.localPosition = new Vector3(0f, 3.5f, -0.1f);
        RectTransform badgeRect = badgeObject.GetComponent<RectTransform>();
        badgeRect.sizeDelta = new Vector2(3f, 3f);

        _modifierBadge = badgeObject.GetComponent<TextMeshPro>();
        _modifierBadge.alignment = TextAlignmentOptions.Center;
        _modifierBadge.fontSize = 4.2f;
        _modifierBadge.fontStyle = FontStyles.Normal;
        _modifierBadge.enableAutoSizing = false;
        _modifierBadge.outlineWidth = 0.045f;
        _modifierBadge.outlineColor = new Color32(33, 24, 39, 255);
        _modifierBadge.raycastTarget = false;
    }

    private static Color GetBodyColor(CarModifier modifier, bool isRevealed)
    {
        return modifier switch
        {
            CarModifier.Express => ExpressColor,
            CarModifier.Broken => BrokenColor,
            _ => Color.white
        };
    }
}
