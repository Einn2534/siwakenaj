// Created: 2025-11-28
// Author: gpt-5.1-codex-max

using System.Collections.Generic;
using UnityEngine;

/// <summary>Tracks score, mistakes, and per-car success counts.</summary>
public class ScoreManager : MonoBehaviour
{
    private const int SCORE_PER_CORRECT = 10;
    private const int SCORE_PER_MISS = -5;

    [SerializeField]
    int targetScore;

    [SerializeField]
    int allowedMisses = 3;

    [SerializeField]
    ScoreLaneUI scoreLaneUi;

    int currentScore;
    int missCount;
    readonly Dictionary<CarType, int> laneCounts = new();

    /// <summary>Resets score and miss counters for a new stage.</summary>
    /// <param name="stageTargetScore">Score required to clear the stage.</param>
    /// <param name="maxMisses">Number of misses allowed.</param>
    public void reset_metrics(int stageTargetScore, int maxMisses)
    {
        targetScore = stageTargetScore;
        allowedMisses = maxMisses;
        currentScore = 0;
        missCount = 0;
        laneCounts.Clear();
        scoreLaneUi.reset_all();
    }

    /// <summary>Processes a successful sort for a specific lane.</summary>
    /// <param name="laneType">Lane identifier that was correctly chosen.</param>
    public void apply_success(CarType laneType)
    {
        currentScore += SCORE_PER_CORRECT;
        increment_lane(laneType);
        scoreLaneUi.update_lane(laneType, laneCounts[laneType]);
        check_clear();
    }

    /// <summary>Processes a miss and checks for game over.</summary>
    public void apply_miss()
    {
        currentScore += SCORE_PER_MISS;
        missCount += 1;

        if (missCount >= allowedMisses)
        {
            GameController controller = FindObjectOfType<GameController>();
            if (controller)
            {
                controller.handle_game_over();
            }
        }
    }

    /// <summary>Checks whether the current score meets the target.</summary>
    void check_clear()
    {
        if (currentScore >= targetScore)
        {
            GameController controller = FindObjectOfType<GameController>();
            if (controller)
            {
                controller.finish_stage();
            }
        }
    }

    /// <summary>Increments the stored lane success count.</summary>
    /// <param name="laneType">Lane identifier to increment.</param>
    void increment_lane(CarType laneType)
    {
        if (!laneCounts.ContainsKey(laneType))
        {
            laneCounts[laneType] = 0;
        }

        laneCounts[laneType] += 1;
    }

    /// <summary>Gets the current miss count.</summary>
    /// <returns>Total accumulated misses.</returns>
    public int get_miss_count()
    {
        return missCount;
    }
}
