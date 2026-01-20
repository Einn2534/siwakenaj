// Created: 2025-05-07
// Updated: 2025-12-01
// Author: gpt-5.1-codex-max

using System.Collections;
using UnityEngine;

/// <summary>UIボタンの入力を判定処理へ中継する。</summary>
public class ButtonInputController : MonoBehaviour
{
    private const float INPUT_COOLDOWN_SECONDS = 0.08f;
    private const float INITIAL_LAST_INPUT_TIME = -1f;
    private const int INITIAL_FRAME = -1;

    [SerializeField]
    // 判定処理の参照。
    JudgeController judgeController;

    [SerializeField]
    // スポーン管理の参照。
    CarSpawner carSpawner;

    [SerializeField]
    // レーンAの車種。
    CarType laneAType = CarType.LightTruck;

    [SerializeField]
    // レーンBの車種。
    CarType laneBType = CarType.CompactCar;

    [SerializeField]
    // レーンCの車種。
    CarType laneCType = CarType.SportsCar;

    // ゲーム状態参照。
    GameController gameController;
    // 最終入力時刻。
    float lastInputTime = INITIAL_LAST_INPUT_TIME;
    // 保留中入力のフレーム番号。
    int pendingFrame = INITIAL_FRAME;
    // 保留中入力の車種。
    CarType pendingLaneType;
    // 保留入力処理用コルーチン。
    Coroutine pendingCoroutine;

    /// <summary>ゲームコントローラー参照を初期化する。</summary>
    void Awake()
    {
        gameController = FindObjectOfType<GameController>();
    }

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
        if (!gameController)
        {
            gameController = FindObjectOfType<GameController>();
        }

        return gameController != null && gameController.is_playing();
    }
}
