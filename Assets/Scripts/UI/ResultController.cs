using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ResultController : MonoBehaviour
{
    private const string MainSceneName = "Main";
    private const string TitleSceneName = "Title";
    private const string StageSelectSceneName = "StageSelect";
    private const string StageDatabaseResourcePath = "StageDatabase";
    private const string StageFormat = "STAGE {0:00}";
    private const string EndlessLabel = "ENDLESS";
    private const string ScoreFormat = "{0:N0}";
    private const string GameClearLabel = "GAME CLEAR";
    private const string GameOverLabel = "GAME OVER";
    private const string EndlessGameOverMessage = "ONE MISS GAME OVER";
    private const float ScoreCountDuration = 0.65f;
    private const float StarRevealInterval = 0.18f;
    private const float ResultStarSize = 128f;
    private const float ResultStarRowHeight = 138f;
    private const string GeneratedButtonLabelName = "GeneratedLabel";

    private static readonly Color SuccessColor = new(0.345f, 0.784f, 0.541f, 1f);
    private static readonly Color FailureColor = new(1f, 0.56f, 0.58f, 1f);
    private static readonly Color NeutralButtonColor = new(0.976f, 0.898f, 0.612f, 1f);
    private static readonly Color NeutralTextColor = new(0.137f, 0.184f, 0.275f, 1f);
    private static readonly Color PanelTextColor = new(1f, 1f, 1f, 1f);
    private static readonly Color MutedTextColor = new(0.84f, 0.93f, 1f, 1f);
    private static readonly Color DisabledButtonColor = new(1f, 1f, 1f, 0.78f);
    private static readonly Color DisabledTextColor = new(0.56f, 0.61f, 0.69f, 1f);
    private static readonly Color ClearTintColor = new(0.70f, 0.90f, 0.78f, 0.22f);
    private static readonly Color GameOverTintColor = new(0.98f, 0.74f, 0.74f, 0.22f);
    private static readonly Color RowBackgroundColor = new(0.057f, 0.165f, 0.22f, 0.66f);
    private static readonly Color ClearMissRowColor = new(0.18f, 0.42f, 0.34f, 0.58f);
    private static readonly Color GameOverMissRowColor = new(0.42f, 0.17f, 0.20f, 0.62f);
    private static readonly Color StarBaseColor = new(0.933f, 0.949f, 0.98f, 1f);
    private static readonly Color StarFilledBackground = new(1f, 0.949f, 0.8f, 1f);
    private static Sprite s_RuntimeButtonSprite;
    private static Texture2D s_RuntimeButtonTexture;

    private static readonly string[] StrongPanelTextPaths =
    {
        "SafeAreaRoot/ContentRoot/ScoreCard/Card/Body/TotalScoreValue",
        "SafeAreaRoot/ContentRoot/ScoreCard/Card/Body/BestScoreRow/BestScoreValue",
        "SafeAreaRoot/ContentRoot/BreakdownCard/Card/Body/StatList/Row_LightTruck/Value",
        "SafeAreaRoot/ContentRoot/BreakdownCard/Card/Body/StatList/Row_CompactCar/Value",
        "SafeAreaRoot/ContentRoot/BreakdownCard/Card/Body/StatList/Row_SportsCar/Value"
    };

    private static readonly string[] MutedPanelTextPaths =
    {
        "SafeAreaRoot/ContentRoot/ScoreCard/Card/Body/HeaderRow/ScoreLabel",
        "SafeAreaRoot/ContentRoot/ScoreCard/Card/Body/BestScoreRow/BestScoreLabel",
        "SafeAreaRoot/ContentRoot/BreakdownCard/Card/Body/DetailsTitle",
        "SafeAreaRoot/ContentRoot/BreakdownCard/Card/Body/StatList/Row_LightTruck/Label",
        "SafeAreaRoot/ContentRoot/BreakdownCard/Card/Body/StatList/Row_CompactCar/Label",
        "SafeAreaRoot/ContentRoot/BreakdownCard/Card/Body/StatList/Row_SportsCar/Label"
    };

    private static readonly string[] StatRowPaths =
    {
        "SafeAreaRoot/ContentRoot/BreakdownCard/Card/Body/StatList/Row_LightTruck",
        "SafeAreaRoot/ContentRoot/BreakdownCard/Card/Body/StatList/Row_CompactCar",
        "SafeAreaRoot/ContentRoot/BreakdownCard/Card/Body/StatList/Row_SportsCar"
    };

    private enum ResultButtonAction
    {
        None,
        Retry,
        Title,
        StageSelect,
        NextStage
    }

    private readonly struct BestUpdateInfo
    {
        public BestUpdateInfo(int bestScore, bool isNewBest)
        {
            BestScore = bestScore;
            IsNewBest = isNewBest;
        }

        public int BestScore { get; }
        public bool IsNewBest { get; }
    }

    private readonly struct NextStageNavigation
    {
        public NextStageNavigation(StageDefinition stageDefinition, bool loadsMainScene)
        {
            StageDefinition = stageDefinition;
            LoadsMainScene = loadsMainScene;
        }

        public StageDefinition StageDefinition { get; }
        public bool LoadsMainScene { get; }
        public bool HasDestination => StageDefinition != null;
    }

    [SerializeField, FormerlySerializedAs("scoreText")]
    private TMP_Text _scoreText;

    [SerializeField, FormerlySerializedAs("bestScoreText")]
    private TMP_Text _bestScoreText;

    [SerializeField, FormerlySerializedAs("stageText")]
    private TMP_Text _stageText;

    [SerializeField, FormerlySerializedAs("resultText")]
    private TMP_Text _resultText;

    [SerializeField]
    private TMP_Text _subMessageText;

    [SerializeField, FormerlySerializedAs("countAText")]
    private TMP_Text _countAText;

    [SerializeField, FormerlySerializedAs("countBText")]
    private TMP_Text _countBText;

    [SerializeField, FormerlySerializedAs("countCText")]
    private TMP_Text _countCText;

    [SerializeField]
    private TMP_Text _missLabelText;

    [SerializeField, FormerlySerializedAs("missCountText")]
    private TMP_Text _missCountText;

    [SerializeField]
    private CarVisualDatabase _visualDatabase;

    [SerializeField, FormerlySerializedAs("clearPanel")]
    private GameObject _clearPanel;

    [SerializeField, FormerlySerializedAs("gameOverPanel")]
    private GameObject _gameOverPanel;

    [SerializeField, FormerlySerializedAs("playerAnimationController")]
    private PlayerAnimationController _playerAnimationController;

    [SerializeField]
    private Image[] _starImages;

    [SerializeField]
    private Image[] _starGlowImages;

    [SerializeField]
    private TMP_Text[] _starLabels;

    [SerializeField]
    private GameObject _newBestBadge;

    [SerializeField]
    private Button _primaryActionButton;

    [SerializeField]
    private Button _secondaryLeftButton;

    [SerializeField]
    private Button _secondaryRightButton;

    [SerializeField]
    private TMP_Text _primaryActionLabel;

    [SerializeField]
    private TMP_Text _secondaryLeftLabel;

    [SerializeField]
    private TMP_Text _secondaryRightLabel;

    [SerializeField]
    private Image _primaryActionIcon;

    [SerializeField]
    private Image _secondaryLeftActionIcon;

    [SerializeField]
    private Image _secondaryRightActionIcon;

    [SerializeField]
    private Image _stateTintImage;

    [SerializeField]
    private Image _stageBadgeBackground;

    [SerializeField]
    private Image _headerAccentImage;

    [SerializeField]
    private Image _scoreAccentImage;

    [SerializeField]
    private Image _detailAccentImage;

    [SerializeField]
    private Image _missRowBackground;

    [SerializeField]
    private Image _lightTruckIcon;

    [SerializeField]
    private Image _compactCarIcon;

    [SerializeField]
    private Image _sportsCarIcon;

    [SerializeField]
    private GameObject _starRowRoot;

    [SerializeField]
    private Sprite _filledStarSprite;

    [SerializeField]
    private Sprite _emptyStarSprite;

    [SerializeField]
    private Sprite _retryButtonSprite;

    [SerializeField]
    private Sprite _titleButtonSprite;

    [SerializeField]
    private Sprite _nextStageButtonSprite;

    [SerializeField]
    private Sprite _stageSelectButtonSprite;

    [SerializeField]
    private Sprite _retryIconSprite;

    [SerializeField]
    private Sprite _stageSelectIconSprite;

    private StageDatabase _stageDatabase;
    private Coroutine _scoreCountCoroutine;
    private Coroutine _starRevealCoroutine;
    private GameMode _resultGameMode = GameMode.Stage;
    private int _resultStageNumber = 1;
    private bool _isNavigating;

    private void Awake()
    {
        _stageDatabase = Resources.Load<StageDatabase>(StageDatabaseResourcePath);
        _visualDatabase ??= CarVisualDatabase.LoadDefault();
        BindDynamicReferences();
        ApplyStarSizing();
        ApplyCarIcons();
    }

    private void Start()
    {
        ApplyResult();
    }

    private void OnDestroy()
    {
        StopRunningCoroutines();
        ClearButtonListeners(_primaryActionButton);
        ClearButtonListeners(_secondaryLeftButton);
        ClearButtonListeners(_secondaryRightButton);
    }

    public void OnRetryPressed()
    {
        NavigateWithInterstitial(() =>
        {
            SelectPlayableStage(_resultGameMode, _resultStageNumber);
            SceneManager.LoadScene(MainSceneName);
        });
    }

    public void OnTitlePressed()
    {
        NavigateWithInterstitial(() => SceneManager.LoadScene(TitleSceneName));
    }

    public void OnStageSelectPressed()
    {
        NavigateToStageSelect(_resultGameMode, _resultStageNumber);
    }

    public void OnNextStagePressed()
    {
        NextStageNavigation navigation = GetNextStageNavigation(_resultStageNumber);
        if (!navigation.HasDestination)
        {
            NavigateToStageSelect(_resultGameMode, _resultStageNumber);
            return;
        }

        int nextStageNumber = StageNumberUtility.Normalize(navigation.StageDefinition.StageNumber);
        if (!navigation.LoadsMainScene)
        {
            NavigateToStageSelect(GameMode.Stage, nextStageNumber);
            return;
        }

        NavigateWithInterstitial(() =>
        {
            SelectPlayableStage(GameMode.Stage, nextStageNumber);
            SceneManager.LoadScene(MainSceneName);
        });
    }

    private void BindDynamicReferences()
    {
        _scoreText ??= FindComponent<TMP_Text>("SafeAreaRoot/ContentRoot/ScoreCard/Card/Body/TotalScoreValue");
        _bestScoreText ??= FindComponent<TMP_Text>("SafeAreaRoot/ContentRoot/ScoreCard/Card/Body/BestScoreRow/BestScoreValue");
        _stageText ??= FindComponent<TMP_Text>("SafeAreaRoot/ContentRoot/HeroCard/Card/Body/StageBadge/StageText");
        _resultText ??= FindComponent<TMP_Text>("SafeAreaRoot/ContentRoot/HeroCard/Card/Body/ResultTitle");
        _subMessageText ??= FindComponent<TMP_Text>("SafeAreaRoot/ContentRoot/HeroCard/Card/Body/SubMessage");
        _countAText ??= FindComponent<TMP_Text>("SafeAreaRoot/ContentRoot/BreakdownCard/Card/Body/StatList/Row_LightTruck/Value");
        _countBText ??= FindComponent<TMP_Text>("SafeAreaRoot/ContentRoot/BreakdownCard/Card/Body/StatList/Row_CompactCar/Value");
        _countCText ??= FindComponent<TMP_Text>("SafeAreaRoot/ContentRoot/BreakdownCard/Card/Body/StatList/Row_SportsCar/Value");
        _lightTruckIcon ??= FindComponent<Image>("SafeAreaRoot/ContentRoot/BreakdownCard/Card/Body/StatList/Row_LightTruck/Icon");
        _compactCarIcon ??= FindComponent<Image>("SafeAreaRoot/ContentRoot/BreakdownCard/Card/Body/StatList/Row_CompactCar/Icon");
        _sportsCarIcon ??= FindComponent<Image>("SafeAreaRoot/ContentRoot/BreakdownCard/Card/Body/StatList/Row_SportsCar/Icon");
        _missLabelText ??= FindComponent<TMP_Text>("SafeAreaRoot/ContentRoot/BreakdownCard/Card/Body/StatList/Row_Misses/Label");
        _missCountText ??= FindComponent<TMP_Text>("SafeAreaRoot/ContentRoot/BreakdownCard/Card/Body/StatList/Row_Misses/Value");
        _newBestBadge ??= FindChild("SafeAreaRoot/ContentRoot/ScoreCard/Card/Body/NewBestBadge")?.gameObject;
        _primaryActionButton ??= FindComponent<Button>("SafeAreaRoot/ActionDock/RetryButton") ?? FindComponent<Button>("SafeAreaRoot/ActionDock/PrimaryButton");
        _secondaryLeftButton ??= FindComponent<Button>("SafeAreaRoot/ActionDock/TitleButton") ?? FindComponent<Button>("SafeAreaRoot/ActionDock/SecondaryRow/RetryButton");
        _secondaryRightButton ??= FindComponent<Button>("SafeAreaRoot/ActionDock/StageSelectButton") ?? FindComponent<Button>("SafeAreaRoot/ActionDock/SecondaryRow/StageSelectButton");
        _primaryActionLabel ??= FindComponent<TMP_Text>("SafeAreaRoot/ActionDock/RetryButton/Content/Label") ?? FindComponent<TMP_Text>("SafeAreaRoot/ActionDock/PrimaryButton/Content/Label");
        _secondaryLeftLabel ??= FindComponent<TMP_Text>("SafeAreaRoot/ActionDock/TitleButton/Content/Label") ?? FindComponent<TMP_Text>("SafeAreaRoot/ActionDock/SecondaryRow/RetryButton/Content/Label");
        _secondaryRightLabel ??= FindComponent<TMP_Text>("SafeAreaRoot/ActionDock/StageSelectButton/Content/Label") ?? FindComponent<TMP_Text>("SafeAreaRoot/ActionDock/SecondaryRow/StageSelectButton/Content/Label");
        _primaryActionIcon ??= FindComponent<Image>("SafeAreaRoot/ActionDock/RetryButton/Content/Icon") ?? FindComponent<Image>("SafeAreaRoot/ActionDock/PrimaryButton/Content/Icon");
        _secondaryLeftActionIcon ??= FindComponent<Image>("SafeAreaRoot/ActionDock/TitleButton/Content/Icon") ?? FindComponent<Image>("SafeAreaRoot/ActionDock/SecondaryRow/RetryButton/Content/Icon");
        _secondaryRightActionIcon ??= FindComponent<Image>("SafeAreaRoot/ActionDock/StageSelectButton/Content/Icon") ?? FindComponent<Image>("SafeAreaRoot/ActionDock/SecondaryRow/StageSelectButton/Content/Icon");
        _stateTintImage ??= FindComponent<Image>("Background/StateTint");
        _stageBadgeBackground ??= FindComponent<Image>("SafeAreaRoot/ContentRoot/HeroCard/Card/Body/StageBadge/BadgeBackground");
        _headerAccentImage ??= FindComponent<Image>("SafeAreaRoot/ContentRoot/HeroCard/Card/AccentBar");
        _scoreAccentImage ??= FindComponent<Image>("SafeAreaRoot/ContentRoot/ScoreCard/Card/AccentBar");
        _detailAccentImage ??= FindComponent<Image>("SafeAreaRoot/ContentRoot/BreakdownCard/Card/AccentBar");
        _missRowBackground ??= FindComponent<Image>("SafeAreaRoot/ContentRoot/BreakdownCard/Card/Body/StatList/Row_Misses");
        _starRowRoot ??= FindChild("SafeAreaRoot/ContentRoot/ScoreCard/Card/Body/StarRow")?.gameObject;
        _clearPanel ??= FindChild("Background/FX_Back/ClearPanel")?.gameObject;
        _gameOverPanel ??= FindChild("Background/FX_Back/GameOverPanel")?.gameObject;

        if (_starImages == null || _starImages.Length == 0 || _starLabels == null || _starLabels.Length == 0)
        {
            Transform starRow = FindChild("SafeAreaRoot/ContentRoot/ScoreCard/Card/Body/StarRow");
            if (starRow != null)
            {
                _starImages = new Image[Mathf.Min(starRow.childCount, StarRatingUtility.MaxStars)];
                _starGlowImages = new Image[_starImages.Length];
                _starLabels = new TMP_Text[_starImages.Length];
                for (int i = 0; i < _starImages.Length; i += 1)
                {
                    Transform star = starRow.GetChild(i);
                    _starImages[i] = star.GetComponent<Image>();
                    _starGlowImages[i] = star.Find("Glow")?.GetComponent<Image>();
                    _starLabels[i] = star.GetComponentInChildren<TMP_Text>(true);
                }
            }
        }
    }

    private T FindComponent<T>(string path) where T : Component
    {
        Transform target = FindChild(path);
        return target != null ? target.GetComponent<T>() : null;
    }

    private Transform FindChild(string path)
    {
        return transform.Find(path);
    }

    private void ApplyCarIcons()
    {
        ApplyCarIcon(_lightTruckIcon, CarType.LightTruck);
        ApplyCarIcon(_compactCarIcon, CarType.CompactCar);
        ApplyCarIcon(_sportsCarIcon, CarType.SportsCar);
    }

    private void ApplyCarIcon(Image targetImage, CarType carType)
    {
        if (targetImage == null)
        {
            return;
        }

        _visualDatabase ??= CarVisualDatabase.LoadDefault();
        Sprite sprite = _visualDatabase != null ? _visualDatabase.GetIconSprite(carType) : null;
        if (sprite == null)
        {
            targetImage.sprite = null;
            targetImage.enabled = false;
            return;
        }

        targetImage.sprite = sprite;
        targetImage.enabled = true;
        targetImage.color = Color.white;
        targetImage.preserveAspect = true;
    }

    private void ApplyResult()
    {
        GameResultData result = SessionState.LastResult ?? GameResultData.Empty(SessionState.SelectedGameMode, SessionState.SelectedStageNumber);
        int stageNumber = StageNumberUtility.Normalize(result.StageNumber);
        _resultGameMode = result.Mode;
        _resultStageNumber = stageNumber;
        int starRating = StarRatingUtility.CalculateForResult(result);
        BestUpdateInfo bestUpdate = UpdateBestResults(result.Mode, stageNumber, result.Score, starRating);
        StageDefinition stageDefinition = _stageDatabase != null
            ? (result.IsEndless ? _stageDatabase.GetEndlessStageDefinition(stageNumber) : _stageDatabase.GetStageDefinition(stageNumber))
            : null;

        ApplyCarIcons();
        SetText(_stageText, result.IsEndless ? EndlessLabel : string.Format(StageFormat, stageNumber));
        SetText(_scoreText, string.Format(ScoreFormat, 0));
        SetText(_bestScoreText, string.Format(ScoreFormat, bestUpdate.BestScore));
        SetText(_countAText, string.Format(ScoreFormat, result.LightTruckCount));
        SetText(_countBText, string.Format(ScoreFormat, result.CompactCarCount));
        SetText(_countCText, string.Format(ScoreFormat, result.SportsCarCount));
        SetText(_missLabelText, "Mistakes");
        SetText(_missCountText, FormatMissCount(result, stageDefinition));
        SetText(_resultText, result.IsClear ? GameClearLabel + "!" : GameOverLabel);
        SetText(_subMessageText, GetSubMessage(result, stageDefinition));

        if (_newBestBadge != null)
        {
            _newBestBadge.SetActive(bestUpdate.IsNewBest);
        }

        ApplyVisualState(result.IsClear, !result.IsEndless);
        ConfigureButtons(result);
        ResetStarDisplay();
        StartAnimations(result.Score, starRating, result.IsClear);

        if (_playerAnimationController != null)
        {
            if (result.IsClear)
            {
                _playerAnimationController.PlayWin();
            }
            else
            {
                _playerAnimationController.PlayCry();
            }
        }
    }

    private void ApplyVisualState(bool isClear, bool showStars)
    {
        Color stateColor = isClear ? SuccessColor : FailureColor;
        Color missLabelColor = isClear ? MutedTextColor : FailureColor;
        Color missValueColor = isClear ? PanelTextColor : FailureColor;

        ApplyPanelPalette();
        SetAccentBarsVisible(false);
        SetPanelActive(_clearPanel, isClear);
        SetPanelActive(_gameOverPanel, !isClear);
        SetImageColor(_stateTintImage, isClear ? ClearTintColor : GameOverTintColor);
        SetImageColor(_missRowBackground, isClear ? ClearMissRowColor : GameOverMissRowColor);
        SetPanelActive(_starRowRoot, showStars);

        if (_stageBadgeBackground != null)
        {
            _stageBadgeBackground.color = isClear
                ? Color.white
                : new Color(0.88f, 0.89f, 0.92f, 1f);
        }

        if (_resultText != null)
        {
            _resultText.color = stateColor;
        }

        if (_subMessageText != null)
        {
            _subMessageText.color = isClear ? MutedTextColor : FailureColor;
        }

        if (_missLabelText != null)
        {
            _missLabelText.color = missLabelColor;
        }

        if (_missCountText != null)
        {
            _missCountText.color = missValueColor;
        }
    }

    private void SetAccentBarsVisible(bool isVisible)
    {
        SetImageObjectActive(_headerAccentImage, isVisible);
        SetImageObjectActive(_scoreAccentImage, isVisible);
        SetImageObjectActive(_detailAccentImage, isVisible);
    }

    private void ApplyPanelPalette()
    {
        if (_stageText != null)
        {
            _stageText.color = NeutralTextColor;
        }

        foreach (string path in StrongPanelTextPaths)
        {
            SetTextColor(path, PanelTextColor);
        }

        foreach (string path in MutedPanelTextPaths)
        {
            SetTextColor(path, MutedTextColor);
        }

        foreach (string path in StatRowPaths)
        {
            SetImageColor(FindComponent<Image>(path), RowBackgroundColor);
        }
    }

    private void ApplyStarSizing()
    {
        if (_starRowRoot != null)
        {
            LayoutElement rowLayout = _starRowRoot.GetComponent<LayoutElement>();
            if (rowLayout != null)
            {
                rowLayout.preferredHeight = ResultStarRowHeight;
            }

            RectTransform rowRect = _starRowRoot.GetComponent<RectTransform>();
            if (rowRect != null)
            {
                rowRect.sizeDelta = new Vector2(rowRect.sizeDelta.x, ResultStarRowHeight);
            }
        }

        if (_starImages == null)
        {
            return;
        }

        foreach (Image starImage in _starImages)
        {
            if (starImage == null)
            {
                continue;
            }

            RectTransform starRect = starImage.rectTransform;
            starRect.sizeDelta = new Vector2(ResultStarSize, ResultStarSize);

            LayoutElement starLayout = starImage.GetComponent<LayoutElement>();
            if (starLayout != null)
            {
                starLayout.preferredWidth = ResultStarSize;
                starLayout.preferredHeight = ResultStarSize;
            }
        }
    }

    private void ConfigureButtons(GameResultData result)
    {
        if (result != null && result.IsClear && !result.IsEndless)
        {
            NextStageNavigation navigation = GetNextStageNavigation(_resultStageNumber);
            if (navigation.HasDestination)
            {
                ConfigureButton(_primaryActionButton, _primaryActionLabel, _primaryActionIcon, "Next Stage", ResultButtonAction.NextStage, true, NeutralTextColor);
                ConfigureButton(_secondaryLeftButton, _secondaryLeftLabel, _secondaryLeftActionIcon, "Retry", ResultButtonAction.Retry, true, NeutralTextColor);
                ConfigureButton(_secondaryRightButton, _secondaryRightLabel, _secondaryRightActionIcon, "Stage Select", ResultButtonAction.StageSelect, true, NeutralTextColor);
                return;
            }

            ConfigureButton(_primaryActionButton, _primaryActionLabel, _primaryActionIcon, "Stage Select", ResultButtonAction.StageSelect, true, NeutralTextColor);
            ConfigureButton(_secondaryLeftButton, _secondaryLeftLabel, _secondaryLeftActionIcon, "Retry", ResultButtonAction.Retry, true, NeutralTextColor);
            ConfigureButton(_secondaryRightButton, _secondaryRightLabel, _secondaryRightActionIcon, "Title", ResultButtonAction.Title, true, NeutralTextColor);
            return;
        }

        ConfigureButton(_primaryActionButton, _primaryActionLabel, _primaryActionIcon, "Retry", ResultButtonAction.Retry, true, NeutralTextColor);
        ConfigureButton(_secondaryLeftButton, _secondaryLeftLabel, _secondaryLeftActionIcon, "Title", ResultButtonAction.Title, true, NeutralTextColor);
        ConfigureButton(_secondaryRightButton, _secondaryRightLabel, _secondaryRightActionIcon, "Stage Select", ResultButtonAction.StageSelect, true, NeutralTextColor);
    }

    private void ConfigureButton(Button button, TMP_Text label, Image icon, string text, ResultButtonAction action, bool interactable, Color textColor)
    {
        if (button == null)
        {
            return;
        }

        button.gameObject.SetActive(true);
        Image buttonImage = button.targetGraphic as Image ?? button.GetComponent<Image>();
        Color resolvedBackground = NeutralButtonColor;
        Color resolvedText = interactable ? textColor : DisabledTextColor;
        Sprite resolvedSprite = ResolveButtonSprite(action);
        bool usesCompositeSprite = resolvedSprite != null;

        ApplyButtonLabel(button, label, !usesCompositeSprite, text, resolvedText);

        ApplyButtonIcon(icon, action, interactable, usesCompositeSprite);

        if (buttonImage != null)
        {
            buttonImage.sprite = resolvedSprite != null ? resolvedSprite : GetRuntimeButtonSprite();
            buttonImage.type = resolvedSprite != null ? Image.Type.Simple : Image.Type.Sliced;
            buttonImage.preserveAspect = resolvedSprite != null;
            buttonImage.color = resolvedSprite != null
                ? new Color(1f, 1f, 1f, interactable ? 1f : 0.5f)
                : resolvedBackground;
        }

        button.interactable = interactable;
        ClearButtonListeners(button);
        if (interactable)
        {
            button.onClick.AddListener(() => ExecuteAction(action));
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.selectedColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.96f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.92f);
        colors.disabledColor = DisabledButtonColor;
        colors.colorMultiplier = 1f;
        button.colors = colors;
    }

    private Sprite ResolveButtonSprite(ResultButtonAction action)
    {
        return action switch
        {
            ResultButtonAction.Retry => _retryButtonSprite,
            ResultButtonAction.Title => _titleButtonSprite,
            ResultButtonAction.StageSelect => _stageSelectButtonSprite,
            ResultButtonAction.NextStage => _nextStageButtonSprite,
            _ => null
        };
    }

    private Sprite ResolveButtonIconSprite(ResultButtonAction action)
    {
        return action switch
        {
            ResultButtonAction.Retry => _retryIconSprite,
            ResultButtonAction.StageSelect => _stageSelectIconSprite,
            _ => null
        };
    }

    private void ApplyButtonIcon(Image icon, ResultButtonAction action, bool interactable, bool hideForCompositeSprite)
    {
        if (icon == null)
        {
            return;
        }

        if (hideForCompositeSprite)
        {
            icon.enabled = false;
            icon.gameObject.SetActive(false);
            return;
        }

        icon.gameObject.SetActive(true);
        Sprite iconSprite = ResolveButtonIconSprite(action);
        if (iconSprite == null)
        {
            icon.sprite = null;
            icon.enabled = false;
            icon.gameObject.SetActive(false);
            return;
        }

        icon.sprite = iconSprite;
        icon.enabled = true;
        icon.color = new Color(1f, 1f, 1f, interactable ? 1f : 0.5f);
        icon.preserveAspect = true;
    }

    private static void ApplyButtonLabel(Button button, TMP_Text serializedLabel, bool isVisible, string text, Color color)
    {
        SetButtonLabel(serializedLabel, false, text, color);

        TMP_Text generatedLabel = FindGeneratedButtonLabel(button);
        if (!isVisible)
        {
            SetButtonLabel(generatedLabel, false, text, color);
            return;
        }

        TMP_Text targetLabel = serializedLabel != null ? serializedLabel : generatedLabel ?? CreateGeneratedButtonLabel(button);
        if (serializedLabel != null)
        {
            SetButtonLabel(generatedLabel, false, text, color);
        }

        SetButtonLabel(targetLabel, true, text, color);
    }

    private static void SetButtonLabel(TMP_Text label, bool isVisible, string text, Color color)
    {
        if (label == null)
        {
            return;
        }

        label.gameObject.SetActive(isVisible);
        if (!isVisible)
        {
            return;
        }

        label.text = text;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMax = 42f;
        label.fontSizeMin = 22f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;

        RectTransform rectTransform = label.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(18f, 10f);
        rectTransform.offsetMax = new Vector2(-18f, -10f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    private static TMP_Text FindGeneratedButtonLabel(Button button)
    {
        if (button == null)
        {
            return null;
        }

        Transform labelTransform = button.transform.Find(GeneratedButtonLabelName);
        return labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : null;
    }

    private static TMP_Text CreateGeneratedButtonLabel(Button button)
    {
        if (button == null)
        {
            return null;
        }

        GameObject labelObject = new(GeneratedButtonLabelName, typeof(RectTransform));
        RectTransform rectTransform = labelObject.GetComponent<RectTransform>();
        rectTransform.SetParent(button.transform, false);
        rectTransform.SetAsLastSibling();

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont != null)
        {
            label.font = defaultFont;
        }

        return label;
    }

    private static Sprite GetRuntimeButtonSprite()
    {
        if (s_RuntimeButtonSprite != null)
        {
            return s_RuntimeButtonSprite;
        }

        s_RuntimeButtonTexture = new Texture2D(8, 8, TextureFormat.RGBA32, false)
        {
            name = "ResultRuntimeButtonTexture",
            hideFlags = HideFlags.HideAndDontSave
        };

        Color32[] pixels = new Color32[8 * 8];
        for (int i = 0; i < pixels.Length; i += 1)
        {
            pixels[i] = new Color32(255, 255, 255, 255);
        }

        s_RuntimeButtonTexture.SetPixels32(pixels);
        s_RuntimeButtonTexture.Apply(false, true);

        s_RuntimeButtonSprite = Sprite.Create(
            s_RuntimeButtonTexture,
            new Rect(0f, 0f, 8f, 8f),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(3f, 3f, 3f, 3f));

        s_RuntimeButtonSprite.name = "ResultRuntimeButtonSprite";
        s_RuntimeButtonSprite.hideFlags = HideFlags.HideAndDontSave;
        return s_RuntimeButtonSprite;
    }

    private void ExecuteAction(ResultButtonAction action)
    {
        switch (action)
        {
            case ResultButtonAction.Retry:
                OnRetryPressed();
                break;
            case ResultButtonAction.Title:
                OnTitlePressed();
                break;
            case ResultButtonAction.StageSelect:
                OnStageSelectPressed();
                break;
            case ResultButtonAction.NextStage:
                OnNextStagePressed();
                break;
        }
    }

    private void NavigateToStageSelect(GameMode mode, int stageNumber)
    {
        NavigateWithInterstitial(() => LoadStageSelectFocused(mode, stageNumber));
    }

    private void NavigateWithInterstitial(Action loadSceneAction)
    {
        if (_isNavigating || loadSceneAction == null)
        {
            return;
        }

        _isNavigating = true;
        SetActionButtonsInteractable(false);
        UnityAdsManager.Instance.ShowInterstitialThenContinue(loadSceneAction);
    }

    private void SetActionButtonsInteractable(bool isInteractable)
    {
        if (_primaryActionButton != null)
        {
            _primaryActionButton.interactable = isInteractable;
        }

        if (_secondaryLeftButton != null)
        {
            _secondaryLeftButton.interactable = isInteractable;
        }

        if (_secondaryRightButton != null)
        {
            _secondaryRightButton.interactable = isInteractable;
        }
    }

    private void StartAnimations(int finalScore, int starRating, bool isClear)
    {
        StopRunningCoroutines();

        if (_scoreText != null)
        {
            _scoreCountCoroutine = StartCoroutine(CountScore(finalScore));
        }
        else
        {
            SetText(_scoreText, string.Format(ScoreFormat, finalScore));
        }

        if (isClear && starRating > 0)
        {
            _starRevealCoroutine = StartCoroutine(RevealStars(starRating));
        }
    }

    private void StopRunningCoroutines()
    {
        if (_scoreCountCoroutine != null)
        {
            StopCoroutine(_scoreCountCoroutine);
            _scoreCountCoroutine = null;
        }

        if (_starRevealCoroutine != null)
        {
            StopCoroutine(_starRevealCoroutine);
            _starRevealCoroutine = null;
        }
    }

    private IEnumerator CountScore(int finalScore)
    {
        float elapsed = 0f;
        while (elapsed < ScoreCountDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / ScoreCountDuration);
            int displayScore = Mathf.RoundToInt(Mathf.Lerp(0f, finalScore, t));
            SetText(_scoreText, string.Format(ScoreFormat, displayScore));
            yield return null;
        }

        SetText(_scoreText, string.Format(ScoreFormat, finalScore));
        _scoreCountCoroutine = null;
    }

    private IEnumerator RevealStars(int starRating)
    {
        int clampedStars = StarRatingUtility.Clamp(starRating);
        for (int i = 0; i < clampedStars; i += 1)
        {
            SetStarFilled(i);
            yield return new WaitForSecondsRealtime(StarRevealInterval);
        }

        _starRevealCoroutine = null;
    }

    private void ResetStarDisplay()
    {
        if (_starLabels == null || _starImages == null)
        {
            return;
        }

        int count = Mathf.Min(_starLabels.Length, _starImages.Length);
        for (int i = 0; i < count; i += 1)
        {
            if (_starLabels[i] != null)
            {
                _starLabels[i].text = string.Empty;
                _starLabels[i].color = new Color(1f, 1f, 1f, 0f);
            }

            if (_starImages[i] != null)
            {
                _starImages[i].sprite = _emptyStarSprite != null ? _emptyStarSprite : _starImages[i].sprite;
                _starImages[i].color = _emptyStarSprite != null ? Color.white : StarBaseColor;
                _starImages[i].type = Image.Type.Simple;
                _starImages[i].preserveAspect = true;
                _starImages[i].gameObject.SetActive(true);
            }

            if (_starGlowImages != null && i < _starGlowImages.Length && _starGlowImages[i] != null)
            {
                _starGlowImages[i].color = new Color(1f, 1f, 1f, 0f);
                _starGlowImages[i].enabled = false;
            }
        }
    }

    private void SetStarFilled(int index)
    {
        if (_starLabels == null || _starImages == null || index < 0 || index >= _starLabels.Length || index >= _starImages.Length)
        {
            return;
        }

        if (_starLabels[index] != null)
        {
            _starLabels[index].text = string.Empty;
            _starLabels[index].color = new Color(1f, 1f, 1f, 0f);
        }

        if (_starImages[index] != null)
        {
            if (_filledStarSprite != null)
            {
                _starImages[index].sprite = _filledStarSprite;
                _starImages[index].color = Color.white;
                _starImages[index].type = Image.Type.Simple;
                _starImages[index].preserveAspect = true;
            }
            else
            {
                _starImages[index].color = StarFilledBackground;
            }
        }

        if (_starGlowImages != null && index < _starGlowImages.Length && _starGlowImages[index] != null)
        {
            _starGlowImages[index].enabled = true;
            _starGlowImages[index].color = new Color(1f, 1f, 1f, 0.78f);
        }
    }

    private BestUpdateInfo UpdateBestResults(GameMode mode, int stageNumber, int score, int starRating)
    {
        if (mode == GameMode.Endless)
        {
            int endlessBestScore = SaveService.GetBestEndlessScore();
            bool isEndlessScoreUpdated = score > endlessBestScore;
            if (isEndlessScoreUpdated)
            {
                SaveService.SetBestEndlessScore(score);
                SaveService.Save();
                endlessBestScore = score;
            }

            return new BestUpdateInfo(endlessBestScore, isEndlessScoreUpdated);
        }

        int currentBestScore = SaveService.GetBestScore(stageNumber);
        int currentBestStars = SaveService.GetStarRating(stageNumber);
        bool isScoreUpdated = score > currentBestScore;
        bool isStarUpdated = starRating > currentBestStars;

        if (isScoreUpdated)
        {
            SaveService.SetBestScore(stageNumber, score);
            currentBestScore = score;
        }

        if (isStarUpdated)
        {
            SaveService.SetStarRating(stageNumber, starRating);
        }

        if (isScoreUpdated || isStarUpdated)
        {
            SaveService.Save();
        }

        return new BestUpdateInfo(currentBestScore, isScoreUpdated || isStarUpdated);
    }

    private NextStageNavigation GetNextStageNavigation(int stageNumber)
    {
        if (_stageDatabase == null)
        {
            return default;
        }

        StageDefinition nextStage = _stageDatabase.GetNextStageDefinition(stageNumber);
        if (nextStage == null)
        {
            return default;
        }

        return new NextStageNavigation(nextStage, nextStage.IsImplemented);
    }

    private static void SelectPlayableStage(GameMode mode, int stageNumber)
    {
        if (mode == GameMode.Endless)
        {
            StageSelectionService.SelectEndless(stageNumber);
            return;
        }

        StageSelectionService.SelectStage(stageNumber);
    }

    private static void LoadStageSelectFocused(GameMode mode, int stageNumber)
    {
        if (mode == GameMode.Endless)
        {
            StageSelectionService.RememberLastEndless(stageNumber);
        }
        else
        {
            StageSelectionService.RememberLastStage(stageNumber);
        }

        SceneManager.LoadScene(StageSelectSceneName);
    }

    private static string GetSubMessage(GameResultData result, StageDefinition stageDefinition)
    {
        if (result == null)
        {
            return "Try again";
        }

        if (result.IsEndless)
        {
            return EndlessGameOverMessage;
        }

        if (result.IsClear)
        {
            return "MISSION COMPLETE";
        }

        if (stageDefinition != null && stageDefinition.MissLimit > 0 && result.MissCount >= stageDefinition.MissLimit)
        {
            return $"MISS LIMIT REACHED ({result.MissCount}/{stageDefinition.MissLimit})";
        }

        return "TRY AGAIN";
    }

    private static string FormatMissCount(GameResultData result, StageDefinition stageDefinition)
    {
        if (result == null)
        {
            return string.Format(ScoreFormat, 0);
        }

        if (!result.IsClear && stageDefinition != null && stageDefinition.MissLimit > 0 && result.MissCount >= stageDefinition.MissLimit)
        {
            return $"{result.MissCount} / {stageDefinition.MissLimit}";
        }

        return string.Format(ScoreFormat, result.MissCount);
    }

    private static void ClearButtonListeners(Button button)
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }
    }

    private void SetTextColor(string path, Color color)
    {
        TMP_Text text = FindComponent<TMP_Text>(path);
        if (text != null)
        {
            text.color = color;
        }
    }

    private static void SetText(TMP_Text textElement, string value)
    {
        if (textElement != null)
        {
            textElement.text = value;
        }
    }

    private static void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }

    private static void SetImageColor(Image image, Color color)
    {
        if (image != null)
        {
            image.color = color;
        }
    }

    private static void SetImageObjectActive(Image image, bool isActive)
    {
        if (image != null)
        {
            image.gameObject.SetActive(isActive);
        }
    }
}
