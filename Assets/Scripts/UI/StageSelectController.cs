using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class StageSelectController : MonoBehaviour
{
    private const string MainSceneName = "Main";
    private const string TitleSceneName = "Title";
    private const string StageDatabaseResourcePath = "StageDatabase";
    private const string SettingsIconResourcePath = "UI/ui_settings_icon";
    private const string HowToIconResourcePath = "UI/ui_howto_icon";
    private const string ToggleOnResourcePath = "UI/ui_settings_toggle_on";
    private const string SwipeHintSeenKey = "StageSelect_SwipeHintSeen";
    private const float SettingsPanelWidth = 900f;
    private const float SettingsPanelHeight = 1080f;
    private const float SettingsRowHeight = 204f;
    private const float SettingsSliderHandleWidth = 34f;
    private const float SettingsSliderHandleHeight = 2f;
    private const float HowToPanelWidth = 900f;
    private const float HowToPanelHeight = 1260f;
    private const float StageNavButtonWidth = 112f;
    private const float StageNavButtonHeight = 132f;
    private const float StageNavButtonInset = 46f;
    private const float StagePageDotWidth = 34f;
    private const float StagePageDotHeight = 8f;

    private static readonly Color ModalScrimColor = new(0.04f, 0.07f, 0.11f, 0.58f);
    private static readonly Color CardColor = new(1f, 1f, 1f, 0.96f);
    private static readonly Color CardShadowColor = new(0.08f, 0.15f, 0.24f, 0.12f);
    private static readonly Color TextColor = new(0.137f, 0.184f, 0.275f, 1f);
    private static readonly Color MutedTextColor = new(0.44f, 0.49f, 0.58f, 1f);
    private static readonly Color RowColor = new(0.975f, 0.982f, 0.992f, 1f);
    private static readonly Color DividerColor = new(0.902f, 0.922f, 0.953f, 1f);
    private static readonly Color SuccessColor = new(0.345f, 0.784f, 0.541f, 1f);
    private static readonly Color BlueAccentColor = new(0.219f, 0.643f, 0.94f, 1f);
    private static readonly Color WarningAccentColor = new(0.949f, 0.772f, 0.259f, 1f);
    private static readonly Color DangerAccentColor = new(0.914f, 0.408f, 0.416f, 1f);
    private static readonly Color InkColor = new(0.025f, 0.075f, 0.145f, 1f);
    private static readonly Color StageNavButtonColor = new(1f, 0.97f, 0.84f, 0.86f);
    private static readonly Color StageNavDisabledColor = new(1f, 0.97f, 0.84f, 0.34f);
    private static readonly Color ActiveDotColor = new(1f, 0.86f, 0.25f, 1f);
    private static readonly Color InactiveDotColor = new(1f, 1f, 1f, 0.42f);
    private static Sprite s_RuntimeUiSprite;

    [SerializeField, FormerlySerializedAs("swipeSnapController")]
    private SwipeSnapController _swipeSnapController;

    [SerializeField, FormerlySerializedAs("stageCardViews")]
    private StageCardView[] _stageCardViews;

    [SerializeField]
    private RectTransform _stageCardContainer;

    [SerializeField]
    private StageCardView _stageCardPrefab;

    [SerializeField, FormerlySerializedAs("playButton")]
    private Button _playButton;

    private StageDatabase _stageDatabase;
    private StageCardView _stageCardTemplate;
    private GameObject _settingsOverlay;
    private GameObject _howToOverlay;
    private Button _settingsButton;
    private Button _howToButton;
    private Button _settingsCloseButton;
    private Button _howToCloseButton;
    private Button _howToTopCloseButton;
    private Button _howToTutorialButton;
    private Button _previousStageButton;
    private Button _nextStageButton;
    private RectTransform _pageDotContainer;
    private TMP_Text _swipeHintText;
    private readonly List<Image> _pageDots = new();
    private Sprite _toggleOnSprite;
    private GameMode _selectedGameMode = GameMode.Stage;
    private int _selectedStageNumber = 1;
    private bool _utilityUiBuilt;

    private void OnEnable()
    {
        if (_stageDatabase != null)
        {
            UpdateCards();
        }
    }

    private IEnumerator Start()
    {
        SoundManager.EnsureInstance().PlayStageSelectBgm();
        BuildUtilityUi();

        _stageDatabase = Resources.Load<StageDatabase>(StageDatabaseResourcePath);
        EnsureStageCardViews();

        if (_swipeSnapController != null)
        {
            _swipeSnapController.OnPageChanged += OnSelectionChanged;
        }

        UpdateCards();
        yield return null;

        int lastStageNumber = SaveService.GetLastStage();
        int startIndex = GetInitialStageIndex(lastStageNumber);
        _swipeSnapController?.JumpToIndex(startIndex);
        ApplySelectionIndex(startIndex);
    }

    private void OnDestroy()
    {
        if (_swipeSnapController != null)
        {
            _swipeSnapController.OnPageChanged -= OnSelectionChanged;
        }

        RemoveUtilityListeners();
    }

    public void OnSelectionChanged(int index)
    {
        MarkSwipeHintSeen();
        ApplySelectionIndex(index);
    }

    public void OnPlayPressed()
    {
        if (!CanPlaySelectedStage())
        {
            return;
        }

        if (_selectedGameMode == GameMode.Endless)
        {
            StageSelectionService.SelectEndless(_selectedStageNumber);
        }
        else
        {
            StageSelectionService.SelectStage(_selectedStageNumber);
        }

        SceneManager.LoadScene(MainSceneName);
    }

    public void OnBackPressed()
    {
        SceneManager.LoadScene(TitleSceneName);
    }

    public void OnSettingsOpen()
    {
        SetPanelActive(_howToOverlay, false);
        SetPanelActive(_settingsOverlay, true);
    }

    public void OnSettingsClose()
    {
        SetPanelActive(_settingsOverlay, false);
    }

    public void OnHowToOpen()
    {
        SetPanelActive(_settingsOverlay, false);
        SetPanelActive(_howToOverlay, true);
    }

    public void OnHowToClose()
    {
        SetPanelActive(_howToOverlay, false);
        SaveService.SetHowToShown(true);
        SaveService.Save();
    }

    public void OnTutorialReplayPressed()
    {
        SaveService.SetHowToShown(true);
        SaveService.Save();
        StageSelectionService.SelectStage(TutorialLaunchService.TutorialStageNumber);
        TutorialLaunchService.RequestReplay();
        SceneManager.LoadScene(MainSceneName);
    }

    public void OnPreviousStagePressed()
    {
        if (_swipeSnapController == null)
        {
            return;
        }

        MarkSwipeHintSeen();
        int previousIndex = Mathf.Max(0, _swipeSnapController.GetCurrentIndex() - 1);
        _swipeSnapController.JumpToIndex(previousIndex);
        ApplySelectionIndex(previousIndex);
    }

    public void OnNextStagePressed()
    {
        if (_swipeSnapController == null)
        {
            return;
        }

        MarkSwipeHintSeen();
        int nextIndex = Mathf.Min(Mathf.Max(0, GetSelectableCardCount() - 1), _swipeSnapController.GetCurrentIndex() + 1);
        _swipeSnapController.JumpToIndex(nextIndex);
        ApplySelectionIndex(nextIndex);
    }

    public int GetSelectedStageNumber()
    {
        return _selectedStageNumber;
    }

    public GameMode GetSelectedGameMode()
    {
        return _selectedGameMode;
    }

    private void BuildUtilityUi()
    {
        if (_utilityUiBuilt)
        {
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        RectTransform canvasTransform = canvas != null ? canvas.transform as RectTransform : null;
        RectTransform safeArea = ResolveSafeArea(canvasTransform);
        if (canvasTransform == null || safeArea == null)
        {
            return;
        }

        _utilityUiBuilt = true;
        Sprite slicedSprite = GetBuiltinUiSprite();
        _toggleOnSprite = Resources.Load<Sprite>(ToggleOnResourcePath);

        _settingsButton = CreateRoundMenuButton("SettingsButtonTop", safeArea, Resources.Load<Sprite>(SettingsIconResourcePath), "\u8a2d\u5b9a", slicedSprite, "SET");
        SetAnchored((RectTransform)_settingsButton.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(108f, -90f), new Vector2(148f, 148f));
        _settingsButton.onClick.AddListener(OnSettingsOpen);

        _howToButton = CreateRoundMenuButton("HowToButtonTop", safeArea, Resources.Load<Sprite>(HowToIconResourcePath), "\u3042\u305d\u3073\u304b\u305f", slicedSprite, "?");
        SetAnchored((RectTransform)_howToButton.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-108f, -90f), new Vector2(148f, 148f));
        _howToButton.onClick.AddListener(OnHowToOpen);
        BuildStageNavigationUi(safeArea, slicedSprite);

        _settingsOverlay = BuildSettingsOverlay(canvasTransform, slicedSprite).gameObject;
        _howToOverlay = BuildHowToOverlay(canvasTransform, slicedSprite).gameObject;

        SetPanelActive(_settingsOverlay, false);
        SetPanelActive(_howToOverlay, false);

        if (!SaveService.GetHowToShown())
        {
            OnHowToOpen();
        }
    }

    private void RemoveUtilityListeners()
    {
        if (_settingsButton != null)
        {
            _settingsButton.onClick.RemoveListener(OnSettingsOpen);
        }

        if (_howToButton != null)
        {
            _howToButton.onClick.RemoveListener(OnHowToOpen);
        }

        if (_settingsCloseButton != null)
        {
            _settingsCloseButton.onClick.RemoveListener(OnSettingsClose);
        }

        if (_howToCloseButton != null)
        {
            _howToCloseButton.onClick.RemoveListener(OnHowToClose);
        }

        if (_howToTopCloseButton != null)
        {
            _howToTopCloseButton.onClick.RemoveListener(OnHowToClose);
        }

        if (_howToTutorialButton != null)
        {
            _howToTutorialButton.onClick.RemoveListener(OnTutorialReplayPressed);
        }

        if (_previousStageButton != null)
        {
            _previousStageButton.onClick.RemoveListener(OnPreviousStagePressed);
        }

        if (_nextStageButton != null)
        {
            _nextStageButton.onClick.RemoveListener(OnNextStagePressed);
        }
    }

    private RectTransform BuildSettingsOverlay(RectTransform canvasTransform, Sprite slicedSprite)
    {
        RectTransform overlay = CreatePanel("SettingsOverlay", canvasTransform, slicedSprite, ModalScrimColor);
        Stretch(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform dialog = CreateModalDialog("SettingsPanel", overlay, slicedSprite, new Vector2(SettingsPanelWidth, SettingsPanelHeight));
        BuildModalHeader(dialog, "SETTINGS", slicedSprite, out _settingsCloseButton);
        _settingsCloseButton.onClick.AddListener(OnSettingsClose);

        RectTransform body = CreateUiObject("Body", dialog);
        Stretch(body, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(60f, 150f), new Vector2(-60f, -128f));

        VerticalLayoutGroup bodyLayout = body.gameObject.AddComponent<VerticalLayoutGroup>();
        bodyLayout.spacing = 24f;
        bodyLayout.childAlignment = TextAnchor.UpperCenter;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = false;

        CreateSettingsRow(body, "BGM", "TITLE / STAGE MUSIC", SuccessColor, _toggleOnSprite, out RectTransform bgmRow);
        CreateSettingsVolumeControl(bgmRow, "BGM", SuccessColor, slicedSprite);
        CreateSettingsRow(body, "SE", "BUTTON / JUDGE SOUNDS", BlueAccentColor, _toggleOnSprite, out RectTransform seRow);
        CreateSettingsVolumeControl(seRow, "SE", BlueAccentColor, slicedSprite);
        CreateSettingsRow(body, "VIBRATION", "JUDGE / RESULT FEEDBACK", WarningAccentColor, _toggleOnSprite, out _);

        dialog.gameObject.AddComponent<SettingsPanelController>();
        overlay.gameObject.SetActive(false);
        return overlay;
    }

    private RectTransform BuildHowToOverlay(RectTransform canvasTransform, Sprite slicedSprite)
    {
        RectTransform overlay = CreatePanel("HowToOverlay", canvasTransform, slicedSprite, ModalScrimColor);
        Stretch(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform dialog = CreateModalDialog("HowToPanel", overlay, slicedSprite, new Vector2(HowToPanelWidth, HowToPanelHeight));
        BuildModalHeader(dialog, "HOW TO", slicedSprite, out _howToTopCloseButton);
        _howToTopCloseButton.onClick.AddListener(OnHowToClose);

        RectTransform body = CreateUiObject("Body", dialog);
        Stretch(body, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(58f, 138f), new Vector2(-58f, -164f));

        VerticalLayoutGroup bodyLayout = body.gameObject.AddComponent<VerticalLayoutGroup>();
        bodyLayout.spacing = 18f;
        bodyLayout.childAlignment = TextAnchor.UpperCenter;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = false;

        CarVisualDatabase visualDatabase = CarVisualDatabase.LoadDefault();
        CreateHowToStep(body, "01", "COMING CAR", "\u6765\u305f\u8eca\u3068\u540c\u3058\u30dc\u30bf\u30f3\u3092\u62bc\u305d\u3046", BlueAccentColor, GetCarIcon(visualDatabase, CarType.LightTruck), slicedSprite);
        CreateHowToStep(body, "02", "OK / GOOD", "\u6b63\u3057\u304f\u4ed5\u5206\u3051\u308b\u3068\u30b9\u30b3\u30a2\u30a2\u30c3\u30d7", SuccessColor, GetCarIcon(visualDatabase, CarType.CompactCar), slicedSprite);
        CreateHowToStep(body, "03", "MISS LIMIT", "MISS \u304c\u4e0a\u9650\u306b\u5c4a\u304f\u3068 GAME OVER", DangerAccentColor, GetCarIcon(visualDatabase, CarType.SportsCar), slicedSprite);

        _howToCloseButton = CreateModalCloseButton(dialog, "CloseButton", "OK", slicedSprite, new Vector2(-210f, 34f), new Vector2(360f, 136f));
        _howToCloseButton.onClick.AddListener(OnHowToClose);
        _howToTutorialButton = CreateModalCloseButton(dialog, "TutorialButton", "TUTORIAL", slicedSprite, new Vector2(210f, 34f), new Vector2(360f, 136f));
        _howToTutorialButton.onClick.AddListener(OnTutorialReplayPressed);

        overlay.gameObject.SetActive(false);
        return overlay;
    }

    private void BuildStageNavigationUi(RectTransform safeArea, Sprite slicedSprite)
    {
        _previousStageButton = CreateButton("PreviousStageButton", safeArea, slicedSprite, new Vector2(StageNavButtonWidth, StageNavButtonHeight), "<", 58f, 34f, FontStyles.Bold, TextColor);
        SetAnchored((RectTransform)_previousStageButton.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(StageNavButtonInset, 0f), new Vector2(StageNavButtonWidth, StageNavButtonHeight));
        _previousStageButton.GetComponent<Image>().color = StageNavButtonColor;
        _previousStageButton.onClick.AddListener(OnPreviousStagePressed);
        AddShadow(_previousStageButton.gameObject, new Color(0.02f, 0.06f, 0.11f, 0.34f), new Vector2(0f, -5f));

        _nextStageButton = CreateButton("NextStageButton", safeArea, slicedSprite, new Vector2(StageNavButtonWidth, StageNavButtonHeight), ">", 58f, 34f, FontStyles.Bold, TextColor);
        SetAnchored((RectTransform)_nextStageButton.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-StageNavButtonInset, 0f), new Vector2(StageNavButtonWidth, StageNavButtonHeight));
        _nextStageButton.GetComponent<Image>().color = StageNavButtonColor;
        _nextStageButton.onClick.AddListener(OnNextStagePressed);
        AddShadow(_nextStageButton.gameObject, new Color(0.02f, 0.06f, 0.11f, 0.34f), new Vector2(0f, -5f));

        _pageDotContainer = CreateUiObject("StagePageDots", safeArea);
        SetAnchored(_pageDotContainer, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(520f, 36f));

        HorizontalLayoutGroup dotLayout = _pageDotContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
        dotLayout.childAlignment = TextAnchor.MiddleCenter;
        dotLayout.childControlWidth = false;
        dotLayout.childControlHeight = false;
        dotLayout.childForceExpandWidth = false;
        dotLayout.childForceExpandHeight = false;
        dotLayout.spacing = 14f;

        _swipeHintText = CreateText("SwipeHintText", safeArea, "\u5de6\u53f3\u306b\u30b9\u30ef\u30a4\u30d7", 28f, 18f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        SetAnchored((RectTransform)_swipeHintText.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 292f), new Vector2(520f, 46f));
        _swipeHintText.outlineColor = InkColor;
        _swipeHintText.outlineWidth = 0.16f;
        _swipeHintText.textWrappingMode = TextWrappingModes.NoWrap;
        _swipeHintText.gameObject.SetActive(PlayerPrefs.GetInt(SwipeHintSeenKey, 0) == 0);
    }

    private void RefreshStageNavigationUi()
    {
        int pageCount = GetSelectableCardCount();
        int currentIndex = _swipeSnapController != null ? _swipeSnapController.GetCurrentIndex() : 0;
        bool hasMultiplePages = pageCount > 1;

        SetStageNavButtonState(_previousStageButton, hasMultiplePages && currentIndex > 0);
        SetStageNavButtonState(_nextStageButton, hasMultiplePages && currentIndex < pageCount - 1);
        SetPanelActive(_pageDotContainer != null ? _pageDotContainer.gameObject : null, hasMultiplePages);

        EnsurePageDots(pageCount);
        for (int i = 0; i < _pageDots.Count; i += 1)
        {
            Image dot = _pageDots[i];
            if (dot == null)
            {
                continue;
            }

            bool isVisible = i < pageCount;
            dot.gameObject.SetActive(isVisible);
            if (isVisible)
            {
                dot.color = i == currentIndex ? ActiveDotColor : InactiveDotColor;
            }
        }
    }

    private void EnsurePageDots(int pageCount)
    {
        if (_pageDotContainer == null)
        {
            return;
        }

        Sprite dotSprite = GetBuiltinUiSprite();
        while (_pageDots.Count < pageCount)
        {
            RectTransform dotRect = CreatePanel($"Dot_{_pageDots.Count + 1:00}", _pageDotContainer, dotSprite, InactiveDotColor);
            dotRect.GetComponent<Image>().raycastTarget = false;

            LayoutElement layoutElement = dotRect.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = StagePageDotWidth;
            layoutElement.preferredHeight = StagePageDotHeight;
            layoutElement.minWidth = StagePageDotWidth;
            layoutElement.minHeight = StagePageDotHeight;

            _pageDots.Add(dotRect.GetComponent<Image>());
        }
    }

    private static void SetStageNavButtonState(Button button, bool isInteractable)
    {
        if (button == null)
        {
            return;
        }

        button.interactable = isInteractable;
        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = isInteractable ? StageNavButtonColor : StageNavDisabledColor;
        }
    }

    private void MarkSwipeHintSeen()
    {
        if (_swipeHintText != null && _swipeHintText.gameObject.activeSelf)
        {
            _swipeHintText.gameObject.SetActive(false);
            PlayerPrefs.SetInt(SwipeHintSeenKey, 1);
            PlayerPrefs.Save();
        }
    }

    private static RectTransform ResolveSafeArea(RectTransform canvasTransform)
    {
        if (canvasTransform == null)
        {
            return null;
        }

        Transform safeArea = canvasTransform.Find("SafeArea");
        return safeArea != null ? safeArea as RectTransform : canvasTransform;
    }

    private static Sprite GetCarIcon(CarVisualDatabase visualDatabase, CarType carType)
    {
        return visualDatabase != null ? visualDatabase.GetIconSprite(carType) : null;
    }

    private static void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }

    private void ApplySelectionIndex(int index)
    {
        if (_stageDatabase == null)
        {
            return;
        }

        int selectableCount = GetSelectableCardCount();
        if (selectableCount <= 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(index, 0, selectableCount - 1);
        if (IsEndlessIndex(clampedIndex))
        {
            _selectedGameMode = GameMode.Endless;
            _selectedStageNumber = GetEndlessSourceStageNumber();
        }
        else
        {
            _selectedGameMode = GameMode.Stage;
            StageDefinition stageDefinition = _stageDatabase.Stages[clampedIndex];
            _selectedStageNumber = GetDisplayStageNumber(stageDefinition, clampedIndex);
        }

        UpdateCards();

        bool canPlay = CanPlaySelectedStage();
        if (canPlay)
        {
            if (_selectedGameMode == GameMode.Endless)
            {
                StageSelectionService.RememberLastEndless(_selectedStageNumber);
            }
            else
            {
                StageSelectionService.RememberLastStage(_selectedStageNumber);
            }
        }

        if (_playButton != null)
        {
            _playButton.interactable = canPlay;
        }

        RefreshStageNavigationUi();
    }

    private void UpdateCards()
    {
        if (_stageDatabase == null || _stageCardViews == null)
        {
            return;
        }

        int count = Mathf.Min(GetSelectableCardCount(), _stageCardViews.Length);
        for (int i = 0; i < count; i += 1)
        {
            if (IsEndlessIndex(i))
            {
                _stageCardViews[i].SetEndlessData(SaveService.GetBestEndlessScore());
                _stageCardViews[i].SetSelected(_selectedGameMode == GameMode.Endless);
                continue;
            }

            StageDefinition stageDefinition = _stageDatabase.Stages[i];
            int stageNumber = GetDisplayStageNumber(stageDefinition, i);
            bool isUnlocked = _stageDatabase.IsStageUnlocked(i, SaveService.GetBestScore);
            StageCardStatus cardStatus = GetCardStatus(stageDefinition, isUnlocked);
            int bestScore = cardStatus == StageCardStatus.Unlocked ? SaveService.GetBestScore(stageNumber) : 0;
            int starRating = cardStatus == StageCardStatus.Unlocked ? SaveService.GetStarRating(stageNumber) : 0;
            _stageCardViews[i].SetData(
                stageNumber,
                stageDefinition != null ? stageDefinition.TargetScore : 0,
                bestScore,
                cardStatus,
                starRating,
                cardStatus == StageCardStatus.Locked ? GetRequiredStageNumber(i) : 0);
            _stageCardViews[i].SetSelected(_selectedGameMode == GameMode.Stage && stageNumber == _selectedStageNumber);
        }
    }

    private void EnsureStageCardViews()
    {
        if (_stageDatabase == null || !ResolveStageCardContainer())
        {
            return;
        }

        List<StageCardView> cardViews = GetOrderedStageCardViews();
        ResolveStageCardTemplate(cardViews);
        if (cardViews.Count == 0 && _stageCardTemplate == null && _stageCardPrefab == null)
        {
            return;
        }

        int requiredCount = GetSelectableCardCount();
        while (cardViews.Count < requiredCount)
        {
            StageCardView clone = CreateStageCard(cardViews.Count + 1);
            if (clone == null)
            {
                break;
            }

            cardViews.Add(clone);
        }

        List<StageCardView> activeCardViews = new List<StageCardView>(requiredCount);
        for (int i = 0; i < cardViews.Count; i += 1)
        {
            bool shouldBeActive = i < requiredCount;
            StageCardView cardView = cardViews[i];
            cardView.gameObject.SetActive(shouldBeActive);

            if (!shouldBeActive)
            {
                continue;
            }

            cardView.transform.SetSiblingIndex(activeCardViews.Count);
            activeCardViews.Add(cardView);
        }

        _stageCardViews = activeCardViews.ToArray();
        _swipeSnapController?.Refresh();
    }

    private bool ResolveStageCardContainer()
    {
        if (_stageCardContainer != null)
        {
            return true;
        }

        if (_stageCardViews != null)
        {
            foreach (StageCardView cardView in _stageCardViews)
            {
                if (cardView == null)
                {
                    continue;
                }

                _stageCardContainer ??= cardView.transform.parent as RectTransform;
                if (_stageCardContainer != null)
                {
                    return true;
                }
            }
        }

        _stageCardContainer ??= _swipeSnapController != null ? _swipeSnapController.GetContent() : null;
        return _stageCardContainer != null;
    }

    private List<StageCardView> GetOrderedStageCardViews()
    {
        List<StageCardView> cardViews = new List<StageCardView>();
        if (_stageCardContainer == null)
        {
            return cardViews;
        }

        for (int i = 0; i < _stageCardContainer.childCount; i += 1)
        {
            StageCardView cardView = _stageCardContainer.GetChild(i).GetComponent<StageCardView>();
            if (cardView != null)
            {
                cardViews.Add(cardView);
            }
        }

        return cardViews;
    }

    private void ResolveStageCardTemplate(List<StageCardView> cardViews)
    {
        if (_stageCardTemplate != null)
        {
            return;
        }

        if (cardViews.Count > 0)
        {
            _stageCardTemplate = cardViews[0];
            return;
        }

        if (_stageCardContainer != null)
        {
            _stageCardTemplate = _stageCardContainer.GetComponentInChildren<StageCardView>(true);
        }
    }

    private StageCardView CreateStageCard(int cardNumber)
    {
        StageCardView source = _stageCardPrefab != null ? _stageCardPrefab : _stageCardTemplate;
        if (source == null || _stageCardContainer == null)
        {
            return null;
        }

        StageCardView clone = Instantiate(source, _stageCardContainer, false);
        clone.name = $"{source.name.Trim()} ({cardNumber})";
        clone.gameObject.SetActive(true);
        clone.transform.SetAsLastSibling();
        return clone;
    }

    private bool CanPlaySelectedStage()
    {
        if (_stageDatabase == null)
        {
            return false;
        }

        if (_selectedGameMode == GameMode.Endless)
        {
            return true;
        }

        if (!_stageDatabase.TryGetStageIndex(_selectedStageNumber, out int index))
        {
            return false;
        }

        return IsPlayableStage(index);
    }

    private int GetInitialStageIndex(int preferredStageNumber)
    {
        if (_stageDatabase == null || _stageDatabase.Stages.Count == 0)
        {
            return 0;
        }

        if (SaveService.GetLastGameMode() == GameMode.Endless)
        {
            return GetEndlessIndex();
        }

        if (_stageDatabase.TryGetStageIndex(preferredStageNumber, out int preferredIndex) && IsPlayableStage(preferredIndex))
        {
            return preferredIndex;
        }

        for (int i = 0; i < _stageDatabase.Stages.Count; i += 1)
        {
            if (IsPlayableStage(i))
            {
                return i;
            }
        }

        return 0;
    }

    private bool IsPlayableStage(int stageIndex)
    {
        if (_stageDatabase == null || stageIndex < 0 || stageIndex >= _stageDatabase.Stages.Count)
        {
            return false;
        }

        StageDefinition stageDefinition = _stageDatabase.Stages[stageIndex];
        return stageDefinition != null
            && stageDefinition.IsImplemented
            && _stageDatabase.IsStageUnlocked(stageIndex, SaveService.GetBestScore);
    }

    private static StageCardStatus GetCardStatus(StageDefinition stageDefinition, bool isUnlocked)
    {
        if (stageDefinition == null || !stageDefinition.IsImplemented)
        {
            return StageCardStatus.ComingSoon;
        }

        return isUnlocked ? StageCardStatus.Unlocked : StageCardStatus.Locked;
    }

    private int GetRequiredStageNumber(int stageIndex)
    {
        return _stageDatabase != null ? _stageDatabase.GetRequiredClearStageNumber(stageIndex) : 0;
    }

    private int GetSelectableCardCount()
    {
        return _stageDatabase != null ? _stageDatabase.Stages.Count + 1 : 0;
    }

    private int GetEndlessIndex()
    {
        return _stageDatabase != null ? _stageDatabase.Stages.Count : 0;
    }

    private bool IsEndlessIndex(int index)
    {
        return _stageDatabase != null && index == GetEndlessIndex();
    }

    private int GetEndlessSourceStageNumber()
    {
        if (_stageDatabase == null || _stageDatabase.Stages.Count == 0)
        {
            return StageNumberUtility.MinimumStageNumber;
        }

        for (int i = _stageDatabase.Stages.Count - 1; i >= 0; i -= 1)
        {
            StageDefinition stageDefinition = _stageDatabase.Stages[i];
            if (stageDefinition != null && stageDefinition.IsImplemented)
            {
                return StageNumberUtility.Normalize(stageDefinition.StageNumber);
            }
        }

        return StageNumberUtility.MinimumStageNumber;
    }

    private static int GetDisplayStageNumber(StageDefinition stageDefinition, int stageIndex)
    {
        if (stageDefinition != null)
        {
            return StageNumberUtility.Normalize(stageDefinition.StageNumber);
        }

        return StageNumberUtility.FromIndex(stageIndex);
    }

    private static Button CreateRoundMenuButton(string name, Transform parent, Sprite iconSprite, string label, Sprite slicedSprite, string fallbackIconText)
    {
        Button button = CreateButton(name, parent, slicedSprite, new Vector2(148f, 148f), string.Empty, 1f, 1f, FontStyles.Normal, TextColor);
        RectTransform rect = (RectTransform)button.transform;
        Image background = button.GetComponent<Image>();
        background.color = new Color(1f, 0.97f, 0.84f, 1f);
        AddShadow(button.gameObject, new Color(0.02f, 0.06f, 0.11f, 0.48f), new Vector2(0f, -8f));

        if (iconSprite != null)
        {
            Image icon = CreateImage("Icon", rect, iconSprite, Color.white, true);
            SetAnchored(icon.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(62f, 62f));
        }
        else if (!string.IsNullOrEmpty(fallbackIconText))
        {
            TMP_Text iconText = CreateText("IconText", rect, fallbackIconText, 62f, 42f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            SetAnchored((RectTransform)iconText.transform, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(88f, 62f));
            iconText.outlineColor = InkColor;
            iconText.outlineWidth = 0.18f;
            iconText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        TMP_Text labelText = CreateText("Label", rect, label, 28f, 20f, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
        SetAnchored((RectTransform)labelText.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(150f, 44f));
        labelText.outlineColor = InkColor;
        labelText.outlineWidth = 0.18f;
        labelText.textWrappingMode = TextWrappingModes.NoWrap;
        return button;
    }

    private static RectTransform CreateModalDialog(string name, RectTransform overlay, Sprite slicedSprite, Vector2 size)
    {
        RectTransform shadow = CreatePanel($"{name}Shadow", overlay, slicedSprite, CardShadowColor);
        SetAnchored(shadow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), size);
        shadow.GetComponent<Image>().raycastTarget = false;

        RectTransform dialog = CreatePanel(name, overlay, slicedSprite, CardColor);
        SetAnchored(dialog, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        return dialog;
    }

    private static void BuildModalHeader(RectTransform dialog, string title, Sprite slicedSprite, out Button closeButton)
    {
        TMP_Text titleText = CreateText("Title", dialog, title, 58f, 36f, FontStyles.Normal, TextColor, TextAlignmentOptions.Left);
        SetAnchored((RectTransform)titleText.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(58f, -42f), new Vector2(-230f, 92f));
        titleText.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform divider = CreatePanel("Divider", dialog, slicedSprite, DividerColor);
        SetAnchored(divider, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(-116f, 4f));
        divider.GetComponent<Image>().raycastTarget = false;

        closeButton = CreateTopCloseButton(dialog, slicedSprite);
    }

    private static void CreateSettingsRow(RectTransform parent, string label, string detail, Color accentColor, Sprite toggleSprite, out RectTransform row)
    {
        row = CreatePanel($"{label}Row", parent, GetBuiltinUiSprite(), RowColor);
        LayoutElement layoutElement = row.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = SettingsRowHeight;
        layoutElement.flexibleWidth = 1f;

        RectTransform accent = CreatePanel("AccentBar", row, GetBuiltinUiSprite(), accentColor);
        Stretch(accent, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(12f, 0f));
        accent.GetComponent<Image>().raycastTarget = false;

        TMP_Text labelText = CreateText("Label", row, label, 52f, 34f, FontStyles.Normal, TextColor, TextAlignmentOptions.Left);
        SetAnchored((RectTransform)labelText.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(58f, 34f), new Vector2(360f, 70f));
        labelText.textWrappingMode = TextWrappingModes.NoWrap;

        TMP_Text detailText = CreateText("Detail", row, detail, 27f, 20f, FontStyles.Normal, MutedTextColor, TextAlignmentOptions.Left);
        SetAnchored((RectTransform)detailText.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(60f, -36f), new Vector2(500f, 58f));
        detailText.textWrappingMode = TextWrappingModes.NoWrap;

        TMP_Text stateText = CreateText("StateText", row, "ON", 38f, 28f, FontStyles.Normal, TextColor, TextAlignmentOptions.Center);
        SetAnchored((RectTransform)stateText.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-248f, 0f), new Vector2(96f, 60f));

        RectTransform toggleRect = CreateUiObject($"{label}Toggle", row, typeof(Image), typeof(Toggle));
        Image toggleImage = toggleRect.GetComponent<Image>();
        toggleImage.sprite = toggleSprite;
        toggleImage.color = Color.white;
        toggleImage.preserveAspect = true;
        toggleImage.raycastTarget = true;

        Toggle toggle = toggleRect.GetComponent<Toggle>();
        toggle.targetGraphic = toggleImage;
        toggle.graphic = null;
        SetAnchored(toggleRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-48f, 0f), new Vector2(172f, 102f));
    }

    private static void CreateSettingsVolumeControl(RectTransform row, string label, Color accentColor, Sprite slicedSprite)
    {
        RectTransform sliderRect = CreateUiObject($"{label}VolumeSlider", row, typeof(Slider));
        SetAnchored(sliderRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-58f, 24f), new Vector2(-282f, 38f));

        RectTransform background = CreatePanel("Background", sliderRect, slicedSprite, DividerColor);
        Stretch(background, Vector2.zero, Vector2.one, new Vector2(0f, 13f), new Vector2(0f, -13f));
        background.GetComponent<Image>().raycastTarget = false;

        RectTransform fillArea = CreateUiObject("Fill Area", sliderRect);
        Stretch(fillArea, Vector2.zero, Vector2.one, new Vector2(0f, 13f), new Vector2(0f, -13f));

        RectTransform fill = CreatePanel("Fill", fillArea, slicedSprite, accentColor);
        Stretch(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        fill.GetComponent<Image>().raycastTarget = false;

        RectTransform handleArea = CreateUiObject("Handle Slide Area", sliderRect);
        Stretch(handleArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform handle = CreatePanel("Handle", handleArea, slicedSprite, Color.white);
        SetAnchored(handle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(SettingsSliderHandleWidth, SettingsSliderHandleHeight));

        Slider slider = sliderRect.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();

        TMP_Text valueText = CreateText($"{label}VolumeValueText", row, "100%", 26f, 18f, FontStyles.Normal, TextColor, TextAlignmentOptions.Center);
        SetAnchored((RectTransform)valueText.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-58f, 24f), new Vector2(112f, 44f));
        valueText.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private static void CreateHowToStep(RectTransform parent, string number, string title, string detail, Color accentColor, Sprite iconSprite, Sprite slicedSprite)
    {
        RectTransform row = CreatePanel($"Step_{number}", parent, slicedSprite, RowColor);
        LayoutElement layoutElement = row.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 230f;
        layoutElement.flexibleWidth = 1f;

        RectTransform accent = CreatePanel("AccentBar", row, slicedSprite, accentColor);
        Stretch(accent, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(12f, 0f));
        accent.GetComponent<Image>().raycastTarget = false;

        TMP_Text numberText = CreateText("Number", row, number, 48f, 32f, FontStyles.Bold, accentColor, TextAlignmentOptions.Center);
        SetAnchored((RectTransform)numberText.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(84f, 0f), new Vector2(118f, 92f));

        Image iconImage = CreateImage("Icon", row, iconSprite, Color.white, true);
        SetAnchored(iconImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(218f, 0f), new Vector2(126f, 126f));

        TMP_Text titleText = CreateText("Title", row, title, 38f, 24f, FontStyles.Bold, TextColor, TextAlignmentOptions.Left);
        SetAnchored((RectTransform)titleText.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(300f, 42f), new Vector2(-330f, 58f));
        titleText.textWrappingMode = TextWrappingModes.NoWrap;

        TMP_Text detailText = CreateText("Detail", row, detail, 30f, 22f, FontStyles.Normal, MutedTextColor, TextAlignmentOptions.Left);
        SetAnchored((RectTransform)detailText.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(300f, -36f), new Vector2(-330f, 74f));
        detailText.textWrappingMode = TextWrappingModes.Normal;
    }

    private static Button CreateTopCloseButton(Transform parent, Sprite slicedSprite)
    {
        Button button = CreateButton("CloseButtonTop", parent, slicedSprite, new Vector2(180f, 82f), "X", 34f, 24f, FontStyles.Bold, Color.white);
        SetAnchored((RectTransform)button.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-42f, -34f), new Vector2(180f, 82f));
        button.GetComponent<Image>().color = BlueAccentColor;
        return button;
    }

    private static Button CreateModalCloseButton(Transform parent, string name, string label, Sprite slicedSprite, Vector2 position, Vector2 size)
    {
        Button button = CreateButton(name, parent, slicedSprite, size, label, 48f, 28f, FontStyles.Normal, TextColor);
        SetAnchored((RectTransform)button.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position, size);
        button.GetComponent<Image>().color = new Color(0.96f, 0.98f, 1f, 1f);
        AddShadow(button.gameObject, new Color(0.08f, 0.15f, 0.24f, 0.18f), new Vector2(0f, -4f));
        return button;
    }

    private static Button CreateButton(string name, Transform parent, Sprite sprite, Vector2 size, string label, float fontSize, float fontSizeMin, FontStyles fontStyle, Color textColor)
    {
        RectTransform rect = CreatePanel(name, parent, sprite, Color.white, typeof(Button), typeof(LayoutElement));
        rect.sizeDelta = size;

        LayoutElement layoutElement = rect.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;

        Button button = rect.GetComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        ApplyButtonColors(button);

        if (!string.IsNullOrEmpty(label))
        {
            TMP_Text labelText = CreateText("Label", rect, label, fontSize, fontSizeMin, fontStyle, textColor, TextAlignmentOptions.Center);
            Stretch((RectTransform)labelText.transform, Vector2.zero, Vector2.one, new Vector2(42f, 18f), new Vector2(-42f, -18f));
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        return button;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool preserveAspect)
    {
        RectTransform rect = CreateUiObject(name, parent, typeof(Image));
        Image image = rect.GetComponent<Image>();
        image.sprite = sprite;
        image.color = sprite != null ? color : Color.clear;
        image.raycastTarget = false;
        image.preserveAspect = preserveAspect;
        return image;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Sprite sprite, Color color, params Type[] extraComponents)
    {
        Type[] components = new Type[extraComponents.Length + 1];
        components[0] = typeof(Image);
        for (int i = 0; i < extraComponents.Length; i += 1)
        {
            components[i + 1] = extraComponents[i];
        }

        RectTransform rect = CreateUiObject(name, parent, components);
        Image image = rect.GetComponent<Image>();
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = color;
        image.raycastTarget = true;
        return rect;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float fontSizeMax, float fontSizeMin, FontStyles fontStyle, Color color, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateUiObject(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSizeMax;
        text.fontSizeMax = fontSizeMax;
        text.fontSizeMin = fontSizeMin;
        text.enableAutoSizing = true;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.richText = true;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private static RectTransform CreateUiObject(string name, Transform parent, params Type[] components)
    {
        Type[] allComponents = new Type[components.Length + 2];
        allComponents[0] = typeof(RectTransform);
        allComponents[1] = typeof(CanvasRenderer);
        for (int i = 0; i < components.Length; i += 1)
        {
            allComponents[i + 2] = components[i];
        }

        GameObject gameObject = new(name, allComponents);
        gameObject.layer = parent != null ? parent.gameObject.layer : LayerMask.NameToLayer("UI");
        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }

        return (RectTransform)gameObject.transform;
    }

    private static Sprite GetBuiltinUiSprite()
    {
        if (s_RuntimeUiSprite != null)
        {
            return s_RuntimeUiSprite;
        }

        const int textureSize = 32;
        Texture2D texture = new(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            name = "StageSelectRuntimeUiSpriteTexture",
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color[] pixels = new Color[textureSize * textureSize];
        for (int i = 0; i < pixels.Length; i += 1)
        {
            pixels[i] = Color.white;
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);

        s_RuntimeUiSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(8f, 8f, 8f, 8f));
        s_RuntimeUiSprite.name = "StageSelectRuntimeUiSprite";
        s_RuntimeUiSprite.hideFlags = HideFlags.HideAndDontSave;
        return s_RuntimeUiSprite;
    }

    private static void ApplyButtonColors(Button button)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.selectedColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.82f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.42f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
    }

    private static void AddShadow(GameObject gameObject, Color color, Vector2 distance)
    {
        Shadow shadow = gameObject.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = gameObject.AddComponent<Shadow>();
        }

        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static void SetAnchored(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localScale = Vector3.one;
    }
}
