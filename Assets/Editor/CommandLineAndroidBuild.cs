using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class CommandLineAndroidBuild
{
    private const string DefaultOutputPath = "build.apk";

    [MenuItem("Siwakenja/Build/Android APK")]
    public static void BuildAndroidApk()
    {
        BuildAndroid(false, DefaultOutputPath);
    }

    [MenuItem("Siwakenja/Build/Google Play AAB")]
    public static void BuildGooglePlayAab()
    {
        bool previousCustomKeystore = PlayerSettings.Android.useCustomKeystore;
        string previousKeystore = PlayerSettings.Android.keystoreName;
        string previousAlias = PlayerSettings.Android.keyaliasName;
        string previousStorePassword = PlayerSettings.Android.keystorePass;
        string previousAliasPassword = PlayerSettings.Android.keyaliasPass;
        bool previousDependencyReporting = PlayerSettings.Android.reportGooglePlayAppDependencies;
        try
        {
            if (Environment.GetEnvironmentVariable("SIWAKENJA_SKIP_DEPENDENCY_REPORT") == "1")
            {
                PlayerSettings.Android.reportGooglePlayAppDependencies = false;
                Debug.LogWarning("Building an internal-test candidate without Unity's optional Play dependency metadata. Verify SDK dependencies before production submission.");
            }
            string keystore = Environment.GetEnvironmentVariable("SIWAKENJA_KEYSTORE_PATH");
            if (!string.IsNullOrEmpty(keystore))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = keystore;
                PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("SIWAKENJA_KEY_ALIAS");
                PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("SIWAKENJA_STORE_PASSWORD");
                PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("SIWAKENJA_KEY_PASSWORD");
            }
            ValidateGooglePlaySettings();
            BuildAndroid(true, "Builds/GooglePlay/siwakenja.aab");
        }
        finally
        {
            PlayerSettings.Android.useCustomKeystore = previousCustomKeystore;
            PlayerSettings.Android.keystoreName = previousKeystore;
            PlayerSettings.Android.keyaliasName = previousAlias;
            PlayerSettings.Android.keystorePass = previousStorePassword;
            PlayerSettings.Android.keyaliasPass = previousAliasPassword;
            PlayerSettings.Android.reportGooglePlayAppDependencies = previousDependencyReporting;
            AssetDatabase.SaveAssets();
        }
    }

    [MenuItem("Siwakenja/Build/Check Google Play Settings")]
    public static void ValidateGooglePlaySettings()
    {
        List<string> errors = new();
        string identifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        if (string.IsNullOrWhiteSpace(identifier) || identifier.Contains("DefaultCompany"))
            errors.Add("Set the final Android application identifier.");
        if (!PlayerSettings.Android.useCustomKeystore ||
            string.IsNullOrWhiteSpace(PlayerSettings.Android.keystoreName) ||
            string.IsNullOrWhiteSpace(PlayerSettings.Android.keyaliasName))
            errors.Add("Configure the Google Play upload keystore and alias in Publishing Settings.");
        if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
            errors.Add("Use IL2CPP for the Google Play build.");
        if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
            errors.Add("Enable ARM64 for the Google Play build.");
        if (string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion) || PlayerSettings.Android.bundleVersionCode < 1)
            errors.Add("Set a version name and a positive Android version code.");

        if (errors.Count > 0)
            throw new BuildFailedException("Google Play settings are incomplete:\n- " + string.Join("\n- ", errors));

        Debug.Log("Google Play basic build settings passed. Store declarations, icons, ads, target API and 16 KB compatibility still require release verification.");
    }

    private static void BuildAndroid(bool appBundle, string defaultOutputPath)
    {
        string outputPath = GetArgument("-outputPath") ?? defaultOutputPath;
        string expectedExtension = appBundle ? ".aab" : ".apk";
        if (!string.Equals(Path.GetExtension(outputPath), expectedExtension, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Output path must end with {expectedExtension}.");
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes found in Build Settings.");
        }

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Android,
                BuildTarget.Android);

            if (!switched)
            {
                throw new InvalidOperationException("Failed to switch active build target to Android.");
            }
        }

        BuildPlayerOptions options = new()
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None,
        };

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)));
        bool previousAppBundle = EditorUserBuildSettings.buildAppBundle;
        bool previousExportProject = EditorUserBuildSettings.exportAsGoogleAndroidProject;
        BuildReport report;
        try
        {
            EditorUserBuildSettings.buildAppBundle = appBundle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            report = BuildPipeline.BuildPlayer(options);
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = previousAppBundle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = previousExportProject;
        }
        BuildSummary summary = report.summary;

        Debug.Log(
            $"Android build result: {summary.result}, output: {summary.outputPath}, size: {summary.totalSize} bytes, time: {summary.totalTime}");

        if (summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"Android build failed with result: {summary.result}");
        }
    }

    private static string GetArgument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
