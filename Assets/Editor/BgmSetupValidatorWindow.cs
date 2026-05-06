using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BgmSetupValidatorWindow : EditorWindow
{
    private const string TitleScenePath = "Assets/Scenes/Title.unity";
    private const string StageSelectScenePath = "Assets/Scenes/StageSelect.unity";
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const string ResultScenePath = "Assets/Scenes/Result.unity";
    private const string SoundManagerScriptPath = "Assets/Scripts/System/SoundManager.cs";

    private static readonly string[] ExpectedSceneOrder =
    {
        TitleScenePath,
        StageSelectScenePath,
        MainScenePath,
        ResultScenePath
    };

    private static readonly AudioResourceCheck[] AudioResources =
    {
        new("Title", "Audio/TitleBgm", "Assets/Resources/Audio/TitleBgm.ogg"),
        new("Stage Select", "Audio/StageSelectBgm", "Assets/Resources/Audio/StageSelectBgm.ogg"),
        new("Main", "Audio/Bgm", "Assets/Resources/Audio/Bgm.wav")
    };

    private readonly List<CheckResult> _results = new();
    private Vector2 _scrollPosition;

    [MenuItem("Tools/Audio/BGM Setup Validator")]
    public static void Open()
    {
        BgmSetupValidatorWindow window = GetWindow<BgmSetupValidatorWindow>("BGM Validator");
        window.minSize = new Vector2(520f, 360f);
        window.RunChecks();
    }

    private void OnEnable()
    {
        RunChecks();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("BGM Setup Validator", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Checks scene flow, runtime SoundManager ownership, and required Resources audio clips.");

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Run Checks", GUILayout.Width(120f)))
            {
                RunChecks();
            }

            if (GUILayout.Button("Open SoundManager", GUILayout.Width(150f)))
            {
                Object script = AssetDatabase.LoadAssetAtPath<Object>(SoundManagerScriptPath);
                Selection.activeObject = script;
                EditorGUIUtility.PingObject(script);
            }
        }

        EditorGUILayout.Space(8f);
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        foreach (CheckResult result in _results)
        {
            MessageType messageType = result.Status switch
            {
                CheckStatus.Ok => MessageType.Info,
                CheckStatus.Warning => MessageType.Warning,
                _ => MessageType.Error
            };

            EditorGUILayout.HelpBox(result.Message, messageType);
        }

        EditorGUILayout.EndScrollView();
    }

    private void RunChecks()
    {
        _results.Clear();
        CheckBuildSceneOrder();
        CheckAudioResources();
        CheckSceneSoundManagerPlacement();
        CheckControllerHooks();
        Repaint();
    }

    private void CheckBuildSceneOrder()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        bool isValid = true;
        for (int i = 0; i < ExpectedSceneOrder.Length; i += 1)
        {
            if (i >= scenes.Length || scenes[i].path != ExpectedSceneOrder[i] || !scenes[i].enabled)
            {
                isValid = false;
                AddError($"Build Settings scene {i} should be enabled and set to {ExpectedSceneOrder[i]}.");
                continue;
            }
        }

        if (isValid)
        {
            AddOk("Build Settings scene order is Title -> StageSelect -> Main -> Result.");
        }
    }

    private void CheckAudioResources()
    {
        foreach (AudioResourceCheck audioResource in AudioResources)
        {
            AudioClip clip = Resources.Load<AudioClip>(audioResource.ResourcePath);
            if (clip == null)
            {
                AddError($"{audioResource.Label} BGM is missing. Expected Resources.Load path: {audioResource.ResourcePath} ({audioResource.AssetPath}).");
                continue;
            }

            AddOk($"{audioResource.Label} BGM loads from {audioResource.ResourcePath}: {AssetDatabase.GetAssetPath(clip)}");
        }
    }

    private void CheckSceneSoundManagerPlacement()
    {
        string soundManagerGuid = AssetDatabase.AssetPathToGUID(SoundManagerScriptPath);
        if (string.IsNullOrEmpty(soundManagerGuid))
        {
            AddError($"Could not resolve SoundManager script GUID at {SoundManagerScriptPath}.");
            return;
        }

        foreach (string scenePath in ExpectedSceneOrder)
        {
            string sceneText = File.Exists(scenePath) ? File.ReadAllText(scenePath) : string.Empty;
            if (string.IsNullOrEmpty(sceneText))
            {
                AddError($"Scene file is missing or empty: {scenePath}");
                continue;
            }

            if (sceneText.Contains(soundManagerGuid))
            {
                AddError($"{scenePath} contains a scene-placed SoundManager. BGM should be owned by the runtime singleton only.");
                continue;
            }

            AddOk($"{scenePath} has no scene-placed SoundManager.");
        }
    }

    private void CheckControllerHooks()
    {
        CheckScriptContains("Assets/Scripts/UI/TitleController.cs", "SoundManager.EnsureInstance().PlayTitleBgm()", "Title requests title BGM.");
        CheckScriptContains("Assets/Scripts/UI/StageSelectController.cs", "SoundManager.EnsureInstance().PlayStageSelectBgm()", "StageSelect requests stage select BGM.");
        CheckScriptContains("Assets/Scripts/Game/GameFlowController.cs", "SoundManager.EnsureInstance().PlayBgm()", "Main requests gameplay BGM.");
        CheckScriptContains(SoundManagerScriptPath, "RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)", "SoundManager is bootstrapped before scene load.");
        CheckScriptContains(SoundManagerScriptPath, "_bgmSource.mute = !_isBgmOn", "BGM OFF is applied to the AudioSource when settings change or clips swap.");
    }

    private void CheckScriptContains(string assetPath, string expectedText, string successMessage)
    {
        string text = File.Exists(assetPath) ? File.ReadAllText(assetPath) : string.Empty;
        if (text.Contains(expectedText))
        {
            AddOk(successMessage);
            return;
        }

        AddError($"{assetPath} does not contain expected hook: {expectedText}");
    }

    private void AddOk(string message)
    {
        _results.Add(new CheckResult(CheckStatus.Ok, message));
    }

    private void AddError(string message)
    {
        _results.Add(new CheckResult(CheckStatus.Error, message));
    }

    private readonly struct AudioResourceCheck
    {
        public AudioResourceCheck(string label, string resourcePath, string assetPath)
        {
            Label = label;
            ResourcePath = resourcePath;
            AssetPath = assetPath;
        }

        public string Label { get; }
        public string ResourcePath { get; }
        public string AssetPath { get; }
    }

    private readonly struct CheckResult
    {
        public CheckResult(CheckStatus status, string message)
        {
            Status = status;
            Message = message;
        }

        public CheckStatus Status { get; }
        public string Message { get; }
    }

    private enum CheckStatus
    {
        Ok,
        Warning,
        Error
    }
}
