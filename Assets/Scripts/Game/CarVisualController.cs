using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CarVisualController : MonoBehaviour
{
    [SerializeField]
    private CarVisualDatabase _visualDatabase;

    [SerializeField]
    private SpriteRenderer _spriteRenderer;

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
        CacheReferences();
        if (_visualDatabase == null || _spriteRenderer == null)
        {
            return;
        }

        Sprite sprite = _visualDatabase.GetBodySprite(carType);
        if (sprite != null)
        {
            _spriteRenderer.sprite = sprite;
        }
    }

    private void CacheReferences()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
}
