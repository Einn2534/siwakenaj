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
    private const int MaxStarCount = 3;
    private const string FilledStarTag = "<color=#FFD45C>\u2605</color>";
    private const string EmptyStarTag = "<color=#FFD45C55>\u2605</color>";

    private static readonly Color LockedBackgroundColor = new(0.84f, 0.89f, 0.98f, 0.26f);
    private static readonly Color ComingSoonBackgroundColor = new(0.92f, 0.96f, 1f, 0.3f);
    private static readonly Color LockedPrimaryTextColor = new(0.84f, 0.9f, 0.98f, 0.9f);
    private static readonly Color LockedSecondaryTextColor = new(0.73f, 0.8f, 0.9f, 0.92f);
    private static readonly Color ComingSoonPrimaryTextColor = new(1f, 0.93f, 0.62f, 1f);
    private static readonly Color ComingSoonSecondaryTextColor = new(0.92f, 0.96f, 1f, 0.96f);
    private static readonly Color ClearedBestScoreColor = new(1f, 0.89f, 0.42f, 1f);
    private static readonly Color UnlockedThumbnailColor = Color.white;
    private static readonly Color LockedThumbnailColor = new(0.42f, 0.48f, 0.56f, 0.72f);
    private static readonly Color ComingSoonThumbnailColor = new(0.30f, 0.42f, 0.58f, 0.58f);

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
        _currentStageNumber = Mathf.Max(1, stageNumber);
        _currentStatus = status;

        SetText(_stageNumberText, $"STAGE <color=#35D7FF>{_currentStageNumber:00}</color>");

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

        int clampedStars = Mathf.Clamp(starRating, 0, MaxStarCount);
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
        SetText(_targetScoreText, $"<color=#FFD84D>TARGET</color>  {targetScore:N0}");
        SetText(_bestScoreText, bestScore > 0 ? $"<color=#FFE05D>BEST</color>  {bestScore:N0}" : "<color=#FFE05D>BEST</color>  -");
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
    }

    private void ApplyLockedState(int stageNumber, int requiredStageNumber)
    {
        int resolvedRequiredStageNumber = requiredStageNumber > 0
            ? requiredStageNumber
            : Mathf.Max(1, stageNumber - 1);

        SetText(_targetScoreText, $"CLEAR STAGE {resolvedRequiredStageNumber:00}");
        SetText(_bestScoreText, "TO UNLOCK");
        SetText(_statusText, "LOCKED");

        SetColor(_stageNumberText, LockedPrimaryTextColor);
        SetColor(_targetScoreText, LockedSecondaryTextColor);
        SetColor(_bestScoreText, LockedSecondaryTextColor);
        SetColor(_statusText, LockedPrimaryTextColor);
        SetBackgroundColor(LockedBackgroundColor);

        if (_statusText != null)
        {
            _statusText.gameObject.SetActive(true);
        }
    }

    private void ApplyComingSoonState()
    {
        SetText(_targetScoreText, "NEW STAGE");
        SetText(_bestScoreText, "IN PREPARATION");
        SetText(_statusText, "COMING SOON");

        SetColor(_stageNumberText, ComingSoonSecondaryTextColor);
        SetColor(_targetScoreText, ComingSoonSecondaryTextColor);
        SetColor(_bestScoreText, ComingSoonSecondaryTextColor);
        SetColor(_statusText, ComingSoonPrimaryTextColor);
        SetBackgroundColor(ComingSoonBackgroundColor);

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

        _thumbnailImage.preserveAspect = true;
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
                : new Color(0.75f, 0.84f, 0.95f, 0.82f);
        }

        if (_lockOverlayImage != null)
        {
            _lockOverlayImage.gameObject.SetActive(status == StageCardStatus.Locked);
        }
    }

    private void RefreshStarImages(int starRating, bool canShowStars)
    {
        int clampedStars = Mathf.Clamp(starRating, 0, MaxStarCount);
        for (int i = 0; i < _starImages.Length; i += 1)
        {
            Image starImage = _starImages[i];
            if (starImage == null)
            {
                continue;
            }

            bool isVisibleSlot = canShowStars && i < MaxStarCount;
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

            starImage.color = Color.white;
            starImage.preserveAspect = true;
        }
    }

    private void ApplySelectionState()
    {
        if (_selectionGlowImage != null)
        {
            _selectionGlowImage.gameObject.SetActive(_isSelected && _currentStatus == StageCardStatus.Unlocked);
        }
    }

    private void SetBackgroundColor(Color color)
    {
        if (_backgroundImage != null)
        {
            _backgroundImage.color = _frameImage != null ? Color.clear : color;
        }
    }

    private static string BuildStarBadge(int starRating)
    {
        StringBuilder builder = new StringBuilder(MaxStarCount * FilledStarTag.Length);
        for (int i = 0; i < MaxStarCount; i += 1)
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
