// Created: 2025-02-14
// Author: gpt-5.2-codex

using TMPro;
using UnityEngine;

/// <summary>ステージカードの表示を更新する。</summary>
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

    /// <summary>カード表示内容を更新する。</summary>
    /// <param name="stageNumber">ステージ番号。</param>
    /// <param name="targetScore">目標スコア。</param>
    /// <param name="bestScore">ベストスコア。</param>
    /// <param name="status">状態表示。</param>
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
            statusText.text = status;
        }
    }
}
