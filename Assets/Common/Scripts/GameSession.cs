// Created: 2025-02-14
// Author: gpt-5.2-codex

/// <summary>シーン間で共有するリザルトデータを保持する。</summary>
public static class GameSession
{
    private const int DEFAULT_STAGE_NUMBER = 1;

    public static int stageIndex { get; private set; } = DEFAULT_STAGE_NUMBER;
    public static bool isClear { get; private set; }
    public static int score { get; private set; }
    public static int missCount { get; private set; }
    public static int correctCountA { get; private set; }
    public static int correctCountB { get; private set; }
    public static int correctCountC { get; private set; }

    /// <summary>ステージ番号を保存する。</summary>
    /// <param name="stageNumber">1 から始まるステージ番号。</param>
    public static void set_stage(int stageNumber)
    {
        stageIndex = stageNumber < DEFAULT_STAGE_NUMBER ? DEFAULT_STAGE_NUMBER : stageNumber;
    }

    /// <summary>リザルト用のデータを保存する。</summary>
    /// <param name="stageNumber">1 から始まるステージ番号。</param>
    /// <param name="clear">クリアなら true。</param>
    /// <param name="totalScore">今回スコア。</param>
    /// <param name="misses">ミス回数。</param>
    /// <param name="countA">車種 A の正解数。</param>
    /// <param name="countB">車種 B の正解数。</param>
    /// <param name="countC">車種 C の正解数。</param>
    public static void set_result(
        int stageNumber,
        bool clear,
        int totalScore,
        int misses,
        int countA,
        int countB,
        int countC)
    {
        set_stage(stageNumber);
        isClear = clear;
        score = totalScore;
        missCount = misses;
        correctCountA = countA;
        correctCountB = countB;
        correctCountC = countC;
    }
}
