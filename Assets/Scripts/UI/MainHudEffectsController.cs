using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MainHudEffectsController : MonoBehaviour
{
    private const string CountdownObjectName = "CountdownEffect";
    private const string JudgeObjectName = "JudgeEffect";

    [Header("Countdown")]
    [SerializeField, FormerlySerializedAs("countdownImage")]
    private Image _countdownImage;

    [SerializeField]
    private Sprite _countdown3Sprite;

    [SerializeField]
    private Sprite _countdown2Sprite;

    [SerializeField]
    private Sprite _countdown1Sprite;

    [SerializeField]
    private Sprite _countdownReadySprite;

    [SerializeField, Min(0.05f)]
    private float _countdownStepSeconds = 0.7f;

    [SerializeField, Min(0.05f)]
    private float _readyStepSeconds = 0.85f;

    [Header("Judge")]
    [SerializeField, FormerlySerializedAs("judgeImage")]
    private Image _judgeImage;

    [SerializeField]
    private Sprite _correctJudgeSprite;

    [SerializeField]
    private Sprite _missJudgeSprite;

    [SerializeField, Min(0.05f)]
    private float _judgeSeconds = 0.38f;

    [SerializeField]
    private Vector2 _countdownSize = new(560f, 260f);

    [SerializeField]
    private Vector2 _judgeSize = new(420f, 170f);

    private Coroutine _countdownRoutine;
    private Coroutine _judgeRoutine;

    private void Awake()
    {
        EnsureImages();
        HideImage(_countdownImage);
        HideImage(_judgeImage);
    }

    private void OnDisable()
    {
        StopEffects();
    }

    public IEnumerator PlayReadyCountdown()
    {
        EnsureImages();
        StopCountdown();
        HideImage(_judgeImage);

        _countdownRoutine = StartCoroutine(PlayReadyCountdownRoutine());
        yield return _countdownRoutine;
        _countdownRoutine = null;
    }

    public void StopEffects()
    {
        StopCountdown();
        StopJudge();
        HideImage(_countdownImage);
        HideImage(_judgeImage);
    }

    private IEnumerator PlayReadyCountdownRoutine()
    {
        EnsureImages();

        yield return PlayCountdownStep(_countdown3Sprite, _countdownStepSeconds);
        yield return PlayCountdownStep(_countdown2Sprite, _countdownStepSeconds);
        yield return PlayCountdownStep(_countdown1Sprite, _countdownStepSeconds);
        yield return PlayCountdownStep(_countdownReadySprite, _readyStepSeconds);

        HideImage(_countdownImage);
    }

    public void ShowCorrectJudge()
    {
        ShowJudge(_correctJudgeSprite);
    }

    public void ShowMissJudge()
    {
        ShowJudge(_missJudgeSprite);
    }

    private void StopCountdown()
    {
        if (_countdownRoutine == null)
        {
            return;
        }

        StopCoroutine(_countdownRoutine);
        _countdownRoutine = null;
        HideImage(_countdownImage);
    }

    private void StopJudge()
    {
        if (_judgeRoutine == null)
        {
            return;
        }

        StopCoroutine(_judgeRoutine);
        _judgeRoutine = null;
        HideImage(_judgeImage);
    }

    private IEnumerator PlayCountdownStep(Sprite sprite, float seconds)
    {
        if (_countdownImage == null || sprite == null)
        {
            yield break;
        }

        _countdownImage.sprite = sprite;
        _countdownImage.SetNativeSize();
        FitToSize(_countdownImage.rectTransform, _countdownSize);
        _countdownImage.gameObject.SetActive(true);

        yield return AnimateImage(_countdownImage, seconds, 0.82f, 1.08f);
    }

    private void ShowJudge(Sprite sprite)
    {
        if (_judgeImage == null || sprite == null)
        {
            return;
        }

        if (_judgeRoutine != null)
        {
            StopJudge();
        }

        _judgeRoutine = StartCoroutine(ShowJudgeRoutine(sprite));
    }

    private IEnumerator ShowJudgeRoutine(Sprite sprite)
    {
        _judgeImage.sprite = sprite;
        _judgeImage.SetNativeSize();
        FitToSize(_judgeImage.rectTransform, _judgeSize);
        _judgeImage.gameObject.SetActive(true);

        yield return AnimateImage(_judgeImage, _judgeSeconds, 0.9f, 1.05f);

        HideImage(_judgeImage);
        _judgeRoutine = null;
    }

    private static IEnumerator AnimateImage(Image image, float seconds, float startScale, float peakScale)
    {
        float elapsed = 0f;
        seconds = Mathf.Max(0.05f, seconds);

        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / seconds);
            float alpha = progress < 0.72f ? 1f : Mathf.InverseLerp(1f, 0.72f, progress);
            float scale = Mathf.Lerp(startScale, peakScale, EaseOutBack(Mathf.Min(progress / 0.72f, 1f)));

            SetAlpha(image, alpha);
            image.rectTransform.localScale = Vector3.one * scale;
            yield return null;
        }

        SetAlpha(image, 0f);
    }

    private void EnsureImages()
    {
        if (_countdownImage == null)
        {
            _countdownImage = CreateEffectImage(CountdownObjectName, _countdownSize);
        }

        if (_judgeImage == null)
        {
            _judgeImage = CreateEffectImage(JudgeObjectName, _judgeSize);
        }

        if (_countdownImage != null)
        {
            _countdownImage.transform.SetAsLastSibling();
        }

        if (_judgeImage != null)
        {
            _judgeImage.transform.SetAsLastSibling();
        }
    }

    private Image CreateEffectImage(string objectName, Vector2 size)
    {
        GameObject effectObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        effectObject.layer = gameObject.layer;
        effectObject.transform.SetParent(transform, false);

        RectTransform rectTransform = effectObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = size;

        Image image = effectObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    private static void FitToSize(RectTransform rectTransform, Vector2 maxSize)
    {
        if (rectTransform == null)
        {
            return;
        }

        Vector2 size = rectTransform.sizeDelta;
        if (size.x <= 0f || size.y <= 0f)
        {
            rectTransform.sizeDelta = maxSize;
            return;
        }

        float scale = Mathf.Min(maxSize.x / size.x, maxSize.y / size.y);
        rectTransform.sizeDelta = size * Mathf.Min(scale, 1f);
    }

    private static void HideImage(Image image)
    {
        if (image == null)
        {
            return;
        }

        SetAlpha(image, 0f);
        image.rectTransform.localScale = Vector3.one;
        image.gameObject.SetActive(false);
    }

    private static void SetAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = Mathf.Clamp01(alpha);
        image.color = color;
    }

    private static float EaseOutBack(float value)
    {
        const float overshoot = 1.70158f;
        value -= 1f;
        return 1f + value * value * ((overshoot + 1f) * value + overshoot);
    }
}
