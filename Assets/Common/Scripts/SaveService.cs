// Created: 2025-02-14
// Author: gpt-5.2-codex

using UnityEngine;

/// <summary>PlayerPrefs を用いた保存処理をまとめる。</summary>
public static class SaveService
{
    private const string BGM_ON_KEY = "BGM_On";
    private const string SE_ON_KEY = "SE_On";
    private const string HOW_TO_SHOWN_KEY = "HowTo_Shown";
    private const string LAST_STAGE_KEY = "LastStage";
    private const string BEST_SCORE_KEY_FORMAT = "BestScore_Stage{0}";
    private const int BOOL_TRUE = 1;
    private const int BOOL_FALSE = 0;
    private const int DEFAULT_STAGE_NUMBER = 1;
    private const int DEFAULT_BEST_SCORE = 0;

    /// <summary>BGM 設定を取得する。</summary>
    /// <returns>BGM が有効なら true。</returns>
    public static bool get_bgm_on()
    {
        return PlayerPrefs.GetInt(BGM_ON_KEY, BOOL_TRUE) == BOOL_TRUE;
    }

    /// <summary>BGM 設定を保存する。</summary>
    /// <param name="isOn">有効なら true。</param>
    public static void set_bgm_on(bool isOn)
    {
        PlayerPrefs.SetInt(BGM_ON_KEY, isOn ? BOOL_TRUE : BOOL_FALSE);
    }

    /// <summary>SE 設定を取得する。</summary>
    /// <returns>SE が有効なら true。</returns>
    public static bool get_se_on()
    {
        return PlayerPrefs.GetInt(SE_ON_KEY, BOOL_TRUE) == BOOL_TRUE;
    }

    /// <summary>SE 設定を保存する。</summary>
    /// <param name="isOn">有効なら true。</param>
    public static void set_se_on(bool isOn)
    {
        PlayerPrefs.SetInt(SE_ON_KEY, isOn ? BOOL_TRUE : BOOL_FALSE);
    }

    /// <summary>HowTo 表示済みフラグを取得する。</summary>
    /// <returns>表示済みなら true。</returns>
    public static bool get_how_to_shown()
    {
        return PlayerPrefs.GetInt(HOW_TO_SHOWN_KEY, BOOL_FALSE) == BOOL_TRUE;
    }

    /// <summary>HowTo 表示済みフラグを保存する。</summary>
    /// <param name="isShown">表示済みなら true。</param>
    public static void set_how_to_shown(bool isShown)
    {
        PlayerPrefs.SetInt(HOW_TO_SHOWN_KEY, isShown ? BOOL_TRUE : BOOL_FALSE);
    }

    /// <summary>前回選択されたステージ番号を取得する。</summary>
    /// <returns>1 から始まるステージ番号。</returns>
    public static int get_last_stage()
    {
        return Mathf.Max(DEFAULT_STAGE_NUMBER, PlayerPrefs.GetInt(LAST_STAGE_KEY, DEFAULT_STAGE_NUMBER));
    }

    /// <summary>前回選択されたステージ番号を保存する。</summary>
    /// <param name="stageNumber">1 から始まるステージ番号。</param>
    public static void set_last_stage(int stageNumber)
    {
        PlayerPrefs.SetInt(LAST_STAGE_KEY, Mathf.Max(DEFAULT_STAGE_NUMBER, stageNumber));
    }

    /// <summary>ステージ別ベストスコアを取得する。</summary>
    /// <param name="stageNumber">1 から始まるステージ番号。</param>
    /// <returns>ベストスコア。</returns>
    public static int get_best_score(int stageNumber)
    {
        string key = get_best_score_key(stageNumber);
        return PlayerPrefs.GetInt(key, DEFAULT_BEST_SCORE);
    }

    /// <summary>ステージ別ベストスコアを保存する。</summary>
    /// <param name="stageNumber">1 から始まるステージ番号。</param>
    /// <param name="score">保存するスコア。</param>
    public static void set_best_score(int stageNumber, int score)
    {
        string key = get_best_score_key(stageNumber);
        PlayerPrefs.SetInt(key, Mathf.Max(DEFAULT_BEST_SCORE, score));
    }

    /// <summary>PlayerPrefs を即時保存する。</summary>
    public static void save()
    {
        PlayerPrefs.Save();
    }

    /// <summary>ベストスコア用のキーを生成する。</summary>
    /// <param name="stageNumber">1 から始まるステージ番号。</param>
    /// <returns>キー文字列。</returns>
    static string get_best_score_key(int stageNumber)
    {
        int safeStageNumber = Mathf.Max(DEFAULT_STAGE_NUMBER, stageNumber);
        return string.Format(BEST_SCORE_KEY_FORMAT, safeStageNumber);
    }
}
