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
    private const string ScenePath = "Assets/Scenes/Result.unity";
    private const string TitleFontSourcePath = "Assets/Fonts/Y1BadBoySlab.otf";
    private const string TitleFontAssetPath = "Assets/Fonts/Y1BadBoySlab SDF.asset";
    private const string UiFontSourcePath = "Assets/Fonts/Y1YomiyasuWide-Bold.otf";
    private const string UiFontAssetPath = "Assets/Fonts/Y1YomiyasuWide-Bold SDF.asset";
    private const string YellowResultButtonPath = "Assets/Art/Sprites/Reslt/button_yellow_result.png";
    private const string SecondaryOutlineButtonPath = "Assets/Art/Sprites/Reslt/button_secondary_outline_strong.png";
    private const string StageChipBackgroundPath = "Assets/Art/Sprites/Reslt/stage_chip_bg.png";
    private const string CardBackgroundPath = "Assets/Art/Sprites/Reslt/card_bg_soft.png";
    private const string CardAccentLinePath = "Assets/Art/Sprites/Reslt/card_accent_line.png";
    private const string FilledStarPath = "Assets/Art/Sprites/Reslt/star_filled.png";
    private const string EmptyStarPath = "Assets/Art/Sprites/Reslt/star_empty.png";
    private const string StarGlowPath = "Assets/Art/Sprites/Reslt/star_glow_soft.png";
    private const string HeroClearGlowPath = "Assets/Art/Sprites/Reslt/hero_clear_glow.png";
    private const string RetryIconPath = "Assets/Art/Sprites/Reslt/icon_retry_rotate.png";
    private const string StageSelectIconPath = "Assets/Art/Sprites/Reslt/Stage_Select_Button_Icon.png";
    private const string MissHeartIconPath = "Assets/Art/Sprites/Reslt/icon_miss_heart.png";
    private const string NewBestBadgePath = "Assets/Art/Sprites/Reslt/badge_new_best_stamp.png";

    private static readonly Color CameraColor = new(0.945f, 0.962f, 0.99f, 1f);
    private static readonly Color ClearTintColor = new(0.70f, 0.90f, 0.78f, 0.22f);
    private static readonly Color GameOverTintColor = new(0.98f, 0.74f, 0.74f, 0.22f);
    private static readonly Color CardColor = new(1f, 1f, 1f, 0.96f);
    private static readonly Color CardShadowColor = new(0.08f, 0.15f, 0.24f, 0.10f);
    private static readonly Color TextColor = new(0.137f, 0.184f, 0.275f, 1f);
    private static readonly Color MutedTextColor = new(0.44f, 0.49f, 0.58f, 1f);
    private static readonly Color SuccessColor = new(0.345f, 0.784f, 0.541f, 1f);
    private static readonly Color FailureColor = new(0.914f, 0.408f, 0.416f, 1f);
    private static readonly Color AccentColor = new(0.949f, 0.772f, 0.259f, 1f);
    private static readonly Color NeutralButtonColor = new(1f, 1f, 1f, 1f);
    private static readonly Color OutlineColor = new(0.824f, 0.855f, 0.902f, 1f);
    private static readonly Color DividerColor = new(0.902f, 0.922f, 0.953f, 1f);
    private static readonly Color RowColor = new(0.975f, 0.982f, 0.992f, 1f);

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
        TMP_FontAsset titleFontAsset = EnsureFontAsset(TitleFontAssetPath, TitleFontSourcePath, 108, 8, 1024, 1024);
        TMP_FontAsset uiFontAsset = EnsureFontAsset(UiFontAssetPath, UiFontSourcePath, 86, 6, 1024, 1024);
        titleFontAsset ??= TMP_Settings.defaultFontAsset;
        uiFontAsset ??= titleFontAsset;

        Sprite slicedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Sprite cardBackgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardBackgroundPath);
        Sprite accentLineSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardAccentLinePath);
        Sprite stageChipSprite = AssetDatabase.LoadAssetAtPath<Sprite>(StageChipBackgroundPath);
        Sprite secondaryOutlineButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SecondaryOutlineButtonPath);
        Sprite yellowButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(YellowResultButtonPath);
        Sprite filledStarSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FilledStarPath);
        Sprite emptyStarSprite = AssetDatabase.LoadAssetAtPath<Sprite>(EmptyStarPath);
        Sprite starGlowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(StarGlowPath);
        Sprite heroClearGlowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HeroClearGlowPath);
        Sprite retryIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RetryIconPath);
        Sprite stageSelectIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(StageSelectIconPath);
        Sprite missHeartIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MissHeartIconPath);
        Sprite newBestBadgeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(NewBestBadgePath);

        RectTransform background = CreateUIObject("Background", canvas.transform);
        Stretch(background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

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
        Stretch(contentRoot, Vector2.zero, Vector2.one, new Vector2(0f, 320f), Vector2.zero);

        VerticalLayoutGroup contentLayout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(48, 48, 72, 0);
        contentLayout.spacing = 24;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        RectTransform heroCard = CreateCardShell("HeroCard", contentRoot, cardBackgroundSprite != null ? cardBackgroundSprite : slicedSprite, 360f);
        BuildHeaderCard(heroCard, titleFontAsset, uiFontAsset, stageChipSprite != null ? stageChipSprite : slicedSprite, accentLineSprite != null ? accentLineSprite : slicedSprite,
            out Image stageBadgeBackground,
            out TMP_Text stageText,
            out TMP_Text resultText,
            out TMP_Text subMessageText,
            out Image headerAccent);

        RectTransform scoreCard = CreateCardShell("ScoreCard", contentRoot, cardBackgroundSprite != null ? cardBackgroundSprite : slicedSprite, 390f);
        BuildScoreCard(scoreCard, titleFontAsset, uiFontAsset, accentLineSprite != null ? accentLineSprite : slicedSprite, emptyStarSprite, starGlowSprite,
            out Image scoreAccent,
            out TMP_Text scoreText,
            out TMP_Text bestScoreText,
            out GameObject starRowRoot,
            out Image[] starImages,
            out Image[] starGlowImages,
            out TMP_Text[] starLabels,
            out GameObject newBestBadge,
            newBestBadgeSprite);

        RectTransform detailCard = CreateCardShell("BreakdownCard", contentRoot, cardBackgroundSprite != null ? cardBackgroundSprite : slicedSprite, 500f);
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
            out Image sportsCarIcon);

        RectTransform actionDock = CreateUIObject("ActionDock", safeAreaRoot);
        actionDock.anchorMin = new Vector2(0f, 0f);
        actionDock.anchorMax = new Vector2(1f, 0f);
        actionDock.pivot = new Vector2(0.5f, 0f);
        actionDock.offsetMin = Vector2.zero;
        actionDock.offsetMax = new Vector2(0f, 300f);

        VerticalLayoutGroup actionLayout = actionDock.gameObject.AddComponent<VerticalLayoutGroup>();
        actionLayout.padding = new RectOffset(48, 48, 0, 48);
        actionLayout.spacing = 16;
        actionLayout.childAlignment = TextAnchor.UpperCenter;
        actionLayout.childControlWidth = true;
        actionLayout.childControlHeight = true;
        actionLayout.childForceExpandWidth = true;
        actionLayout.childForceExpandHeight = false;

        Button primaryActionButton = CreateActionButton(actionDock, "PrimaryButton", "Next Stage", yellowButtonSprite != null ? yellowButtonSprite : slicedSprite, Image.Type.Sliced, Color.white, new Color(0.45f, 0.24f, 0f, 1f), uiFontAsset, 124f, 42f, out TMP_Text primaryActionLabel, out Image primaryActionIcon);

        RectTransform secondaryRow = CreateUIObject("SecondaryRow", actionDock);
        secondaryRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
        HorizontalLayoutGroup secondaryLayout = secondaryRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        secondaryLayout.spacing = 16;
        secondaryLayout.childAlignment = TextAnchor.MiddleCenter;
        secondaryLayout.childControlWidth = true;
        secondaryLayout.childControlHeight = true;
        secondaryLayout.childForceExpandWidth = true;
        secondaryLayout.childForceExpandHeight = true;

        Button secondaryLeftButton = CreateActionButton(secondaryRow, "RetryButton", "Retry", secondaryOutlineButtonSprite != null ? secondaryOutlineButtonSprite : slicedSprite, Image.Type.Sliced, Color.white, TextColor, uiFontAsset, 96f, 38f, out TMP_Text secondaryLeftLabel, out Image secondaryLeftIcon);
        Button secondaryRightButton = CreateActionButton(secondaryRow, "StageSelectButton", "Stage Select", secondaryOutlineButtonSprite != null ? secondaryOutlineButtonSprite : slicedSprite, Image.Type.Sliced, Color.white, TextColor, uiFontAsset, 96f, 36f, out TMP_Text secondaryRightLabel, out Image secondaryRightIcon);

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
            starRowRoot,
            filledStarSprite,
            emptyStarSprite,
            yellowButtonSprite,
            secondaryOutlineButtonSprite,
            retryIconSprite,
            stageSelectIconSprite);
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
            heroGlowImage.color = new Color(1f, 1f, 1f, 0.58f);
            heroGlowImage.raycastTarget = false;
        }

        RectTransform glowTop = CreatePanel("GlowTop", clearRoot, slicedSprite, new Color(0.396f, 0.859f, 0.631f, 0.12f));
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

        RectTransform body = CreateUIObject("Body", parent);
        Stretch(body, Vector2.zero, Vector2.one, new Vector2(40f, 30f), new Vector2(-40f, -30f));
        VerticalLayoutGroup layout = body.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 20, 0);
        layout.spacing = 16;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RectTransform badgeRoot = CreateUIObject("StageBadge", body);
        badgeRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
        stageBadgeBackground = CreatePanel("BadgeBackground", badgeRoot, stageChipSprite, Color.white).GetComponent<Image>();
        stageBadgeBackground.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        stageBadgeBackground.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        stageBadgeBackground.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        stageBadgeBackground.rectTransform.sizeDelta = new Vector2(336f, 64f);
        stageBadgeBackground.type = Image.Type.Sliced;
        stageBadgeBackground.preserveAspect = false;

        stageText = CreateText("StageText", badgeRoot, "STAGE 08", uiFontAsset, 40f, TextColor, TextAlignmentOptions.Center);
        Stretch(stageText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        stageText.fontStyle = FontStyles.Bold;
        stageText.enableAutoSizing = true;
        stageText.fontSizeMin = 34f;
        stageText.fontSizeMax = 42f;

        resultText = CreateText("ResultTitle", body, "GAME CLEAR!", titleFontAsset, 92f, SuccessColor, TextAlignmentOptions.Center);
        resultText.gameObject.AddComponent<LayoutElement>().preferredHeight = 128f;
        resultText.enableAutoSizing = true;
        resultText.fontSizeMin = 76f;
        resultText.fontSizeMax = 100f;
        resultText.fontStyle = FontStyles.Normal;
        resultText.characterSpacing = 0.5f;

        subText = CreateText("SubMessage", body, "MISSION COMPLETE", uiFontAsset, 40f, MutedTextColor, TextAlignmentOptions.Center);
        subText.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
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

        RectTransform body = CreateUIObject("Body", parent);
        Stretch(body, Vector2.zero, Vector2.one, new Vector2(40f, 34f), new Vector2(-40f, -34f));

        VerticalLayoutGroup layout = body.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 14;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RectTransform headerRow = CreateUIObject("HeaderRow", body);
        headerRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
        HorizontalLayoutGroup headerLayout = headerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerLayout.childAlignment = TextAnchor.MiddleCenter;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = true;
        headerLayout.childForceExpandHeight = true;

        TMP_Text scoreLabel = CreateText("ScoreLabel", headerRow, "SCORE", uiFontAsset, 38f, MutedTextColor, TextAlignmentOptions.MidlineLeft);
        scoreLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        RectTransform badge = CreateUIObject("NewBestBadge", headerRow);
        LayoutElement badgeLayout = badge.gameObject.AddComponent<LayoutElement>();
        badgeLayout.preferredWidth = 188f;
        badgeLayout.preferredHeight = 58f;
        Image badgeImage = badge.gameObject.AddComponent<Image>();
        badgeImage.sprite = newBestBadgeSprite;
        badgeImage.type = Image.Type.Simple;
        badgeImage.preserveAspect = newBestBadgeSprite != null;
        badgeImage.color = Color.white;
        badgeImage.raycastTarget = false;
        badge.gameObject.SetActive(false);
        newBestBadge = badge.gameObject;

        RectTransform starRow = CreateUIObject("StarRow", body);
        starRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 78f;
        starRowRoot = starRow.gameObject;
        HorizontalLayoutGroup starLayout = starRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        starLayout.spacing = 18;
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

        scoreText = CreateText("TotalScoreValue", body, "12,450", displayFontAsset, 80f, TextColor, TextAlignmentOptions.Center);
        scoreText.gameObject.AddComponent<LayoutElement>().preferredHeight = 112f;
        scoreText.enableAutoSizing = true;
        scoreText.fontSizeMin = 78f;
        scoreText.fontSizeMax = 104f;
        scoreText.fontStyle = FontStyles.Normal;

        RectTransform bestRow = CreateUIObject("BestScoreRow", body);
        bestRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;
        HorizontalLayoutGroup bestLayout = bestRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        bestLayout.spacing = 8;
        bestLayout.childAlignment = TextAnchor.MiddleCenter;
        bestLayout.childControlWidth = false;
        bestLayout.childControlHeight = true;
        bestLayout.childForceExpandWidth = false;
        bestLayout.childForceExpandHeight = true;

        TMP_Text bestLabel = CreateText("BestScoreLabel", bestRow, "BEST", uiFontAsset, 36f, MutedTextColor, TextAlignmentOptions.MidlineLeft);
        bestLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 110f;
        bestScoreText = CreateText("BestScoreValue", bestRow, "11,920", uiFontAsset, 42f, TextColor, TextAlignmentOptions.MidlineRight);
        bestScoreText.gameObject.AddComponent<LayoutElement>().preferredWidth = 190f;
        bestScoreText.fontStyle = FontStyles.Bold;
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
        out Image sportsCarIcon)
    {
        detailAccent = CreatePanel("AccentBar", parent, accentLineSprite, SuccessColor).GetComponent<Image>();
        detailAccent.rectTransform.anchorMin = new Vector2(0f, 1f);
        detailAccent.rectTransform.anchorMax = new Vector2(1f, 1f);
        detailAccent.rectTransform.pivot = new Vector2(0.5f, 1f);
        detailAccent.rectTransform.sizeDelta = new Vector2(0f, 16f);
        detailAccent.type = Image.Type.Simple;

        RectTransform body = CreateUIObject("Body", parent);
        Stretch(body, Vector2.zero, Vector2.one, new Vector2(40f, 34f), new Vector2(-40f, -34f));
        VerticalLayoutGroup layout = body.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text detailLabel = CreateText("DetailsTitle", body, "CORRECT CARS", fontAsset, 38f, MutedTextColor, TextAlignmentOptions.Left);
        detailLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 46f;

        RectTransform statList = CreateUIObject("StatList", body);
        VerticalLayoutGroup statLayout = statList.gameObject.AddComponent<VerticalLayoutGroup>();
        statLayout.spacing = 14;
        statLayout.childAlignment = TextAnchor.UpperCenter;
        statLayout.childControlWidth = true;
        statLayout.childControlHeight = false;
        statLayout.childForceExpandWidth = true;
        statLayout.childForceExpandHeight = false;

        countAText = CreateBreakdownRow(statList, "Row_LightTruck", "Light Truck", null, fontAsset, "12", false, out _, out _, out lightTruckIcon);
        countBText = CreateBreakdownRow(statList, "Row_CompactCar", "Compact Car", null, fontAsset, "9", false, out _, out _, out compactCarIcon);
        countCText = CreateBreakdownRow(statList, "Row_SportsCar", "Sports Car", null, fontAsset, "7", false, out _, out _, out sportsCarIcon);

        RectTransform divider = CreatePanel("Divider", statList, accentLineSprite, DividerColor);
        divider.gameObject.AddComponent<LayoutElement>().preferredHeight = 4f;

        missCountText = CreateBreakdownRow(statList, "Row_Misses", "Mistakes", missIconSprite, fontAsset, "1", true, out missLabelText, out missRowBackground, out _);
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
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 82f;

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 0, 0);
        layout.spacing = 16;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        rowBackground = row.gameObject.AddComponent<Image>();
        rowBackground.color = RowColor;
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

        labelText = CreateText("Label", row, label, fontAsset, 34f, isMissRow ? FailureColor : TextColor, TextAlignmentOptions.MidlineLeft);
        labelText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        labelText.fontStyle = FontStyles.Bold;

        TMP_Text valueText = CreateText("Value", row, initialValue, fontAsset, 46f, isMissRow ? FailureColor : TextColor, TextAlignmentOptions.MidlineRight);
        valueText.gameObject.AddComponent<LayoutElement>().preferredWidth = 120f;
        valueText.fontStyle = FontStyles.Bold;
        return valueText;
    }

    private static void CreateStarToken(Transform parent, Sprite starSprite, Sprite glowSprite, TMP_FontAsset fontAsset, out Image background, out Image glowImage, out TMP_Text label)
    {
        RectTransform starRoot = CreateUIObject("Star", parent);
        LayoutElement layout = starRoot.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 80f;
        layout.preferredHeight = 80f;

        RectTransform glowRoot = CreateUIObject("Glow", starRoot);
        Stretch(glowRoot, Vector2.zero, Vector2.one, new Vector2(-8f, -8f), new Vector2(8f, 8f));
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

    private static Button CreateActionButton(
        Transform parent,
        string name,
        string label,
        Sprite buttonSprite,
        Image.Type imageType,
        Color buttonColor,
        Color textColor,
        TMP_FontAsset fontAsset,
        float preferredHeight,
        float fontSize,
        out TMP_Text labelText,
        out Image iconImage)
    {
        RectTransform buttonRect = CreateUIObject(name, parent);
        LayoutElement layout = buttonRect.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = preferredHeight;
        layout.flexibleWidth = 1f;

        Image image = buttonRect.gameObject.AddComponent<Image>();
        image.sprite = buttonSprite;
        image.type = imageType;
        image.color = buttonColor;
        image.preserveAspect = imageType == Image.Type.Simple;

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = image;

        RectTransform content = CreateUIObject("Content", buttonRect);
        Stretch(content, Vector2.zero, Vector2.one, new Vector2(20f, 10f), new Vector2(-20f, -10f));
        HorizontalLayoutGroup contentLayout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
        contentLayout.spacing = 10;
        contentLayout.childAlignment = TextAnchor.MiddleCenter;
        contentLayout.childControlWidth = false;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = false;
        contentLayout.childForceExpandHeight = false;
        contentLayout.padding = new RectOffset(12, 12, 0, 0);

        RectTransform iconRoot = CreateUIObject("Icon", content);
        LayoutElement iconLayout = iconRoot.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = preferredHeight >= 108f ? 40f : 34f;
        iconLayout.preferredHeight = preferredHeight >= 108f ? 40f : 34f;
        iconImage = iconRoot.gameObject.AddComponent<Image>();
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;
        iconImage.color = Color.white;
        iconImage.enabled = false;

        labelText = CreateText("Label", content, label, fontAsset, fontSize, textColor, TextAlignmentOptions.Center);
        LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
        labelLayout.flexibleWidth = 1f;
        labelText.enableAutoSizing = true;
        labelText.fontSizeMin = fontSize - 2f;
        labelText.fontSizeMax = fontSize + 4f;
        labelText.fontStyle = FontStyles.Bold;
        return button;
    }

    private static RectTransform CreateCardShell(string name, Transform parent, Sprite cardSprite, float preferredHeight)
    {
        RectTransform root = CreateUIObject(name, parent);
        root.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight + 10f;

        RectTransform shadow = CreatePanel("Shadow", root, cardSprite, CardShadowColor);
        Stretch(shadow, Vector2.zero, Vector2.one, new Vector2(8f, -10f), new Vector2(8f, -10f));
        shadow.GetComponent<Image>().raycastTarget = false;

        RectTransform card = CreatePanel("Card", root, cardSprite, cardSprite != null ? Color.white : CardColor);
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
        GameObject starRowRoot,
        Sprite filledStarSprite,
        Sprite emptyStarSprite,
        Sprite nextStageButtonSprite,
        Sprite secondaryButtonSprite,
        Sprite retryIconSprite,
        Sprite stageSelectIconSprite)
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
        SetObjectReference(serializedObject, "_starRowRoot", starRowRoot);
        SetObjectReference(serializedObject, "_filledStarSprite", filledStarSprite);
        SetObjectReference(serializedObject, "_emptyStarSprite", emptyStarSprite);
        SetObjectReference(serializedObject, "_nextStageButtonSprite", nextStageButtonSprite);
        SetObjectReference(serializedObject, "_retryButtonSprite", secondaryButtonSprite);
        SetObjectReference(serializedObject, "_stageSelectButtonSprite", secondaryButtonSprite);
        SetObjectReference(serializedObject, "_retryIconSprite", retryIconSprite);
        SetObjectReference(serializedObject, "_stageSelectIconSprite", stageSelectIconSprite);
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
