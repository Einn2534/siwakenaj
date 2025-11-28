// Created: 2025-05-07
// Author: gpt-5.1-codex-max

using UnityEngine;

/// <summary>UIボタンの入力を判定処理へ中継する。</summary>
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

    /// <summary>1番目のボタンが押された際の処理。</summary>
    public void press_lane_a()
    {
        handle_press(laneAType);
    }

    /// <summary>2番目のボタンが押された際の処理。</summary>
    public void press_lane_b()
    {
        handle_press(laneBType);
    }

    /// <summary>3番目のボタンが押された際の処理。</summary>
    public void press_lane_c()
    {
        handle_press(laneCType);
    }

    /// <summary>現在の車と入力された車種を判定ロジックへ渡す。</summary>
    /// <param name="laneType">押下されたボタンに対応する車種。</param>
    void handle_press(CarType laneType)
    {
        if (!judgeController || !carSpawner)
        {
            return;
        }

        judgeController.judge(carSpawner.get_active_car(), laneType);
    }
}
