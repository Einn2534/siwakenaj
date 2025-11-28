// Created: 2025-05-07
// Author: gpt-5.1-codex-max

using UnityEngine;

/// <summary>Routes UI button presses to the judge with lane awareness.</summary>
public class ButtonInputController : MonoBehaviour
{
    private const int LANE_INDEX_A = 0;
    private const int LANE_INDEX_B = 1;
    private const int LANE_INDEX_C = 2;

    [SerializeField]
    JudgeController judgeController;

    [SerializeField]
    CarSpawner carSpawner;

    /// <summary>Handles the tap for the first lane button.</summary>
    public void press_lane_a()
    {
        handle_press(LANE_INDEX_A);
    }

    /// <summary>Handles the tap for the second lane button.</summary>
    public void press_lane_b()
    {
        handle_press(LANE_INDEX_B);
    }

    /// <summary>Handles the tap for the third lane button.</summary>
    public void press_lane_c()
    {
        handle_press(LANE_INDEX_C);
    }

    /// <summary>Dispatches the press to the judge with the current car.</summary>
    /// <param name="laneIndex">The lane identifier corresponding to the pressed button.</param>
    void handle_press(int laneIndex)
    {
        if (!judgeController || !carSpawner)
        {
            return;
        }

        judgeController.judge(carSpawner.get_active_car(), laneIndex);
    }
}
