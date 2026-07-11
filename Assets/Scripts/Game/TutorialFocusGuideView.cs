using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class TutorialFocusGuideView : MonoBehaviour
{
    private const float RingPadding = 19f;
    private const float RingPulseSeconds = 0.6f;
    private const float PointerBounceSeconds = 0.7f;
    private const float PointerBouncePixels = 13f;

    private Canvas _canvas;
    private RectTransform _root;
    private RectTransform _ringRect;
    private RectTransform _pointerRect;
    private Image _ringImage;
    private Image _pointerImage;
    private RectTransform _targetButton;
    private Coroutine _animationRoutine;
    private Vector2 _pointerBasePosition;

    public void Initialize(Canvas canvas, RectTransform parent)
    {
        if (_root != null || canvas == null || parent == null)
        {
            return;
        }

        _canvas = canvas;
        _root = CreateRect("TutorialButtonGuide", parent);
        Stretch(_root);

        _ringRect = CreateRect("CorrectRing", _root);
        _ringRect.anchorMin = new Vector2(0.5f, 0.5f);
        _ringRect.anchorMax = new Vector2(0.5f, 0.5f);
        _ringRect.pivot = new Vector2(0.5f, 0.5f);
        _ringImage = _ringRect.gameObject.AddComponent<Image>();
        _ringImage.sprite = CreateRingSprite();
        _ringImage.color = new Color(1f, 217f / 255f, 74f / 255f, 1f);
        _ringImage.preserveAspect = true;
        _ringImage.raycastTarget = false;

        _pointerRect = CreateRect("WitchPointer", _root);
        _pointerRect.anchorMin = new Vector2(0.5f, 0.5f);
        _pointerRect.anchorMax = new Vector2(0.5f, 0.5f);
        _pointerRect.pivot = new Vector2(0.5f, 0.5f);
        _pointerImage = _pointerRect.gameObject.AddComponent<Image>();
        _pointerImage.sprite = LoadSprite("UI/Tutorial/tutorial_pointer_up_right");
        _pointerImage.preserveAspect = true;
        _pointerImage.raycastTarget = false;

        _root.gameObject.SetActive(false);
    }

    public void ShowFocus(CarType expectedType, LaneInputController laneInputController, CarController activeCar)
    {
        if (_root == null || laneInputController == null
            || !laneInputController.TryGetButtonForLane(expectedType, out RectTransform button)
            || button == null)
        {
            HideFocus();
            return;
        }

        _targetButton = button;
        _root.gameObject.SetActive(true);
        UpdateLayout();
        Pulse();
    }

    public void HideFocus()
    {
        _targetButton = null;
        if (_animationRoutine != null)
        {
            StopCoroutine(_animationRoutine);
            _animationRoutine = null;
        }

        if (_root != null)
        {
            _root.gameObject.SetActive(false);
        }
    }

    public void Pulse()
    {
        if (_root == null || !_root.gameObject.activeInHierarchy)
        {
            return;
        }

        if (_animationRoutine != null)
        {
            StopCoroutine(_animationRoutine);
        }

        _animationRoutine = StartCoroutine(AnimateGuide());
    }

    private void LateUpdate()
    {
        if (_targetButton != null && _root != null && _root.gameObject.activeInHierarchy)
        {
            UpdateLayout();
        }
    }

    private void UpdateLayout()
    {
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(_root, _targetButton);
        Vector2 center = bounds.center;
        float diameter = Mathf.Max(bounds.size.x, bounds.size.y) + (RingPadding * 2f);
        _ringRect.anchoredPosition = center;
        _ringRect.sizeDelta = new Vector2(diameter, diameter);

        Sprite pointerSprite = _pointerImage.sprite;
        float pointerWidth = 165f;
        float pointerHeight = pointerSprite != null && pointerSprite.rect.width > 0f
            ? pointerWidth * (pointerSprite.rect.height / pointerSprite.rect.width)
            : pointerWidth;
        _pointerRect.sizeDelta = new Vector2(pointerWidth, pointerHeight);
        _pointerBasePosition = center + new Vector2(44f, -146f);
        if (_animationRoutine == null)
        {
            _pointerRect.anchoredPosition = _pointerBasePosition;
        }
    }

    private IEnumerator AnimateGuide()
    {
        float elapsed = 0f;
        while (_targetButton != null && _root != null && _root.gameObject.activeInHierarchy)
        {
            elapsed += Time.unscaledDeltaTime;
            float ringWave = Mathf.PingPong(elapsed / (RingPulseSeconds * 0.5f), 1f);
            float pointerWave = Mathf.Sin((elapsed / PointerBounceSeconds) * Mathf.PI * 2f);
            _ringRect.localScale = Vector3.one * Mathf.Lerp(1f, 1.06f, ringWave);
            _pointerRect.anchoredPosition = _pointerBasePosition + new Vector2(0f, pointerWave * PointerBouncePixels);
            yield return null;
        }

        _animationRoutine = null;
    }

    private static Sprite CreateRingSprite()
    {
        const int size = 256;
        const float outerRadius = 126f;
        const float innerRadius = 116f;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
        {
            name = "TutorialCorrectRing",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[size * size];
        Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
        for (int y = 0; y < size; y += 1)
        {
            for (int x = 0; x < size; x += 1)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float outerAlpha = Mathf.Clamp01(outerRadius - distance + 1f);
                float innerAlpha = Mathf.Clamp01(distance - innerRadius + 1f);
                byte alpha = (byte)Mathf.RoundToInt(255f * Mathf.Min(outerAlpha, innerAlpha));
                pixels[(y * size) + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite LoadSprite(string resourcePath)
    {
        return Resources.Load<Sprite>(resourcePath);
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform), typeof(CanvasRenderer));
        gameObject.layer = LayerMask.NameToLayer("UI");
        gameObject.transform.SetParent(parent, false);
        return (RectTransform)gameObject.transform;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localScale = Vector3.one;
    }
}
