using System;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class LocalGameplayEvent
{
    public string EventName;
    public string UtcTimestamp;
    public string Mode;
    public int StageNumber;
    public bool FirstAttempt;
    public bool Cleared;
    public string FailureReason;
    public float PlayTimeSeconds;
    public int MissCount;
    public int MaxCombo;
    public bool Retried;
    public bool RewardedContinueSelected;
}

public static class LocalTelemetryService
{
    private const string AttemptKeyFormat = "Telemetry_Attempted_{0}_{1}";
    private const string FileName = "siwakenja-events-v1.jsonl";

    public static string OutputPath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool BeginAttempt(GameMode mode, int stageNumber)
    {
        string key = string.Format(AttemptKeyFormat, (int)mode, StageNumberUtility.Normalize(stageNumber));
        bool firstAttempt = PlayerPrefs.GetInt(key, 0) == 0;
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        return firstAttempt;
    }

    public static void RecordResult(GameResultData result, bool firstAttempt)
    {
        if (result == null)
        {
            return;
        }

        Append(new LocalGameplayEvent
        {
            EventName = "run_result",
            UtcTimestamp = DateTime.UtcNow.ToString("O"),
            Mode = result.Mode.ToString(),
            StageNumber = result.StageNumber,
            FirstAttempt = firstAttempt,
            Cleared = result.IsClear,
            FailureReason = result.IsClear ? string.Empty : "miss_limit",
            PlayTimeSeconds = result.PlayTimeSeconds,
            MissCount = result.MissCount,
            MaxCombo = result.MaxCombo
        });
    }

    public static void RecordRetry(GameResultData result)
    {
        RecordAction("retry", result, retried: true, rewardedContinueSelected: false);
    }

    public static void RecordRewardedContinueSelected(GameResultData result)
    {
        RecordAction("rewarded_continue_selected", result, retried: false, rewardedContinueSelected: true);
    }

    private static void RecordAction(string eventName, GameResultData result, bool retried, bool rewardedContinueSelected)
    {
        Append(new LocalGameplayEvent
        {
            EventName = eventName,
            UtcTimestamp = DateTime.UtcNow.ToString("O"),
            Mode = result != null ? result.Mode.ToString() : SessionState.SelectedGameMode.ToString(),
            StageNumber = result != null ? result.StageNumber : SessionState.SelectedStageNumber,
            Retried = retried,
            RewardedContinueSelected = rewardedContinueSelected
        });
    }

    private static void Append(LocalGameplayEvent gameplayEvent)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            File.AppendAllText(OutputPath, JsonUtility.ToJson(gameplayEvent) + Environment.NewLine);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Local telemetry write failed: {exception.Message}");
        }
    }
}
