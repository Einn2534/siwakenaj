using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// File-backed results survive the domain reload that disconnects a synchronous CLI request.
[InitializeOnLoad]
public sealed class SortingThemeTestRunner : ICallbacks
{
    private const string Key = "SortingThemes.TestOutput";
    static SortingThemeTestRunner() => TestRunnerApi.RegisterTestCallback(new SortingThemeTestRunner());

    public static string Run(string mode, string filter, string runName)
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("A test run is already active.");
        string output = Path.GetFullPath("Deliverables/SortingThemes/Tests/" + runName);
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        UnityEditor.SessionState.SetString(Key, output);
        File.WriteAllText(output + ".progress", "Starting " + DateTime.UtcNow.ToString("O") + "\n");
        Filter selection = new() { testMode = mode == "EditMode" ? TestMode.EditMode : TestMode.PlayMode };
        if (!string.IsNullOrEmpty(filter)) selection.testNames = filter.Split(';');
        string id = ScriptableObject.CreateInstance<TestRunnerApi>().Execute(new ExecutionSettings(selection));
        File.AppendAllText(output + ".progress", "RunId=" + id + "\n");
        return id;
    }

    public void RunStarted(ITestAdaptor testsToRun) { }
    public void TestStarted(ITestAdaptor test)
    {
        string output = UnityEditor.SessionState.GetString(Key, "");
        if (output.Length > 0 && !test.IsSuite) File.AppendAllText(output + ".progress", "RUN " + test.FullName + "\n");
    }
    public void TestFinished(ITestResultAdaptor result)
    {
        string output = UnityEditor.SessionState.GetString(Key, "");
        if (output.Length > 0 && !result.Test.IsSuite)
            File.AppendAllText(output + ".progress", result.TestStatus + " " + result.Test.FullName + " " + result.Message + "\n" + (result.TestStatus == TestStatus.Failed ? result.StackTrace : ""));
    }
    public void RunFinished(ITestResultAdaptor result)
    {
        string output = UnityEditor.SessionState.GetString(Key, "");
        if (output.Length == 0) return;
        TestRunnerApi.SaveResultToFile(result, output + ".xml");
        File.AppendAllText(output + ".progress", $"COMPLETE {result.TestStatus} passed={result.PassCount} failed={result.FailCount} skipped={result.SkipCount}\n");
        UnityEditor.SessionState.EraseString(Key);
    }
}
