using UnityEngine;

public enum StageOutcome
{
    Playing,
    Cleared,
    Failed
}

public readonly struct StageProgressSnapshot
{
    public StageProgressSnapshot(string mainLabel, string detailLabel, float progress01, int currentValue, int targetValue)
    {
        MainLabel = mainLabel ?? string.Empty;
        DetailLabel = detailLabel ?? string.Empty;
        Progress01 = Mathf.Clamp01(progress01);
        CurrentValue = currentValue;
        TargetValue = targetValue;
    }

    public string MainLabel { get; }
    public string DetailLabel { get; }
    public float Progress01 { get; }
    public int CurrentValue { get; }
    public int TargetValue { get; }
}

public sealed class StageObjectiveEvaluator
{
    private readonly StageDefinition _stage;
    private readonly ScoreState _scoreState;
    private readonly GameMode _mode;

    public StageObjectiveEvaluator(StageDefinition stage, ScoreState scoreState, GameMode mode)
    {
        _stage = stage ?? StageDefinition.CreateFallback(1);
        _scoreState = scoreState ?? new ScoreState(_stage.TargetScore, _stage.MissLimit, _stage.FeverComboThreshold);
        _mode = mode;
    }

    public float ElapsedSeconds { get; private set; }

    public StageOutcome Outcome
    {
        get
        {
            if (_scoreState.HasReachedMissLimit)
            {
                return StageOutcome.Failed;
            }

            if (_mode == GameMode.Endless)
            {
                return StageOutcome.Playing;
            }

            return HasReachedGoal() ? StageOutcome.Cleared : StageOutcome.Playing;
        }
    }

    public bool CanOfferRewardedContinue => Outcome == StageOutcome.Failed
        && _stage.AllowRewardedContinue
        && _mode != GameMode.Daily;

    public void Tick(float deltaTime, bool gameplayActive)
    {
        if (!gameplayActive || Outcome != StageOutcome.Playing || deltaTime <= 0f)
        {
            return;
        }

        ElapsedSeconds += deltaTime;
    }

    public StageProgressSnapshot GetProgressSnapshot()
    {
        if (_mode == GameMode.Endless)
        {
            return new StageProgressSnapshot(
                $"SCORE {_scoreState.CurrentScore:N0}",
                $"{_scoreState.TotalCorrectCount:N0}{_stage.Theme.CountUnit} / MAX {_scoreState.MaxComboCount:N0} COMBO",
                1f,
                _scoreState.CurrentScore,
                0);
        }

        return _stage.GoalType switch
        {
            StageGoalType.TotalCorrect => BuildCountSnapshot(_scoreState.TotalCorrectCount, _stage.EffectiveGoalValue, _stage.Theme.CountUnit),
            StageGoalType.Combo => BuildComboSnapshot(),
            StageGoalType.VehicleQuota => BuildQuotaSnapshot(),
            StageGoalType.Survival => BuildSurvivalSnapshot(),
            _ => BuildScoreSnapshot()
        };
    }

    private bool HasReachedGoal()
    {
        return _stage.GoalType switch
        {
            StageGoalType.TotalCorrect => _scoreState.TotalCorrectCount >= _stage.EffectiveGoalValue,
            StageGoalType.Combo => _scoreState.ComboCount >= _stage.EffectiveGoalValue,
            StageGoalType.VehicleQuota => HasReachedVehicleQuota(),
            StageGoalType.Survival => ElapsedSeconds >= Mathf.Max(0f, _stage.SurvivalSeconds),
            _ => _stage.EffectiveGoalValue > 0 && _scoreState.CurrentScore >= _stage.EffectiveGoalValue
        };
    }

    private bool HasReachedVehicleQuota()
    {
        return _scoreState.GetCorrectCount(CarType.LightTruck) >= Mathf.Max(0, _stage.LightTruckQuota)
            && _scoreState.GetCorrectCount(CarType.CompactCar) >= Mathf.Max(0, _stage.CompactCarQuota)
            && _scoreState.GetCorrectCount(CarType.SportsCar) >= Mathf.Max(0, _stage.SportsCarQuota)
            && _stage.TotalVehicleQuota > 0;
    }

    private StageProgressSnapshot BuildScoreSnapshot()
    {
        int target = _stage.EffectiveGoalValue;
        int current = _scoreState.CurrentScore;
        int remaining = Mathf.Max(0, target - current);
        return new StageProgressSnapshot($"あと{remaining:N0}点", $"{current:N0} / {target:N0} 点", target > 0 ? current / (float)target : 1f, current, target);
    }

    private StageProgressSnapshot BuildCountSnapshot(int current, int target, string unit)
    {
        int remaining = Mathf.Max(0, target - current);
        return new StageProgressSnapshot($"あと{remaining:N0}{unit}", $"{current:N0} / {target:N0}{unit}", target > 0 ? current / (float)target : 0f, current, target);
    }

    private StageProgressSnapshot BuildComboSnapshot()
    {
        int target = _stage.EffectiveGoalValue;
        int current = _scoreState.ComboCount;
        return new StageProgressSnapshot($"{current:N0} / {target:N0} COMBO", $"MAX {_scoreState.MaxComboCount:N0} COMBO", target > 0 ? current / (float)target : 0f, current, target);
    }

    private StageProgressSnapshot BuildQuotaSnapshot()
    {
        int lightTarget = Mathf.Max(0, _stage.LightTruckQuota);
        int compactTarget = Mathf.Max(0, _stage.CompactCarQuota);
        int sportsTarget = Mathf.Max(0, _stage.SportsCarQuota);
        int light = Mathf.Min(_scoreState.GetCorrectCount(CarType.LightTruck), lightTarget);
        int compact = Mathf.Min(_scoreState.GetCorrectCount(CarType.CompactCar), compactTarget);
        int sports = Mathf.Min(_scoreState.GetCorrectCount(CarType.SportsCar), sportsTarget);
        int current = light + compact + sports;
        int target = _stage.TotalVehicleQuota;
        CarVisualDatabase theme = _stage.Theme;
        string detail = $"{theme.GetDisplayName(CarType.LightTruck)} {light}/{lightTarget}  {theme.GetDisplayName(CarType.CompactCar)} {compact}/{compactTarget}  {theme.GetDisplayName(CarType.SportsCar)} {sports}/{sportsTarget}";
        return new StageProgressSnapshot("種類別ノルマ", detail, target > 0 ? current / (float)target : 0f, current, target);
    }

    private StageProgressSnapshot BuildSurvivalSnapshot()
    {
        float targetSeconds = Mathf.Max(0f, _stage.SurvivalSeconds);
        int displaySeconds = Mathf.CeilToInt(Mathf.Max(0f, targetSeconds - ElapsedSeconds));
        string detail = _mode == GameMode.Daily
            ? $"SCORE {_scoreState.CurrentScore:N0} / MAX {_scoreState.MaxComboCount:N0} COMBO"
            : $"{Mathf.Min(ElapsedSeconds, targetSeconds):0.0} / {targetSeconds:0}秒";
        return new StageProgressSnapshot($"残り{displaySeconds / 60:00}:{displaySeconds % 60:00}", detail, targetSeconds > 0f ? ElapsedSeconds / targetSeconds : 0f, Mathf.FloorToInt(ElapsedSeconds), Mathf.CeilToInt(targetSeconds));
    }
}
