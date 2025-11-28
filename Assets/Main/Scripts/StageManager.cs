// Created: 2025-11-28
// Author: gpt-5.1-codex-max

using UnityEngine;

/// <summary>Provides stage-specific targets and parameters.</summary>
public class StageManager : MonoBehaviour
{
    private const int DEFAULT_TARGET_SCORE = 100;
    private const int DEFAULT_ALLOWED_MISSES = 3;

    [SerializeField]
    int[] stageTargetScores = { DEFAULT_TARGET_SCORE };

    [SerializeField]
    int[] stageAllowedMisses = { DEFAULT_ALLOWED_MISSES };

    int currentStageIndex;

    /// <summary>Applies stored parameters for the chosen stage.</summary>
    /// <param name="stageIndex">Stage index to load.</param>
    public void apply_stage(int stageIndex)
    {
        currentStageIndex = Mathf.Clamp(stageIndex, 0, stageTargetScores.Length - 1);
    }

    /// <summary>Gets the target score for the active stage.</summary>
    /// <returns>Score threshold for clearing.</returns>
    public int get_target_score()
    {
        return stageTargetScores.Length > 0 ? stageTargetScores[currentStageIndex] : DEFAULT_TARGET_SCORE;
    }

    /// <summary>Gets the allowed misses for the active stage.</summary>
    /// <returns>Maximum miss count before failure.</returns>
    public int get_allowed_misses()
    {
        return stageAllowedMisses.Length > 0 ? stageAllowedMisses[currentStageIndex] : DEFAULT_ALLOWED_MISSES;
    }
}
