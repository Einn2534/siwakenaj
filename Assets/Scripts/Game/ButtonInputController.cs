// Created: 2025-05-07
// Updated: 2026-02-26
// Author: Einn

using System.Collections;
using UnityEngine;

/// <summary>UIボタンの入力を判定処理へ中継する。</summary>
public class ButtonInputController : MonoBehaviour
{
    private const float INPUT_COOLDOWN_SECONDS = 0.08f;
    private const float INITIAL_LAST_INPUT_TIME = -1f;
    private const int INITIAL_FRAME = -1;

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

    [SerializeField]
    GameController gameController;

    float lastInputTime = INITIAL_LAST_INPUT_TIME;
    int pendingFrame = INITIAL_FRAME;
    CarType pendingLaneType;
    Coroutine pendingCoroutine;

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

        if (!is_playing())
        {
            return;
        }

        if (Time.time - lastInputTime < INPUT_COOLDOWN_SECONDS)
        {
            return;
        }

        pendingLaneType = laneType;
        pendingFrame = Time.frameCount;

        if (pendingCoroutine == null)
        {
            pendingCoroutine = StartCoroutine(process_pending_input());
        }
    }

    /// <summary>同一フレーム内の最後の入力のみを判定に渡す。</summary>
    /// <returns>コルーチン。</returns>
    IEnumerator process_pending_input()
    {
        int frame = pendingFrame;
        yield return new WaitForEndOfFrame();

        if (frame == pendingFrame)
        {
            if (is_playing())
            {
                judgeController.judge(carSpawner.get_active_car(), pendingLaneType);
                lastInputTime = Time.time;
            }
        }

        pendingCoroutine = null;
    }

    /// <summary>ゲームがプレイ中かどうかを確認する。</summary>
    /// <returns>プレイ中なら true。</returns>
    bool is_playing()
    {
        return gameController != null && gameController.is_playing();
    }
}
