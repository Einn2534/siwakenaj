using UnityEngine;

public static class SaveService
{
    private const string BgmOnKey = "BGM_On";
    private const string SeOnKey = "SE_On";
    private const string HowToShownKey = "HowTo_Shown";
    private const string LastStageKey = "LastStage";
    private const string BestScoreKeyFormat = "BestScore_Stage{0}";
    private const int BoolTrue = 1;
    private const int BoolFalse = 0;
    private const int DefaultStageNumber = 1;
    private const int DefaultBestScore = 0;

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

    public static bool GetHowToShown()
    {
        return GetBool(HowToShownKey, false);
    }

    public static void SetHowToShown(bool isShown)
    {
        SetBool(HowToShownKey, isShown);
    }

    public static int GetLastStage()
    {
        return Mathf.Max(DefaultStageNumber, PlayerPrefs.GetInt(LastStageKey, DefaultStageNumber));
    }

    public static void SetLastStage(int stageNumber)
    {
        PlayerPrefs.SetInt(LastStageKey, Mathf.Max(DefaultStageNumber, stageNumber));
    }

    public static int GetBestScore(int stageNumber)
    {
        return PlayerPrefs.GetInt(GetBestScoreKey(stageNumber), DefaultBestScore);
    }

    public static void SetBestScore(int stageNumber, int score)
    {
        PlayerPrefs.SetInt(GetBestScoreKey(stageNumber), Mathf.Max(DefaultBestScore, score));
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    private static string GetBestScoreKey(int stageNumber)
    {
        return string.Format(BestScoreKeyFormat, Mathf.Max(DefaultStageNumber, stageNumber));
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
}
