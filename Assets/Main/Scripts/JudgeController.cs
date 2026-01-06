// Created: 2025-12-01
// Author: gpt-5.1-codex-max

using UnityEngine;

/// <summary>
/// プレイヤー入力と現在の車を照合して結果を処理する。
/// </summary>
public class JudgeController : MonoBehaviour
{
    [SerializeField]
    // スコア管理参照。
    ScoreManager scoreManager;

    [SerializeField]
    // プレイヤーアニメ制御参照。
    PlayerAnimationController playerAnimationController;

    [SerializeField]
    // サウンド制御参照。
    SoundManager soundManager;

    // ゲーム状態参照。
    GameController gameController;

    /// <summary>ゲームコントローラー参照を初期化する。</summary>
    void Awake()
    {
        gameController = FindFirstObjectByType<GameController>();
    }

    /// <summary>
    /// 現在の車と、ボタンで選ばれた車種を判定する。
    /// </summary>
    /// <param name="car">コンベア上の車（存在しない場合は null）。</param>
    /// <param name="expectedLane">押されたボタンに対応する車種。</param>
    public void judge(CarController car, CarType expectedLane)
    {
        if (!is_playing())
        {
            return;
        }

        if (!car)
        {
            scoreManager.apply_miss();
            playerAnimationController.play_cry();
            soundManager.play_miss();
            return;
        }

        CarType actual = car.get_car_type();
        bool isCorrect = actual == expectedLane;

        if (isCorrect)
        {
            scoreManager.apply_success(expectedLane);
            playerAnimationController.play_happy();
            soundManager.play_correct();
            Destroy(car.gameObject);
        }
        else
        {
            scoreManager.apply_miss();
            playerAnimationController.play_cry();
            soundManager.play_miss();
        }
    }

    /// <summary>ゲームがプレイ中かどうかを確認する。</summary>
    /// <returns>プレイ中なら true。</returns>
    bool is_playing()
    {
        if (!gameController)
        {
            gameController = FindFirstObjectByType<GameController>();
        }

        return gameController != null && gameController.is_playing();
    }
}
