using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RegressionCaptureRunner
{
    private const string DefaultOutputDirectory = "RegressionArtifacts";
    private const float GeometryTolerancePixels = 4f;

    private static readonly DeviceProfile[] DeviceProfiles =
    {
        new("Android_1080x1920", 1080, 1920, new Rect(0f, 0f, 1080f, 1920f)),
        new("iPhone_1170x2532", 1170, 2532, new Rect(0f, 102f, 1170f, 2289f)),
        new("AndroidTall_1440x3200", 1440, 3200, new Rect(0f, 120f, 1440f, 2960f))
    };

    private static readonly List<LayoutCaptureResult> Results = new();

    [MenuItem("Tools/Siwakenja/Regression/Capture Layout Matrix")]
    public static void RunFromMenu()
    {
        Run(GetCommandLineValue("-regressionOutput", DefaultOutputDirectory));
    }

    public static void RunFromBatchMode()
    {
        Run(GetCommandLineValue("-regressionOutput", DefaultOutputDirectory));
    }

    private static void Run(string outputDirectory)
    {
        string absoluteOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(absoluteOutputDirectory);
        Results.Clear();

        try
        {
            foreach (DeviceProfile profile in DeviceProfiles)
            {
                CaptureMain(profile, absoluteOutputDirectory);
                CaptureMainStageFiveHud(profile, absoluteOutputDirectory);
                CapturePause(profile, absoluteOutputDirectory);
                CaptureHowTo(profile, absoluteOutputDirectory);
                CaptureResult(profile, absoluteOutputDirectory, isClear: true);
                CaptureResult(profile, absoluteOutputDirectory, isClear: false);
            }
        }
        finally
        {
            SafeAreaFitter.ClearEditorSimulation();
        }

        string reportPath = Path.Combine(absoluteOutputDirectory, "layout-validation.md");
        File.WriteAllText(reportPath, BuildReport(), Encoding.UTF8);

        int failureCount = Results.Sum(result => result.Failures.Count);
        Debug.Log($"[RegressionCaptureRunner] Captured {Results.Count} states. Layout failures: {failureCount}. Report: {reportPath}");
        if (failureCount > 0)
        {
            throw new InvalidOperationException($"Regression layout validation found {failureCount} failure(s). See {reportPath}.");
        }
    }

    private static void CaptureMain(DeviceProfile profile, string outputDirectory)
    {
        CaptureState(
            profile,
            outputDirectory,
            "Main",
            "Assets/Scenes/Main.unity",
            () =>
            {
                PowaScreenshotCapture.PrepareMainScoreLanePreview();
                MainSafeAreaLayout.EnsureInstalled(SceneManager.GetActiveScene());
            },
            "Canvas");
    }

    private static void CapturePause(DeviceProfile profile, string outputDirectory)
    {
        CaptureState(
            profile,
            outputDirectory,
            "Pause",
            "Assets/Scenes/Main.unity",
            PreparePausePreview,
            "PausePanel");
    }

    private static void CaptureMainStageFiveHud(DeviceProfile profile, string outputDirectory)
    {
        CaptureState(
            profile,
            outputDirectory,
            "Main_Stage5Hud",
            "Assets/Scenes/Main.unity",
            PrepareMainStageFiveHudPreview,
            "Canvas");
    }

    private static void CaptureHowTo(DeviceProfile profile, string outputDirectory)
    {
        CaptureState(
            profile,
            outputDirectory,
            "HowTo",
            "Assets/Scenes/Title.unity",
            () =>
            {
                TitleController controller = UnityEngine.Object.FindFirstObjectByType<TitleController>(FindObjectsInactive.Include);
                controller?.OnHowToOpen();
            },
            "HowToOverlay");
    }

    private static void CaptureResult(DeviceProfile profile, string outputDirectory, bool isClear)
    {
        string stateName = isClear ? "Result_Clear" : "Result_GameOver";
        CaptureState(
            profile,
            outputDirectory,
            stateName,
            "Assets/Scenes/Result.unity",
            isClear
                ? PowaScreenshotCapture.PrepareClearResultPreview
                : PowaScreenshotCapture.PrepareGameOverResultPreview,
            "SafeAreaRoot");
    }

    private static void CaptureState(
        DeviceProfile profile,
        string outputDirectory,
        string stateName,
        string scenePath,
        Action prepare,
        string validationRootName)
    {
        SafeAreaFitter.SetEditorSimulation(profile.ScreenSize, profile.SafeArea);
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        prepare?.Invoke();
        RefreshSafeAreaFitters(scene);

        string screenshotPath = Path.Combine(outputDirectory, $"{stateName}_{profile.Name}.png");
        List<string> failures = new();

        PowaScreenshotCapture.CaptureSceneForRegression(
            scene,
            screenshotPath,
            profile.Width,
            profile.Height,
            camera =>
            {
                RefreshSafeAreaFitters(scene);
                ForceLayouts(scene);
                Transform validationRoot = FindTransform(scene, validationRootName);
                failures.AddRange(ValidateLayout(scene, validationRoot, profile));
            });

        Results.Add(new LayoutCaptureResult(stateName, profile, screenshotPath, failures));
    }

    private static void PreparePausePreview()
    {
        SessionState.SelectStage(5);
        MainPauseMenuController controller = UnityEngine.Object.FindFirstObjectByType<MainPauseMenuController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            GameObject controllerObject = new("MainPauseMenuController_RegressionPreview");
            controller = controllerObject.AddComponent<MainPauseMenuController>();
        }

        if (FindTransform(SceneManager.GetActiveScene(), "PauseMenuCanvas") == null)
        {
            InvokePrivate(controller, "BuildInterface");
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

        InvokePrivate(controller, "RefreshStageText");
        InvokePrivate(controller, "ShowPauseMenuPanel");
        Transform pauseButton = FindTransform(SceneManager.GetActiveScene(), "PauseButton");
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(false);
        }
    }

    private static void PrepareMainStageFiveHudPreview()
    {
        SessionState.SelectStage(5);
        PowaScreenshotCapture.PrepareMainScoreLanePreview();
        MainSafeAreaLayout.EnsureInstalled(SceneManager.GetActiveScene());

        StageProgressHudController progressHud = UnityEngine.Object.FindFirstObjectByType<StageProgressHudController>(FindObjectsInactive.Include);
        if (progressHud == null)
        {
            GameObject progressObject = new("StageProgressHudController_RegressionPreview");
            progressHud = progressObject.AddComponent<StageProgressHudController>();
        }

        if (FindTransform(SceneManager.GetActiveScene(), "StageProgressHudCanvas") == null)
        {
            InvokePrivate(progressHud, "Awake");
        }

        progressHud.SetTutorialMode(true);
        progressHud.SetTutorialProgress(378, 420, 1);

        GimmickHudController gimmickHud = UnityEngine.Object.FindFirstObjectByType<GimmickHudController>(FindObjectsInactive.Include);
        if (gimmickHud == null)
        {
            GameObject gimmickObject = new("GimmickHudController_RegressionPreview");
            gimmickHud = gimmickObject.AddComponent<GimmickHudController>();
        }

        StageDefinition stageFive = new()
        {
            StageNumber = 5,
            TargetScore = 420,
            BrokenChance = 0.16f,
            FeverComboThreshold = 6
        };
        LaneInputController laneInput = UnityEngine.Object.FindFirstObjectByType<LaneInputController>(FindObjectsInactive.Include);
        gimmickHud.Initialize(stageFive, laneInput);

        Transform messageRoot = FindTransform(SceneManager.GetActiveScene(), "GimmickMessage");
        if (messageRoot != null)
        {
            TMP_Text messageText = messageRoot.GetComponentInChildren<TMP_Text>(true);
            if (messageText != null)
            {
                messageText.text = "急送車！  「!」は得点×2";
            }

            messageRoot.gameObject.SetActive(true);
        }

        Canvas.ForceUpdateCanvases();
    }

    private static IEnumerable<string> ValidateLayout(
        Scene scene,
        Transform validationRoot,
        DeviceProfile profile)
    {
        List<string> failures = new();
        if (validationRoot == null)
        {
            failures.Add("validation root was not found");
            return failures;
        }

        ValidateSafeAreaFitters(scene, profile, failures);
        ValidateButtons(validationRoot, profile, failures);
        ValidateText(validationRoot, failures);
        return failures;
    }

    private static void ValidateSafeAreaFitters(
        Scene scene,
        DeviceProfile profile,
        ICollection<string> failures)
    {
        SafeAreaFitter[] fitters = UnityEngine.Object.FindObjectsByType<SafeAreaFitter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (SafeAreaFitter fitter in fitters)
        {
            if (fitter == null || fitter.gameObject.scene != scene || !fitter.gameObject.activeInHierarchy)
            {
                continue;
            }

            Rect actual = ToScreenRect(fitter.transform as RectTransform, profile);
            if (!ApproximatelyContains(profile.SafeArea, actual, GeometryTolerancePixels))
            {
                failures.Add($"safe area root '{GetPath(fitter.transform)}' is outside {FormatRect(profile.SafeArea)}: {FormatRect(actual)}");
            }
        }
    }

    private static void ValidateButtons(
        Transform validationRoot,
        DeviceProfile profile,
        ICollection<string> failures)
    {
        Button[] buttons = validationRoot.GetComponentsInChildren<Button>(false)
            .Where(button => button != null && button.isActiveAndEnabled)
            .ToArray();
        Dictionary<Button, Rect> rects = new();

        foreach (Button button in buttons)
        {
            Rect rect = ToScreenRect(button.transform as RectTransform, profile);
            rects[button] = rect;
            if (!ApproximatelyContains(profile.SafeArea, rect, GeometryTolerancePixels))
            {
                failures.Add($"button '{GetPath(button.transform)}' is outside the safe area: {FormatRect(rect)}");
            }
        }

        for (int i = 0; i < buttons.Length; i += 1)
        {
            for (int j = i + 1; j < buttons.Length; j += 1)
            {
                Button first = buttons[i];
                Button second = buttons[j];
                if (first.transform.IsChildOf(second.transform) || second.transform.IsChildOf(first.transform))
                {
                    continue;
                }

                Rect intersection = Intersect(rects[first], rects[second]);
                if (intersection.width > GeometryTolerancePixels && intersection.height > GeometryTolerancePixels)
                {
                    failures.Add($"buttons overlap: '{GetPath(first.transform)}' and '{GetPath(second.transform)}' ({FormatRect(intersection)})");
                }
            }
        }
    }

    private static void ValidateText(Transform validationRoot, ICollection<string> failures)
    {
        TMP_Text[] texts = validationRoot.GetComponentsInChildren<TMP_Text>(false);
        foreach (TMP_Text text in texts)
        {
            if (text == null || !text.isActiveAndEnabled || string.IsNullOrWhiteSpace(text.text))
            {
                continue;
            }

            text.ForceMeshUpdate(ignoreActiveState: false, forceTextReparsing: true);
            if (text.textInfo.characterCount == 0)
            {
                failures.Add($"text produced no characters: '{GetPath(text.transform)}' value='{Compact(text.text)}'");
                continue;
            }

            bool clipsOverflow = text.overflowMode == TextOverflowModes.Truncate
                || text.overflowMode == TextOverflowModes.Ellipsis
                || text.overflowMode == TextOverflowModes.Masking;
            if (clipsOverflow && text.firstOverflowCharacterIndex >= 0)
            {
                failures.Add($"text is truncated at character {text.firstOverflowCharacterIndex}: '{GetPath(text.transform)}' value='{Compact(text.text)}'");
            }
        }
    }

    private static void RefreshSafeAreaFitters(Scene scene)
    {
        SafeAreaFitter[] fitters = UnityEngine.Object.FindObjectsByType<SafeAreaFitter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (SafeAreaFitter fitter in fitters)
        {
            if (fitter != null && fitter.gameObject.scene == scene)
            {
                fitter.Refresh();
            }
        }

        MainSafeAreaLayout mainLayout = MainSafeAreaLayout.EnsureInstalled(scene);
        mainLayout?.ApplyNow();
    }

    private static void ForceLayouts(Scene scene)
    {
        Canvas.ForceUpdateCanvases();
        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null && canvas.gameObject.scene == scene && canvas.transform is RectTransform rectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    private static Rect ToScreenRect(RectTransform rectTransform, DeviceProfile profile)
    {
        if (rectTransform == null)
        {
            return Rect.zero;
        }

        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
        if (canvasRect == null || Mathf.Approximately(canvasRect.rect.width, 0f) || Mathf.Approximately(canvasRect.rect.height, 0f))
        {
            return Rect.zero;
        }

        Vector3[] worldCorners = new Vector3[4];
        rectTransform.GetWorldCorners(worldCorners);
        Vector3 firstCorner = canvasRect.InverseTransformPoint(worldCorners[0]);
        float xMin = firstCorner.x;
        float yMin = firstCorner.y;
        float xMax = firstCorner.x;
        float yMax = firstCorner.y;
        for (int i = 1; i < worldCorners.Length; i += 1)
        {
            Vector3 localCorner = canvasRect.InverseTransformPoint(worldCorners[i]);
            xMin = Mathf.Min(xMin, localCorner.x);
            yMin = Mathf.Min(yMin, localCorner.y);
            xMax = Mathf.Max(xMax, localCorner.x);
            yMax = Mathf.Max(yMax, localCorner.y);
        }

        Rect canvasBounds = canvasRect.rect;
        return Rect.MinMaxRect(
            (xMin - canvasBounds.xMin) / canvasBounds.width * profile.Width,
            (yMin - canvasBounds.yMin) / canvasBounds.height * profile.Height,
            (xMax - canvasBounds.xMin) / canvasBounds.width * profile.Width,
            (yMax - canvasBounds.yMin) / canvasBounds.height * profile.Height);
    }

    private static bool ApproximatelyContains(Rect outer, Rect inner, float tolerance)
    {
        return inner.xMin >= outer.xMin - tolerance
            && inner.yMin >= outer.yMin - tolerance
            && inner.xMax <= outer.xMax + tolerance
            && inner.yMax <= outer.yMax + tolerance;
    }

    private static Rect Intersect(Rect first, Rect second)
    {
        float xMin = Mathf.Max(first.xMin, second.xMin);
        float yMin = Mathf.Max(first.yMin, second.yMin);
        float xMax = Mathf.Min(first.xMax, second.xMax);
        float yMax = Mathf.Min(first.yMax, second.yMax);
        return xMax > xMin && yMax > yMin
            ? Rect.MinMaxRect(xMin, yMin, xMax, yMax)
            : Rect.zero;
    }

    private static Transform FindTransform(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (string.Equals(candidate.name, objectName, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target?.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method?.Invoke(target, null);
    }

    private static string BuildReport()
    {
        StringBuilder report = new();
        report.AppendLine("# Siwakenja regression layout validation");
        report.AppendLine();
        report.AppendLine("Validated active buttons against the simulated safe area, checked active TMP text truncation, and checked active button pairs for overlap.");
        report.AppendLine();

        foreach (LayoutCaptureResult result in Results)
        {
            report.AppendLine($"## {result.StateName} / {result.Profile.Name}");
            report.AppendLine();
            report.AppendLine($"- viewport: {result.Profile.Width} x {result.Profile.Height}");
            report.AppendLine($"- safe area: {FormatRect(result.Profile.SafeArea)}");
            report.AppendLine($"- screenshot: `{result.ScreenshotPath}`");
            report.AppendLine($"- result: {(result.Failures.Count == 0 ? "PASS" : "FAIL")}");
            foreach (string failure in result.Failures)
            {
                report.AppendLine($"  - {failure}");
            }

            report.AppendLine();
        }

        int failureCount = Results.Sum(result => result.Failures.Count);
        report.AppendLine($"Final result: {(failureCount == 0 ? "PASS" : "FAIL")} ({Results.Count} captures, {failureCount} failures)");
        return report.ToString();
    }

    private static string GetPath(Transform transform)
    {
        if (transform == null)
        {
            return "<missing>";
        }

        Stack<string> names = new();
        Transform current = transform;
        while (current != null)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names);
    }

    private static string FormatRect(Rect rect)
    {
        return $"({rect.xMin:0},{rect.yMin:0})-({rect.xMax:0},{rect.yMax:0})";
    }

    private static string Compact(string value)
    {
        string compact = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return compact.Length <= 60 ? compact : compact.Substring(0, 57) + "...";
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

    private readonly struct DeviceProfile
    {
        public DeviceProfile(string name, int width, int height, Rect safeArea)
        {
            Name = name;
            Width = width;
            Height = height;
            SafeArea = safeArea;
        }

        public string Name { get; }
        public int Width { get; }
        public int Height { get; }
        public Vector2Int ScreenSize => new(Width, Height);
        public Rect SafeArea { get; }
    }

    private sealed class LayoutCaptureResult
    {
        public LayoutCaptureResult(string stateName, DeviceProfile profile, string screenshotPath, List<string> failures)
        {
            StateName = stateName;
            Profile = profile;
            ScreenshotPath = screenshotPath;
            Failures = failures;
        }

        public string StateName { get; }
        public DeviceProfile Profile { get; }
        public string ScreenshotPath { get; }
        public List<string> Failures { get; }
    }
}
