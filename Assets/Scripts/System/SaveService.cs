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
        return PlayerPrefs.GetInt(LastGameModeKey, 0) == (int)GameMode.Endless
            ? GameMode.Endless
            : GameMode.Stage;
    }

    public static void SetLastGameMode(GameMode mode)
    {
        PlayerPrefs.SetInt(LastGameModeKey, mode == GameMode.Endless ? (int)GameMode.Endless : (int)GameMode.Stage);
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
        PlayerPrefs.SetInt(GetBestScoreKey(stageNumber), Mathf.Max(DefaultBestScore, score));
    }

    public static int GetBestEndlessScore()
    {
        return PlayerPrefs.GetInt(EndlessBestScoreKey, DefaultBestScore);
    }

    public static void SetBestEndlessScore(int score)
    {
        PlayerPrefs.SetInt(EndlessBestScoreKey, Mathf.Max(DefaultBestScore, score));
    }

    public static int GetStarRating(int stageNumber)
    {
        return StarRatingUtility.Clamp(PlayerPrefs.GetInt(GetStarRatingKey(stageNumber), DefaultStarRating));
    }

    public static void SetStarRating(int stageNumber, int stars)
    {
        PlayerPrefs.SetInt(GetStarRatingKey(stageNumber), StarRatingUtility.Clamp(stars));
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    private static string GetBestScoreKey(int stageNumber)
    {
        return GetStageKey(BestScoreKeyFormat, stageNumber);
    }

    private static string GetStarRatingKey(int stageNumber)
    {
        return GetStageKey(StarRatingKeyFormat, stageNumber);
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
