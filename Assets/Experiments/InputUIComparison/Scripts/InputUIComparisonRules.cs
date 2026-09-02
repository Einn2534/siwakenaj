namespace Siwakenja.Experiments.InputUIComparison
{
    public static class InputUIComparisonRules
    {
        public const float GlobalInputCooldownSeconds = 0.08f;
        public const float ExpressSpeedMultiplier = 1.55f;
        public const float BrokenSpeedMultiplier = 0.82f;
        public const float CoveredRevealProgress = 0.52f;
        public const float LowSpeedNormalizedPerSecond = 0.68f;
        public const float NormalSpeedNormalizedPerSecond = 0.84f;
        public const float HighSpeedNormalizedPerSecond = 0.98f;
        public const float NormalizedTravelDistance = 1.10f;

        public static ExperimentInput ExpectedInput(ExperimentVehicle vehicle, ExperimentSpecialState state)
        {
            if (state == ExperimentSpecialState.Broken)
            {
                return ExperimentInput.Repair;
            }

            switch (vehicle)
            {
                case ExperimentVehicle.Sports: return ExperimentInput.Sports;
                case ExperimentVehicle.Compact: return ExperimentInput.Compact;
                case ExperimentVehicle.LightTruck: return ExperimentInput.LightTruck;
                default: return ExperimentInput.None;
            }
        }

        public static bool IsVehicleInput(ExperimentInput input)
        {
            return input == ExperimentInput.Sports
                || input == ExperimentInput.Compact
                || input == ExperimentInput.LightTruck;
        }

        public static bool IsActionable(ExperimentSpecialState state, bool coveredRevealed)
        {
            return state != ExperimentSpecialState.Covered || coveredRevealed;
        }

        public static InputEvaluation Evaluate(
            ExperimentVehicle vehicle,
            ExperimentSpecialState state,
            bool coveredRevealed,
            ExperimentInput input)
        {
            if (state == ExperimentSpecialState.Covered && !coveredRevealed)
            {
                if (input == ExperimentInput.Repair)
                {
                    return new InputEvaluation(false, true, "修理は不要");
                }

                return new InputEvaluation(false, true, "まだ見えない");
            }

            if (state == ExperimentSpecialState.Broken)
            {
                return input == ExperimentInput.Repair
                    ? new InputEvaluation(true, false, "整備完了")
                    : new InputEvaluation(false, true, "修理が必要");
            }

            if (input == ExperimentInput.Repair)
            {
                return new InputEvaluation(false, false, "修理は不要");
            }

            bool correct = input == ExpectedInput(vehicle, state);
            return correct
                ? new InputEvaluation(true, false, "正解")
                : new InputEvaluation(false, false, "車種が違う");
        }

        public static float TravelSeconds(ExperimentSpeed speed, ExperimentSpecialState state)
        {
            float normalizedSpeed;
            switch (speed)
            {
                case ExperimentSpeed.Low: normalizedSpeed = LowSpeedNormalizedPerSecond; break;
                case ExperimentSpeed.High: normalizedSpeed = HighSpeedNormalizedPerSecond; break;
                default: normalizedSpeed = NormalSpeedNormalizedPerSecond; break;
            }

            float speedMultiplier = 1f;
            if (state == ExperimentSpecialState.Express)
            {
                speedMultiplier = ExpressSpeedMultiplier;
            }
            else if (state == ExperimentSpecialState.Broken)
            {
                speedMultiplier = BrokenSpeedMultiplier;
            }

            return NormalizedTravelDistance / (normalizedSpeed * speedMultiplier);
        }

        public static float NominalGapSeconds(ExperimentSpeed speed, bool shortGap)
        {
            if (shortGap) return 0.22f;
            switch (speed)
            {
                case ExperimentSpeed.Low: return 1.00f;
                case ExperimentSpeed.High: return 0.56f;
                default: return 0.72f;
            }
        }
    }
}
