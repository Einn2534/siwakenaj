// Created: 2025-05-07
// Author: gpt-5.1-codex-max

using UnityEngine;

/// <summary>Routes UI button presses to the judge with lane awareness.</summary>
public class ButtonInputController : MonoBehaviour
{
    [SerializeField]
    JudgeController judgeController;

    [SerializeField]
    CarSpawner carSpawner;

    [SerializeField]
    CarType laneAType = CarType.LightTruck;

    [SerializeField]
    CarType laneBType = CarType.CompactCar;

    [SerializeField]
    CarType laneCType = CarType.SportsCar;

    /// <summary>Handles the tap for the first lane button.</summary>
    public void press_lane_a()
    {
        handle_press(laneAType);
    }

    /// <summary>Handles the tap for the second lane button.</summary>
    public void press_lane_b()
    {
        handle_press(laneBType);
    }

    /// <summary>Handles the tap for the third lane button.</summary>
    public void press_lane_c()
    {
        handle_press(laneCType);
    }

    /// <summary>Dispatches the press to the judge with the current car.</summary>
    /// <param name="laneType">The lane identifier corresponding to the pressed button.</param>
    void handle_press(CarType laneType)
    {
        if (!judgeController || !carSpawner)
        {
            return;
        }

        judgeController.judge(carSpawner.get_active_car(), laneType);
    }
}
