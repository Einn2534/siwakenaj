using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class CommandLineAndroidBuild
{
    private const string DefaultOutputPath = "build.apk";

    [MenuItem("Siwakenja/Build/Android APK")]
    public static void BuildAndroidApk()
    {
        string outputPath = GetArgument("-outputPath") ?? DefaultOutputPath;
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

        EditorUserBuildSettings.buildAppBundle = false;

        BuildPlayerOptions options = new()
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
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
