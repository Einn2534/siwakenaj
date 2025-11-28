// Created: 2025-11-28
// Author: gpt-5.1-codex-max

using UnityEngine;

/// <summary>プレイヤー入力と現在の車を照合して結果を処理する。</summary>
public class JudgeController : MonoBehaviour
{
    [SerializeField]
    ScoreManager scoreManager;

    [SerializeField]
    PlayerAnimationController playerAnimationController;

    [SerializeField]
    SoundManager soundManager;

    /// <summary>押下された車種が現在の車と一致するかを判定する。</summary>
    /// <param name="car">コンベア上に存在する車。</param>
    /// <param name="expectedLane">プレイヤーが選んだ車種。</param>
    public void judge(CarController car, CarType expectedLane)
    {
        if (!car)
        {
            return;
        }

        bool isCorrect = car.get_car_type() == expectedLane;
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
}
