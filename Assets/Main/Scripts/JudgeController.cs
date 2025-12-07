using UnityEngine;

/// <summary>
/// プレイヤー入力と現在の車を照合して結果を処理する。
/// </summary>
public class JudgeController : MonoBehaviour
{
    [SerializeField]
    ScoreManager scoreManager;

    [SerializeField]
    PlayerAnimationController playerAnimationController;

    [SerializeField]
    SoundManager soundManager;

    /// <summary>
    /// 現在の車と、ボタンで選ばれた車種を判定する。
    /// </summary>
    /// <param name="car">コンベア上の車（存在しない場合は null）。</param>
    /// <param name="expectedLane">押されたボタンに対応する車種。</param>
    public void judge(CarController car, CarType expectedLane)
    {
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

}
