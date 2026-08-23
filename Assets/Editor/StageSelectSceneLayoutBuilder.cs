using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class StageSelectSceneLayoutBuilder
{
    private const string ScenePath = "Assets/Scenes/StageSelect.unity";
    private const string PrefabPath = "Assets/Prefabs/StageCard.prefab";
    private const string BackgroundPath = "Assets/Art/Sprites/Backgrounds/magic_shop_background.png";
    private const string TitleFontPath = "Assets/Fonts/Y1YomiyasuWide-Bold SDF.asset";
    private const string BodyFontPath = "Assets/Fonts/DotGothic16-Regular SDF.asset";

    private static readonly Color Ink = new(0.169f, 0.145f, 0.188f, 1f);
    private static readonly Color Cream = new(1f, 0.992f, 0.965f, 1f);
    private static readonly Color Yellow = new(1f, 0.835f, 0.29f, 1f);
    private static readonly Color Scrim = new(0.078f, 0.055f, 0.11f, 0.55f);

    [MenuItem("Tools/Scenes/Rebuild Stage Select Scene UI (Request Cards)")]
    public static void RebuildFromMenu() => BuildScene();

    public static void BuildFromBatchMode() => BuildScene();

    private static void BuildScene()
    {
        StageCardPrefabUpgrader.UpgradeStageCardPrefab();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = BuildCamera();
        Canvas canvas = BuildCanvas(camera);
        BuildEventSystem();

        TMP_FontAsset titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFontPath) ?? TMP_Settings.defaultFontAsset;
        TMP_FontAsset bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BodyFontPath) ?? titleFont;
        Sprite backgroundSprite = LoadSprite(BackgroundPath);
        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Sprite buttonSprite = LoadSprite("Assets/Resources/UI/Tutorial/speech_panel.png") ?? uiSprite;

        RectTransform background = CreateImage("Background", canvas.transform, backgroundSprite, Color.white);
        Stretch(background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.preserveAspect = false;

        RectTransform scrim = CreateImage("Scrim", canvas.transform, uiSprite, Scrim);
        Stretch(scrim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform safeArea = CreateRect("SafeAreaRoot", canvas.transform);
        Stretch(safeArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        safeArea.gameObject.AddComponent<SafeAreaFitter>();

        TMP_Text title = CreateText("HeaderText", safeArea, "おしごとをえらぶ", titleFont, 76f, Ink, TextAlignmentOptions.Center);
        AnchorTop(title.rectTransform, 57f, 900f, 96f);
        AddTextShadow(title, new Color(0f, 0f, 0f, 0.45f), new Vector2(0f, -10f));
        title.color = new Color(1f, 0.968f, 0.918f, 1f);

        TMP_Text subtitle = CreateText("SubtitleText", safeArea, "STAGE SELECT", bodyFont, 32f, new Color(1f, 0.968f, 0.918f, 0.6f), TextAlignmentOptions.Center);
        AnchorTop(subtitle.rectTransform, 157f, 760f, 48f);
        subtitle.characterSpacing = 10f;

        RectTransform scrollRectTransform = CreateRect("Scroll View", safeArea);
        Stretch(scrollRectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 665f), new Vector2(0f, -245f));
        ScrollRect scrollRect = scrollRectTransform.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;
        scrollRect.scrollSensitivity = 0f;

        RectTransform viewport = CreateRect("Viewport", scrollRectTransform);
        Stretch(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        scrollRect.viewport = viewport;

        RectTransform content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 0.5f);
        content.anchorMax = new Vector2(0f, 0.5f);
        content.pivot = new Vector2(0f, 0.5f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 950f);
        HorizontalLayoutGroup layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(120, 120, 0, 0);
        layout.spacing = 240f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        scrollRect.content = content;

        SwipeSnapController swipe = scrollRectTransform.gameObject.AddComponent<SwipeSnapController>();
        SerializedObject serializedSwipe = new(swipe);
        serializedSwipe.FindProperty("_scrollRect").objectReferenceValue = scrollRect;
        serializedSwipe.FindProperty("_content").objectReferenceValue = content;
        serializedSwipe.ApplyModifiedPropertiesWithoutUndo();

        StageCardView prefab = AssetDatabase.LoadAssetAtPath<StageCardView>(PrefabPath);
        StageCardView template = null;
        if (prefab != null)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab.gameObject, content) as GameObject;
            template = instance != null ? instance.GetComponent<StageCardView>() : null;
            if (template != null)
            {
                template.name = "StageCard_01";
            }
        }

        StageSelectController controller = new GameObject("StageSelectManager").AddComponent<StageSelectController>();

        Sprite powaSprite = LoadSprite("Assets/Art/Sprites/Characters/Powa/Powa_Idle.png");
        RectTransform powa = CreateImage("Powa", safeArea, powaSprite, Color.white);
        powa.anchorMin = powa.anchorMax = new Vector2(0f, 0f);
        powa.pivot = new Vector2(0f, 0f);
        powa.anchoredPosition = new Vector2(-4f, 335f);
        powa.sizeDelta = new Vector2(260f, 300f);
        powa.GetComponent<Image>().type = Image.Type.Simple;
        powa.GetComponent<Image>().preserveAspect = true;

        RectTransform pageDots = CreateRect("StagePageDots", safeArea);
        pageDots.anchorMin = pageDots.anchorMax = new Vector2(0.5f, 0f);
        pageDots.pivot = new Vector2(0.5f, 0.5f);
        pageDots.anchoredPosition = new Vector2(0f, 390f);
        pageDots.sizeDelta = new Vector2(260f, 32f);
        HorizontalLayoutGroup dotLayout = pageDots.gameObject.AddComponent<HorizontalLayoutGroup>();
        dotLayout.spacing = 18f;
        dotLayout.childAlignment = TextAnchor.MiddleCenter;
        dotLayout.childControlWidth = false;
        dotLayout.childControlHeight = false;
        dotLayout.childForceExpandWidth = false;
        dotLayout.childForceExpandHeight = false;
        Sprite dotSprite = LoadSprite("Assets/Resources/UI/Tutorial/miss_orb_empty.png");
        for (int i = 0; i < 3; i += 1)
        {
            RectTransform dot = CreateImage($"Dot_{i + 1}", pageDots, dotSprite, i == 0
                ? new Color(1f, 0.85f, 0.29f, 1f)
                : new Color(1f, 0.97f, 0.92f, 0.48f));
            dot.sizeDelta = new Vector2(25f, 25f);
            dot.GetComponent<Image>().type = Image.Type.Simple;
            dot.GetComponent<Image>().preserveAspect = true;
        }

        TMP_Text swipeHint = CreateText("SwipeHintText", safeArea, "< スワイプでめくる >", bodyFont, 28f, new Color(1f, 0.97f, 0.92f, 0.48f), TextAlignmentOptions.Center);
        swipeHint.rectTransform.anchorMin = swipeHint.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        swipeHint.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        swipeHint.rectTransform.anchoredPosition = new Vector2(0f, 335f);
        swipeHint.rectTransform.sizeDelta = new Vector2(520f, 44f);

        Button backButton = CreateButton("BackButton", safeArea, buttonSprite, Cream, new Vector2(273f, 180f), "< もどる", bodyFont, 38f, Ink);
        AnchorBottomHorizontalRange(backButton.transform as RectTransform, 0f, 0.31f, 70f, 0f, 57f);
        AddPanelShadow(backButton.gameObject, new Color(0.169f, 0.145f, 0.188f, 0.55f), new Vector2(0f, -10f));
        UnityEventTools.AddPersistentListener(backButton.onClick, controller.OnBackPressed);

        Button playButton = CreateButton("PlayButton", safeArea, buttonSprite, Yellow, new Vector2(635f, 180f), "このおしごとにする!\nPLAY", titleFont, 42f, Ink);
        ColorBlock playColors = playButton.colors;
        playColors.disabledColor = Color.white;
        playButton.colors = playColors;
        AnchorBottomHorizontalRange(playButton.transform as RectTransform, 0.33f, 1f, 0f, 70f, 57f);
        AddPanelShadow(playButton.gameObject, new Color(0.71f, 0.57f, 0.10f, 0.9f), new Vector2(0f, -13f));
        UnityEventTools.AddPersistentListener(playButton.onClick, controller.OnPlayPressed);

        SerializedObject serializedController = new(controller);
        serializedController.FindProperty("_swipeSnapController").objectReferenceValue = swipe;
        serializedController.FindProperty("_stageCardContainer").objectReferenceValue = content;
        serializedController.FindProperty("_stageCardPrefab").objectReferenceValue = prefab;
        serializedController.FindProperty("_playButton").objectReferenceValue = playButton;
        SerializedProperty cards = serializedController.FindProperty("_stageCardViews");
        cards.arraySize = template != null ? 1 : 0;
        if (template != null)
        {
            cards.GetArrayElementAtIndex(0).objectReferenceValue = template;
        }
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[StageSelectSceneLayoutBuilder] Request-card stage select scene rebuilt.");
    }

    private static Camera BuildCamera()
    {
        GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.078f, 0.055f, 0.11f, 1f);
        camera.orthographic = true;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        return camera;
    }

    private static Canvas BuildCanvas(Camera camera)
    {
        GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = camera;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void BuildEventSystem()
    {
        _ = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static Button CreateButton(string name, Transform parent, Sprite sprite, Color color, Vector2 size, string label, TMP_FontAsset font, float fontSize, Color textColor)
    {
        RectTransform rect = CreateImage(name, parent, sprite, color);
        rect.sizeDelta = size;
        Image image = rect.GetComponent<Image>();
        image.type = Image.Type.Sliced;
        image.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        TMP_Text text = CreateText("Label", rect, label, font, fontSize, textColor, TextAlignmentOptions.Center);
        Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 10f), new Vector2(-20f, -10f));
        return button;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, TMP_FontAsset font, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        return rect;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        gameObject.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void AnchorTop(RectTransform rect, float top, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -top);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void AnchorBottomHorizontalRange(
        RectTransform rect,
        float anchorMinX,
        float anchorMaxX,
        float leftInset,
        float rightInset,
        float bottom)
    {
        float height = rect.sizeDelta.y;
        rect.anchorMin = new Vector2(anchorMinX, 0f);
        rect.anchorMax = new Vector2(anchorMaxX, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(leftInset, bottom);
        rect.offsetMax = new Vector2(-rightInset, bottom + height);
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void AddPanelShadow(GameObject target, Color color, Vector2 distance)
    {
        Shadow shadow = target.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static void AddTextShadow(TMP_Text target, Color color, Vector2 distance)
    {
        Shadow shadow = target.gameObject.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is Sprite nested) return nested;
        }
        return null;
    }
}
