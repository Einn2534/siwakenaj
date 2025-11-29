// Created: 2025-11-28
// Author: gpt-5.1-codex-max

using UnityEngine;

/// <summary>ステージごとの目標値やパラメーターを管理する。</summary>
public class StageManager : MonoBehaviour
{
    private const int DEFAULT_TARGET_SCORE = 100;
    private const int DEFAULT_ALLOWED_MISSES = 3;

    [SerializeField]
    int[] stageTargetScores = { DEFAULT_TARGET_SCORE };

    [SerializeField]
    int[] stageAllowedMisses = { DEFAULT_ALLOWED_MISSES };

    int currentStageIndex;

    /// <summary>選択されたステージの設定を適用する。</summary>
    /// <param name="stageIndex">読み込むステージ番号。</param>
    public void apply_stage(int stageIndex)
    {
        currentStageIndex = Mathf.Clamp(stageIndex, 0, stageTargetScores.Length - 1);
    }

    /// <summary>現在のステージの目標スコアを取得する。</summary>
    /// <returns>クリアに必要なスコア。</returns>
    public int get_target_score()
    {
        return stageTargetScores.Length > 0 ? stageTargetScores[currentStageIndex] : DEFAULT_TARGET_SCORE;
    }

    /// <summary>現在のステージで許容されるミス回数を取得する。</summary>
    /// <returns>ゲームオーバーとなるまでのミス上限。</returns>
    public int get_allowed_misses()
    {
        return stageAllowedMisses.Length > 0 ? stageAllowedMisses[currentStageIndex] : DEFAULT_ALLOWED_MISSES;
    }
}
