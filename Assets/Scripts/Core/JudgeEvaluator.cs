public static class JudgeEvaluator
{
    public static JudgeResult Evaluate(CarType? actualCarType, CarType expectedLaneType)
    {
        if (!actualCarType.HasValue)
        {
            return JudgeResult.NoCar;
        }

        return actualCarType.Value == expectedLaneType
            ? JudgeResult.Correct
            : JudgeResult.WrongLane;
    }
}
