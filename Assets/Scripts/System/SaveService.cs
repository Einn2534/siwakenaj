using UnityEngine;

public static class SaveService
{
    private const string BgmOnKey = "BGM_On";
    private const string SeOnKey = "SE_On";
    private const string BgmVolumeKey = "BGM_Volume";
    private const string SeVolumeKey = "SE_Volume";
    private const string VibrationOnKey = "Vibration_On";
    private const string HowToShownKey = "HowTo_Shown";
    private const string TutorialCompletedKey = "Tutorial_Completed";
    private const string TutorialSkippedKey = "Tutorial_Skipped";
    private const string SelectedStageKey = "SelectedStage";
    private const string LastStageKey = "LastStage";
    private const string LastGameModeKey = "LastGameMode";
    private const string EndlessBestScoreKey = "BestScore_Endless";
    private const string BestScoreKeyFormat = "BestScore_Stage{0}";
    private const string StarRatingKeyFormat = "StarRating_Stage{0}";
    private const string StageClearKeyFormat = "StageClear_{0}";
    private const string BestTimeKeyFormat = "BestTime_Stage{0}";
    private const string GimmickHintSeenKeyFormat = "GimmickHintSeen_{0}";
    private const string ProgressMigrationVersionKey = "ProgressMigrationVersion";
    private const int CurrentProgressMigrationVersion = 1;
    private const int BoolTrue = 1;
    private const int BoolFalse = 0;
    private const int DefaultBestScore = 0;
    private const int DefaultStarRating = 0;
    private const float DefaultVolume = 1f;

    public static bool GetBgmOn()
    {
        return GetBool(BgmOnKey, true);
    }

    public static void SetBgmOn(bool isOn)
    {
        SetBool(BgmOnKey, isOn);
    }

    public static bool GetSeOn()
    {
        return GetBool(SeOnKey, true);
    }

    public static void SetSeOn(bool isOn)
    {
        SetBool(SeOnKey, isOn);
    }

    public static float GetBgmVolume()
    {
        return GetVolume(BgmVolumeKey);
    }

    public static void SetBgmVolume(float volume)
    {
        SetVolume(BgmVolumeKey, volume);
    }

    public static float GetSeVolume()
    {
        return GetVolume(SeVolumeKey);
    }

    public static void SetSeVolume(float volume)
    {
        SetVolume(SeVolumeKey, volume);
    }

    public static bool GetVibrationOn()
    {
        return GetBool(VibrationOnKey, true);
    }

    public static void SetVibrationOn(bool isOn)
    {
        SetBool(VibrationOnKey, isOn);
    }

    public static bool GetHowToShown()
    {
        return GetBool(HowToShownKey, false);
    }

    public static void SetHowToShown(bool isShown)
    {
        SetBool(HowToShownKey, isShown);
    }

    public static bool GetTutorialCompleted()
    {
        return GetBool(TutorialCompletedKey, false);
    }

    public static void SetTutorialCompleted(bool isCompleted)
    {
        SetBool(TutorialCompletedKey, isCompleted);
    }

    public static bool GetTutorialSkipped()
    {
        return GetBool(TutorialSkippedKey, false);
    }

    public static void SetTutorialSkipped(bool isSkipped)
    {
        SetBool(TutorialSkippedKey, isSkipped);
    }

    public static int GetLastStage()
    {
        return GetStageNumber(LastStageKey);
    }

    public static void SetLastStage(int stageNumber)
    {
        SetStageNumber(LastStageKey, stageNumber);
    }

    public static GameMode GetLastGameMode()
    {
        int raw = PlayerPrefs.GetInt(LastGameModeKey, 0);
        return raw == (int)GameMode.Endless
            ? GameMode.Endless
            : raw == (int)GameMode.Daily ? GameMode.Daily : GameMode.Stage;
    }

    public static void SetLastGameMode(GameMode mode)
    {
        PlayerPrefs.SetInt(LastGameModeKey, (int)mode);
    }

    public static int GetSelectedStage()
    {
        return GetStageNumber(SelectedStageKey);
    }

    public static void SetSelectedStage(int stageNumber)
    {
        SetStageNumber(SelectedStageKey, stageNumber);
    }

    public static int GetBestScore(int stageNumber)
    {
        return PlayerPrefs.GetInt(GetBestScoreKey(stageNumber), DefaultBestScore);
    }

    public static void SetBestScore(int stageNumber, int score)
    {
        int bestScore = Mathf.Max(GetBestScore(stageNumber), score);
        PlayerPrefs.SetInt(GetBestScoreKey(stageNumber), Mathf.Max(DefaultBestScore, bestScore));
    }

    public static int GetBestEndlessScore()
    {
        return PlayerPrefs.GetInt(EndlessBestScoreKey, DefaultBestScore);
    }

    public static void SetBestEndlessScore(int score)
    {
        PlayerPrefs.SetInt(EndlessBestScoreKey, Mathf.Max(GetBestEndlessScore(), score));
    }

    public static int GetStarRating(int stageNumber)
    {
        return StarRatingUtility.Clamp(PlayerPrefs.GetInt(GetStarRatingKey(stageNumber), DefaultStarRating));
    }

    public static void SetStarRating(int stageNumber, int stars)
    {
        int bestStars = Mathf.Max(GetStarRating(stageNumber), stars);
        PlayerPrefs.SetInt(GetStarRatingKey(stageNumber), StarRatingUtility.Clamp(bestStars));
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    public static bool HasSeenGimmickHint(CarModifier modifier)
    {
        return modifier == CarModifier.Normal
            || GetBool(GetGimmickHintSeenKey(modifier), false);
    }

    public static void SetGimmickHintSeen(CarModifier modifier)
    {
        if (modifier == CarModifier.Normal)
        {
            return;
        }

        SetBool(GetGimmickHintSeenKey(modifier), true);
        Save();
    }

    private static string GetBestScoreKey(int stageNumber)
    {
        return GetStageKey(BestScoreKeyFormat, stageNumber);
    }

    public static bool GetStageCleared(int stageNumber)
    {
        return GetBool(GetStageClearKey(stageNumber), false);
    }

    public static void SetStageCleared(int stageNumber, bool isCleared)
    {
        SetBool(GetStageClearKey(stageNumber), isCleared);
    }

    public static float GetBestTime(int stageNumber)
    {
        return Mathf.Max(0f, PlayerPrefs.GetFloat(GetBestTimeKey(stageNumber), 0f));
    }

    public static void SetBestTime(int stageNumber, float seconds)
    {
        if (seconds <= 0f)
        {
            return;
        }

        string key = GetBestTimeKey(stageNumber);
        float current = GetBestTime(stageNumber);
        if (current <= 0f || seconds < current)
        {
            PlayerPrefs.SetFloat(key, seconds);
        }
    }

    public static void RecordStageResult(GameResultData result, StageDefinition stageDefinition, int stars)
    {
        if (result == null || result.Mode != GameMode.Stage)
        {
            return;
        }

        int stageNumber = StageNumberUtility.Normalize(result.StageNumber);
        if (result.Score > GetBestScore(stageNumber))
        {
            SetBestScore(stageNumber, result.Score);
        }

        if (stars > GetStarRating(stageNumber))
        {
            SetStarRating(stageNumber, stars);
        }

        if (result.IsClear)
        {
            SetStageCleared(stageNumber, true);
            SetBestTime(stageNumber, result.PlayTimeSeconds);
        }

        Save();
    }

    public static int GetHighestClearedStageNumber(StageDatabase database)
    {
        if (database == null)
        {
            return StageNumberUtility.MinimumStageNumber;
        }

        StageDefinition highest = database.GetHighestClearedStage(GetStageCleared);
        return highest != null ? StageNumberUtility.Normalize(highest.StageNumber) : StageNumberUtility.MinimumStageNumber;
    }

    public static bool IsExtendedModeUnlocked()
    {
        return GetStageCleared(5);
    }

    public static void EnsureProgressMigration(StageDatabase database)
    {
        if (database == null || PlayerPrefs.GetInt(ProgressMigrationVersionKey, 0) >= CurrentProgressMigrationVersion)
        {
            return;
        }

        foreach (StageDefinition stage in database.Stages)
        {
            if (stage == null || stage.StageNumber > 5 || GetStageCleared(stage.StageNumber))
            {
                continue;
            }

            int legacyTarget = stage.LegacyClearScore > 0 ? stage.LegacyClearScore : stage.TargetScore;
            if (legacyTarget > 0 && GetBestScore(stage.StageNumber) >= legacyTarget)
            {
                SetStageCleared(stage.StageNumber, true);
            }
        }

        PlayerPrefs.SetInt(ProgressMigrationVersionKey, CurrentProgressMigrationVersion);
        Save();
    }

    private static string GetStarRatingKey(int stageNumber)
    {
        return GetStageKey(StarRatingKeyFormat, stageNumber);
    }

    private static string GetStageClearKey(int stageNumber)
    {
        return GetStageKey(StageClearKeyFormat, stageNumber);
    }

    private static string GetBestTimeKey(int stageNumber)
    {
        return GetStageKey(BestTimeKeyFormat, stageNumber);
    }

    private static string GetGimmickHintSeenKey(CarModifier modifier)
    {
        return string.Format(GimmickHintSeenKeyFormat, modifier);
    }

    private static int GetStageNumber(string key)
    {
        return StageNumberUtility.Normalize(PlayerPrefs.GetInt(key, StageNumberUtility.MinimumStageNumber));
    }

    private static void SetStageNumber(string key, int stageNumber)
    {
        PlayerPrefs.SetInt(key, StageNumberUtility.Normalize(stageNumber));
    }

    private static string GetStageKey(string keyFormat, int stageNumber)
    {
        return string.Format(keyFormat, StageNumberUtility.Normalize(stageNumber));
    }

    private static bool GetBool(string key, bool defaultValue)
    {
        int defaultInt = defaultValue ? BoolTrue : BoolFalse;
        return PlayerPrefs.GetInt(key, defaultInt) == BoolTrue;
    }

    private static void SetBool(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? BoolTrue : BoolFalse);
    }

    private static float GetVolume(string key)
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(key, DefaultVolume));
    }

    private static void SetVolume(string key, float volume)
    {
        PlayerPrefs.SetFloat(key, Mathf.Clamp01(volume));
    }
}
