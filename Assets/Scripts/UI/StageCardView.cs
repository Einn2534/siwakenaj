// Created: 2025-02-14
// Updated: 2026-02-26
// Author: Einn

using TMPro;
using UnityEngine;

/// <summary>ステージカード1枚分の表示を更新する。</summary>
public class StageCardView : MonoBehaviour
{
    [SerializeField]
    TMP_Text stageNumberText;

    [SerializeField]
    TMP_Text targetScoreText;

    [SerializeField]
    TMP_Text bestScoreText;

    [SerializeField]
    TMP_Text statusText;

    /// <summary>カードの表示データを一括設定する。</summary>
    /// <param name="stageNumber">ステージ番号。</param>
    /// <param name="targetScore">目標スコア。</param>
    /// <param name="bestScore">ベストスコア。</param>
    /// <param name="status">状態ラベル。</param>
    public void set_data(int stageNumber, int targetScore, int bestScore, string status)
    {
        if (stageNumberText)
        {
            stageNumberText.text = stageNumber.ToString();
        }

        if (targetScoreText)
        {
            targetScoreText.text = targetScore.ToString();
        }

        if (bestScoreText)
        {
            bestScoreText.text = bestScore.ToString();
        }

        if (statusText)
        {
            statusText.text = status ?? "";
        }
    }
}
