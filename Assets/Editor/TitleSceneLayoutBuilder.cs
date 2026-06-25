using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class TitleSceneLayoutBuilder
{
    private const string ScenePath = "Assets/Scenes/Title.unity";
    private const string TitleFontAssetPath = "Assets/Fonts/DotGothic16-Regular SDF.asset";
    private const string HeadlineFontAssetPath = "Assets/Fonts/DotGothic16-Regular SDF.asset";
    private const string UiFontAssetPath = "Assets/Fonts/DotGothic16-Regular SDF.asset";
    private const string JapaneseFallbackFontAssetPath = "Assets/Fonts/DotGothic16-Regular SDF.asset";
    private const string TitleBackgroundPath = "Assets/Art/UI/Sprites/Title/title_background_factory.png";
    private const string TitleLogoPath = "Assets/Art/UI/Sprites/Title/siwakennja.png";
    private const string SecondaryButtonPath = "Assets/Art/UI/Sprites/Result/Common/button_secondary_outline_strong.png";
    private const string BackButtonPath = "Assets/Art/UI/Sprites/Buttons/ui_button_back_small.png";
    private const string SettingsIconPath = "Assets/Art/UI/Sprites/Settings/ui_settings_icon.png";
    private const string HowToIconPath = "Assets/Art/UI/Sprites/Settings/ui_howto_icon.png";
    private const string ToggleOnPath = "Assets/Art/UI/Sprites/Settings/ui_settings_toggle_on.png";
    private const string ToggleOffPath = "Assets/Art/UI/Sprites/Settings/ui_settings_toggle_off.png";
    private const string PowaIdlePath = "Assets/Art/Sprites/Characters/Powa/Powa_Idle.png";
    private const string TruckSpritePath = "Assets/Art/Sprites/Vehicles/truck.png";
    private const string CarSpritePath = "Assets/Art/Sprites/Vehicles/car.png";
    private const string SportsCarSpritePath = "Assets/Art/Sprites/Vehicles/sportscar.png";
    private const string CompactCarIconPath = "Assets/Art/Sprites/Vehicles/car.png";
    private const string LightTruckIconPath = "Assets/Art/Sprites/Vehicles/truck.png";
    private const string SportsCarIconPath = "Assets/Art/Sprites/Vehicles/sportscar.png";

    private static readonly Color CameraColor = new(0.63f, 0.84f, 1f, 1f);
    private static readonly Color CardColor = new(1f, 1f, 1f, 0.96f);
    private static readonly Color CardShadowColor = new(0.08f, 0.15f, 0.24f, 0.12f);
    private static readonly Color TextColor = new(0.137f, 0.184f, 0.275f, 1f);
    private static readonly Color MutedTextColor = new(0.44f, 0.49f, 0.58f, 1f);
    private static readonly Color AccentColor = new(0.949f, 0.772f, 0.259f, 1f);
    private static readonly Color SuccessColor = new(0.345f, 0.784f, 0.541f, 1f);
    private static readonly Color BlueAccentColor = new(0.219f, 0.643f, 0.94f, 1f);
    private static readonly Color InkColor = new(0.025f, 0.075f, 0.145f, 1f);
    private static readonly Color LogoYellowColor = new(1f, 0.86f, 0.12f, 1f);
    private static readonly Color RowColor = new(0.975f, 0.982f, 0.992f, 1f);
    private static readonly Color DividerColor = new(0.902f, 0.922f, 0.953f, 1f);
    private static readonly Color ModalScrimColor = new(0.04f, 0.07f, 0.11f, 0.58f);

    private static TMP_FontAsset s_JapaneseFontAsset;

    private readonly struct SettingsBindings
    {
        public SettingsBindings(
            Toggle bgmToggle,
            Toggle seToggle,
            Toggle vibrationToggle,
            Slider bgmVolumeSlider,
            Slider seVolumeSlider,
            TMP_Text bgmStateText,
            TMP_Text seStateText,
            TMP_Text vibrationStateText,
            TMP_Text bgmVolumeValueText,
            TMP_Text seVolumeValueText,
            Image bgmToggleImage,
            Image seToggleImage,
            Image vibrationToggleImage,
            Image bgmAccentImage,
            Image seAccentImage,
            Image vibrationAccentImage)
        {
            BgmToggle = bgmToggle;
            SeToggle = seToggle;
            VibrationToggle = vibrationToggle;
            BgmVolumeSlider = bgmVolumeSlider;
            SeVolumeSlider = seVolumeSlider;
            BgmStateText = bgmStateText;
            SeStateText = seStateText;
            VibrationStateText = vibrationStateText;
            BgmVolumeValueText = bgmVolumeValueText;
            SeVolumeValueText = seVolumeValueText;
            BgmToggleImage = bgmToggleImage;
            SeToggleImage = seToggleImage;
            VibrationToggleImage = vibrationToggleImage;
            BgmAccentImage = bgmAccentImage;
            SeAccentImage = seAccentImage;
            VibrationAccentImage = vibrationAccentImage;
        }

        public Toggle BgmToggle { get; }
        public Toggle SeToggle { get; }
        public Toggle VibrationToggle { get; }
        public Slider BgmVolumeSlider { get; }
        public Slider SeVolumeSlider { get; }
        public TMP_Text BgmStateText { get; }
        public TMP_Text SeStateText { get; }
        public TMP_Text VibrationStateText { get; }
        public TMP_Text BgmVolumeValueText { get; }
        public TMP_Text SeVolumeValueText { get; }
        public Image BgmToggleImage { get; }
        public Image SeToggleImage { get; }
        public Image VibrationToggleImage { get; }
        public Image BgmAccentImage { get; }
        public Image SeAccentImage { get; }
        public Image VibrationAccentImage { get; }
    }

    [MenuItem("Tools/Scenes/Rebuild Title Scene UI")]
    public static void RebuildFromMenu() => BuildScene();

    public static void BuildFromBatchMode() => BuildScene();

    private static void BuildScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Camera camera = EnsureMainCamera(scene);
        EnsureEventSystem(scene);
        Canvas canvas = EnsureCanvas(scene);
        GameObject titleManagerObject = EnsureTitleManager(scene);
        TitleController titleController = GetOrAddComponent<TitleController>(titleManagerObject);
        HowToOverlayController howToOverlayController = GetOrAddComponent<HowToOverlayController>(titleManagerObject);
        GetOrAddComponent<OrientationController>(titleManagerObject);

        ClearChildren(canvas.transform);
        BuildLayout(canvas, titleController, howToOverlayController);

        EditorUtility.SetDirty(camera);
        EditorUtility.SetDirty(canvas.gameObject);
        EditorUtility.SetDirty(titleManagerObject);
        EditorUtility.SetDirty(titleController);
        EditorUtility.SetDirty(howToOverlayController);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("Title scene UI rebuilt.");
    }

    private static Camera EnsureMainCamera(Scene scene)
    {
        Camera camera = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
        if (camera == null)
        {
            GameObject cameraObject = new("Main Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.gameObject.name = "Main Camera";
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = CameraColor;
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.transform.rotation = Quaternion.identity;
        camera.allowHDR = true;
        camera.allowMSAA = true;
        return camera;
    }

    private static EventSystem EnsureEventSystem(Scene scene)
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new("EventSystem");
            SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        return eventSystem;
    }

    private static Canvas EnsureCanvas(Scene scene)
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            GameObject canvasObject = new("Canvas");
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            canvas = canvasObject.AddComponent<Canvas>();
        }

        canvas.gameObject.name = "Canvas";
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.gameObject.layer = LayerMask.NameToLayer("UI");

        CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(canvas.gameObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        CanvasMatchByAspect aspect = GetOrAddComponent<CanvasMatchByAspect>(canvas.gameObject);
        aspect.wideMatch = 0.5f;
        aspect.tallMatch = 0.5f;

        GetOrAddComponent<GraphicRaycaster>(canvas.gameObject);

        RectTransform rectTransform = canvas.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        return canvas;
    }

    private static GameObject EnsureTitleManager(Scene scene)
    {
        GameObject titleManagerObject = GameObject.Find("TitleManager");
        if (titleManagerObject == null)
        {
            titleManagerObject = new GameObject("TitleManager");
            SceneManager.MoveGameObjectToScene(titleManagerObject, scene);
        }

        titleManagerObject.transform.position = Vector3.zero;
        titleManagerObject.transform.rotation = Quaternion.identity;
        titleManagerObject.transform.localScale = Vector3.one;
        return titleManagerObject;
    }

    private static void BuildLayout(Canvas canvas, TitleController titleController, HowToOverlayController howToOverlayController)
    {
        TMP_FontAsset titleFontAsset = LoadFont(TitleFontAssetPath);
        TMP_FontAsset headlineFontAsset = LoadFont(HeadlineFontAssetPath);
        TMP_FontAsset uiFontAsset = LoadFont(UiFontAssetPath);
        TMP_FontAsset japaneseFallbackFontAsset = LoadFont(JapaneseFallbackFontAssetPath);
        titleFontAsset ??= TMP_Settings.defaultFontAsset;
        headlineFontAsset ??= titleFontAsset;
        uiFontAsset ??= headlineFontAsset;
        s_JapaneseFontAsset = japaneseFallbackFontAsset != null ? japaneseFallbackFontAsset : uiFontAsset;

        EnsureFallbackFont(titleFontAsset, japaneseFallbackFontAsset);
        EnsureFallbackFont(headlineFontAsset, japaneseFallbackFontAsset);
        EnsureFallbackFont(uiFontAsset, japaneseFallbackFontAsset);

        Sprite slicedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Sprite backgroundSprite = LoadSprite(TitleBackgroundPath);
        Sprite logoSprite = LoadSprite(TitleLogoPath);
        Sprite secondaryButtonSprite = LoadSprite(SecondaryButtonPath);
        Sprite backButtonSprite = LoadSprite(BackButtonPath);
        Sprite settingsIconSprite = LoadSprite(SettingsIconPath);
        Sprite howToIconSprite = LoadSprite(HowToIconPath);
        Sprite toggleOnSprite = LoadSprite(ToggleOnPath);
        Sprite toggleOffSprite = LoadSprite(ToggleOffPath);
        Sprite powaSprite = LoadSprite(PowaIdlePath);
        Sprite truckSprite = LoadSprite(TruckSpritePath);
        Sprite carSprite = LoadSprite(CarSpritePath);
        Sprite sportsCarSprite = LoadSprite(SportsCarSpritePath);

        RectTransform background = CreateUIObject("Background", canvas.transform);
        Stretch(background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        BuildTitleBackground(background, slicedSprite, backgroundSprite);

        RectTransform safeAreaRoot = CreateUIObject("SafeAreaRoot", canvas.transform);
        Stretch(safeAreaRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        GetOrAddComponent<SafeAreaFitter>(safeAreaRoot.gameObject);

        BuildTitleContent(safeAreaRoot, titleController, titleFontAsset, headlineFontAsset, uiFontAsset, logoSprite, settingsIconSprite, howToIconSprite, powaSprite, truckSprite, carSprite, sportsCarSprite, slicedSprite);

        RectTransform howToOverlay = BuildHowToOverlay(canvas.transform, howToOverlayController, titleFontAsset, headlineFontAsset, uiFontAsset, secondaryButtonSprite, backButtonSprite, slicedSprite);
        RectTransform settingsOverlay = BuildSettingsOverlay(canvas.transform, titleController, headlineFontAsset, uiFontAsset, slicedSprite, secondaryButtonSprite, backButtonSprite, toggleOnSprite, toggleOffSprite, slicedSprite, out SettingsPanelController settingsPanelController, out SettingsBindings settingsBindings);

        ApplyTitleControllerBindings(titleController, howToOverlay.gameObject, settingsOverlay.gameObject, howToOverlayController);
        ApplyHowToBindings(howToOverlayController, howToOverlay.gameObject);
        ApplySettingsBindings(settingsPanelController, settingsBindings, toggleOnSprite, toggleOffSprite);
    }

    private static void BuildTitleBackground(RectTransform parent, Sprite slicedSprite, Sprite backgroundSprite)
    {
        if (backgroundSprite != null)
        {
            Image backgroundImage = CreateImage("TitleBackgroundImage", parent, backgroundSprite, Color.white, false);
            RectTransform backgroundRect = backgroundImage.rectTransform;
            Stretch(backgroundRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            AspectRatioFitter aspectRatioFitter = backgroundImage.gameObject.AddComponent<AspectRatioFitter>();
            aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspectRatioFitter.aspectRatio = backgroundSprite.rect.width / backgroundSprite.rect.height;
            return;
        }

        RectTransform baseColor = CreatePanel("BaseColor", parent, slicedSprite, CameraColor);
        Stretch(baseColor, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private static void BuildTitleContent(
        RectTransform parent,
        TitleController titleController,
        TMP_FontAsset titleFontAsset,
        TMP_FontAsset headlineFontAsset,
        TMP_FontAsset uiFontAsset,
        Sprite logoSprite,
        Sprite settingsIconSprite,
        Sprite howToIconSprite,
        Sprite powaSprite,
        Sprite truckSprite,
        Sprite carSprite,
        Sprite sportsCarSprite,
        Sprite slicedSprite)
    {
        RectTransform contentRoot = CreateUIObject("ContentRoot", parent);
        Stretch(contentRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Button settingsButton = CreateRoundMenuButton("SettingsButtonTop", contentRoot, settingsIconSprite, "設定", uiFontAsset, slicedSprite);
        SetAnchored((RectTransform)settingsButton.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(108f, -90f), new Vector2(142f, 142f));
        AddButtonListener(settingsButton, titleController.OnSettingsOpen);
        Button howToButton = CreateRoundMenuButton("HowToButton", contentRoot, howToIconSprite, "あそびかた", uiFontAsset, slicedSprite, "?");
        SetAnchored((RectTransform)howToButton.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-116f, -90f), new Vector2(164f, 142f));
        AddButtonListener(howToButton, titleController.OnHowToOpen);

        RectTransform logoBlock = CreateUIObject("LogoBlock", contentRoot);
        SetAnchored(logoBlock, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -136f), new Vector2(990f, 410f));
        BuildLogoBlock(logoBlock, logoSprite, titleFontAsset, headlineFontAsset, uiFontAsset, slicedSprite);

        RectTransform catchCopyBubble = CreatePanel("CatchCopyBubble", contentRoot, slicedSprite, new Color(1f, 0.98f, 0.90f, 1f));
        SetAnchored(catchCopyBubble, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -570f), new Vector2(820f, 96f));
        AddShadow(catchCopyBubble.gameObject, new Color(0.04f, 0.08f, 0.13f, 0.44f), new Vector2(0f, -8f));
        TMP_Text catchCopy = CreateText("CatchCopy", catchCopyBubble, "<color=#246FEA>見分けろ</color>、<color=#F44336>押せ</color>、<color=#1A9A3A>仕分けろ！</color>", headlineFontAsset, 42f, 30f, InkColor, TextAlignmentOptions.Center);
        Stretch((RectTransform)catchCopy.transform, Vector2.zero, Vector2.one, new Vector2(34f, 16f), new Vector2(-34f, -14f));
        catchCopy.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform hero = CreateUIObject("HeroStage", contentRoot);
        SetAnchored(hero, new Vector2(0.5f, 0.43f), new Vector2(0.5f, 0.43f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1010f, 570f));
        BuildHeroStage(hero, powaSprite, truckSprite, carSprite, sportsCarSprite, slicedSprite);

        RectTransform bottomRoot = CreateUIObject("BottomUtilityRoot", contentRoot);
        SetAnchored(bottomRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(1080f, 1152f));

        HorizontalLayoutGroup bottomGroup = bottomRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        bottomGroup.padding = new RectOffset(0, 0, 0, 0);
        bottomGroup.spacing = 36;
        bottomGroup.childAlignment = TextAnchor.MiddleCenter;
        bottomGroup.childControlWidth = true;
        bottomGroup.childControlHeight = true;
        bottomGroup.childForceExpandWidth = true;
        bottomGroup.childForceExpandHeight = true;
        Button stageButton = CreateWideIconButton("StageSelectButton", bottomRoot, null, null, "<color=#FFFFFF>TAP TO START</color>", headlineFontAsset);
        AddButtonListener(stageButton, titleController.OnStartPressed);

        settingsButton.transform.SetAsLastSibling();
        howToButton.transform.SetAsLastSibling();
    }

    private static void BuildLogoBlock(RectTransform parent, Sprite logoSprite, TMP_FontAsset titleFontAsset, TMP_FontAsset headlineFontAsset, TMP_FontAsset uiFontAsset, Sprite slicedSprite)
    {
        if (logoSprite != null)
        {
            Image logo = CreateImage("TitleLogo", parent, logoSprite, Color.white, true);
            SetAnchored(logo.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 514f));
        }
    }

    private static void BuildHeroStage(RectTransform parent, Sprite powaSprite, Sprite truckSprite, Sprite carSprite, Sprite sportsCarSprite, Sprite slicedSprite)
    {
        Image powa = CreateImage("Powa", parent, powaSprite, Color.white, true);
        SetAnchored(powa.rectTransform, new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.5f), new Vector2(0f, 5f), new Vector2(490f, 490f));

        Image truck = CreateImage("Truck", parent, truckSprite, Color.white, true);
        SetAnchored(truck.rectTransform, new Vector2(0.18f, 0.17f), new Vector2(0.18f, 0.17f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 210f));

        Image sports = CreateImage("SportsCar", parent, sportsCarSprite, Color.white, true);
        SetAnchored(sports.rectTransform, new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(330f, 190f));

        Image car = CreateImage("CompactCar", parent, carSprite, Color.white, true);
        SetAnchored(car.rectTransform, new Vector2(0.82f, 0.17f), new Vector2(0.82f, 0.17f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(305f, 205f));

    }

    private static Button CreateRoundMenuButton(string name, Transform parent, Sprite iconSprite, string label, TMP_FontAsset fontAsset, Sprite slicedSprite, string fallbackIconText = null)
    {
        Button button = CreateButton(name, parent, slicedSprite, new Vector2(148f, 148f), string.Empty, fontAsset, 1f, InkColor);
        RectTransform rect = (RectTransform)button.transform;
        Image bg = button.GetComponent<Image>();
        bg.color = new Color(1f, 0.97f, 0.84f, 1f);
        AddShadow(button.gameObject, new Color(0.02f, 0.06f, 0.11f, 0.48f), new Vector2(0f, -8f));

        if (iconSprite != null)
        {
            Image icon = CreateImage("Icon", rect, iconSprite, Color.white, true);
            SetAnchored(icon.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(62f, 62f));
        }
        else if (!string.IsNullOrEmpty(fallbackIconText))
        {
            TMP_Text iconText = CreateText("IconText", rect, fallbackIconText, fontAsset, 62f, 42f, Color.white, TextAlignmentOptions.Center);
            SetAnchored((RectTransform)iconText.transform, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(62f, 62f));
            iconText.fontStyle = FontStyles.Bold;
            iconText.outlineColor = InkColor;
            iconText.outlineWidth = 0.18f;
            iconText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        TMP_Text text = CreateText("Label", rect, label, fontAsset, 28f, 20f, Color.white, TextAlignmentOptions.Center);
        SetAnchored((RectTransform)text.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(150f, 44f));
        text.outlineColor = InkColor;
        text.outlineWidth = 0.18f;
        return button;
    }

    private static Button CreateWideIconButton(string name, Transform parent, Sprite buttonSprite, Sprite iconSprite, string label, TMP_FontAsset fontAsset)
    {
        Button button = CreateButton(name, parent, buttonSprite, new Vector2(1080f, 1152f), string.Empty, fontAsset, 1f, InkColor);
        if (buttonSprite == null)
        {
            button.GetComponent<Image>().color = Color.clear;
            button.targetGraphic = null;
        }

        LayoutElement layoutElement = button.GetComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1f;
        layoutElement.preferredHeight = 1152f;

        RectTransform rect = (RectTransform)button.transform;
        if (iconSprite != null)
        {
            Image icon = CreateImage("Icon", rect, iconSprite, Color.white, true);
            SetAnchored(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(70f, 0f), new Vector2(74f, 74f));
        }

        TMP_Text text = CreateText("Label", rect, label, fontAsset, 40f, 28f, Color.white, TextAlignmentOptions.Center);
        if (iconSprite != null)
        {
            Stretch((RectTransform)text.transform, Vector2.zero, Vector2.one, new Vector2(122f, 12f), new Vector2(-28f, -12f));
        }
        else
        {
            SetAnchored((RectTransform)text.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -400f), new Vector2(1024f, 112f));
        }
        text.fontStyle = FontStyles.Bold;
        text.outlineColor = InkColor;
        text.outlineWidth = 0.18f;
        return button;
    }

    private static void CreateStar(RectTransform parent, string name, Vector2 anchoredPosition, TMP_FontAsset fontAsset)
    {
        TMP_Text star = CreateText(name, parent, "*", fontAsset, 48f, 32f, AccentColor, TextAlignmentOptions.Center);
        SetAnchored((RectTransform)star.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, new Vector2(70f, 70f));
        star.outlineColor = Color.white;
        star.outlineWidth = 0.14f;
    }

    private static void AddShadow(GameObject gameObject, Color color, Vector2 distance)
    {
        Shadow shadow = GetOrAddComponent<Shadow>(gameObject);
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static void BuildInfoDock(RectTransform infoDock, TMP_FontAsset headlineFontAsset, TMP_FontAsset uiFontAsset, Sprite slicedSprite)
    {
        VerticalLayoutGroup dockLayout = infoDock.gameObject.AddComponent<VerticalLayoutGroup>();
        dockLayout.padding = new RectOffset(34, 34, 26, 26);
        dockLayout.spacing = 18;
        dockLayout.childAlignment = TextAnchor.UpperCenter;
        dockLayout.childControlWidth = true;
        dockLayout.childControlHeight = true;
        dockLayout.childForceExpandWidth = true;
        dockLayout.childForceExpandHeight = false;

        RectTransform headerRow = CreateUIObject("HeaderRow", infoDock);
        headerRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;

        TMP_Text header = CreateText("Title", headerRow, "READY TO SORT", headlineFontAsset, 38f, 26f, TextColor, TextAlignmentOptions.Left);
        Stretch((RectTransform)header.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform accent = CreatePanel("Accent", headerRow, slicedSprite, AccentColor);
        SetAnchored(accent, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-4f, 0f), new Vector2(172f, 10f));

        RectTransform chipRow = CreateUIObject("ChipRow", infoDock);
        chipRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 150f;
        HorizontalLayoutGroup chipLayout = chipRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        chipLayout.spacing = 16;
        chipLayout.childAlignment = TextAnchor.MiddleCenter;
        chipLayout.childControlWidth = true;
        chipLayout.childControlHeight = true;
        chipLayout.childForceExpandWidth = true;
        chipLayout.childForceExpandHeight = true;

        CreateInfoChip(chipRow, "STAGE", "SELECT", BlueAccentColor, headlineFontAsset, uiFontAsset, slicedSprite);
        CreateInfoChip(chipRow, "RULE", "HOW TO", SuccessColor, headlineFontAsset, uiFontAsset, slicedSprite);
        CreateInfoChip(chipRow, "SOUND", "BGM / SE", AccentColor, headlineFontAsset, uiFontAsset, slicedSprite);
    }

    private static void CreateInfoChip(RectTransform parent, string title, string value, Color accentColor, TMP_FontAsset headlineFontAsset, TMP_FontAsset uiFontAsset, Sprite slicedSprite)
    {
        RectTransform chip = CreatePanel($"Chip_{title}", parent, slicedSprite, RowColor);
        chip.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        RectTransform accent = CreatePanel("AccentBar", chip, slicedSprite, accentColor);
        Stretch(accent, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(9f, 0f));

        TMP_Text titleText = CreateText("Title", chip, title, uiFontAsset, 25f, 18f, MutedTextColor, TextAlignmentOptions.Center);
        SetAnchored((RectTransform)titleText.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(6f, -34f), new Vector2(220f, 42f));
        titleText.textWrappingMode = TextWrappingModes.NoWrap;

        TMP_Text valueText = CreateText("Value", chip, value, headlineFontAsset, 32f, 21f, TextColor, TextAlignmentOptions.Center);
        SetAnchored((RectTransform)valueText.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(6f, 28f), new Vector2(230f, 58f));
        valueText.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private static RectTransform BuildHowToOverlay(
        Transform canvasTransform,
        HowToOverlayController howToOverlayController,
        TMP_FontAsset titleFontAsset,
        TMP_FontAsset headlineFontAsset,
        TMP_FontAsset uiFontAsset,
        Sprite secondaryButtonSprite,
        Sprite backButtonSprite,
        Sprite slicedSprite)
    {
        RectTransform overlay = CreatePanel("HowToOverlay", canvasTransform, slicedSprite, ModalScrimColor);
        Stretch(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform dialog = CreateModalDialog("HowToPanel", overlay, slicedSprite, new Vector2(900f, 1260f));
        BuildModalHeader(dialog, "HOW TO", titleFontAsset, backButtonSprite, out Button topCloseButton);

        RectTransform body = CreateUIObject("Body", dialog);
        Stretch(body, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(58f, 138f), new Vector2(-58f, -164f));
        VerticalLayoutGroup bodyLayout = body.gameObject.AddComponent<VerticalLayoutGroup>();
        bodyLayout.spacing = 18;
        bodyLayout.childAlignment = TextAnchor.UpperCenter;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = false;

        CreateHowToStep(body, "01", "COMING CAR", "車を見て、同じレーンのボタンをタップ", BlueAccentColor, LoadSprite(LightTruckIconPath), headlineFontAsset, uiFontAsset, slicedSprite);
        CreateHowToStep(body, "02", "OK / GOOD", "正しく仕分けるとスコアアップ", SuccessColor, LoadSprite(CompactCarIconPath), headlineFontAsset, uiFontAsset, slicedSprite);
        CreateHowToStep(body, "03", "MISS LIMIT", "MISS が上限に届くと GAME OVER", new Color(0.914f, 0.408f, 0.416f, 1f), LoadSprite(SportsCarIconPath), headlineFontAsset, uiFontAsset, slicedSprite);

        Button closeButton = CreateButton("CloseButton", dialog, secondaryButtonSprite != null ? secondaryButtonSprite : slicedSprite, new Vector2(560f, 136f), "OK", headlineFontAsset, 48f, TextColor);
        SetAnchored((RectTransform)closeButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(560f, 136f));

        SerializedObject serializedHowTo = new(howToOverlayController);
        SetObjectReference(serializedHowTo, "_overlayPanel", overlay.gameObject);
        SetObjectReference(serializedHowTo, "_closeButton", closeButton);
        SetObjectArray(serializedHowTo.FindProperty("_extraCloseButtons"), new Object[] { topCloseButton });
        serializedHowTo.ApplyModifiedPropertiesWithoutUndo();

        overlay.gameObject.SetActive(false);
        return overlay;
    }

    private static void CreateHowToStep(
        RectTransform parent,
        string number,
        string title,
        string detail,
        Color accentColor,
        Sprite iconSprite,
        TMP_FontAsset headlineFontAsset,
        TMP_FontAsset uiFontAsset,
        Sprite slicedSprite)
    {
        RectTransform row = CreatePanel($"Step_{number}", parent, slicedSprite, RowColor);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 230f;

        RectTransform accent = CreatePanel("AccentBar", row, slicedSprite, accentColor);
        Stretch(accent, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(12f, 0f));

        TMP_Text numberText = CreateText("Number", row, number, headlineFontAsset, 48f, 32f, accentColor, TextAlignmentOptions.Center);
        SetAnchored((RectTransform)numberText.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(84f, 0f), new Vector2(118f, 92f));

        Image iconImage = CreateImage("Icon", row, iconSprite, Color.white, true);
        SetAnchored(iconImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(218f, 0f), new Vector2(126f, 126f));

        TMP_Text titleText = CreateText("Title", row, title, headlineFontAsset, 38f, 24f, TextColor, TextAlignmentOptions.Left);
        SetAnchored((RectTransform)titleText.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(300f, 42f), new Vector2(-330f, 58f));
        titleText.textWrappingMode = TextWrappingModes.NoWrap;

        TMP_Text detailText = CreateText("Detail", row, detail, uiFontAsset, 30f, 22f, MutedTextColor, TextAlignmentOptions.Left);
        SetAnchored((RectTransform)detailText.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(300f, -36f), new Vector2(-330f, 74f));
        detailText.textWrappingMode = TextWrappingModes.Normal;
    }

    private static RectTransform BuildSettingsOverlay(
        Transform canvasTransform,
        TitleController titleController,
        TMP_FontAsset headlineFontAsset,
        TMP_FontAsset uiFontAsset,
        Sprite panelSprite,
        Sprite secondaryButtonSprite,
        Sprite backButtonSprite,
        Sprite toggleOnSprite,
        Sprite toggleOffSprite,
        Sprite slicedSprite,
        out SettingsPanelController settingsPanelController,
        out SettingsBindings settingsBindings)
    {
        RectTransform overlay = CreatePanel("SettingsOverlay", canvasTransform, slicedSprite, ModalScrimColor);
        Stretch(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform dialog = CreateModalDialog("SettingsPanel", overlay, panelSprite, new Vector2(900f, 1080f));
        settingsPanelController = GetOrAddComponent<SettingsPanelController>(dialog.gameObject);
        BuildModalHeader(dialog, "SETTINGS", headlineFontAsset, backButtonSprite, out Button topCloseButton);
        AddButtonListener(topCloseButton, titleController.OnSettingsClose);

        RectTransform body = CreateUIObject("Body", dialog);
        Stretch(body, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(60f, 150f), new Vector2(-60f, -128f));

        VerticalLayoutGroup bodyLayout = body.gameObject.AddComponent<VerticalLayoutGroup>();
        bodyLayout.spacing = 24;
        bodyLayout.childAlignment = TextAnchor.UpperCenter;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = false;

        CreateSettingsRow(body, "BGM", "TITLE / STAGE MUSIC", SuccessColor, toggleOnSprite, headlineFontAsset, uiFontAsset, slicedSprite, out RectTransform bgmRow, out Toggle bgmToggle, out TMP_Text bgmState, out Image bgmToggleImage, out Image bgmAccent);
        CreateSettingsVolumeControl(bgmRow, "BGM", SuccessColor, headlineFontAsset, slicedSprite, out Slider bgmVolumeSlider, out TMP_Text bgmVolumeValueText);
        CreateSettingsRow(body, "SE", "BUTTON / JUDGE SOUNDS", BlueAccentColor, toggleOnSprite, headlineFontAsset, uiFontAsset, slicedSprite, out RectTransform seRow, out Toggle seToggle, out TMP_Text seState, out Image seToggleImage, out Image seAccent);
        CreateSettingsVolumeControl(seRow, "SE", BlueAccentColor, headlineFontAsset, slicedSprite, out Slider seVolumeSlider, out TMP_Text seVolumeValueText);
        CreateSettingsRow(body, "VIBRATION", "JUDGE / RESULT FEEDBACK", AccentColor, toggleOnSprite, headlineFontAsset, uiFontAsset, slicedSprite, out _, out Toggle vibrationToggle, out TMP_Text vibrationState, out Image vibrationToggleImage, out Image vibrationAccent);

        settingsBindings = new SettingsBindings(bgmToggle, seToggle, vibrationToggle, bgmVolumeSlider, seVolumeSlider, bgmState, seState, vibrationState, bgmVolumeValueText, seVolumeValueText, bgmToggleImage, seToggleImage, vibrationToggleImage, bgmAccent, seAccent, vibrationAccent);
        overlay.gameObject.SetActive(false);
        return overlay;
    }

    private static void CreateSettingsRow(
        RectTransform parent,
        string label,
        string detail,
        Color accentColor,
        Sprite toggleSprite,
        TMP_FontAsset headlineFontAsset,
        TMP_FontAsset uiFontAsset,
        Sprite slicedSprite,
        out RectTransform row,
        out Toggle toggle,
        out TMP_Text stateText,
        out Image toggleImage,
        out Image accentImage)
    {
        row = CreatePanel($"{label}Row", parent, slicedSprite, RowColor);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 204f;

        accentImage = CreatePanel("AccentBar", row, slicedSprite, accentColor).GetComponent<Image>();
        Stretch((RectTransform)accentImage.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(12f, 0f));

        TMP_Text labelText = CreateText("Label", row, label, headlineFontAsset, 52f, 34f, TextColor, TextAlignmentOptions.Left);
        SetAnchored((RectTransform)labelText.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(58f, 34f), new Vector2(360f, 70f));
        labelText.textWrappingMode = TextWrappingModes.NoWrap;

        TMP_Text detailText = CreateText("Detail", row, detail, uiFontAsset, 27f, 20f, MutedTextColor, TextAlignmentOptions.Left);
        SetAnchored((RectTransform)detailText.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(60f, -36f), new Vector2(500f, 58f));
        detailText.textWrappingMode = TextWrappingModes.NoWrap;

        stateText = CreateText("StateText", row, "ON", headlineFontAsset, 38f, 28f, TextColor, TextAlignmentOptions.Center);
        SetAnchored((RectTransform)stateText.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-248f, 0f), new Vector2(96f, 60f));

        GameObject toggleObject = new($"{label}Toggle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
        toggleObject.layer = parent.gameObject.layer;
        toggleObject.transform.SetParent(row, false);
        toggleImage = toggleObject.GetComponent<Image>();
        toggleImage.sprite = toggleSprite;
        toggleImage.color = Color.white;
        toggleImage.preserveAspect = true;
        toggleImage.raycastTarget = true;
        toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = toggleImage;
        toggle.graphic = null;
        SetAnchored((RectTransform)toggleObject.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-48f, 0f), new Vector2(172f, 102f));
    }

    private static void CreateSettingsVolumeControl(
        RectTransform row,
        string label,
        Color accentColor,
        TMP_FontAsset headlineFontAsset,
        Sprite slicedSprite,
        out Slider slider,
        out TMP_Text valueText)
    {
        RectTransform sliderRect = CreateUIObject($"{label}VolumeSlider", row);
        SetAnchored(sliderRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-58f, 24f), new Vector2(-282f, 38f));

        RectTransform background = CreatePanel("Background", sliderRect, slicedSprite, DividerColor);
        Stretch(background, Vector2.zero, Vector2.one, new Vector2(0f, 13f), new Vector2(0f, -13f));

        RectTransform fillArea = CreateUIObject("Fill Area", sliderRect);
        Stretch(fillArea, Vector2.zero, Vector2.one, new Vector2(0f, 13f), new Vector2(0f, -13f));

        RectTransform fill = CreatePanel("Fill", fillArea, slicedSprite, accentColor);
        Stretch(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        fill.GetComponent<Image>().raycastTarget = false;

        RectTransform handleArea = CreateUIObject("Handle Slide Area", sliderRect);
        Stretch(handleArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform handle = CreatePanel("Handle", handleArea, slicedSprite, Color.white);
        SetAnchored(handle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 2f));

        slider = sliderRect.gameObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();

        valueText = CreateText($"{label}VolumeValueText", row, "100%", headlineFontAsset, 26f, 18f, TextColor, TextAlignmentOptions.Center);
        SetAnchored((RectTransform)valueText.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-58f, 24f), new Vector2(112f, 44f));
        valueText.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private static RectTransform CreateModalDialog(string name, RectTransform overlay, Sprite sprite, Vector2 size)
    {
        RectTransform shadow = CreatePanel($"{name}Shadow", overlay, AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"), CardShadowColor);
        SetAnchored(shadow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), size);

        RectTransform dialog = CreatePanel(name, overlay, sprite, CardColor);
        SetAnchored(dialog, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        return dialog;
    }

    private static void BuildModalHeader(RectTransform dialog, string title, TMP_FontAsset fontAsset, Sprite backButtonSprite, out Button closeButton)
    {
        TMP_Text titleText = CreateText("Title", dialog, title, fontAsset, 58f, 36f, TextColor, TextAlignmentOptions.Left);
        SetAnchored((RectTransform)titleText.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(58f, -42f), new Vector2(-230f, 92f));
        titleText.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform divider = CreatePanel("Divider", dialog, AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"), DividerColor);
        SetAnchored(divider, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(-116f, 4f));

        closeButton = CreateButton("CloseButtonTop", dialog, backButtonSprite != null ? backButtonSprite : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"), new Vector2(180f, 82f), string.Empty, fontAsset, 1f, TextColor);
        SetAnchored((RectTransform)closeButton.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-42f, -34f), new Vector2(180f, 82f));
    }

    private static RectTransform CreateSurface(string name, Transform parent, Sprite sprite, Color color, float preferredHeight)
    {
        RectTransform surface = CreatePanel(name, parent, sprite, color);
        LayoutElement layoutElement = surface.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = preferredHeight;
        layoutElement.flexibleWidth = 1f;
        return surface;
    }

    private static Button CreateButton(string name, Transform parent, Sprite sprite, Vector2 size, string label, TMP_FontAsset fontAsset, float fontSize, Color textColor)
    {
        GameObject gameObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        gameObject.layer = parent.gameObject.layer;
        gameObject.transform.SetParent(parent, false);

        RectTransform rectTransform = (RectTransform)gameObject.transform;
        rectTransform.sizeDelta = size;

        Image image = gameObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.raycastTarget = true;

        LayoutElement layoutElement = gameObject.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;

        Button button = gameObject.GetComponent<Button>();
        button.targetGraphic = image;
        ApplyButtonColors(button);

        if (!string.IsNullOrEmpty(label))
        {
            TMP_Text labelText = CreateText("Label", rectTransform, label, fontAsset, fontSize, Mathf.Max(20f, fontSize * 0.58f), textColor, TextAlignmentOptions.Center);
            Stretch((RectTransform)labelText.transform, Vector2.zero, Vector2.one, new Vector2(42f, 18f), new Vector2(-42f, -18f));
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        return button;
    }

    private static Button CreateIconButton(string name, Transform parent, Sprite buttonSprite, string textIcon, Sprite iconSprite, TMP_FontAsset headlineFontAsset, TMP_FontAsset uiFontAsset, string label)
    {
        Button button = CreateButton(name, parent, buttonSprite, new Vector2(250f, 166f), string.Empty, headlineFontAsset, 1f, TextColor);
        LayoutElement layoutElement = button.GetComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1f;

        RectTransform buttonRect = (RectTransform)button.transform;
        if (iconSprite != null)
        {
            Image iconImage = CreateImage("Icon", buttonRect, iconSprite, Color.white, true);
            SetAnchored(iconImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(90f, 90f));
        }
        else
        {
            TMP_Text iconText = CreateText("IconText", buttonRect, textIcon, headlineFontAsset, 78f, 44f, TextColor, TextAlignmentOptions.Center);
            SetAnchored((RectTransform)iconText.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 24f), new Vector2(120f, 96f));
            iconText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        TMP_Text labelText = CreateText("Label", buttonRect, label, uiFontAsset, 25f, 18f, TextColor, TextAlignmentOptions.Center);
        SetAnchored((RectTransform)labelText.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(210f, 42f));
        labelText.textWrappingMode = TextWrappingModes.NoWrap;
        return button;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool preserveAspect)
    {
        GameObject gameObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gameObject.layer = parent.gameObject.layer;
        gameObject.transform.SetParent(parent, false);
        Image image = gameObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        image.preserveAspect = preserveAspect;
        return image;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Sprite sprite, Color color)
    {
        RectTransform rectTransform = CreateUIObject(name, parent);
        Image image = rectTransform.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = color;
        image.raycastTarget = true;
        return rectTransform;
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, TMP_FontAsset fontAsset, float fontSizeMax, float fontSizeMin, Color color, TextAlignmentOptions alignment)
    {
        fontAsset = ResolveFontForText(text, fontAsset);
        GameObject gameObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        gameObject.layer = parent.gameObject.layer;
        gameObject.transform.SetParent(parent, false);

        TextMeshProUGUI textComponent = gameObject.GetComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.font = fontAsset;
        textComponent.fontSize = fontSizeMax;
        textComponent.fontSizeMax = fontSizeMax;
        textComponent.fontSizeMin = fontSizeMin;
        textComponent.enableAutoSizing = true;
        textComponent.alignment = alignment;
        textComponent.color = color;
        textComponent.raycastTarget = false;
        textComponent.richText = true;
        textComponent.textWrappingMode = TextWrappingModes.Normal;
        return textComponent;
    }

    private static TMP_FontAsset ResolveFontForText(string text, TMP_FontAsset requestedFontAsset)
    {
        TMP_FontAsset resolvedFontAsset = requestedFontAsset ?? TMP_Settings.defaultFontAsset;
        if (ContainsJapanese(text) && s_JapaneseFontAsset != null)
        {
            resolvedFontAsset = s_JapaneseFontAsset;
        }

        return resolvedFontAsset;
    }

    private static bool ContainsJapanese(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        foreach (char character in text)
        {
            if ((character >= '\u3040' && character <= '\u30ff') ||
                (character >= '\u3400' && character <= '\u9fff') ||
                (character >= '\uf900' && character <= '\ufaff') ||
                (character >= '\uff00' && character <= '\uffef'))
            {
                return true;
            }
        }

        return false;
    }

    private static void EnsureFallbackFont(TMP_FontAsset fontAsset, TMP_FontAsset fallbackFontAsset)
    {
        if (fontAsset == null || fallbackFontAsset == null || fontAsset == fallbackFontAsset)
        {
            return;
        }

        if (fontAsset.fallbackFontAssetTable == null)
        {
            Debug.LogWarning($"[TitleSceneLayoutBuilder] Missing fallback table on font: {fontAsset.name}");
            return;
        }

        if (!fontAsset.fallbackFontAssetTable.Contains(fallbackFontAsset))
        {
            fontAsset.fallbackFontAssetTable.Add(fallbackFontAsset);
            EditorUtility.SetDirty(fontAsset);
        }
    }

    private static RectTransform CreateUIObject(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform), typeof(CanvasRenderer));
        gameObject.layer = parent.gameObject.layer;
        gameObject.transform.SetParent(parent, false);
        return (RectTransform)gameObject.transform;
    }

    private static void ApplyTitleControllerBindings(TitleController titleController, GameObject howToPanel, GameObject settingsPanel, HowToOverlayController howToOverlayController)
    {
        SerializedObject serializedTitle = new(titleController);
        SetObjectReference(serializedTitle, "_howToPanel", howToPanel);
        SetObjectReference(serializedTitle, "_settingsPanel", settingsPanel);
        SetObjectReference(serializedTitle, "_howToOverlayController", howToOverlayController);
        serializedTitle.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ApplyHowToBindings(HowToOverlayController howToOverlayController, GameObject howToPanel)
    {
        SerializedObject serializedHowTo = new(howToOverlayController);
        SetObjectReference(serializedHowTo, "_overlayPanel", howToPanel);
        serializedHowTo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ApplySettingsBindings(SettingsPanelController settingsPanelController, SettingsBindings bindings, Sprite toggleOnSprite, Sprite toggleOffSprite)
    {
        SerializedObject serializedSettings = new(settingsPanelController);
        SetRequiredObjectReference(serializedSettings, "_bgmToggle", bindings.BgmToggle);
        SetRequiredObjectReference(serializedSettings, "_seToggle", bindings.SeToggle);
        SetRequiredObjectReference(serializedSettings, "_vibrationToggle", bindings.VibrationToggle);
        SetRequiredObjectReference(serializedSettings, "_bgmVolumeSlider", bindings.BgmVolumeSlider);
        SetRequiredObjectReference(serializedSettings, "_seVolumeSlider", bindings.SeVolumeSlider);
        SetRequiredObjectReference(serializedSettings, "_bgmStateText", bindings.BgmStateText);
        SetRequiredObjectReference(serializedSettings, "_seStateText", bindings.SeStateText);
        SetRequiredObjectReference(serializedSettings, "_vibrationStateText", bindings.VibrationStateText);
        SetRequiredObjectReference(serializedSettings, "_bgmVolumeValueText", bindings.BgmVolumeValueText);
        SetRequiredObjectReference(serializedSettings, "_seVolumeValueText", bindings.SeVolumeValueText);
        SetRequiredObjectReference(serializedSettings, "_bgmToggleImage", bindings.BgmToggleImage);
        SetRequiredObjectReference(serializedSettings, "_seToggleImage", bindings.SeToggleImage);
        SetRequiredObjectReference(serializedSettings, "_vibrationToggleImage", bindings.VibrationToggleImage);
        SetRequiredObjectReference(serializedSettings, "_bgmAccentImage", bindings.BgmAccentImage);
        SetRequiredObjectReference(serializedSettings, "_seAccentImage", bindings.SeAccentImage);
        SetRequiredObjectReference(serializedSettings, "_vibrationAccentImage", bindings.VibrationAccentImage);
        SetObjectReference(serializedSettings, "_toggleOnSprite", toggleOnSprite);
        SetObjectReference(serializedSettings, "_toggleOffSprite", toggleOffSprite);
        serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settingsPanelController);
    }

    private static void AddButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i -= 1)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, i);
        }

        button.onClick.RemoveAllListeners();
        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }

    private static void ApplyButtonColors(Button button)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.96f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.9f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.55f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    private static void SetObjectArray(SerializedProperty property, Object[] values)
    {
        if (property == null)
        {
            Debug.LogWarning("[TitleSceneLayoutBuilder] Missing serialized array property.");
            return;
        }

        values ??= new Object[0];
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i += 1)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void SetObjectReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"[TitleSceneLayoutBuilder] Missing serialized property: {propertyName}");
            return;
        }

        property.objectReferenceValue = value;
    }

    private static void SetRequiredObjectReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new System.InvalidOperationException($"[TitleSceneLayoutBuilder] Missing required serialized property: {propertyName}");
        }

        if (value == null)
        {
            throw new System.InvalidOperationException($"[TitleSceneLayoutBuilder] Missing required binding value: {propertyName}");
        }

        property.objectReferenceValue = value;
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

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i -= 1)
        {
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }

    private static Sprite LoadSprite(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        string expectedName = $"{System.IO.Path.GetFileNameWithoutExtension(path)}_0";

        foreach (Object asset in assets)
        {
            if (asset is Sprite childSprite && childSprite.name == expectedName)
            {
                return childSprite;
            }
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            foreach (Object asset in assets)
            {
                if (asset is Sprite childSprite)
                {
                    sprite = childSprite;
                    break;
                }
            }
        }

        if (sprite == null)
        {
            Debug.LogWarning($"[TitleSceneLayoutBuilder] Missing sprite: {path}");
        }

        return sprite;
    }

    private static TMP_FontAsset LoadFont(string path)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (font == null)
        {
            Debug.LogWarning($"[TitleSceneLayoutBuilder] Missing font: {path}");
        }

        return font;
    }
}
