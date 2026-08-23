using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class ResultSceneLayoutBuilder
{
    private const float ResultStarSize = 100f;
    private const float ResultStarRowHeight = 112f;
    private const float BreakdownCardHeight = 500f;
    private const float BreakdownRowHeight = 82f;
    private const float MissRowHeight = 94f;
    private const string ScenePath = "Assets/Scenes/Result.unity";
    private const string FontSourcePath = "Assets/Fonts/DotGothic16-Regular.ttf";
    private const string FontAssetPath = "Assets/Fonts/DotGothic16-Regular SDF.asset";
    private const string RetryResultButtonPath = "Assets/Art/UI/Sprites/Result/Buttons/button_retry_normal.png";
    private const string TitleResultButtonPath = "Assets/Art/UI/Sprites/Result/Buttons/button_title_normal.png";
    private const string StageSelectResultButtonPath = "Assets/Art/UI/Sprites/Result/Buttons/button_stage_select_normal.png";
    private const string StageChipBackgroundPath = "Assets/Art/UI/Sprites/Result/Common/stage_chip_bg.png";
    private const string CardBackgroundPath = "Assets/Art/UI/Sprites/Result/Common/card_bg_soft.png";
    private const string CardAccentLinePath = "Assets/Art/UI/Sprites/Result/Common/card_accent_line.png";
    private const string FilledStarPath = "Assets/Art/UI/Sprites/Result/Stars/star_filled_1.png";
    private const string EmptyStarPath = "Assets/Art/UI/Sprites/Result/Stars/star_empty_1.png";
    private const string StarGlowPath = "Assets/Art/UI/Sprites/Result/Common/star_glow_soft.png";
    private const string HeroClearGlowPath = "Assets/Art/UI/Sprites/Result/Common/hero_clear_glow.png";
    private const string MissHeartIconPath = "Assets/Art/UI/Sprites/Result/Common/icon_miss_heart.png";
    private const string NewBestBadgePath = "Assets/Art/UI/Sprites/Result/Common/badge_new_best_stamp.png";
    private const string GameBackgroundPath = "Assets/Art/Sprites/Backgrounds/magic_shop_background.png";

    private static readonly Color CameraColor = new(0.945f, 0.962f, 0.99f, 1f);
    private static readonly Color ClearTintColor = new(0.078f, 0.055f, 0.11f, 0.50f);
    private static readonly Color GameOverTintColor = new(0.133f, 0.055f, 0.078f, 0.62f);
    private static readonly Color CardColor = new(1f, 0.992f, 0.965f, 1f);
    private static readonly Color CardShadowColor = new(0f, 0f, 0f, 0.40f);
    private static readonly Color TextColor = new(0.169f, 0.145f, 0.188f, 1f);
    private static readonly Color BadgeTextColor = new(0.137f, 0.184f, 0.275f, 1f);
    private static readonly Color MutedTextColor = new(0.169f, 0.145f, 0.188f, 0.50f);
    private static readonly Color SuccessColor = new(1f, 0.968f, 0.918f, 1f);
    private static readonly Color FailureColor = new(1f, 0.867f, 0.843f, 1f);
    private static readonly Color RowColor = new(1f, 1f, 1f, 0f);
    private static readonly Color MissRowColor = new(1f, 0.55f, 0.50f, 0.16f);

    [MenuItem("Tools/Scenes/Rebuild Result Scene UI")]
    public static void RebuildFromMenu() => BuildScene();

    public static void BuildFromBatchMode() => BuildScene();

    private static void BuildScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Camera camera = EnsureMainCamera(scene);
        EventSystem _ = EnsureEventSystem(scene);
        Canvas canvas = EnsureCanvas(scene);
        ResultController resultController = GetOrAddComponent<ResultController>(canvas.gameObject);

        ClearChildren(canvas.transform);
        BuildLayout(canvas, resultController);

        EditorUtility.SetDirty(camera);
        EditorUtility.SetDirty(canvas.gameObject);
        EditorUtility.SetDirty(resultController);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("Result scene UI rebuilt.");
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
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }
        else if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
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

    private static void BuildLayout(Canvas canvas, ResultController resultController)
    {
        TMP_FontAsset titleFontAsset = EnsureFontAsset(FontAssetPath, FontSourcePath, 108, 8, 1024, 1024);
        TMP_FontAsset uiFontAsset = titleFontAsset;
        titleFontAsset ??= TMP_Settings.defaultFontAsset;
        uiFontAsset ??= titleFontAsset;

        Sprite slicedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Sprite cardBackgroundSprite = LoadSpriteAtPath("Assets/Resources/UI/Tutorial/speech_panel.png") ?? slicedSprite;
        Sprite accentLineSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardAccentLinePath);
        Sprite stageChipSprite = AssetDatabase.LoadAssetAtPath<Sprite>(StageChipBackgroundPath);
        Sprite retryButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RetryResultButtonPath);
        Sprite titleButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TitleResultButtonPath);
        Sprite stageSelectButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(StageSelectResultButtonPath);
        Sprite filledStarSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FilledStarPath);
        Sprite emptyStarSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EmptyStarPath);
        Sprite starGlowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(StarGlowPath);
        Sprite heroClearGlowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HeroClearGlowPath);
        Sprite missHeartIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MissHeartIconPath);
        Sprite newBestBadgeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(NewBestBadgePath);
        Sprite gameBackgroundSprite = LoadSpriteAtPath(GameBackgroundPath);
        Sprite powaSprite = LoadSpriteAtPath("Assets/Art/Sprites/Characters/Powa/Powa_Idle.png");

        RectTransform background = CreateUIObject("Background", canvas.transform);
        Stretch(background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform gameBackground = CreatePanel("GameBackground", background, gameBackgroundSprite, Color.white);
        Stretch(gameBackground, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image gameBackgroundImage = gameBackground.GetComponent<Image>();
        gameBackgroundImage.type = Image.Type.Simple;
        gameBackgroundImage.preserveAspect = false;

        RectTransform stateTint = CreatePanel("StateTint", background, slicedSprite, ClearTintColor);
        Stretch(stateTint, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform fxBack = CreateUIObject("FX_Back", background);
        Stretch(fxBack, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        GameObject clearPanel = BuildClearFx(fxBack, slicedSprite, heroClearGlowSprite);
        GameObject gameOverPanel = BuildGameOverFx(fxBack, slicedSprite);
        gameOverPanel.SetActive(false);

        RectTransform safeAreaRoot = CreateUIObject("SafeAreaRoot", canvas.transform);
        Stretch(safeAreaRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        GetOrAddComponent<SafeAreaFitter>(safeAreaRoot.gameObject);

        RectTransform contentRoot = CreateUIObject("ContentRoot", safeAreaRoot);
        Stretch(contentRoot, Vector2.zero, Vector2.one, new Vector2(0f, 300f), Vector2.zero);

        VerticalLayoutGroup contentLayout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(70, 70, 63, 0);
        contentLayout.spacing = 40;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        RectTransform heroCard = CreateCardShell("HeroCard", contentRoot, cardBackgroundSprite != null ? cardBackgroundSprite : slicedSprite, 370f);
        BuildHeaderCard(heroCard, titleFontAsset, uiFontAsset, stageChipSprite != null ? stageChipSprite : slicedSprite, accentLineSprite != null ? accentLineSprite : slicedSprite,
            out Image stageBadgeBackground,
            out TMP_Text stageText,
            out TMP_Text resultText,
            out TMP_Text subMessageText,
            out Image headerAccent);
        heroCard.GetComponent<Image>().color = Color.clear;
        Image heroShadow = heroCard.parent.Find("Shadow")?.GetComponent<Image>();
        if (heroShadow != null)
        {
            heroShadow.color = Color.clear;
        }
        stageBadgeBackground.sprite = slicedSprite;
        stageBadgeBackground.color = new Color(0.36f, 0.20f, 0.10f, 1f);

        RectTransform powa = CreatePanel("Powa", safeAreaRoot, powaSprite, Color.white);
        powa.anchorMin = powa.anchorMax = new Vector2(1f, 1f);
        powa.pivot = new Vector2(1f, 1f);
        powa.anchoredPosition = new Vector2(-19f, -330f);
        powa.sizeDelta = new Vector2(230f, 230f);
        Image powaImage = powa.GetComponent<Image>();
        powaImage.type = Image.Type.Simple;
        powaImage.preserveAspect = true;

        RectTransform scoreCard = CreateCardShell("ScoreCard", contentRoot, cardBackgroundSprite != null ? cardBackgroundSprite : slicedSprite, 430f);
        BuildScoreCard(scoreCard, titleFontAsset, uiFontAsset, accentLineSprite != null ? accentLineSprite : slicedSprite, filledStarSprite, starGlowSprite,
            out Image scoreAccent,
            out TMP_Text scoreText,
            out TMP_Text bestScoreText,
            out GameObject starRowRoot,
            out Image[] starImages,
            out Image[] starGlowImages,
            out TMP_Text[] starLabels,
            out GameObject newBestBadge,
            newBestBadgeSprite);

        RectTransform detailCard = CreateCardShell("BreakdownCard", contentRoot, cardBackgroundSprite != null ? cardBackgroundSprite : slicedSprite, BreakdownCardHeight);
        BuildDetailCard(detailCard, uiFontAsset, accentLineSprite != null ? accentLineSprite : slicedSprite, missHeartIconSprite,
            out Image detailAccent,
            out TMP_Text countAText,
            out TMP_Text countBText,
            out TMP_Text countCText,
            out TMP_Text missLabelText,
            out TMP_Text missCountText,
            out Image missRowBackground,
            out Image lightTruckIcon,
            out Image compactCarIcon,
            out Image sportsCarIcon,
            out Image[] missOrbImages);
        detailCard.GetComponent<Image>().color = new Color(0.43f, 0.29f, 0.18f, 1f);
        Image detailShadow = detailCard.parent.Find("Shadow")?.GetComponent<Image>();
        if (detailShadow != null)
        {
            detailShadow.color = new Color(0f, 0f, 0f, 0.38f);
        }

        RectTransform actionDock = CreateUIObject("ActionDock", safeAreaRoot);
        actionDock.anchorMin = new Vector2(0f, 0f);
        actionDock.anchorMax = new Vector2(1f, 0f);
        actionDock.pivot = new Vector2(0.5f, 0f);
        actionDock.offsetMin = Vector2.zero;
        actionDock.offsetMax = new Vector2(0f, 430f);

        Button primaryActionButton = CreateImageActionButton(actionDock, "PrimaryButton", cardBackgroundSprite, new Vector2(940f, 230f));
        RectTransform primaryRect = primaryActionButton.transform as RectTransform;
        primaryRect.anchorMin = primaryRect.anchorMax = new Vector2(0.5f, 0f);
        primaryRect.pivot = new Vector2(0.5f, 0f);
        primaryRect.anchoredPosition = new Vector2(0f, 255f);
        primaryActionButton.GetComponent<Image>().color = new Color(1f, 0.796f, 0.224f, 1f);
        Shadow primaryShadow = primaryActionButton.gameObject.AddComponent<Shadow>();
        primaryShadow.effectColor = new Color(0.47f, 0.31f, 0.08f, 0.9f);
        primaryShadow.effectDistance = new Vector2(0f, -12f);

        RectTransform secondaryRow = CreateUIObject("SecondaryRow", actionDock);
        secondaryRow.anchorMin = secondaryRow.anchorMax = new Vector2(0.5f, 0f);
        secondaryRow.pivot = new Vector2(0.5f, 0f);
        secondaryRow.anchoredPosition = new Vector2(0f, 90f);
        secondaryRow.sizeDelta = new Vector2(940f, 110f);
        HorizontalLayoutGroup secondaryLayout = secondaryRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        secondaryLayout.spacing = 30f;
        secondaryLayout.childAlignment = TextAnchor.MiddleCenter;
        secondaryLayout.childControlWidth = false;
        secondaryLayout.childControlHeight = false;
        secondaryLayout.childForceExpandWidth = false;
        secondaryLayout.childForceExpandHeight = false;

        Button secondaryLeftButton = CreateImageActionButton(secondaryRow, "RetryButton", cardBackgroundSprite, new Vector2(455f, 160f));
        Button secondaryRightButton = CreateImageActionButton(secondaryRow, "StageSelectButton", cardBackgroundSprite, new Vector2(455f, 160f));
        secondaryLeftButton.GetComponent<Image>().color = CardColor;
        secondaryRightButton.GetComponent<Image>().color = CardColor;
        TMP_Text primaryActionLabel = CreateText("GeneratedLabel", primaryActionButton.transform, "つぎのステージへ\n<size=55%><color=#2B253070>NEXT STAGE</color></size>", titleFontAsset, 48f, BadgeTextColor, TextAlignmentOptions.Center);
        Stretch(primaryActionLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 10f), new Vector2(-18f, -10f));
        primaryActionLabel.textWrappingMode = TextWrappingModes.Normal;
        TMP_Text secondaryLeftLabel = CreateText("GeneratedLabel", secondaryLeftButton.transform, "もういちど", titleFontAsset, 38f, BadgeTextColor, TextAlignmentOptions.Center);
        Stretch(secondaryLeftLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 10f), new Vector2(-18f, -10f));
        TMP_Text secondaryRightLabel = CreateText("GeneratedLabel", secondaryRightButton.transform, "ステージ選択", titleFontAsset, 38f, BadgeTextColor, TextAlignmentOptions.Center);
        Stretch(secondaryRightLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 10f), new Vector2(-18f, -10f));
        Image primaryActionIcon = null;
        Image secondaryLeftIcon = null;
        Image secondaryRightIcon = null;

        RectTransform fxFront = CreateUIObject("FX_Front", canvas.transform);
        Stretch(fxFront, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        AssignResultController(
            resultController,
            scoreText,
            bestScoreText,
            stageText,
            resultText,
            subMessageText,
            countAText,
            countBText,
            countCText,
            missLabelText,
            missCountText,
            clearPanel,
            gameOverPanel,
            starImages,
            starGlowImages,
            starLabels,
            newBestBadge,
            primaryActionButton,
            secondaryLeftButton,
            secondaryRightButton,
            primaryActionLabel,
            secondaryLeftLabel,
            secondaryRightLabel,
            primaryActionIcon,
            secondaryLeftIcon,
            secondaryRightIcon,
            stateTint.GetComponent<Image>(),
            stageBadgeBackground,
            headerAccent,
            scoreAccent,
            detailAccent,
            missRowBackground,
            lightTruckIcon,
            compactCarIcon,
            sportsCarIcon,
            missOrbImages,
            starRowRoot,
            filledStarSprite,
            emptyStarSprite,
            cardBackgroundSprite,
            null,
            null,
            null);
    }

    private static GameObject BuildClearFx(Transform parent, Sprite slicedSprite, Sprite heroClearGlowSprite)
    {
        RectTransform clearRoot = CreateUIObject("ClearPanel", parent);
        Stretch(clearRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        if (heroClearGlowSprite != null)
        {
            RectTransform heroGlow = CreateUIObject("HeroGlow", clearRoot);
            heroGlow.anchorMin = new Vector2(0.08f, 0.72f);
            heroGlow.anchorMax = new Vector2(0.92f, 0.97f);
            heroGlow.offsetMin = Vector2.zero;
            heroGlow.offsetMax = Vector2.zero;
            Image heroGlowImage = heroGlow.gameObject.AddComponent<Image>();
            heroGlowImage.sprite = heroClearGlowSprite;
            heroGlowImage.type = Image.Type.Simple;
            heroGlowImage.preserveAspect = false;
            heroGlowImage.color = Color.clear;
            heroGlowImage.raycastTarget = false;
        }

        RectTransform glowTop = CreatePanel("GlowTop", clearRoot, slicedSprite, Color.clear);
        glowTop.anchorMin = new Vector2(0.18f, 0.7f);
        glowTop.anchorMax = new Vector2(0.82f, 0.96f);
        glowTop.offsetMin = Vector2.zero;
        glowTop.offsetMax = Vector2.zero;
        return clearRoot.gameObject;
    }

    private static GameObject BuildGameOverFx(Transform parent, Sprite slicedSprite)
    {
        RectTransform failureRoot = CreateUIObject("GameOverPanel", parent);
        Stretch(failureRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform vignetteTop = CreatePanel("VignetteTop", failureRoot, slicedSprite, new Color(0.965f, 0.529f, 0.545f, 0.16f));
        vignetteTop.anchorMin = new Vector2(0f, 0.72f);
        vignetteTop.anchorMax = new Vector2(1f, 1f);
        vignetteTop.offsetMin = Vector2.zero;
        vignetteTop.offsetMax = Vector2.zero;

        RectTransform vignetteBottom = CreatePanel("VignetteBottom", failureRoot, slicedSprite, new Color(0.949f, 0.58f, 0.58f, 0.08f));
        vignetteBottom.anchorMin = new Vector2(0f, 0f);
        vignetteBottom.anchorMax = new Vector2(1f, 0.34f);
        vignetteBottom.offsetMin = Vector2.zero;
        vignetteBottom.offsetMax = Vector2.zero;
        return failureRoot.gameObject;
    }

    private static void BuildHeaderCard(
        RectTransform parent,
        TMP_FontAsset titleFontAsset,
        TMP_FontAsset uiFontAsset,
        Sprite stageChipSprite,
        Sprite accentLineSprite,
        out Image stageBadgeBackground,
        out TMP_Text stageText,
        out TMP_Text resultText,
        out TMP_Text subText,
        out Image headerAccent)
    {
        headerAccent = CreatePanel("AccentBar", parent, accentLineSprite, SuccessColor).GetComponent<Image>();
        headerAccent.rectTransform.anchorMin = new Vector2(0f, 1f);
        headerAccent.rectTransform.anchorMax = new Vector2(1f, 1f);
        headerAccent.rectTransform.pivot = new Vector2(0.5f, 1f);
        headerAccent.rectTransform.sizeDelta = new Vector2(0f, 16f);
        headerAccent.rectTransform.anchoredPosition = Vector2.zero;
        headerAccent.type = Image.Type.Simple;
        headerAccent.gameObject.SetActive(false);

        RectTransform body = CreateUIObject("Body", parent);
        Stretch(body, Vector2.zero, Vector2.one, new Vector2(40f, 0f), new Vector2(-40f, 0f));
        VerticalLayoutGroup layout = body.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 12;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RectTransform badgeRoot = CreateUIObject("StageBadge", body);
        badgeRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
        stageBadgeBackground = CreatePanel("BadgeBackground", badgeRoot, stageChipSprite, new Color(0.36f, 0.20f, 0.10f, 1f)).GetComponent<Image>();
        stageBadgeBackground.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        stageBadgeBackground.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        stageBadgeBackground.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        stageBadgeBackground.rectTransform.sizeDelta = new Vector2(336f, 64f);
        stageBadgeBackground.type = Image.Type.Sliced;
        stageBadgeBackground.preserveAspect = false;

        stageText = CreateText("StageText", badgeRoot, "ステージ 8", uiFontAsset, 40f, new Color(1f, 0.925f, 0.788f, 1f), TextAlignmentOptions.Center);
        Stretch(stageText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        stageText.fontStyle = FontStyles.Bold;
        stageText.enableAutoSizing = true;
        stageText.fontSizeMin = 34f;
        stageText.fontSizeMax = 42f;

        resultText = CreateText("ResultTitle", body, "クリア!", titleFontAsset, 108f, SuccessColor, TextAlignmentOptions.Center);
        resultText.gameObject.AddComponent<LayoutElement>().preferredHeight = 132f;
        resultText.enableAutoSizing = true;
        resultText.fontSizeMin = 68f;
        resultText.fontSizeMax = 110f;
        resultText.fontStyle = FontStyles.Normal;
        resultText.characterSpacing = 0.5f;

        subText = CreateText("SubMessage", body, "STAGE CLEAR", uiFontAsset, 35f, new Color(1f, 0.925f, 0.788f, 0.72f), TextAlignmentOptions.Center);
        subText.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
        subText.enableAutoSizing = true;
        subText.fontSizeMin = 34f;
        subText.fontSizeMax = 42f;
    }

    private static void BuildScoreCard(
        RectTransform parent,
        TMP_FontAsset displayFontAsset,
        TMP_FontAsset uiFontAsset,
        Sprite accentLineSprite,
        Sprite emptyStarSprite,
        Sprite starGlowSprite,
        out Image scoreAccent,
        out TMP_Text scoreText,
        out TMP_Text bestScoreText,
        out GameObject starRowRoot,
        out Image[] starImages,
        out Image[] starGlowImages,
        out TMP_Text[] starLabels,
        out GameObject newBestBadge,
        Sprite newBestBadgeSprite)
    {
        scoreAccent = CreatePanel("AccentBar", parent, accentLineSprite, SuccessColor).GetComponent<Image>();
        scoreAccent.rectTransform.anchorMin = new Vector2(0f, 1f);
        scoreAccent.rectTransform.anchorMax = new Vector2(1f, 1f);
        scoreAccent.rectTransform.pivot = new Vector2(0.5f, 1f);
        scoreAccent.rectTransform.sizeDelta = new Vector2(0f, 16f);
        scoreAccent.type = Image.Type.Simple;
        scoreAccent.gameObject.SetActive(false);

        RectTransform body = CreateUIObject("Body", parent);
        Stretch(body, Vector2.zero, Vector2.one, new Vector2(40f, 34f), new Vector2(-40f, -34f));

        VerticalLayoutGroup layout = body.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RectTransform headerRow = CreateUIObject("HeaderRow", body);
        headerRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
        HorizontalLayoutGroup headerLayout = headerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerLayout.childAlignment = TextAnchor.MiddleCenter;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = true;
        headerLayout.childForceExpandHeight = true;

        TMP_Text scoreLabel = CreateText("ScoreLabel", headerRow, string.Empty, uiFontAsset, 1f, Color.clear, TextAlignmentOptions.MidlineLeft);
        scoreLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        RectTransform badge = CreateUIObject("NewBestBadge", headerRow);
        LayoutElement badgeLayout = badge.gameObject.AddComponent<LayoutElement>();
        badgeLayout.preferredWidth = 120f;
        badgeLayout.preferredHeight = 36f;
        Image badgeImage = badge.gameObject.AddComponent<Image>();
        badgeImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        badgeImage.type = Image.Type.Sliced;
        badgeImage.preserveAspect = false;
        badgeImage.color = new Color(0.94f, 0.29f, 0.34f, 1f);
        badgeImage.raycastTarget = false;
        TMP_Text badgeText = CreateText("Label", badge, "記録更新!", uiFontAsset, 20f, Color.white, TextAlignmentOptions.Center);
        Stretch(badgeText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        badge.gameObject.SetActive(false);
        newBestBadge = badge.gameObject;

        RectTransform starRow = CreateUIObject("StarRow", body);
        starRow.gameObject.AddComponent<LayoutElement>().preferredHeight = ResultStarRowHeight;
        starRowRoot = starRow.gameObject;
        HorizontalLayoutGroup starLayout = starRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        starLayout.spacing = 22;
        starLayout.childAlignment = TextAnchor.MiddleCenter;
        starLayout.childControlWidth = false;
        starLayout.childControlHeight = false;
        starLayout.childForceExpandWidth = false;
        starLayout.childForceExpandHeight = false;

        starImages = new Image[3];
        starGlowImages = new Image[3];
        starLabels = new TMP_Text[3];
        for (int i = 0; i < 3; i += 1)
        {
            CreateStarToken(starRow, emptyStarSprite != null ? emptyStarSprite : accentLineSprite, starGlowSprite, uiFontAsset, out starImages[i], out starGlowImages[i], out starLabels[i]);
        }

        scoreText = CreateText("TotalScoreValue", body, "12,450", displayFontAsset, 83f, TextColor, TextAlignmentOptions.Center);
        scoreText.gameObject.AddComponent<LayoutElement>().preferredHeight = 100f;
        scoreText.enableAutoSizing = true;
        scoreText.fontSizeMin = 68f;
        scoreText.fontSizeMax = 92f;
        scoreText.fontStyle = FontStyles.Normal;

        RectTransform bestRow = CreateUIObject("BestScoreRow", body);
        bestRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
        HorizontalLayoutGroup bestLayout = bestRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        bestLayout.spacing = 14;
        bestLayout.childAlignment = TextAnchor.MiddleCenter;
        bestLayout.childControlWidth = false;
        bestLayout.childControlHeight = false;
        bestLayout.childForceExpandWidth = false;
        bestLayout.childForceExpandHeight = false;

        TMP_Text bestLabel = CreateText("BestScoreLabel", bestRow, "ベスト", uiFontAsset, 35f, MutedTextColor, TextAlignmentOptions.MidlineLeft);
        bestLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 118f;
        bestLabel.enableAutoSizing = true;
        bestLabel.fontSizeMin = 28f;
        bestLabel.fontSizeMax = 36f;
        bestScoreText = CreateText("BestScoreValue", bestRow, "11,920", uiFontAsset, 34f, TextColor, TextAlignmentOptions.MidlineRight);
        bestScoreText.gameObject.AddComponent<LayoutElement>().preferredWidth = 290f;
        bestScoreText.enableAutoSizing = true;
        bestScoreText.fontSizeMin = 26f;
        bestScoreText.fontSizeMax = 36f;
        bestScoreText.fontStyle = FontStyles.Normal;
        badge.transform.SetParent(bestRow, false);
        badge.transform.SetAsLastSibling();
        (badge.transform as RectTransform).sizeDelta = new Vector2(120f, 36f);
        headerRow.gameObject.SetActive(false);
    }

    private static void BuildDetailCard(
        RectTransform parent,
        TMP_FontAsset fontAsset,
        Sprite accentLineSprite,
        Sprite missIconSprite,
        out Image detailAccent,
        out TMP_Text countAText,
        out TMP_Text countBText,
        out TMP_Text countCText,
        out TMP_Text missLabelText,
        out TMP_Text missCountText,
        out Image missRowBackground,
        out Image lightTruckIcon,
        out Image compactCarIcon,
        out Image sportsCarIcon,
        out Image[] missOrbImages)
    {
        detailAccent = CreatePanel("AccentBar", parent, accentLineSprite, SuccessColor).GetComponent<Image>();
        detailAccent.rectTransform.anchorMin = new Vector2(0f, 1f);
        detailAccent.rectTransform.anchorMax = new Vector2(1f, 1f);
        detailAccent.rectTransform.pivot = new Vector2(0.5f, 1f);
        detailAccent.rectTransform.sizeDelta = new Vector2(0f, 16f);
        detailAccent.type = Image.Type.Simple;
        detailAccent.gameObject.SetActive(false);

        RectTransform body = CreateUIObject("Body", parent);
        Stretch(body, Vector2.zero, Vector2.one, new Vector2(40f, 34f), new Vector2(-40f, -34f));
        VerticalLayoutGroup layout = body.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 0;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text detailLabel = CreateText("DetailsTitle", body, string.Empty, fontAsset, 1f, Color.clear, TextAlignmentOptions.Left);
        detailLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 0f;

        RectTransform statList = CreateUIObject("StatList", body);
        VerticalLayoutGroup statLayout = statList.gameObject.AddComponent<VerticalLayoutGroup>();
        statLayout.spacing = 0;
        statLayout.childAlignment = TextAnchor.UpperCenter;
        statLayout.childControlWidth = true;
        statLayout.childControlHeight = false;
        statLayout.childForceExpandWidth = true;
        statLayout.childForceExpandHeight = false;

        countAText = CreateBreakdownRow(statList, "Row_LightTruck", "トラック", null, fontAsset, "12", false, out _, out _, out lightTruckIcon);
        countBText = CreateBreakdownRow(statList, "Row_CompactCar", "普通車", null, fontAsset, "9", false, out _, out _, out compactCarIcon);
        countCText = CreateBreakdownRow(statList, "Row_SportsCar", "スポーツカー", null, fontAsset, "7", false, out _, out _, out sportsCarIcon);

        missCountText = CreateBreakdownRow(statList, "Row_Misses", "ミス", null, fontAsset, "1", true, out missLabelText, out missRowBackground, out _);
        Transform missRow = missCountText.transform.parent;
        Transform missIcon = missRow.Find("Icon");
        if (missIcon != null)
        {
            missIcon.gameObject.SetActive(false);
        }

        RectTransform orbRow = CreateUIObject("MissOrbs", missRow);
        orbRow.SetSiblingIndex(Mathf.Max(1, missCountText.transform.GetSiblingIndex()));
        LayoutElement orbRowLayout = orbRow.gameObject.AddComponent<LayoutElement>();
        orbRowLayout.preferredWidth = 180f;
        orbRowLayout.preferredHeight = 48f;
        HorizontalLayoutGroup orbLayout = orbRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        orbLayout.spacing = 12f;
        orbLayout.childAlignment = TextAnchor.MiddleCenter;
        orbLayout.childControlWidth = false;
        orbLayout.childControlHeight = false;
        orbLayout.childForceExpandWidth = false;
        orbLayout.childForceExpandHeight = false;
        Sprite filledOrbSprite = LoadSpriteAtPath("Assets/Resources/UI/Tutorial/miss_orb_lit.png");
        Sprite emptyOrbSprite = LoadSpriteAtPath("Assets/Resources/UI/Tutorial/miss_orb_empty.png");
        missOrbImages = new Image[3];
        for (int i = 0; i < missOrbImages.Length; i += 1)
        {
            RectTransform orb = CreatePanel($"Orb{i + 1}", orbRow, i == 0 ? filledOrbSprite : emptyOrbSprite, Color.white);
            orb.sizeDelta = new Vector2(46f, 46f);
            orb.GetComponent<Image>().type = Image.Type.Simple;
            orb.GetComponent<Image>().preserveAspect = true;
            missOrbImages[i] = orb.GetComponent<Image>();
        }
    }

    private static TMP_Text CreateBreakdownRow(
        Transform parent,
        string name,
        string label,
        Sprite iconSprite,
        TMP_FontAsset fontAsset,
        string initialValue,
        bool isMissRow = false)
    {
        return CreateBreakdownRow(parent, name, label, iconSprite, fontAsset, initialValue, isMissRow, out _, out _, out _);
    }

    private static TMP_Text CreateBreakdownRow(
        Transform parent,
        string name,
        string label,
        Sprite iconSprite,
        TMP_FontAsset fontAsset,
        string initialValue,
        bool isMissRow,
        out TMP_Text labelText,
        out Image rowBackground,
        out Image iconImage)
    {
        RectTransform row = CreateUIObject(name, parent);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = isMissRow ? MissRowHeight : BreakdownRowHeight;

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 0, 0);
        layout.spacing = 16;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        rowBackground = row.gameObject.AddComponent<Image>();
        rowBackground.color = isMissRow ? MissRowColor : RowColor;
        rowBackground.raycastTarget = false;

        RectTransform iconRoot = CreateUIObject("Icon", row);
        LayoutElement iconLayout = iconRoot.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 56f;
        iconLayout.preferredHeight = 56f;

        iconImage = iconRoot.gameObject.AddComponent<Image>();
        iconImage.sprite = iconSprite;
        iconImage.color = Color.white;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        iconImage.enabled = iconSprite != null;

        if (iconSprite == null && isMissRow)
        {
            TMP_Text iconText = CreateText("IconText", iconRoot, "!", fontAsset, 32f, FailureColor, TextAlignmentOptions.Center);
            Stretch(iconText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            iconText.fontStyle = FontStyles.Bold;
        }

        labelText = CreateText("Label", row, label, fontAsset, isMissRow ? 38f : 34f, isMissRow ? FailureColor : new Color(1f, 0.968f, 0.918f, 1f), TextAlignmentOptions.MidlineLeft);
        labelText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        labelText.fontStyle = FontStyles.Bold;

        TMP_Text valueText = CreateText("Value", row, initialValue, fontAsset, isMissRow ? 46f : 42f, isMissRow ? FailureColor : new Color(1f, 0.968f, 0.918f, 1f), TextAlignmentOptions.MidlineRight);
        valueText.gameObject.AddComponent<LayoutElement>().preferredWidth = 120f;
        valueText.fontStyle = FontStyles.Bold;
        return valueText;
    }

    private static void CreateStarToken(Transform parent, Sprite starSprite, Sprite glowSprite, TMP_FontAsset fontAsset, out Image background, out Image glowImage, out TMP_Text label)
    {
        RectTransform starRoot = CreateUIObject("Star", parent);
        LayoutElement layout = starRoot.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = ResultStarSize;
        layout.preferredHeight = ResultStarSize;

        RectTransform glowRoot = CreateUIObject("Glow", starRoot);
        Stretch(glowRoot, Vector2.zero, Vector2.one, new Vector2(-10f, -10f), new Vector2(10f, 10f));
        glowImage = glowRoot.gameObject.AddComponent<Image>();
        glowImage.sprite = glowSprite;
        glowImage.type = Image.Type.Simple;
        glowImage.preserveAspect = true;
        glowImage.color = new Color(1f, 1f, 1f, 0f);
        glowImage.raycastTarget = false;

        background = starRoot.gameObject.AddComponent<Image>();
        background.sprite = starSprite;
        background.type = Image.Type.Simple;
        background.preserveAspect = true;
        background.color = Color.white;
        background.raycastTarget = false;

        label = CreateText("Label", starRoot, string.Empty, fontAsset, 1f, new Color(1f, 1f, 1f, 0f), TextAlignmentOptions.Center);
        Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private static Button CreateImageActionButton(
        Transform parent,
        string name,
        Sprite buttonSprite,
        Vector2 fallbackSize)
    {
        RectTransform buttonRect = CreateUIObject(name, parent);
        Vector2 size = fallbackSize;
        buttonRect.sizeDelta = size;

        LayoutElement layout = buttonRect.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;
        layout.minWidth = size.x;
        layout.minHeight = size.y;

        Image image = buttonRect.gameObject.AddComponent<Image>();
        image.sprite = buttonSprite;
        image.type = buttonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = Color.white;
        image.preserveAspect = false;

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = image;
        return button;
    }

    private static Vector2 GetSpriteSize(Sprite sprite, Vector2 fallbackSize)
    {
        if (sprite == null || sprite.rect.width <= 0f || sprite.rect.height <= 0f)
        {
            return fallbackSize;
        }

        return new Vector2(sprite.rect.width, sprite.rect.height);
    }

    private static RectTransform CreateCardShell(string name, Transform parent, Sprite cardSprite, float preferredHeight)
    {
        RectTransform root = CreateUIObject(name, parent);
        root.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight + 10f;

        RectTransform shadow = CreatePanel("Shadow", root, cardSprite, CardShadowColor);
        Stretch(shadow, Vector2.zero, Vector2.one, new Vector2(8f, -10f), new Vector2(8f, -10f));
        shadow.GetComponent<Image>().raycastTarget = false;

        RectTransform card = CreatePanel("Card", root, cardSprite, CardColor);
        Stretch(card, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 10f));
        return card;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Sprite slicedSprite, Color color)
    {
        RectTransform rectTransform = CreateUIObject(name, parent);
        Image image = rectTransform.gameObject.AddComponent<Image>();
        image.sprite = slicedSprite;
        image.type = slicedSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        return rectTransform;
    }

    private static Sprite LoadSpriteAtPath(string spritePath)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite != null)
        {
            return sprite;
        }

        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(spritePath))
        {
            if (asset is Sprite nestedSprite)
            {
                return nestedSprite;
            }
        }

        return null;
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, TMP_FontAsset fontAsset, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        fontAsset ??= TMP_Settings.defaultFontAsset;
        RectTransform rectTransform = CreateUIObject(name, parent);
        TextMeshProUGUI textComponent = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        textComponent.font = fontAsset;
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.color = color;
        textComponent.alignment = alignment;
        textComponent.textWrappingMode = TextWrappingModes.NoWrap;
        textComponent.raycastTarget = false;
        return textComponent;
    }

    private static TMP_FontAsset EnsureFontAsset(string assetPath, string sourceFontPath, int samplingPointSize, int atlasPadding, int atlasWidth, int atlasHeight)
    {
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (fontAsset != null)
        {
            return fontAsset;
        }

        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourceFontPath);
        if (sourceFont == null)
        {
            return null;
        }

        fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            samplingPointSize,
            atlasPadding,
            GlyphRenderMode.SDFAA,
            atlasWidth,
            atlasHeight,
            AtlasPopulationMode.Dynamic,
            true);

        if (fontAsset == null)
        {
            return null;
        }

        fontAsset.name = Path.GetFileNameWithoutExtension(assetPath);
        AssetDatabase.CreateAsset(fontAsset, assetPath);

        if (fontAsset.material != null)
        {
            fontAsset.material.name = fontAsset.name + " Material";
            if (!AssetDatabase.Contains(fontAsset.material))
            {
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }
        }

        if (fontAsset.atlasTextures != null)
        {
            for (int i = 0; i < fontAsset.atlasTextures.Length; i += 1)
            {
                if (fontAsset.atlasTextures[i] == null)
                {
                    continue;
                }

                fontAsset.atlasTextures[i].name = $"{fontAsset.name} Atlas {i}";
                if (!AssetDatabase.Contains(fontAsset.atlasTextures[i]))
                {
                    AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[i], fontAsset);
                }
            }
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.ImportAsset(assetPath);
        AssetDatabase.SaveAssets();
        return fontAsset;
    }

    private static RectTransform CreateUIObject(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        gameObject.layer = LayerMask.NameToLayer("UI");
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        return rectTransform;
    }

    private static void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }

    private static void AssignResultController(
        ResultController resultController,
        TMP_Text scoreText,
        TMP_Text bestScoreText,
        TMP_Text stageText,
        TMP_Text resultText,
        TMP_Text subMessageText,
        TMP_Text countAText,
        TMP_Text countBText,
        TMP_Text countCText,
        TMP_Text missLabelText,
        TMP_Text missCountText,
        GameObject clearPanel,
        GameObject gameOverPanel,
        Image[] starImages,
        Image[] starGlowImages,
        TMP_Text[] starLabels,
        GameObject newBestBadge,
        Button primaryActionButton,
        Button secondaryLeftButton,
        Button secondaryRightButton,
        TMP_Text primaryActionLabel,
        TMP_Text secondaryLeftLabel,
        TMP_Text secondaryRightLabel,
        Image primaryActionIcon,
        Image secondaryLeftIcon,
        Image secondaryRightIcon,
        Image stateTintImage,
        Image stageBadgeBackground,
        Image headerAccentImage,
        Image scoreAccentImage,
        Image detailAccentImage,
        Image missRowBackground,
        Image lightTruckIcon,
        Image compactCarIcon,
        Image sportsCarIcon,
        Image[] missOrbImages,
        GameObject starRowRoot,
        Sprite filledStarSprite,
        Sprite emptyStarSprite,
        Sprite buttonBackgroundSprite,
        Sprite retryButtonSprite,
        Sprite titleButtonSprite,
        Sprite stageSelectButtonSprite)
    {
        SerializedObject serializedObject = new(resultController);
        SetObjectReference(serializedObject, "_scoreText", scoreText);
        SetObjectReference(serializedObject, "_bestScoreText", bestScoreText);
        SetObjectReference(serializedObject, "_stageText", stageText);
        SetObjectReference(serializedObject, "_resultText", resultText);
        SetObjectReference(serializedObject, "_subMessageText", subMessageText);
        SetObjectReference(serializedObject, "_countAText", countAText);
        SetObjectReference(serializedObject, "_countBText", countBText);
        SetObjectReference(serializedObject, "_countCText", countCText);
        SetObjectReference(serializedObject, "_missLabelText", missLabelText);
        SetObjectReference(serializedObject, "_missCountText", missCountText);
        SetObjectReference(serializedObject, "_clearPanel", clearPanel);
        SetObjectReference(serializedObject, "_gameOverPanel", gameOverPanel);
        SetObjectReference(serializedObject, "_newBestBadge", newBestBadge);
        SetObjectReference(serializedObject, "_primaryActionButton", primaryActionButton);
        SetObjectReference(serializedObject, "_secondaryLeftButton", secondaryLeftButton);
        SetObjectReference(serializedObject, "_secondaryRightButton", secondaryRightButton);
        SetObjectReference(serializedObject, "_primaryActionLabel", primaryActionLabel);
        SetObjectReference(serializedObject, "_secondaryLeftLabel", secondaryLeftLabel);
        SetObjectReference(serializedObject, "_secondaryRightLabel", secondaryRightLabel);
        SetObjectReference(serializedObject, "_primaryActionIcon", primaryActionIcon);
        SetObjectReference(serializedObject, "_secondaryLeftActionIcon", secondaryLeftIcon);
        SetObjectReference(serializedObject, "_secondaryRightActionIcon", secondaryRightIcon);
        SetObjectReference(serializedObject, "_stateTintImage", stateTintImage);
        SetObjectReference(serializedObject, "_stageBadgeBackground", stageBadgeBackground);
        SetObjectReference(serializedObject, "_headerAccentImage", headerAccentImage);
        SetObjectReference(serializedObject, "_scoreAccentImage", scoreAccentImage);
        SetObjectReference(serializedObject, "_detailAccentImage", detailAccentImage);
        SetObjectReference(serializedObject, "_missRowBackground", missRowBackground);
        SetObjectReference(serializedObject, "_lightTruckIcon", lightTruckIcon);
        SetObjectReference(serializedObject, "_compactCarIcon", compactCarIcon);
        SetObjectReference(serializedObject, "_sportsCarIcon", sportsCarIcon);
        SetObjectArray(serializedObject, "_missOrbImages", missOrbImages);
        SetObjectReference(serializedObject, "_filledMissOrbSprite", LoadSpriteAtPath("Assets/Resources/UI/Tutorial/miss_orb_lit.png"));
        SetObjectReference(serializedObject, "_emptyMissOrbSprite", LoadSpriteAtPath("Assets/Resources/UI/Tutorial/miss_orb_empty.png"));
        SetObjectReference(serializedObject, "_starRowRoot", starRowRoot);
        SetObjectReference(serializedObject, "_filledStarSprite", filledStarSprite);
        SetObjectReference(serializedObject, "_emptyStarSprite", emptyStarSprite);
        SetObjectReference(serializedObject, "_buttonBackgroundSprite", buttonBackgroundSprite);
        SetObjectReference(serializedObject, "_retryButtonSprite", retryButtonSprite);
        SetObjectReference(serializedObject, "_titleButtonSprite", titleButtonSprite);
        SetObjectReference(serializedObject, "_nextStageButtonSprite", null);
        SetObjectReference(serializedObject, "_stageSelectButtonSprite", stageSelectButtonSprite);
        SetObjectReference(serializedObject, "_retryIconSprite", null);
        SetObjectReference(serializedObject, "_stageSelectIconSprite", null);
        SetObjectReference(serializedObject, "_playerAnimationController", null);
        SetObjectArray(serializedObject, "_starImages", starImages);
        SetObjectArray(serializedObject, "_starGlowImages", starGlowImages);
        SetObjectArray(serializedObject, "_starLabels", starLabels);

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjectReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"[ResultSceneLayoutBuilder] Missing serialized property: {propertyName}");
            return;
        }

        property.objectReferenceValue = value;
    }

    private static void SetObjectArray(SerializedObject serializedObject, string propertyName, Object[] values)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"[ResultSceneLayoutBuilder] Missing serialized array property: {propertyName}");
            return;
        }

        values ??= new Object[0];
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i += 1)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i -= 1)
        {
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }
}
