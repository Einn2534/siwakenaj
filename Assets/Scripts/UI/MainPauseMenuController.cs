using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainPauseMenuController : MonoBehaviour
{
    private const string MainSceneName = "Main";
    private const string StageSelectSceneName = "StageSelect";
    private const string PauseIconResourcePath = "UI/ui_pause_icon";
    private const string ResumeButtonResourcePath = "UI/button_pause_resume";
    private const string RetryButtonResourcePath = "UI/button_pause_retry";
    private const string StageSelectButtonResourcePath = "UI/button_pause_stage_select";
    private const string HowToIconResourcePath = "UI/ui_howto_icon";
    private const string ToggleOnResourcePath = "UI/ui_settings_toggle_on";
    private const string ToggleOffResourcePath = "UI/ui_settings_toggle_off";
    private const string StageFormat = "STAGE {0:00}";
    private const float ReferenceWidth = 1080f;
    private const float ReferenceHeight = 1920f;
    private const float PauseActionButtonWidth = 360f;
    private const float SettingsPanelWidth = 900f;
    private const float SettingsPanelHeight = 1080f;
    private const float SettingsRowWidth = 780f;
    private const float SettingsRowHeight = 204f;
    private const float SettingsSliderHandleWidth = 34f;
    private const float SettingsSliderHandleHeight = 2f;
    private const float HowToPanelWidth = 900f;
    private const float HowToPanelHeight = 1260f;

    private static readonly Color OverlayColor = new(0.012f, 0.016f, 0.02f, 0.72f);
    private static readonly Color PrimaryButtonColor = new(1f, 0.78f, 0.26f, 1f);
    private static readonly Color NeutralButtonColor = new(0.96f, 0.98f, 1f, 1f);
    private static readonly Color DangerButtonColor = new(1f, 0.46f, 0.43f, 1f);
    private static readonly Color DarkTextColor = new(0.08f, 0.105f, 0.13f, 1f);
    private static readonly Color LightTextColor = new(0.94f, 0.98f, 1f, 1f);
    private static readonly Color ToggleOnColor = new(0.35f, 0.78f, 0.54f, 1f);
    private static readonly Color ToggleOffColor = new(0.42f, 0.47f, 0.54f, 1f);
    private static readonly Color SettingsPanelColor = new(1f, 1f, 1f, 0.96f);
    private static readonly Color SettingsRowColor = new(0.975f, 0.982f, 0.992f, 1f);
    private static readonly Color SettingsTextColor = new(0.137f, 0.184f, 0.275f, 1f);
    private static readonly Color SettingsMutedTextColor = new(0.44f, 0.49f, 0.58f, 1f);
    private static readonly Color SettingsDividerColor = new(0.902f, 0.922f, 0.953f, 1f);
    private static readonly Color SettingsDisabledTextColor = new(0.56f, 0.61f, 0.69f, 1f);
    private static readonly Color SettingsDisabledAccentColor = new(0.824f, 0.855f, 0.902f, 1f);
    private static readonly Color SettingsBgmAccentColor = new(0.345f, 0.784f, 0.541f, 1f);
    private static readonly Color SettingsSeAccentColor = new(0.219f, 0.643f, 0.94f, 1f);
    private static readonly Color SettingsVibrationAccentColor = new(0.949f, 0.772f, 0.259f, 1f);
    private static readonly Color HowToPanelColor = new(1f, 1f, 1f, 0.96f);
    private static readonly Color HowToShadowColor = new(0.08f, 0.15f, 0.24f, 0.12f);
    private static readonly Color HowToRowColor = new(0.975f, 0.982f, 0.992f, 1f);
    private static readonly Color HowToDividerColor = new(0.902f, 0.922f, 0.953f, 1f);
    private static readonly Color HowToTextColor = new(0.137f, 0.184f, 0.275f, 1f);
    private static readonly Color HowToMutedTextColor = new(0.44f, 0.49f, 0.58f, 1f);
    private static readonly Color HowToBlueAccentColor = new(0.219f, 0.643f, 0.94f, 1f);

    private static MainPauseMenuController _activeController;

    private GameFlowController _gameFlowController;
    private GameObject _overlayRoot;
    private Button _pauseButton;
    private Button _resumeButton;
    private Button _retryButton;
    private Button _stageSelectButton;
    private Button _howToButton;
    private Button _howToCloseButton;
    private Button _bgmToggleButton;
    private Button _seToggleButton;
    private Button _vibrationToggleButton;
    private Slider _bgmVolumeSlider;
    private Slider _seVolumeSlider;
    private GameObject _menuPanelRoot;
    private GameObject _howToPanelRoot;
    private TMP_Text _stageText;
    private TMP_Text _bgmToggleText;
    private TMP_Text _seToggleText;
    private TMP_Text _vibrationToggleText;
    private TMP_Text _bgmVolumeValueText;
    private TMP_Text _seVolumeValueText;
    private Image _bgmAccentImage;
    private Image _seAccentImage;
    private Image _vibrationAccentImage;
    private Sprite _toggleOnSprite;
    private Sprite _toggleOffSprite;
    private float _fallbackTimeScaleBeforePause = 1f;
    private bool _isMenuOpen;
    private bool _isHowToOpen;
    private bool _isUsingFallbackPause;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        _activeController = null;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallBootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryInstall(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryInstall(scene);
    }

    private static void TryInstall(Scene scene)
    {
        if (!scene.IsValid() || scene.name != MainSceneName)
        {
            return;
        }

        if (_activeController != null || FindAnyObjectByType<MainPauseMenuController>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        GameObject controllerObject = new("MainPauseMenuController");
        controllerObject.AddComponent<MainPauseMenuController>();
    }

    private void Awake()
    {
        if (_activeController != null && _activeController != this)
        {
            Destroy(gameObject);
            return;
        }

        _activeController = this;
        ResolveGameFlowController();
        EnsureEventSystem();
        BuildInterface();
        SetMenuVisible(false);
        RefreshSoundControls();
        RefreshPauseButtonState();
    }

    private void Update()
    {
        if (_gameFlowController == null)
        {
            ResolveGameFlowController();
        }

        RefreshPauseButtonState();

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBackPressed();
        }
#endif
    }

    private void OnDestroy()
    {
        if (_isMenuOpen)
        {
            ResumeGameplayState();
        }

        if (_activeController == this)
        {
            _activeController = null;
        }
    }

    private void HandleBackPressed()
    {
        if (_isHowToOpen)
        {
            CloseHowTo();
        }
        else if (_isMenuOpen)
        {
            ResumeGame();
        }
        else
        {
            OpenMenu();
        }
    }

    private void OpenMenu()
    {
        if (_isMenuOpen)
        {
            return;
        }

        ResolveGameFlowController();
        bool paused = _gameFlowController != null
            ? _gameFlowController.IsPaused() || _gameFlowController.PauseGame()
            : PauseWithFallback();

        if (!paused)
        {
            return;
        }

        _isMenuOpen = true;
        RefreshSoundControls();
        RefreshStageText();
        ShowPauseMenuPanel();
        RefreshPauseButtonState();

        if (EventSystem.current != null && _resumeButton != null)
        {
            EventSystem.current.SetSelectedGameObject(_resumeButton.gameObject);
        }
    }

    private void ResumeGame()
    {
        if (!_isMenuOpen)
        {
            return;
        }

        ResumeGameplayState();
        _isMenuOpen = false;
        SetMenuVisible(false);
        RefreshPauseButtonState();
    }

    private void OpenHowTo()
    {
        if (!_isMenuOpen)
        {
            return;
        }

        _isHowToOpen = true;
        if (_menuPanelRoot != null)
        {
            _menuPanelRoot.SetActive(false);
        }

        if (_howToPanelRoot != null)
        {
            _howToPanelRoot.SetActive(true);
        }

        if (EventSystem.current != null && _howToCloseButton != null)
        {
            EventSystem.current.SetSelectedGameObject(_howToCloseButton.gameObject);
        }
    }

    private void CloseHowTo()
    {
        if (!_isHowToOpen)
        {
            return;
        }

        _isHowToOpen = false;
        if (_howToPanelRoot != null)
        {
            _howToPanelRoot.SetActive(false);
        }

        if (_menuPanelRoot != null)
        {
            _menuPanelRoot.SetActive(true);
        }

        if (EventSystem.current != null && _howToButton != null)
        {
            EventSystem.current.SetSelectedGameObject(_howToButton.gameObject);
        }
    }

    private void RetryStage()
    {
        ResumeBeforeNavigation();
        StageSelectionService.SelectStage(SessionState.SelectedStageNumber);
        SceneManager.LoadScene(MainSceneName);
    }

    private void ReturnToStageSelect()
    {
        ResumeBeforeNavigation();
        StageSelectionService.RememberLastStage(SessionState.SelectedStageNumber);
        SceneManager.LoadScene(StageSelectSceneName);
    }

    private void ToggleBgm()
    {
        bool isOn = !SaveService.GetBgmOn();
        SaveService.SetBgmOn(isOn);
        SaveService.Save();
        SoundManager.EnsureInstance().SetBgmEnabled(isOn);
        RefreshSoundControls();
    }

    private void ToggleSe()
    {
        bool isOn = !SaveService.GetSeOn();
        SaveService.SetSeOn(isOn);
        SaveService.Save();
        SoundManager.EnsureInstance().SetSeEnabled(isOn);
        RefreshSoundControls();
    }

    private void ToggleVibration()
    {
        bool isOn = !SaveService.GetVibrationOn();
        SaveService.SetVibrationOn(isOn);
        SaveService.Save();
        if (!isOn)
        {
            VibrationService.Stop();
        }

        RefreshSoundControls();
    }

    private void SetBgmVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        SaveService.SetBgmVolume(volume);
        SaveService.Save();
        SoundManager.EnsureInstance().SetBgmVolume(volume);
        RefreshVolumeText(_bgmVolumeValueText, volume);
    }

    private void SetSeVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        SaveService.SetSeVolume(volume);
        SaveService.Save();
        SoundManager.EnsureInstance().SetSeVolume(volume);
        RefreshVolumeText(_seVolumeValueText, volume);
    }

    private bool PauseWithFallback()
    {
        _fallbackTimeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        _isUsingFallbackPause = true;
        return true;
    }

    private void ResumeGameplayState()
    {
        if (_gameFlowController != null && _gameFlowController.IsPaused())
        {
            _gameFlowController.ResumeGame();
        }
        else if (_isUsingFallbackPause)
        {
            Time.timeScale = Mathf.Approximately(_fallbackTimeScaleBeforePause, 0f)
                ? 1f
                : _fallbackTimeScaleBeforePause;
        }

        _isUsingFallbackPause = false;
    }

    private void ResumeBeforeNavigation()
    {
        ResumeGameplayState();
        _isMenuOpen = false;
        SetMenuVisible(false);
    }

    private void ResolveGameFlowController()
    {
        _gameFlowController = FindAnyObjectByType<GameFlowController>();
    }

    private void RefreshPauseButtonState()
    {
        if (_pauseButton == null)
        {
            return;
        }

        bool canPause = _gameFlowController != null && (_gameFlowController.CanPauseGame() || _gameFlowController.IsPaused());
        _pauseButton.gameObject.SetActive(!_isMenuOpen && canPause);
        _pauseButton.interactable = canPause;
    }

    private void RefreshStageText()
    {
        if (_stageText != null)
        {
            _stageText.text = string.Format(StageFormat, StageNumberUtility.Normalize(SessionState.SelectedStageNumber));
        }
    }

    private void RefreshSoundControls()
    {
        bool isBgmOn = SaveService.GetBgmOn();
        bool isSeOn = SaveService.GetSeOn();
        bool isVibrationOn = SaveService.GetVibrationOn();
        float bgmVolume = SaveService.GetBgmVolume();
        float seVolume = SaveService.GetSeVolume();

        SoundManager.EnsureInstance().SetBgmEnabled(isBgmOn);
        SoundManager.EnsureInstance().SetSeEnabled(isSeOn);
        SoundManager.EnsureInstance().SetBgmVolume(bgmVolume);
        SoundManager.EnsureInstance().SetSeVolume(seVolume);

        SetToggleVisual(_bgmToggleButton, _bgmToggleText, _bgmAccentImage, SettingsBgmAccentColor, isBgmOn);
        SetToggleVisual(_seToggleButton, _seToggleText, _seAccentImage, SettingsSeAccentColor, isSeOn);
        SetToggleVisual(_vibrationToggleButton, _vibrationToggleText, _vibrationAccentImage, SettingsVibrationAccentColor, isVibrationOn);

        if (_bgmVolumeSlider != null)
        {
            _bgmVolumeSlider.SetValueWithoutNotify(bgmVolume);
        }

        if (_seVolumeSlider != null)
        {
            _seVolumeSlider.SetValueWithoutNotify(seVolume);
        }

        RefreshVolumeText(_bgmVolumeValueText, bgmVolume);
        RefreshVolumeText(_seVolumeValueText, seVolume);
    }

    private void SetMenuVisible(bool isVisible)
    {
        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(isVisible);
        }

        if (!isVisible)
        {
            _isHowToOpen = false;
            if (_menuPanelRoot != null)
            {
                _menuPanelRoot.SetActive(false);
            }

            if (_howToPanelRoot != null)
            {
                _howToPanelRoot.SetActive(false);
            }
        }
    }

    private void ShowPauseMenuPanel()
    {
        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(true);
        }

        _isHowToOpen = false;
        if (_menuPanelRoot != null)
        {
            _menuPanelRoot.SetActive(true);
        }

        if (_howToPanelRoot != null)
        {
            _howToPanelRoot.SetActive(false);
        }
    }

    private void BuildInterface()
    {
        Canvas canvas = CreateCanvas();
        RectTransform safeRoot = CreateUiObject("SafeAreaRoot", canvas.transform);
        Stretch(safeRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        safeRoot.gameObject.AddComponent<SafeAreaFitter>();

        _pauseButton = CreateIconButton(safeRoot, Resources.Load<Sprite>(PauseIconResourcePath));
        _pauseButton.onClick.AddListener(OpenMenu);

        RectTransform overlayRect = CreatePanel("PauseOverlay", canvas.transform, OverlayColor);
        Stretch(overlayRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        _overlayRoot = overlayRect.gameObject;

        RectTransform panel = CreatePanel("PausePanel", overlayRect, SettingsPanelColor);
        SetAnchored(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 1780f));
        _menuPanelRoot = panel.gameObject;

        CreateText("Title", panel, "PAUSED", 64f, FontStyles.Bold, TextAlignmentOptions.Center, SettingsTextColor, new Vector2(-116f, 760f), new Vector2(420f, 86f));
        _stageText = CreateText("StageText", panel, string.Empty, 30f, FontStyles.Bold, TextAlignmentOptions.Center, SettingsMutedTextColor, new Vector2(0f, 692f), new Vector2(620f, 46f));
        _howToButton = CreateImageOnlyButton(panel, "HowToButton", Resources.Load<Sprite>(HowToIconResourcePath), "HOW TO", NeutralButtonColor, DarkTextColor, new Vector2(286f, 760f), new Vector2(116f, 116f));

        _resumeButton = CreateImageMenuButton(panel, "ResumeButton", Resources.Load<Sprite>(ResumeButtonResourcePath), "RESUME", PrimaryButtonColor, DarkTextColor, new Vector2(0f, 560f));
        _retryButton = CreateImageMenuButton(panel, "RetryButton", Resources.Load<Sprite>(RetryButtonResourcePath), "RETRY", NeutralButtonColor, DarkTextColor, new Vector2(0f, 410f));
        _stageSelectButton = CreateImageMenuButton(panel, "StageSelectButton", Resources.Load<Sprite>(StageSelectButtonResourcePath), "STAGE SELECT", DangerButtonColor, LightTextColor, new Vector2(0f, 260f));

        _resumeButton.onClick.AddListener(ResumeGame);
        _retryButton.onClick.AddListener(RetryStage);
        _stageSelectButton.onClick.AddListener(ReturnToStageSelect);
        _howToButton.onClick.AddListener(OpenHowTo);

        _toggleOnSprite = Resources.Load<Sprite>(ToggleOnResourcePath);
        _toggleOffSprite = Resources.Load<Sprite>(ToggleOffResourcePath);

        RectTransform settingsPanel = CreateSettingsPanel(panel);
        CreateVolumeRow(settingsPanel, "BGM", "TITLE / STAGE MUSIC", new Vector2(0f, 288f), SettingsBgmAccentColor, out _bgmToggleButton, out _bgmToggleText, out _bgmVolumeSlider, out _bgmVolumeValueText, out _bgmAccentImage);
        CreateVolumeRow(settingsPanel, "SE", "BUTTON / JUDGE SOUNDS", new Vector2(0f, 60f), SettingsSeAccentColor, out _seToggleButton, out _seToggleText, out _seVolumeSlider, out _seVolumeValueText, out _seAccentImage);
        CreateToggleRow(settingsPanel, "VIBRATION", "JUDGE / RESULT FEEDBACK", new Vector2(0f, -168f), SettingsVibrationAccentColor, out _vibrationToggleButton, out _vibrationToggleText, out _vibrationAccentImage);

        _bgmToggleButton.onClick.AddListener(ToggleBgm);
        _seToggleButton.onClick.AddListener(ToggleSe);
        _vibrationToggleButton.onClick.AddListener(ToggleVibration);
        _bgmVolumeSlider.onValueChanged.AddListener(SetBgmVolume);
        _seVolumeSlider.onValueChanged.AddListener(SetSeVolume);

        BuildHowToPanel(overlayRect);
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new("PauseMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static Button CreateIconButton(Transform parent, Sprite iconSprite)
    {
        RectTransform rect = CreateUiObject("PauseButton", parent, typeof(Image), typeof(Button));
        SetAnchored(rect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -34f), new Vector2(128f, 128f));

        Image image = rect.GetComponent<Image>();
        image.sprite = iconSprite;
        image.preserveAspect = true;
        image.color = iconSprite != null ? Color.white : PrimaryButtonColor;

        Button button = rect.GetComponent<Button>();
        button.targetGraphic = image;
        ApplyButtonColors(button, Color.white);

        if (iconSprite == null)
        {
            CreateText("FallbackIcon", rect, "II", 48f, FontStyles.Bold, TextAlignmentOptions.Center, DarkTextColor, Vector2.zero, new Vector2(88f, 88f));
        }

        return button;
    }

    private void BuildHowToPanel(Transform parent)
    {
        RectTransform root = CreateUiObject("HowToPanelRoot", parent);
        SetAnchored(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        _howToPanelRoot = root.gameObject;

        RectTransform shadow = CreatePanel("HowToPanelShadow", root, HowToShadowColor);
        SetAnchored(shadow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(HowToPanelWidth, HowToPanelHeight));

        RectTransform panel = CreatePanel("HowToPanel", root, HowToPanelColor);
        SetAnchored(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(HowToPanelWidth, HowToPanelHeight));

        BuildHowToHeader(panel, out Button topCloseButton);
        topCloseButton.onClick.AddListener(CloseHowTo);

        RectTransform body = CreateUiObject("Body", panel);
        Stretch(body, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(58f, 138f), new Vector2(-58f, -164f));

        VerticalLayoutGroup bodyLayout = body.gameObject.AddComponent<VerticalLayoutGroup>();
        bodyLayout.spacing = 18f;
        bodyLayout.childAlignment = TextAnchor.UpperCenter;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = false;

        CarVisualDatabase visualDatabase = CarVisualDatabase.LoadDefault();
        CreateHowToStep(body, "01", "COMING CAR", "\u8eca\u3092\u898b\u3066\u3001\u540c\u3058\u30ec\u30fc\u30f3\u306e\u30dc\u30bf\u30f3\u3092\u30bf\u30c3\u30d7", HowToBlueAccentColor, GetHowToIcon(visualDatabase, CarType.LightTruck));
        CreateHowToStep(body, "02", "OK / GOOD", "\u6b63\u3057\u304f\u4ed5\u5206\u3051\u308b\u3068\u30b9\u30b3\u30a2\u30a2\u30c3\u30d7", ToggleOnColor, GetHowToIcon(visualDatabase, CarType.CompactCar));
        CreateHowToStep(body, "03", "MISS LIMIT", "MISS \u304c\u4e0a\u9650\u306b\u5c4a\u304f\u3068 GAME OVER", DangerButtonColor, GetHowToIcon(visualDatabase, CarType.SportsCar));

        _howToCloseButton = CreateModalCloseButton(panel, "CloseButton", "OK", new Vector2(0f, 34f), new Vector2(560f, 136f));
        _howToCloseButton.onClick.AddListener(CloseHowTo);
        _howToPanelRoot.SetActive(false);
    }

    private static void BuildHowToHeader(Transform parent, out Button closeButton)
    {
        TMP_Text titleText = CreateText("Title", parent, "HOW TO", 58f, FontStyles.Normal, TextAlignmentOptions.Left, HowToTextColor, Vector2.zero, Vector2.zero);
        SetAnchored((RectTransform)titleText.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(58f, -42f), new Vector2(-230f, 92f));
        titleText.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform divider = CreatePanel("Divider", parent, HowToDividerColor);
        SetAnchored(divider, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(-116f, 4f));
        divider.GetComponent<Image>().raycastTarget = false;

        closeButton = CreateTopCloseButton(parent);
    }

    private static Sprite GetHowToIcon(CarVisualDatabase visualDatabase, CarType carType)
    {
        return visualDatabase != null ? visualDatabase.GetIconSprite(carType) : null;
    }

    private static void CreateHowToStep(Transform parent, string number, string title, string detail, Color accentColor, Sprite iconSprite)
    {
        RectTransform row = CreatePanel($"Step{number}", parent, HowToRowColor);
        LayoutElement layoutElement = row.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 230f;
        layoutElement.flexibleWidth = 1f;

        RectTransform accent = CreatePanel("AccentBar", row, accentColor);
        Stretch(accent, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(12f, 0f));
        accent.GetComponent<Image>().raycastTarget = false;

        TMP_Text numberText = CreateText("Number", row, number, 48f, FontStyles.Bold, TextAlignmentOptions.Center, accentColor, Vector2.zero, Vector2.zero);
        SetAnchored((RectTransform)numberText.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(84f, 0f), new Vector2(118f, 92f));

        Image iconImage = CreateImage("Icon", row, iconSprite, Color.white, true);
        SetAnchored(iconImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(218f, 0f), new Vector2(126f, 126f));

        TMP_Text titleText = CreateText("Title", row, title, 38f, FontStyles.Bold, TextAlignmentOptions.Left, HowToTextColor, Vector2.zero, Vector2.zero);
        SetAnchored((RectTransform)titleText.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(300f, 42f), new Vector2(-330f, 58f));
        titleText.textWrappingMode = TextWrappingModes.NoWrap;

        TMP_Text detailText = CreateText("Detail", row, detail, 30f, FontStyles.Normal, TextAlignmentOptions.Left, HowToMutedTextColor, Vector2.zero, Vector2.zero);
        SetAnchored((RectTransform)detailText.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(300f, -36f), new Vector2(-330f, 74f));
        detailText.textWrappingMode = TextWrappingModes.Normal;
    }

    private static Button CreateTopCloseButton(Transform parent)
    {
        RectTransform rect = CreatePanel("CloseButtonTop", parent, HowToBlueAccentColor, typeof(Button));
        SetAnchored(rect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-42f, -34f), new Vector2(180f, 82f));

        Button button = rect.GetComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        ApplyButtonColors(button, Color.white);

        TMP_Text label = CreateText("Label", rect, "X", 34f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, Vector2.zero, new Vector2(132f, 58f));
        label.characterSpacing = 1f;
        return button;
    }

    private static Button CreateModalCloseButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreatePanel(name, parent, Color.white, typeof(Button));
        SetAnchored(rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position, size);

        Button button = rect.GetComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        ApplyButtonColors(button, Color.white);

        TMP_Text labelText = CreateText("Label", rect, label, 48f, FontStyles.Normal, TextAlignmentOptions.Center, HowToTextColor, Vector2.zero, size - new Vector2(84f, 36f));
        labelText.characterSpacing = 2f;
        return button;
    }

    private static Button CreateMenuButton(Transform parent, string name, string label, Color backgroundColor, Color textColor, Vector2 position)
    {
        RectTransform rect = CreatePanel(name, parent, backgroundColor, typeof(Button));
        SetAnchored(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(620f, 96f));

        Button button = rect.GetComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        ApplyButtonColors(button, Color.white);

        TMP_Text text = CreateText("Label", rect, label, 38f, FontStyles.Bold, TextAlignmentOptions.Center, textColor, Vector2.zero, new Vector2(560f, 76f));
        text.characterSpacing = 2f;
        return button;
    }

    private static Button CreateImageMenuButton(
        Transform parent,
        string name,
        Sprite buttonSprite,
        string fallbackLabel,
        Color fallbackBackgroundColor,
        Color fallbackTextColor,
        Vector2 position)
    {
        if (buttonSprite == null)
        {
            return CreateMenuButton(parent, name, fallbackLabel, fallbackBackgroundColor, fallbackTextColor, position);
        }

        Vector2 size = GetSpriteDisplaySize(buttonSprite, PauseActionButtonWidth);
        RectTransform rect = CreatePanel(name, parent, Color.white, typeof(Button));
        SetAnchored(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);

        Image image = rect.GetComponent<Image>();
        image.sprite = buttonSprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;

        Button button = rect.GetComponent<Button>();
        button.targetGraphic = image;
        ApplyButtonColors(button, Color.white);
        return button;
    }

    private static Vector2 GetSpriteDisplaySize(Sprite sprite, float targetWidth)
    {
        if (sprite == null || sprite.rect.width <= 0f || sprite.rect.height <= 0f)
        {
            return new Vector2(targetWidth, targetWidth * 0.42f);
        }

        return new Vector2(targetWidth, targetWidth * sprite.rect.height / sprite.rect.width);
    }

    private static Button CreateImageOnlyButton(
        Transform parent,
        string name,
        Sprite buttonSprite,
        string fallbackLabel,
        Color fallbackBackgroundColor,
        Color fallbackTextColor,
        Vector2 position,
        Vector2 size)
    {
        if (buttonSprite == null)
        {
            return CreateSmallButton(parent, name, fallbackLabel, fallbackBackgroundColor, fallbackTextColor, position, size);
        }

        RectTransform rect = CreatePanel(name, parent, Color.white, typeof(Button));
        SetAnchored(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);

        Image image = rect.GetComponent<Image>();
        image.sprite = buttonSprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;

        Button button = rect.GetComponent<Button>();
        button.targetGraphic = image;
        ApplyButtonColors(button, Color.white);
        return button;
    }

    private static Button CreateSmallButton(Transform parent, string name, string label, Color backgroundColor, Color textColor, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreatePanel(name, parent, backgroundColor, typeof(Button));
        SetAnchored(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);

        Button button = rect.GetComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        ApplyButtonColors(button, Color.white);

        TMP_Text text = CreateText("Label", rect, label, 24f, FontStyles.Bold, TextAlignmentOptions.Center, textColor, Vector2.zero, size - new Vector2(24f, 16f));
        text.characterSpacing = 1f;
        return button;
    }

    private static RectTransform CreateSettingsPanel(Transform parent)
    {
        RectTransform panel = CreatePanel("SettingsPanel", parent, SettingsPanelColor);
        SetAnchored(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -330f), new Vector2(SettingsPanelWidth, SettingsPanelHeight));

        CreateText("Title", panel, "SETTINGS", 58f, FontStyles.Normal, TextAlignmentOptions.Left, SettingsTextColor, new Vector2(-86f, 452f), new Vector2(612f, 92f));

        RectTransform divider = CreatePanel("Divider", panel, SettingsDividerColor);
        SetAnchored(divider, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 412f), new Vector2(784f, 4f));
        divider.GetComponent<Image>().raycastTarget = false;
        return panel;
    }

    private void CreateVolumeRow(
        Transform parent,
        string label,
        string detail,
        Vector2 position,
        Color accentColor,
        out Button toggleButton,
        out TMP_Text toggleText,
        out Slider slider,
        out TMP_Text valueText,
        out Image accentImage)
    {
        RectTransform row = CreatePanel($"{label}Row", parent, SettingsRowColor);
        SetAnchored(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(SettingsRowWidth, SettingsRowHeight));

        accentImage = CreatePanel("AccentBar", row, accentColor).GetComponent<Image>();
        Stretch((RectTransform)accentImage.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(12f, 0f));
        accentImage.raycastTarget = false;

        CreateText("Label", row, label, 52f, FontStyles.Normal, TextAlignmentOptions.Left, SettingsTextColor, new Vector2(-152f, 34f), new Vector2(360f, 70f));
        CreateText("Detail", row, detail, 27f, FontStyles.Normal, TextAlignmentOptions.Left, SettingsMutedTextColor, new Vector2(-80f, -36f), new Vector2(500f, 58f));

        toggleText = CreateText("StateText", row, "ON", 38f, FontStyles.Normal, TextAlignmentOptions.Center, SettingsTextColor, new Vector2(94f, 0f), new Vector2(96f, 60f));

        RectTransform toggleRect = CreatePanel($"{label}ToggleButton", row, Color.white, typeof(Button));
        SetAnchored(toggleRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(256f, 0f), new Vector2(172f, 102f));
        Image toggleImage = toggleRect.GetComponent<Image>();
        toggleImage.sprite = _toggleOnSprite;
        toggleImage.preserveAspect = true;
        toggleButton = toggleRect.GetComponent<Button>();
        toggleButton.targetGraphic = toggleImage;
        ApplyButtonColors(toggleButton, Color.white);

        slider = CreateSlider(row, $"{label}VolumeSlider", new Vector2(-58f, -59f), new Vector2(498f, 38f), accentColor);
        valueText = CreateText($"{label}VolumeValueText", row, "100%", 27f, FontStyles.Normal, TextAlignmentOptions.Center, SettingsTextColor, new Vector2(276f, -59f), new Vector2(112f, 44f));
    }

    private void CreateToggleRow(
        Transform parent,
        string label,
        string detail,
        Vector2 position,
        Color accentColor,
        out Button toggleButton,
        out TMP_Text toggleText,
        out Image accentImage)
    {
        RectTransform row = CreatePanel($"{label}Row", parent, SettingsRowColor);
        SetAnchored(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(SettingsRowWidth, SettingsRowHeight));

        accentImage = CreatePanel("AccentBar", row, accentColor).GetComponent<Image>();
        Stretch((RectTransform)accentImage.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(12f, 0f));
        accentImage.raycastTarget = false;

        CreateText("Label", row, label, 52f, FontStyles.Normal, TextAlignmentOptions.Left, SettingsTextColor, new Vector2(-152f, 34f), new Vector2(360f, 70f));
        CreateText("Detail", row, detail, 27f, FontStyles.Normal, TextAlignmentOptions.Left, SettingsMutedTextColor, new Vector2(-80f, -36f), new Vector2(500f, 58f));

        toggleText = CreateText("StateText", row, "ON", 38f, FontStyles.Normal, TextAlignmentOptions.Center, SettingsTextColor, new Vector2(94f, 0f), new Vector2(96f, 60f));

        RectTransform toggleRect = CreatePanel($"{label}ToggleButton", row, Color.white, typeof(Button));
        SetAnchored(toggleRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(256f, 0f), new Vector2(172f, 102f));
        Image toggleImage = toggleRect.GetComponent<Image>();
        toggleImage.sprite = _toggleOnSprite;
        toggleImage.preserveAspect = true;
        toggleButton = toggleRect.GetComponent<Button>();
        toggleButton.targetGraphic = toggleImage;
        ApplyButtonColors(toggleButton, Color.white);
    }

    private static Slider CreateSlider(Transform parent, string name, Vector2 position, Vector2 size, Color accentColor)
    {
        RectTransform sliderRect = CreateUiObject(name, parent, typeof(Slider));
        SetAnchored(sliderRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);

        RectTransform background = CreatePanel("Background", sliderRect, SettingsDividerColor);
        Stretch(background, Vector2.zero, Vector2.one, new Vector2(0f, 13f), new Vector2(0f, -13f));

        RectTransform fillArea = CreateUiObject("Fill Area", sliderRect);
        Stretch(fillArea, Vector2.zero, Vector2.one, new Vector2(0f, 13f), new Vector2(0f, -13f));

        RectTransform fill = CreatePanel("Fill", fillArea, accentColor);
        Stretch(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform handleArea = CreateUiObject("Handle Slide Area", sliderRect);
        Stretch(handleArea, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));

        RectTransform handle = CreatePanel("Handle", handleArea, Color.white);
        SetAnchored(handle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(SettingsSliderHandleWidth, SettingsSliderHandleHeight));

        Slider slider = sliderRect.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        return slider;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color, params Type[] extraComponents)
    {
        Type[] components = new Type[extraComponents.Length + 1];
        components[0] = typeof(Image);
        for (int i = 0; i < extraComponents.Length; i += 1)
        {
            components[i + 1] = extraComponents[i];
        }

        RectTransform rect = CreateUiObject(name, parent, components);
        Image image = rect.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return rect;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool preserveAspect)
    {
        RectTransform rect = CreateUiObject(name, parent, typeof(Image));
        Image image = rect.GetComponent<Image>();
        image.sprite = sprite;
        image.color = sprite != null ? color : Color.clear;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment,
        Color color,
        Vector2 position,
        Vector2 size)
    {
        RectTransform rect = CreateUiObject(name, parent, typeof(TextMeshProUGUI));
        SetAnchored(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);

        TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontSizeMax = fontSize;
        text.fontSizeMin = Mathf.Max(14f, fontSize * 0.55f);
        text.enableAutoSizing = true;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
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
        gameObject.layer = LayerMask.NameToLayer("UI");
        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }

        return (RectTransform)gameObject.transform;
    }

    private static void ApplyButtonColors(Button button, Color normalColor)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.selectedColor = normalColor;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.82f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.42f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
    }

    private void SetToggleVisual(Button button, TMP_Text text, Image accentImage, Color accentColor, bool isOn)
    {
        if (button != null && button.targetGraphic is Image image)
        {
            Sprite stateSprite = isOn ? _toggleOnSprite : _toggleOffSprite;
            if (stateSprite != null)
            {
                image.sprite = stateSprite;
                image.preserveAspect = true;
                image.type = Image.Type.Simple;
                image.color = Color.white;
            }
            else
            {
                image.color = isOn ? ToggleOnColor : ToggleOffColor;
            }
        }

        if (text != null)
        {
            text.text = isOn ? "ON" : "OFF";
            text.color = isOn ? SettingsTextColor : SettingsDisabledTextColor;
        }

        if (accentImage != null)
        {
            accentImage.color = isOn ? accentColor : SettingsDisabledAccentColor;
        }
    }

    private static void RefreshVolumeText(TMP_Text text, float volume)
    {
        if (text != null)
        {
            text.text = $"{Mathf.RoundToInt(Mathf.Clamp01(volume) * 100f)}%";
        }
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject;
#if ENABLE_INPUT_SYSTEM
        eventSystemObject = new("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
#elif ENABLE_LEGACY_INPUT_MANAGER
        eventSystemObject = new("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#else
        eventSystemObject = new("EventSystem", typeof(EventSystem));
#endif
        eventSystemObject.layer = LayerMask.NameToLayer("UI");
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
