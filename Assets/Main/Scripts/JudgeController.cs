// Created: 2025-11-28
// Author: gpt-5.1-codex-max

using UnityEngine;

/// <summary>Evaluates player input against the current car.</summary>
public class JudgeController : MonoBehaviour
{
    [SerializeField]
    ScoreManager scoreManager;

    [SerializeField]
    PlayerAnimationController playerAnimationController;

    [SerializeField]
    SoundManager soundManager;

    /// <summary>Checks whether the pressed lane matches the car type.</summary>
    /// <param name="car">Car currently on the belt.</param>
    /// <param name="expectedLane">Lane identifier chosen by the player.</param>
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
