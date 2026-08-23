using TMPro;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CarVisualController : MonoBehaviour
{
    private static readonly Color ExpressColor = new(1f, 0.78f, 0.22f, 1f);
    private static readonly Color CoveredColor = new(0.24f, 0.27f, 0.33f, 1f);
    private static readonly Color BrokenColor = new(1f, 0.38f, 0.42f, 1f);

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

        CarType visibleType = isRevealed ? carType : CarType.CompactCar;
        Sprite sprite = _visualDatabase.GetBodySprite(visibleType);
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
        _modifierBadge.fontStyle = FontStyles.Bold;
        _modifierBadge.enableAutoSizing = false;
        _modifierBadge.outlineWidth = 0.22f;
        _modifierBadge.outlineColor = new Color32(33, 24, 39, 255);
        _modifierBadge.raycastTarget = false;
    }

    private static Color GetBodyColor(CarModifier modifier, bool isRevealed)
    {
        return modifier switch
        {
            CarModifier.Express => ExpressColor,
            CarModifier.Covered when !isRevealed => CoveredColor,
            CarModifier.Broken => BrokenColor,
            _ => Color.white
        };
    }
}
