using UnityEngine;

public class JudgeController : MonoBehaviour
{
    public JudgeResult Evaluate(CarController car, CarType expectedLaneType)
    {
        return JudgeEvaluator.Evaluate(car != null ? car.CarType : null, expectedLaneType);
    }
}
