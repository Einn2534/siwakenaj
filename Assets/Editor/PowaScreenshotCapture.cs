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

    private static void CaptureScene(Scene scene, string outputPath, int width, int height)
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
            antiAliasing = 4
        };

        Texture2D texture = new(width, height, TextureFormat.RGBA32, false);

        try
        {
            Canvas.ForceUpdateCanvases();
            foreach (Canvas canvas in canvases)
            {
                if (canvas != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.transform as RectTransform);
                }
            }

            camera.targetTexture = renderTexture;
            camera.aspect = (float)width / height;
            camera.clearFlags = previousClearFlags == CameraClearFlags.Nothing
                ? CameraClearFlags.SolidColor
                : previousClearFlags;
            camera.backgroundColor = previousBackgroundColor;

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

    private static void PrepareMainScoreLanePreview()
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
            int bestScore = status == StageCardStatus.Unlocked ? SaveService.GetBestScore(stageNumber) : 0;
            int starRating = status == StageCardStatus.Unlocked ? SaveService.GetStarRating(stageNumber) : 0;
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
