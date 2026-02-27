// Created: 2026-02-26
// Author: Einn

/// <summary>ステージカード用のデータ。</summary>
[System.Serializable]
public class StageInfo
{
    private const int DEFAULT_STAGE_NUMBER = 1;

    /// <summary>ステージ番号。</summary>
    public int stageNumber = DEFAULT_STAGE_NUMBER;

    /// <summary>目標スコア。</summary>
    public int targetScore = 0;

    /// <summary>実装済みかどうか。</summary>
    public bool isImplemented = true;
}
