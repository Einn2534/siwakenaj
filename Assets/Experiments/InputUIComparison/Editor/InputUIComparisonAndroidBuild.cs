using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Siwakenja.Experiments.InputUIComparison.Editor
{
    /// <summary>
    /// Builds a development APK containing only the isolated Input UI Comparison scene.
    /// This intentionally does not mutate the project's normal Build Settings scene list.
    /// </summary>
    public static class InputUIComparisonAndroidBuild
    {
        private const string DefaultOutputPath = "Builds/InputUIComparison-P00.apk";

        [MenuItem("Tools/しわ賢者/Input UI Comparison/Build P00 Android APK")]
        public static void BuildDevelopmentApk()
        {
            InputUIComparisonSceneBuilder.RebuildScene();
            string outputArgument = GetArgument("-buildOutput") ?? GetArgument("-outputPath");
            string outputPath = Path.GetFullPath(outputArgument ?? DefaultOutputPath);
            string outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            if (!File.Exists(InputUIComparisonSceneBuilder.ScenePath))
            {
                throw new FileNotFoundException(
                    "Input UI Comparison scene was not found.",
                    InputUIComparisonSceneBuilder.ScenePath);
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
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { InputUIComparisonSceneBuilder.ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log(
                $"P00 Android build result: {summary.result}, output: {summary.outputPath}, " +
                $"size: {summary.totalSize} bytes, time: {summary.totalTime}");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"P00 Android build failed: {summary.result}");
            }
        }

        private static string GetArgument(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (arguments[index].Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[index + 1];
                }
            }

            return null;
        }
    }
}
