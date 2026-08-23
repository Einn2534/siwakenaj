using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum StageCardStatus
{
    Unlocked,
    Locked,
    ComingSoon
}

public class StageCardView : MonoBehaviour
{
    private const string StarBadgeTextName = "StarBadgeText";
    private const string FilledStarTag = "<color=#FFD45C>\u2605</color>";
    private const string EmptyStarTag = "<color=#FFD45C55>\u2605</color>";

    private static readonly Color LockedBackgroundColor = new(0.925f, 0.906f, 0.859f, 1f);
    private static readonly Color ComingSoonBackgroundColor = new(0.925f, 0.906f, 0.859f, 1f);
    private static readonly Color LockedPrimaryTextColor = new(0.32f, 0.29f, 0.27f, 0.78f);
    private static readonly Color LockedOverlayTextColor = new(0.94f, 0.92f, 0.86f, 0.9f);
    private static readonly Color LockedSecondaryTextColor = new(0.169f, 0.145f, 0.188f, 0.42f);
    private static readonly Color ComingSoonPrimaryTextColor = new(0.169f, 0.145f, 0.188f, 0.5f);
    private static readonly Color ComingSoonSecondaryTextColor = new(0.169f, 0.145f, 0.188f, 0.42f);
    private static readonly Color ClearedBestScoreColor = new(1f, 0.902f, 0.412f, 1f);
    private static readonly Color EndlessAccentColor = new(1f, 0.42f, 0.46f, 1f);
    private static readonly Color UnlockedThumbnailColor = Color.white;
    private static readonly Color LockedThumbnailColor = new(0.86f, 0.84f, 0.78f, 0.58f);
    private static readonly Color ComingSoonThumbnailColor = new(0.65f, 0.65f, 0.62f, 0.58f);

    [SerializeField, FormerlySerializedAs("stageNumberText")]
    private TMP_Text _stageNumberText;

    [SerializeField, FormerlySerializedAs("targetScoreText")]
    private TMP_Text _targetScoreText;

    [SerializeField, FormerlySerializedAs("bestScoreText")]
    private TMP_Text _bestScoreText;

    [SerializeField, FormerlySerializedAs("statusText")]
    private TMP_Text _statusText;

    [SerializeField]
    private TMP_Text _starBadgeText;

    [SerializeField]
    private TMP_Text _stageNameText;

    [Header("Art Layers")]
    [SerializeField]
    private Image _frameImage;

    [SerializeField]
    private Image _thumbnailImage;

    [SerializeField]
    private Image _lockOverlayImage;

    [SerializeField]
    private Image _selectionGlowImage;

    [SerializeField]
    private Image _vehiclePreviewImage;

    [SerializeField]
    private Image _progressFillImage;

    [SerializeField]
    private Image _infoPanelImage;

    [SerializeField]
    private Image _pinImage;

    [SerializeField]
    private Image[] _starImages;

    [Header("Sprites")]
    [SerializeField]
    private Sprite _filledStarSprite;

    [SerializeField]
    private Sprite _emptyStarSprite;

    [SerializeField]
    private Sprite[] _stageThumbnailSprites;

    private Image _backgroundImage;
    private Color _defaultBackgroundColor;
    private Color _defaultStageNumberColor;
    private Color _defaultTargetScoreColor;
    private Color _defaultBestScoreColor;
    private Color _defaultStatusColor;
    private bool _defaultsCached;
    private StageCardStatus _currentStatus = StageCardStatus.Unlocked;
    private int _currentStageNumber = 1;
    private bool _isSelected;

    public void SetData(int stageNumber, int targetScore, int bestScore, StageCardStatus status, int starRating, int requiredStageNumber)
    {
        CacheDefaultsIfNeeded();
        _currentStageNumber = StageNumberUtility.Normalize(stageNumber);
        _currentStatus = status;

        SetText(_stageNumberText, $"おしごと {_currentStageNumber}");
        SetText(_stageNameText, GetStageName(_currentStageNumber));

        switch (status)
        {
            case StageCardStatus.Locked:
                ApplyLockedState(stageNumber, requiredStageNumber);
                break;
            case StageCardStatus.ComingSoon:
                ApplyComingSoonState();
                break;
            default:
                ApplyUnlockedState(targetScore, bestScore);
                break;
        }

        ApplyStageThumbnail(_currentStageNumber, status);
        ApplyArtState(status);
        RefreshStarBadge(starRating, status == StageCardStatus.Unlocked);
        ApplySelectionState();
    }

    public void RefreshStarBadge(int stageNumber)
    {
        RefreshStarBadge(SaveService.GetStarRating(stageNumber), _currentStatus == StageCardStatus.Unlocked);
    }

    public void SetEndlessData(int bestScore)
    {
        CacheDefaultsIfNeeded();
        _currentStageNumber = StageNumberUtility.MinimumStageNumber;
        _currentStatus = StageCardStatus.Unlocked;

        SetText(_stageNumberText, "エンドレス");
        SetText(_targetScoreText, $"<color=#{ColorUtility.ToHtmlStringRGB(EndlessAccentColor)}>1ミス</color> でゲームオーバー");
        SetText(_bestScoreText, bestScore > 0 ? $"ベスト {bestScore:N0}" : "ベスト -");
        SetText(_statusText, string.Empty);

        SetColor(_stageNumberText, _defaultStageNumberColor);
        SetColor(_targetScoreText, _defaultTargetScoreColor);
        SetColor(_bestScoreText, bestScore > 0 ? ClearedBestScoreColor : _defaultBestScoreColor);
        SetColor(_statusText, _defaultStatusColor);
        SetBackgroundColor(_defaultBackgroundColor);

        if (_statusText != null)
        {
            _statusText.gameObject.SetActive(false);
        }

        ApplyStageThumbnail(StageNumberUtility.MinimumStageNumber, StageCardStatus.Unlocked);
        ApplyArtState(StageCardStatus.Unlocked);
        RefreshStarBadge(0, false);
        ApplySelectionState();
    }

    public void SetSelected(bool isSelected)
    {
        _isSelected = isSelected;
        ApplySelectionState();
    }

    private void RefreshStarBadge(int starRating, bool canShowStars)
    {
        if (_starImages != null && _starImages.Length > 0)
        {
            RefreshStarImages(starRating, canShowStars);
            if (_starBadgeText != null)
            {
                _starBadgeText.gameObject.SetActive(false);
            }

            return;
        }

        if (_starBadgeText == null)
        {
            Transform badgeTransform = transform.Find(StarBadgeTextName);
            if (badgeTransform != null)
            {
                _starBadgeText = badgeTransform.GetComponent<TMP_Text>();
            }
        }

        if (_starBadgeText == null)
        {
            return;
        }

        int clampedStars = StarRatingUtility.Clamp(starRating);
        bool shouldShow = canShowStars && clampedStars > 0;
        _starBadgeText.gameObject.SetActive(shouldShow);
        _starBadgeText.text = shouldShow ? BuildStarBadge(clampedStars) : string.Empty;
    }

    private void CacheDefaultsIfNeeded()
    {
        if (_defaultsCached)
        {
            return;
        }

        _backgroundImage = GetComponent<Image>();
        _defaultBackgroundColor = _backgroundImage != null ? _backgroundImage.color : Color.white;
        _defaultStageNumberColor = _stageNumberText != null ? _stageNumberText.color : Color.white;
        _defaultTargetScoreColor = _targetScoreText != null ? _targetScoreText.color : Color.white;
        _defaultBestScoreColor = _bestScoreText != null ? _bestScoreText.color : Color.white;
        _defaultStatusColor = _statusText != null ? _statusText.color : Color.white;
        _defaultsCached = true;
    }

    private void ApplyUnlockedState(int targetScore, int bestScore)
    {
        SetText(_targetScoreText, $"目標 {targetScore:N0}台");
        SetText(_bestScoreText, bestScore > 0 ? $"ベスト {bestScore:N0}" : "ベスト -");
        SetText(_statusText, string.Empty);

        SetColor(_stageNumberText, _defaultStageNumberColor);
        SetColor(_targetScoreText, _defaultTargetScoreColor);
        SetColor(_bestScoreText, bestScore > 0 ? ClearedBestScoreColor : _defaultBestScoreColor);
        SetColor(_statusText, _defaultStatusColor);
        SetBackgroundColor(_defaultBackgroundColor);
        SetColor(_stageNameText, new Color(0.169f, 0.145f, 0.188f, 0.45f));
        SetPanelColor(_infoPanelImage, new Color(0.43f, 0.25f, 0.13f, 1f));
        SetPanelColor(_pinImage, new Color(1f, 0.851f, 0.29f, 1f));
        SetPanelActive(_vehiclePreviewImage, true);
        SetProgress(bestScore, targetScore, true);
        if (_targetScoreText != null)
        {
            _targetScoreText.rectTransform.anchoredPosition = new Vector2(-150f, -180f);
            _targetScoreText.alignment = TextAlignmentOptions.MidlineLeft;
        }

        if (_statusText != null)
        {
            _statusText.gameObject.SetActive(false);
        }
    }

    private void ApplyLockedState(int stageNumber, int requiredStageNumber)
    {
        int resolvedRequiredStageNumber = requiredStageNumber > 0
            ? requiredStageNumber
            : StageNumberUtility.Normalize(stageNumber - 1);

        SetText(_stageNameText, "???");
        SetText(_targetScoreText, "---");
        SetText(_bestScoreText, string.Empty);
        SetText(_statusText, $"おしごと{resolvedRequiredStageNumber}をクリアでかいふう");

        SetColor(_stageNumberText, LockedPrimaryTextColor);
        SetColor(_targetScoreText, LockedSecondaryTextColor);
        SetColor(_bestScoreText, LockedSecondaryTextColor);
        SetColor(_statusText, LockedOverlayTextColor);
        SetBackgroundColor(LockedBackgroundColor);
        SetColor(_stageNameText, LockedSecondaryTextColor);
        SetPanelColor(_infoPanelImage, new Color(0.82f, 0.80f, 0.76f, 1f));
        SetPanelColor(_pinImage, new Color(0.72f, 0.70f, 0.65f, 1f));
        SetPanelActive(_vehiclePreviewImage, false);
        SetProgress(0, 1, false);
        if (_targetScoreText != null)
        {
            _targetScoreText.rectTransform.anchoredPosition = new Vector2(0f, -205f);
            _targetScoreText.alignment = TextAlignmentOptions.Center;
        }

        if (_statusText != null)
        {
            _statusText.gameObject.SetActive(true);
        }
    }

    private void ApplyComingSoonState()
    {
        SetText(_stageNumberText, "???");
        SetText(_stageNameText, "???");
        SetText(_targetScoreText, "---");
        SetText(_bestScoreText, "じゅんびちゅう");
        SetText(_statusText, "まだえらべない");

        SetColor(_stageNumberText, ComingSoonSecondaryTextColor);
        SetColor(_targetScoreText, ComingSoonSecondaryTextColor);
        SetColor(_bestScoreText, ComingSoonSecondaryTextColor);
        SetColor(_statusText, ComingSoonPrimaryTextColor);
        SetBackgroundColor(ComingSoonBackgroundColor);
        SetColor(_stageNameText, ComingSoonSecondaryTextColor);
        SetPanelColor(_infoPanelImage, new Color(0.82f, 0.80f, 0.76f, 1f));
        SetPanelActive(_vehiclePreviewImage, false);
        SetProgress(0, 1, false);

        if (_statusText != null)
        {
            _statusText.gameObject.SetActive(true);
        }
    }

    private void ApplyStageThumbnail(int stageNumber, StageCardStatus status)
    {
        if (_thumbnailImage == null || _stageThumbnailSprites == null || _stageThumbnailSprites.Length == 0)
        {
            return;
        }

        int thumbnailIndex = Mathf.Abs(stageNumber - 1) % _stageThumbnailSprites.Length;
        Sprite thumbnailSprite = _stageThumbnailSprites[thumbnailIndex];
        if (thumbnailSprite != null)
        {
            _thumbnailImage.sprite = thumbnailSprite;
        }

        _thumbnailImage.preserveAspect = false;
        _thumbnailImage.color = status switch
        {
            StageCardStatus.Locked => LockedThumbnailColor,
            StageCardStatus.ComingSoon => ComingSoonThumbnailColor,
            _ => UnlockedThumbnailColor
        };
    }

    private void ApplyArtState(StageCardStatus status)
    {
        if (_frameImage != null)
        {
            _frameImage.color = status == StageCardStatus.Unlocked
                ? Color.white
                : new Color(0.925f, 0.906f, 0.859f, 1f);
        }

        if (_lockOverlayImage != null)
        {
            _lockOverlayImage.gameObject.SetActive(status == StageCardStatus.Locked);
        }
    }

    private void RefreshStarImages(int starRating, bool canShowStars)
    {
        int clampedStars = StarRatingUtility.Clamp(starRating);
        for (int i = 0; i < _starImages.Length; i += 1)
        {
            Image starImage = _starImages[i];
            if (starImage == null)
            {
                continue;
            }

            bool isVisibleSlot = i < StarRatingUtility.MaxStars;
            starImage.gameObject.SetActive(isVisibleSlot);
            if (!isVisibleSlot)
            {
                continue;
            }

            bool isFilled = canShowStars && i < clampedStars;
            Sprite sprite = isFilled ? _filledStarSprite : _emptyStarSprite;
            if (sprite != null)
            {
                starImage.sprite = sprite;
            }

            starImage.color = canShowStars ? Color.white : new Color(0.55f, 0.59f, 0.60f, 0.72f);
            starImage.preserveAspect = true;
        }
    }

    private void ApplySelectionState()
    {
        if (_selectionGlowImage != null)
        {
            // The selected state is communicated by the pin and carousel position.
            // Keep the legacy full-card glow hidden so it does not obscure the paper card.
            _selectionGlowImage.gameObject.SetActive(false);
        }
    }

    private void SetBackgroundColor(Color color)
    {
        if (_backgroundImage != null)
        {
            _backgroundImage.color = _frameImage != null ? Color.clear : color;
        }
    }

    private void SetProgress(int bestScore, int targetScore, bool visible)
    {
        if (_progressFillImage == null)
        {
            return;
        }

        _progressFillImage.transform.parent.gameObject.SetActive(visible);
        _progressFillImage.fillAmount = visible && targetScore > 0
            ? Mathf.Clamp01(bestScore / (float)targetScore)
            : 0f;
    }

    private static string GetStageName(int stageNumber)
    {
        return stageNumber switch
        {
            1 => "はじまりの街",
            2 => "急ぎの街道",
            3 => "まぼろし工房",
            4 => "こわれもの倉庫",
            5 => "大名行列",
            _ => $"おしごと {stageNumber}"
        };
    }

    private static void SetPanelColor(Image image, Color color)
    {
        if (image != null)
        {
            image.color = color;
        }
    }

    private static void SetPanelActive(Image image, bool active)
    {
        if (image != null)
        {
            image.gameObject.SetActive(active);
        }
    }

    private static string BuildStarBadge(int starRating)
    {
        StringBuilder builder = new StringBuilder(StarRatingUtility.MaxStars * FilledStarTag.Length);
        for (int i = 0; i < StarRatingUtility.MaxStars; i += 1)
        {
            builder.Append(i < starRating ? FilledStarTag : EmptyStarTag);
        }

        return builder.ToString();
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private static void SetColor(TMP_Text text, Color color)
    {
        if (text != null)
        {
            text.color = color;
        }
    }
}
