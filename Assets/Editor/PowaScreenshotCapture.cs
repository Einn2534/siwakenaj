using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PowaScreenshotCapture
{
    private const int DefaultWidth = 1080;
    private const int DefaultHeight = 1920;
    private const string DefaultOutputDirectory = "Temp/PowaScreenshots";

    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/Title.unity",
        "Assets/Scenes/StageSelect.unity",
        "Assets/Scenes/Main.unity",
        "Assets/Scenes/Result.unity"
    };

    public static void CaptureAllFromBatchMode()
    {
        string outputDirectory = GetCommandLineValue("-powaScreenshotOutput", DefaultOutputDirectory);
        int width = GetCommandLineInt("-powaScreenshotWidth", DefaultWidth);
        int height = GetCommandLineInt("-powaScreenshotHeight", DefaultHeight);

        Directory.CreateDirectory(outputDirectory);

        foreach (string scenePath in ScenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            PrepareSceneForCapture(sceneName);
            string outputPath = Path.Combine(outputDirectory, $"{sceneName}_{width}x{height}.png");
            CaptureScene(scene, outputPath, width, height);
        }

        Debug.Log($"[PowaScreenshotCapture] Saved screenshots to {Path.GetFullPath(outputDirectory)}");
    }

    public static void CaptureDesignQaStatesFromBatchMode()
    {
        string outputDirectory = GetCommandLineValue("-powaScreenshotOutput", DefaultOutputDirectory);
        int width = GetCommandLineInt("-powaScreenshotWidth", DefaultWidth);
        int height = GetCommandLineInt("-powaScreenshotHeight", DefaultHeight);
        Directory.CreateDirectory(outputDirectory);

        Scene stageScene = EditorSceneManager.OpenScene("Assets/Scenes/StageSelect.unity", OpenSceneMode.Single);
        PrepareStageSelectCards();
        InvokePrivateMethod(UnityEngine.Object.FindFirstObjectByType<StageSelectController>(FindObjectsInactive.Include), "BuildUtilityUi");
        PrepareStageDots(0);
        CaptureScene(stageScene, Path.Combine(outputDirectory, $"StageSelect_Unlocked_{width}x{height}.png"), width, height);
        PrepareLockedStageSelectPreview();
        PrepareStageDots(2);
        CaptureScene(stageScene, Path.Combine(outputDirectory, $"StageSelect_Locked_{width}x{height}.png"), width, height);

        Scene resultScene = EditorSceneManager.OpenScene("Assets/Scenes/Result.unity", OpenSceneMode.Single);
        PrepareClearResultPreview();
        CaptureScene(resultScene, Path.Combine(outputDirectory, $"Result_Clear_{width}x{height}.png"), width, height);
        PrepareGameOverResultPreview();
        CaptureScene(resultScene, Path.Combine(outputDirectory, $"Result_GameOver_{width}x{height}.png"), width, height);

        Debug.Log($"[PowaScreenshotCapture] Saved design QA states to {Path.GetFullPath(outputDirectory)}");
    }

    public static void CaptureSettingsFromBatchMode()
    {
        string outputDirectory = GetCommandLineValue("-powaScreenshotOutput", DefaultOutputDirectory);
        int width = GetCommandLineInt("-powaScreenshotWidth", DefaultWidth);
        int height = GetCommandLineInt("-powaScreenshotHeight", DefaultHeight);
        Directory.CreateDirectory(outputDirectory);

        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Title.unity", OpenSceneMode.Single);
        TitleController titleController = UnityEngine.Object.FindFirstObjectByType<TitleController>(FindObjectsInactive.Include);
        titleController?.OnSettingsOpen();

        string outputPath = Path.Combine(outputDirectory, $"Title_Settings_{width}x{height}.png");
        CaptureScene(scene, outputPath, width, height);
        Debug.Log($"[PowaScreenshotCapture] Saved settings screenshot to {Path.GetFullPath(outputPath)}");
    }

    public static void CaptureHowToFromBatchMode()
    {
        string outputDirectory = GetCommandLineValue("-powaScreenshotOutput", DefaultOutputDirectory);
        int width = GetCommandLineInt("-powaScreenshotWidth", DefaultWidth);
        int height = GetCommandLineInt("-powaScreenshotHeight", DefaultHeight);
        Directory.CreateDirectory(outputDirectory);

        TitleSceneLayoutBuilder.BuildFromBatchMode();
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Title.unity", OpenSceneMode.Single);
        TitleController titleController = UnityEngine.Object.FindFirstObjectByType<TitleController>(FindObjectsInactive.Include);
        titleController?.OnHowToOpen();

        string outputPath = Path.Combine(outputDirectory, $"Title_HowTo_{width}x{height}.png");
        CaptureScene(scene, outputPath, width, height);
        Debug.Log($"[PowaScreenshotCapture] Saved How To screenshot to {Path.GetFullPath(outputPath)}");
    }

    public static void CapturePauseMenuFromBatchMode()
    {
        string outputDirectory = GetCommandLineValue("-powaScreenshotOutput", DefaultOutputDirectory);
        int width = GetCommandLineInt("-powaScreenshotWidth", DefaultWidth);
        int height = GetCommandLineInt("-powaScreenshotHeight", DefaultHeight);
        Directory.CreateDirectory(outputDirectory);

        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
        SessionState.SelectStage(8);

        MainPauseMenuController controller = UnityEngine.Object.FindFirstObjectByType<MainPauseMenuController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            GameObject controllerObject = new("MainPauseMenuController_Preview");
            controller = controllerObject.AddComponent<MainPauseMenuController>();
        }

        if (GameObject.Find("PauseMenuCanvas") == null)
        {
            InvokePrivateMethod(controller, "BuildInterface");
        }

        GameObject gameplayCanvas = GameObject.Find("Canvas");
        if (gameplayCanvas != null)
        {
            for (int i = 0; i < gameplayCanvas.transform.childCount; i += 1)
            {
                Transform child = gameplayCanvas.transform.GetChild(i);
                child.gameObject.SetActive(string.Equals(child.name, "Background", StringComparison.Ordinal));
            }
        }

        InvokePrivateMethod(controller, "RefreshStageText");
        InvokePrivateMethod(controller, "ShowPauseMenuPanel");
        GameObject pauseButton = GameObject.Find("PauseMenuCanvas/SafeAreaRoot/PauseButton");
        if (pauseButton != null)
        {
            pauseButton.SetActive(false);
        }

        string outputPath = Path.Combine(outputDirectory, $"Main_PauseMenu_{width}x{height}.png");
        CaptureScene(scene, outputPath, width, height);
        Debug.Log($"[PowaScreenshotCapture] Saved pause-menu screenshot to {Path.GetFullPath(outputPath)}");
    }

    internal static void CaptureSceneForRegression(
        Scene scene,
        string outputPath,
        int width,
        int height,
        Action<Camera> onLayoutReady)
    {
        CaptureScene(scene, outputPath, width, height, onLayoutReady);
    }

    private static void CaptureScene(Scene scene, string outputPath, int width, int height)
    {
        CaptureScene(scene, outputPath, width, height, null);
    }

    private static void CaptureScene(
        Scene scene,
        string outputPath,
        int width,
        int height,
        Action<Camera> onLayoutReady)
    {
        Camera camera = Camera.main != null
            ? Camera.main
            : UnityEngine.Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);

        if (camera == null)
        {
            Debug.LogWarning($"[PowaScreenshotCapture] No camera found in {scene.name}");
            return;
        }

        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<CanvasState> canvasStates = new(canvases.Length);
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null)
            {
                continue;
            }

            canvasStates.Add(new CanvasState(canvas));
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
        }

        RenderTexture previousTargetTexture = camera.targetTexture;
        float previousAspect = camera.aspect;
        Color previousBackgroundColor = camera.backgroundColor;
        CameraClearFlags previousClearFlags = camera.clearFlags;
        RenderTexture previousActive = RenderTexture.active;

        RenderTexture renderTexture = new(width, height, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 1
        };

        Texture2D texture = new(width, height, TextureFormat.RGBA32, false);

        try
        {
            camera.targetTexture = renderTexture;
            camera.aspect = (float)width / height;
            camera.clearFlags = previousClearFlags == CameraClearFlags.Nothing
                ? CameraClearFlags.SolidColor
                : previousClearFlags;
            camera.backgroundColor = previousBackgroundColor;

            Canvas.ForceUpdateCanvases();
            foreach (Canvas canvas in canvases)
            {
                if (canvas != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.transform as RectTransform);
                }
            }

            Canvas.ForceUpdateCanvases();
            // A first render makes dynamically-created ScreenSpace canvases adopt
            // the target texture dimensions before regression geometry is read.
            camera.Render();
            Canvas.ForceUpdateCanvases();
            onLayoutReady?.Invoke(camera);

            RenderTexture.active = renderTexture;
            camera.Render();
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();

            File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            Debug.Log($"[PowaScreenshotCapture] Captured {scene.name}: {Path.GetFullPath(outputPath)}");
        }
        finally
        {
            camera.targetTexture = previousTargetTexture;
            camera.aspect = previousAspect;
            camera.clearFlags = previousClearFlags;
            camera.backgroundColor = previousBackgroundColor;
            RenderTexture.active = previousActive;

            foreach (CanvasState state in canvasStates)
            {
                state.Restore();
            }

            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(renderTexture);
        }
    }

    private static void PrepareSceneForCapture(string sceneName)
    {
        if (string.Equals(sceneName, "StageSelect", StringComparison.OrdinalIgnoreCase))
        {
            PrepareStageSelectCards();
        }
        else if (string.Equals(sceneName, "Main", StringComparison.OrdinalIgnoreCase))
        {
            PrepareMainScoreLanePreview();
        }
    }

    internal static void PrepareMainScoreLanePreview()
    {
        ScoreLaneUI scoreLaneUI = UnityEngine.Object.FindFirstObjectByType<ScoreLaneUI>(FindObjectsInactive.Include);
        if (scoreLaneUI == null)
        {
            return;
        }

        scoreLaneUI.gameObject.SetActive(true);
        scoreLaneUI.UpdateLane(CarType.LightTruck, 2);
        scoreLaneUI.UpdateLane(CarType.CompactCar, 3);
        scoreLaneUI.UpdateLane(CarType.SportsCar, 1);
        Canvas.ForceUpdateCanvases();
    }

    private static void PrepareStageSelectCards()
    {
        StageSelectController controller = UnityEngine.Object.FindFirstObjectByType<StageSelectController>(FindObjectsInactive.Include);
        StageDatabase stageDatabase = Resources.Load<StageDatabase>("StageDatabase");
        if (controller == null || stageDatabase == null || stageDatabase.Stages == null || stageDatabase.Stages.Count == 0)
        {
            return;
        }

        SerializedObject serializedController = new(controller);
        RectTransform container = serializedController.FindProperty("_stageCardContainer")?.objectReferenceValue as RectTransform;
        StageCardView prefab = serializedController.FindProperty("_stageCardPrefab")?.objectReferenceValue as StageCardView;
        if (container == null || prefab == null)
        {
            return;
        }

        for (int i = container.childCount - 1; i >= 0; i -= 1)
        {
            UnityEngine.Object.DestroyImmediate(container.GetChild(i).gameObject);
        }

        int count = stageDatabase.Stages.Count;
        for (int i = 0; i < count; i += 1)
        {
            StageDefinition stageDefinition = stageDatabase.Stages[i];
            UnityEngine.Object instance = PrefabUtility.InstantiatePrefab(prefab.gameObject, container);
            StageCardView card = instance is GameObject gameObject
                ? gameObject.GetComponent<StageCardView>()
                : null;
            if (card == null)
            {
                continue;
            }

            int stageNumber = stageDefinition != null
                ? StageNumberUtility.Normalize(stageDefinition.StageNumber)
                : StageNumberUtility.FromIndex(i);
            bool isUnlocked = stageDefinition != null
                && stageDefinition.IsImplemented
                && stageDatabase.IsStageUnlocked(i, SaveService.GetBestScore);
            StageCardStatus status = stageDefinition == null || !stageDefinition.IsImplemented
                ? StageCardStatus.ComingSoon
                : isUnlocked ? StageCardStatus.Unlocked : StageCardStatus.Locked;
            int bestScore = i == 0 && status == StageCardStatus.Unlocked ? 65 : status == StageCardStatus.Unlocked ? SaveService.GetBestScore(stageNumber) : 0;
            int starRating = i == 0 && status == StageCardStatus.Unlocked ? 3 : status == StageCardStatus.Unlocked ? SaveService.GetStarRating(stageNumber) : 0;
            int requiredStageNumber = status == StageCardStatus.Locked ? stageDatabase.GetRequiredClearStageNumber(i) : 0;

            card.name = $"StageCard_Capture_{stageNumber:00}";
            card.gameObject.SetActive(true);
            card.SetData(stageNumber, stageDefinition != null ? stageDefinition.TargetScore : 0, bestScore, status, starRating, requiredStageNumber);
            card.SetSelected(i == 0 && status == StageCardStatus.Unlocked);
        }

        InvokePrivateLayoutPass();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        if (container.parent is RectTransform parent)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        }
    }

    private static void PrepareLockedStageSelectPreview()
    {
        StageSelectController controller = UnityEngine.Object.FindFirstObjectByType<StageSelectController>(FindObjectsInactive.Include);
        SerializedObject serializedController = controller != null ? new SerializedObject(controller) : null;
        RectTransform container = serializedController?.FindProperty("_stageCardContainer")?.objectReferenceValue as RectTransform;
        StageCardView card = container != null && container.childCount > 0 ? container.GetChild(0).GetComponent<StageCardView>() : null;
        if (card != null)
        {
            card.SetData(3, 0, 0, StageCardStatus.Locked, 0, 2);
            card.SetSelected(false);
        }

        if (controller == null)
        {
            return;
        }

        Button playButton = serializedController.FindProperty("_playButton")?.objectReferenceValue as Button;
        if (playButton != null)
        {
            playButton.interactable = false;
            playButton.GetComponent<Image>().color = new Color(0.78f, 0.75f, 0.66f, 1f);
            TMP_Text label = playButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = "まだえらべない\n<size=55%><color=#2B253070>LOCKED</color></size>";
                label.color = new Color(0.39f, 0.37f, 0.33f, 0.58f);
            }
        }
    }

    private static void PrepareStageDots(int activeIndex)
    {
        GameObject containerObject = GameObject.Find("Canvas/SafeAreaRoot/StagePageDots");
        if (containerObject == null)
        {
            return;
        }

        RectTransform container = containerObject.transform as RectTransform;
        while (container.childCount < 3)
        {
            RectTransform dot = CreatePreviewPanel($"Dot_{container.childCount + 1}", container, Color.white);
            LayoutElement layout = dot.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 25f;
            layout.preferredHeight = 25f;
            layout.minWidth = 25f;
            layout.minHeight = 25f;
        }

        for (int i = 0; i < container.childCount; i += 1)
        {
            Image image = container.GetChild(i).GetComponent<Image>();
            if (image != null)
            {
                image.color = i == activeIndex
                    ? new Color(1f, 0.851f, 0.29f, 0.9f)
                    : new Color(1f, 0.969f, 0.918f, 0.45f);
            }
        }
    }

    internal static void PrepareClearResultPreview()
    {
        ResultController controller = UnityEngine.Object.FindFirstObjectByType<ResultController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            return;
        }

        InvokePrivateMethod(controller, "ApplyCarIcons");
        SerializedObject serialized = new(controller);
        SetPreviewText(serialized, "_stageText", "ステージ 8");
        SetPreviewText(serialized, "_resultText", "クリア!");
        SetPreviewText(serialized, "_subMessageText", "STAGE CLEAR");
        SetPreviewText(serialized, "_scoreText", "12,450");
        SetPreviewText(serialized, "_bestScoreText", "11,920");
        SetPreviewText(serialized, "_countAText", "12");
        SetPreviewText(serialized, "_countBText", "9");
        SetPreviewText(serialized, "_countCText", "7");
        SetPreviewText(serialized, "_missCountText", "1");
        SetPreviewStars(serialized, 3);
        SetPreviewMissOrbs(serialized, 1);
        GameObject newBest = serialized.FindProperty("_newBestBadge")?.objectReferenceValue as GameObject;
        newBest?.SetActive(true);
    }

    internal static void PrepareGameOverResultPreview()
    {
        ResultController controller = UnityEngine.Object.FindFirstObjectByType<ResultController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            return;
        }

        SerializedObject serialized = new(controller);
        SetPreviewText(serialized, "_resultText", "ゲームオーバー...");
        SetPreviewText(serialized, "_subMessageText", "GAME OVER");
        SetPreviewText(serialized, "_scoreText", "6,180");
        SetPreviewText(serialized, "_countAText", "5");
        SetPreviewText(serialized, "_countBText", "4");
        SetPreviewText(serialized, "_countCText", "3");
        SetPreviewText(serialized, "_missCountText", "3");
        SetPreviewStars(serialized, 1);
        SetPreviewMissOrbs(serialized, 3);
        GameObject newBest = serialized.FindProperty("_newBestBadge")?.objectReferenceValue as GameObject;
        newBest?.SetActive(false);

        TMP_Text resultText = serialized.FindProperty("_resultText")?.objectReferenceValue as TMP_Text;
        if (resultText != null) resultText.color = new Color(1f, 0.72f, 0.72f, 1f);
        Image stateTint = serialized.FindProperty("_stateTintImage")?.objectReferenceValue as Image;
        if (stateTint != null) stateTint.color = new Color(0.13f, 0.055f, 0.078f, 0.62f);

        Button primary = serialized.FindProperty("_primaryActionButton")?.objectReferenceValue as Button;
        Button secondaryLeft = serialized.FindProperty("_secondaryLeftButton")?.objectReferenceValue as Button;
        SetPreviewButton(primary, "もういちど\n<size=55%><color=#2B253070>RETRY</color></size>");
        SetPreviewButton(secondaryLeft, "タイトル");

    }

    private static void SetPreviewText(SerializedObject serialized, string propertyName, string value)
    {
        TMP_Text text = serialized.FindProperty(propertyName)?.objectReferenceValue as TMP_Text;
        if (text != null) text.text = value;
    }

    private static void SetPreviewStars(SerializedObject serialized, int filledCount)
    {
        SerializedProperty stars = serialized.FindProperty("_starImages");
        Sprite filled = serialized.FindProperty("_filledStarSprite")?.objectReferenceValue as Sprite;
        Sprite empty = serialized.FindProperty("_emptyStarSprite")?.objectReferenceValue as Sprite;
        if (stars == null) return;
        for (int i = 0; i < stars.arraySize; i += 1)
        {
            Image star = stars.GetArrayElementAtIndex(i).objectReferenceValue as Image;
            if (star != null) star.sprite = i < filledCount ? filled : empty;
        }
    }

    private static void SetPreviewMissOrbs(SerializedObject serialized, int filledCount)
    {
        SerializedProperty orbs = serialized.FindProperty("_missOrbImages");
        Sprite filled = serialized.FindProperty("_filledMissOrbSprite")?.objectReferenceValue as Sprite;
        Sprite empty = serialized.FindProperty("_emptyMissOrbSprite")?.objectReferenceValue as Sprite;
        if (orbs == null) return;
        for (int i = 0; i < orbs.arraySize; i += 1)
        {
            Image orb = orbs.GetArrayElementAtIndex(i).objectReferenceValue as Image;
            if (orb != null) orb.sprite = i < filledCount ? filled : empty;
        }
    }

    private static void SetPreviewButton(Button button, string label)
    {
        if (button == null) return;
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.text = label;
    }

    private static void InvokePrivateMethod(object target, string methodName)
    {
        if (target == null) return;
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method?.Invoke(target, null);
    }

    private static void InvokePrivateLayoutPass()
    {
        MethodInfo applyLayout = typeof(StageSelectResponsiveLayout).GetMethod("ApplyLayout", BindingFlags.Instance | BindingFlags.NonPublic);
        if (applyLayout == null)
        {
            return;
        }

        foreach (StageSelectResponsiveLayout layout in UnityEngine.Object.FindObjectsByType<StageSelectResponsiveLayout>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            applyLayout.Invoke(layout, null);
        }
    }

    private static RectTransform CreatePreviewPanel(string name, Transform parent, Color color)
    {
        RectTransform rect = CreatePreviewObject(name, parent, typeof(Image));
        Image image = rect.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static TMP_Text CreatePreviewText(
        string name,
        Transform parent,
        string value,
        TMP_FontAsset fontAsset,
        float fontSizeMax,
        float fontSizeMin,
        FontStyles fontStyle,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = CreatePreviewObject(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = fontAsset;
        text.fontSize = fontSizeMax;
        text.fontSizeMax = fontSizeMax;
        text.fontSizeMin = fontSizeMin;
        text.enableAutoSizing = true;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = new Color(0.96f, 0.99f, 1f, 1f);
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static RectTransform CreatePreviewObject(string name, Transform parent, params Type[] components)
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
        gameObject.transform.SetParent(parent, false);
        return (RectTransform)gameObject.transform;
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

    private static void AddPreviewShadow(GameObject gameObject, Color color, Vector2 distance)
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

    private static string GetCommandLineValue(string key, string fallback)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i += 1)
        {
            if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return fallback;
    }

    private static int GetCommandLineInt(string key, int fallback)
    {
        string value = GetCommandLineValue(key, null);
        return int.TryParse(value, out int parsed) && parsed > 0 ? parsed : fallback;
    }

    private readonly struct CanvasState
    {
        private readonly Canvas _canvas;
        private readonly RenderMode _renderMode;
        private readonly Camera _worldCamera;
        private readonly float _planeDistance;

        public CanvasState(Canvas canvas)
        {
            _canvas = canvas;
            _renderMode = canvas.renderMode;
            _worldCamera = canvas.worldCamera;
            _planeDistance = canvas.planeDistance;
        }

        public void Restore()
        {
            if (_canvas == null)
            {
                return;
            }

            _canvas.renderMode = _renderMode;
            _canvas.worldCamera = _worldCamera;
            _canvas.planeDistance = _planeDistance;
        }
    }
}
