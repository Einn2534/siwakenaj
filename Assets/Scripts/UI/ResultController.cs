using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ResultController : MonoBehaviour
{
    private const string MainSceneName = "Main";
    private const string StageSelectSceneName = "StageSelect";
    private const string StageDatabaseResourcePath = "StageDatabase";
    private const string StageFormat = "STAGE {0:00}";
    private const string ScoreFormat = "{0:N0}";
    private const string GameClearLabel = "GAME CLEAR";
    private const string GameOverLabel = "GAME OVER";
    private const float ScoreCountDuration = 0.65f;
    private const float StarRevealInterval = 0.18f;
    private const int FallbackFinalStageNumber = 5;
    private const int MaxStarCount = 3;

    private static readonly Color SuccessColor = new(0.345f, 0.784f, 0.541f, 1f);
    private static readonly Color FailureColor = new(0.914f, 0.408f, 0.416f, 1f);
    private static readonly Color AccentColor = new(0.949f, 0.772f, 0.259f, 1f);
    private static readonly Color NeutralButtonColor = new(1f, 1f, 1f, 1f);
    private static readonly Color NeutralTextColor = new(0.137f, 0.184f, 0.275f, 1f);
    private static readonly Color MutedTextColor = new(0.44f, 0.49f, 0.58f, 1f);
    private static readonly Color DisabledButtonColor = new(1f, 1f, 1f, 0.78f);
    private static readonly Color DisabledTextColor = new(0.56f, 0.61f, 0.69f, 1f);
    private static readonly Color ClearTintColor = new(0.70f, 0.90f, 0.78f, 0.22f);
    private static readonly Color GameOverTintColor = new(0.98f, 0.74f, 0.74f, 0.22f);
    private static readonly Color ClearMissRowColor = new(0.975f, 0.982f, 0.992f, 1f);
    private static readonly Color GameOverMissRowColor = new(1f, 0.958f, 0.958f, 1f);
    private static readonly Color StarBaseColor = new(0.933f, 0.949f, 0.98f, 1f);
    private static readonly Color StarFilledBackground = new(1f, 0.949f, 0.8f, 1f);

    private enum ResultButtonAction
    {
        None,
        Retry,
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
    private GameObject _starRowRoot;

    [SerializeField]
    private Sprite _filledStarSprite;

    [SerializeField]
    private Sprite _emptyStarSprite;

    [SerializeField]
    private Sprite _retryButtonSprite;

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

    private void Awake()
    {
        _stageDatabase = Resources.Load<StageDatabase>(StageDatabaseResourcePath);
        BindDynamicReferences();
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
        SceneManager.LoadScene(MainSceneName);
    }

    public void OnStageSelectPressed()
    {
        SceneManager.LoadScene(StageSelectSceneName);
    }

    public void OnNextStagePressed()
    {
        int nextStageNumber = Mathf.Min(GetFinalStageNumber(), SessionState.SelectedStageNumber + 1);
        SessionState.SelectStage(nextStageNumber);
        SaveService.SetLastStage(nextStageNumber);
        SaveService.Save();
        SceneManager.LoadScene(MainSceneName);
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
        _missLabelText ??= FindComponent<TMP_Text>("SafeAreaRoot/ContentRoot/BreakdownCard/Card/Body/StatList/Row_Misses/Label");
        _missCountText ??= FindComponent<TMP_Text>("SafeAreaRoot/ContentRoot/BreakdownCard/Card/Body/StatList/Row_Misses/Value");
        _newBestBadge ??= FindChild("SafeAreaRoot/ContentRoot/ScoreCard/Card/Body/NewBestBadge")?.gameObject;
        _primaryActionButton ??= FindComponent<Button>("SafeAreaRoot/ActionDock/PrimaryButton");
        _secondaryLeftButton ??= FindComponent<Button>("SafeAreaRoot/ActionDock/SecondaryRow/RetryButton");
        _secondaryRightButton ??= FindComponent<Button>("SafeAreaRoot/ActionDock/SecondaryRow/StageSelectButton");
        _primaryActionLabel ??= FindComponent<TMP_Text>("SafeAreaRoot/ActionDock/PrimaryButton/Content/Label");
        _secondaryLeftLabel ??= FindComponent<TMP_Text>("SafeAreaRoot/ActionDock/SecondaryRow/RetryButton/Content/Label");
        _secondaryRightLabel ??= FindComponent<TMP_Text>("SafeAreaRoot/ActionDock/SecondaryRow/StageSelectButton/Content/Label");
        _primaryActionIcon ??= FindComponent<Image>("SafeAreaRoot/ActionDock/PrimaryButton/Content/Icon");
        _secondaryLeftActionIcon ??= FindComponent<Image>("SafeAreaRoot/ActionDock/SecondaryRow/RetryButton/Content/Icon");
        _secondaryRightActionIcon ??= FindComponent<Image>("SafeAreaRoot/ActionDock/SecondaryRow/StageSelectButton/Content/Icon");
        _stateTintImage ??= FindComponent<Image>("Background/StateTint");
        _stageBadgeBackground ??= FindComponent<Image>("SafeAreaRoot/ContentRoot/HeroCard/Card/Body/StageBadge/BadgeBackground");
        _headerAccentImage ??= FindComponent<Image>("SafeAreaRoot/ContentRoot/HeroCard/Card/AccentBar");
        _scoreAccentImage ??= FindComponent<Image>("SafeAreaRoot/ContentRoot/ScoreCard/Card/AccentBar");
        _detailAccentImage ??= FindComponent<Image>("SafeAreaRoot/ContentRoot/BreakdownCard/Card/AccentBar");
        _missRowBackground ??= FindComponent<Image>("SafeAreaRoot/ContentRoot/BreakdownCard/Card/Body/StatList/Row_Misses");
        _starRowRoot ??= FindChild("SafeAreaRoot/ContentRoot/HeroCard/Card/Body/StarRow")?.gameObject;
        _clearPanel ??= FindChild("Background/FX_Back/ClearPanel")?.gameObject;
        _gameOverPanel ??= FindChild("Background/FX_Back/GameOverPanel")?.gameObject;

        if (_starImages == null || _starImages.Length == 0 || _starLabels == null || _starLabels.Length == 0)
        {
            Transform starRow = FindChild("SafeAreaRoot/ContentRoot/HeroCard/Card/Body/StarRow");
            if (starRow != null)
            {
                _starImages = new Image[Mathf.Min(starRow.childCount, MaxStarCount)];
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

    private void ApplyResult()
    {
        GameResultData result = SessionState.LastResult ?? GameResultData.Empty(SessionState.SelectedStageNumber);
        int stageNumber = Mathf.Max(1, result.StageNumber);
        int starRating = CalculateStarRating(result);
        BestUpdateInfo bestUpdate = UpdateBestResults(stageNumber, result.Score, starRating);
        StageDefinition stageDefinition = _stageDatabase != null ? _stageDatabase.GetStageDefinition(stageNumber) : null;

        SetText(_stageText, string.Format(StageFormat, stageNumber));
        SetText(_scoreText, string.Format(ScoreFormat, 0));
        SetText(_bestScoreText, string.Format(ScoreFormat, bestUpdate.BestScore));
        SetText(_countAText, string.Format(ScoreFormat, result.LightTruckCount));
        SetText(_countBText, string.Format(ScoreFormat, result.CompactCarCount));
        SetText(_countCText, string.Format(ScoreFormat, result.SportsCarCount));
        SetText(_missLabelText, "Misses");
        SetText(_missCountText, FormatMissCount(result, stageDefinition));
        SetText(_resultText, result.IsClear ? GameClearLabel + "!" : GameOverLabel);
        SetText(_subMessageText, GetSubMessage(result, stageDefinition));

        if (_newBestBadge != null)
        {
            _newBestBadge.SetActive(bestUpdate.IsNewBest);
        }

        ApplyVisualState(result.IsClear);
        ConfigureButtons(result.IsClear, stageNumber);
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

    private void ApplyVisualState(bool isClear)
    {
        Color stateColor = isClear ? SuccessColor : FailureColor;
        Color missLabelColor = isClear ? MutedTextColor : FailureColor;
        Color missValueColor = isClear ? NeutralTextColor : FailureColor;

        SetPanelActive(_clearPanel, isClear);
        SetPanelActive(_gameOverPanel, !isClear);
        SetImageColor(_stateTintImage, isClear ? ClearTintColor : GameOverTintColor);
        SetImageColor(_headerAccentImage, stateColor);
        SetImageColor(_scoreAccentImage, stateColor);
        SetImageColor(_detailAccentImage, stateColor);
        SetImageColor(_missRowBackground, isClear ? ClearMissRowColor : GameOverMissRowColor);
        SetPanelActive(_starRowRoot, isClear);

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

    private void ConfigureButtons(bool isClear, int stageNumber)
    {
        int finalStageNumber = GetFinalStageNumber();
        bool canAdvance = stageNumber < finalStageNumber;

        if (isClear && canAdvance)
        {
            ConfigureButton(_primaryActionButton, _primaryActionLabel, _primaryActionIcon, "Next Stage", ResultButtonAction.NextStage, true, true, new Color(0.43f, 0.24f, 0f, 1f));
            ConfigureButton(_secondaryLeftButton, _secondaryLeftLabel, _secondaryLeftActionIcon, "Retry", ResultButtonAction.Retry, true, false, NeutralTextColor);
            ConfigureButton(_secondaryRightButton, _secondaryRightLabel, _secondaryRightActionIcon, "Stage Select", ResultButtonAction.StageSelect, true, false, NeutralTextColor);
            return;
        }

        if (isClear)
        {
            ConfigureButton(_primaryActionButton, _primaryActionLabel, _primaryActionIcon, "Stage Select", ResultButtonAction.StageSelect, true, true, new Color(0.43f, 0.24f, 0f, 1f));
            ConfigureButton(_secondaryLeftButton, _secondaryLeftLabel, _secondaryLeftActionIcon, "Retry", ResultButtonAction.Retry, true, false, NeutralTextColor);
            ConfigureButton(_secondaryRightButton, _secondaryRightLabel, _secondaryRightActionIcon, "Next Stage", ResultButtonAction.NextStage, false, false, DisabledTextColor);
            return;
        }

        ConfigureButton(_primaryActionButton, _primaryActionLabel, _primaryActionIcon, "Retry", ResultButtonAction.Retry, true, true, new Color(0.43f, 0.24f, 0f, 1f));
        ConfigureButton(_secondaryLeftButton, _secondaryLeftLabel, _secondaryLeftActionIcon, "Stage Select", ResultButtonAction.StageSelect, true, false, NeutralTextColor);
        ConfigureButton(_secondaryRightButton, _secondaryRightLabel, _secondaryRightActionIcon, "Next Stage", ResultButtonAction.NextStage, false, false, DisabledTextColor);
    }

    private void ConfigureButton(Button button, TMP_Text label, Image icon, string text, ResultButtonAction action, bool interactable, bool usePrimaryStyle, Color textColor)
    {
        if (button == null)
        {
            return;
        }

        Image buttonImage = button.targetGraphic as Image ?? button.GetComponent<Image>();
        Color resolvedBackground = usePrimaryStyle ? AccentColor : NeutralButtonColor;
        Color resolvedText = interactable ? textColor : DisabledTextColor;
        Sprite resolvedSprite = ResolveButtonSprite(usePrimaryStyle);

        if (label != null)
        {
            label.text = text;
            label.color = resolvedText;
        }

        ApplyButtonIcon(icon, action, interactable);

        if (buttonImage != null)
        {
            buttonImage.sprite = resolvedSprite != null ? resolvedSprite : buttonImage.sprite;
            buttonImage.type = Image.Type.Sliced;
            buttonImage.preserveAspect = false;
            buttonImage.color = resolvedSprite != null
                ? new Color(1f, 1f, 1f, interactable ? 1f : 0.5f)
                : resolvedBackground;
        }

        button.interactable = interactable;
        button.gameObject.SetActive(true);
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

    private Sprite ResolveButtonSprite(bool usePrimaryStyle)
    {
        return usePrimaryStyle ? _nextStageButtonSprite : _retryButtonSprite;
    }

    private Sprite ResolveButtonIconSprite(ResultButtonAction action)
    {
        return action switch
        {
            ResultButtonAction.Retry => _retryIconSprite,
            _ => null
        };
    }

    private void ApplyButtonIcon(Image icon, ResultButtonAction action, bool interactable)
    {
        if (icon == null)
        {
            return;
        }

        Sprite iconSprite = ResolveButtonIconSprite(action);
        icon.sprite = iconSprite;
        icon.enabled = iconSprite != null;
        icon.color = new Color(1f, 1f, 1f, interactable ? 1f : 0.5f);
        icon.preserveAspect = true;
    }

    private void ExecuteAction(ResultButtonAction action)
    {
        switch (action)
        {
            case ResultButtonAction.Retry:
                OnRetryPressed();
                break;
            case ResultButtonAction.StageSelect:
                OnStageSelectPressed();
                break;
            case ResultButtonAction.NextStage:
                OnNextStagePressed();
                break;
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
        int clampedStars = Mathf.Clamp(starRating, 0, MaxStarCount);
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

    private BestUpdateInfo UpdateBestResults(int stageNumber, int score, int starRating)
    {
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

    private int GetFinalStageNumber()
    {
        int finalStageNumber = 0;
        if (_stageDatabase != null)
        {
            for (int i = 0; i < _stageDatabase.Stages.Count; i += 1)
            {
                StageDefinition stage = _stageDatabase.Stages[i];
                if (stage != null && stage.IsImplemented)
                {
                    finalStageNumber = Mathf.Max(finalStageNumber, stage.StageNumber);
                }
            }
        }

        return finalStageNumber > 0 ? finalStageNumber : FallbackFinalStageNumber;
    }

    private static int CalculateStarRating(GameResultData result)
    {
        if (result == null || !result.IsClear)
        {
            return 0;
        }

        int rating = result.MissCount switch
        {
            <= 0 => 3,
            1 => 2,
            _ => 1
        };

        return Mathf.Clamp(rating, 1, MaxStarCount);
    }

    private static string GetSubMessage(GameResultData result, StageDefinition stageDefinition)
    {
        if (result == null)
        {
            return "Try again";
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
}
