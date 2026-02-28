// Created: 2025-12-01
// Updated: 2026-02-26
// Author: Einn

using UnityEngine;

/// <summary>プレイヤー入力と現在の車を照合して結果を処理する。</summary>
public class JudgeController : MonoBehaviour
{
    [SerializeField]
    ScoreManager scoreManager;

    [SerializeField]
    PlayerAnimationController playerAnimationController;

    [SerializeField]
    GameController gameController;

    /// <summary>現在の車と、ボタンで選ばれた車種を判定する。</summary>
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
            if (scoreManager)
            {
                scoreManager.apply_miss();
            }

            if (playerAnimationController)
            {
                playerAnimationController.play_cry();
            }

            SoundManager.instance?.play_miss();
            return;
        }

        CarType actual = car.get_car_type();
        bool isCorrect = actual == expectedLane;

        if (isCorrect)
        {
            if (scoreManager)
            {
                scoreManager.apply_success(expectedLane);
            }

            if (playerAnimationController)
            {
                playerAnimationController.play_happy();
            }

            SoundManager.instance?.play_correct();
            Destroy(car.gameObject);
        }
        else
        {
            if (scoreManager)
            {
                scoreManager.apply_miss();
            }

            if (playerAnimationController)
            {
                playerAnimationController.play_cry();
            }

            SoundManager.instance?.play_miss();
        }
    }

    /// <summary>ゲームがプレイ中かどうかを確認する。</summary>
    /// <returns>プレイ中なら true。</returns>
    bool is_playing()
    {
        return gameController != null && gameController.is_playing();
    }
}
