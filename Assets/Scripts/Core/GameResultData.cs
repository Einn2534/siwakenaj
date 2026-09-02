public class GameResultData
{
    public GameMode Mode { get; }
    public int StageNumber { get; }
    public bool IsClear { get; }
    public int Score { get; }
    public int MissCount { get; }
    public int LightTruckCount { get; }
    public int CompactCarCount { get; }
    public int SportsCarCount { get; }
    public float PlayTimeSeconds { get; }
    public int MaxCombo { get; }
    public int TotalCorrect { get; }
    public StageGoalType GoalType { get; }
    public string DailyDateKey { get; }
    public uint Seed { get; }
    public int DailyTier { get; }
    public bool IsEndless => Mode == GameMode.Endless;
    public bool IsDaily => Mode == GameMode.Daily;

    public GameResultData(int stageNumber, bool isClear, int score, int missCount, int lightTruckCount, int compactCarCount, int sportsCarCount)
        : this(GameMode.Stage, stageNumber, isClear, score, missCount, lightTruckCount, compactCarCount, sportsCarCount)
    {
    }

    public GameResultData(GameMode mode, int stageNumber, bool isClear, int score, int missCount, int lightTruckCount, int compactCarCount, int sportsCarCount)
        : this(mode, stageNumber, isClear, score, missCount, lightTruckCount, compactCarCount, sportsCarCount, 0f, 0, lightTruckCount + compactCarCount + sportsCarCount, StageGoalType.Score, string.Empty, 0u, 0)
    {
    }

    public GameResultData(
        GameMode mode,
        int stageNumber,
        bool isClear,
        int score,
        int missCount,
        int lightTruckCount,
        int compactCarCount,
        int sportsCarCount,
        float playTimeSeconds,
        int maxCombo,
        int totalCorrect,
        StageGoalType goalType,
        string dailyDateKey,
        uint seed,
        int dailyTier)
    {
        Mode = mode;
        StageNumber = stageNumber;
        IsClear = isClear;
        Score = score;
        MissCount = missCount;
        LightTruckCount = lightTruckCount;
        CompactCarCount = compactCarCount;
        SportsCarCount = sportsCarCount;
        PlayTimeSeconds = playTimeSeconds;
        MaxCombo = maxCombo;
        TotalCorrect = totalCorrect;
        GoalType = goalType;
        DailyDateKey = dailyDateKey ?? string.Empty;
        Seed = seed;
        DailyTier = dailyTier;
    }

    public int GetCorrectCount(CarType carType)
    {
        return carType switch
        {
            CarType.LightTruck => LightTruckCount,
            CarType.CompactCar => CompactCarCount,
            CarType.SportsCar => SportsCarCount,
            _ => 0
        };
    }

    public static GameResultData Empty(int stageNumber)
    {
        return Empty(GameMode.Stage, stageNumber);
    }

    public static GameResultData Empty(GameMode mode, int stageNumber)
    {
        return new GameResultData(mode, stageNumber, false, 0, 0, 0, 0, 0);
    }

    public static GameResultData FromScoreState(int stageNumber, bool isClear, ScoreState scoreState)
    {
        return FromScoreState(GameMode.Stage, stageNumber, isClear, scoreState, null, null);
    }

    public static GameResultData FromScoreState(GameMode mode, int stageNumber, bool isClear, ScoreState scoreState)
    {
        return FromScoreState(mode, stageNumber, isClear, scoreState, null, null);
    }

    public static GameResultData FromScoreState(
        GameMode mode,
        int stageNumber,
        bool isClear,
        ScoreState scoreState,
        StageDefinition stageDefinition,
        StageObjectiveEvaluator evaluator)
    {
        if (scoreState == null)
        {
            return Empty(mode, stageNumber);
        }

        return new GameResultData(
            mode,
            stageNumber,
            isClear,
            scoreState.CurrentScore,
            scoreState.MissCount,
            scoreState.GetCorrectCount(CarType.LightTruck),
            scoreState.GetCorrectCount(CarType.CompactCar),
            scoreState.GetCorrectCount(CarType.SportsCar),
            evaluator != null ? evaluator.ElapsedSeconds : 0f,
            scoreState.MaxComboCount,
            scoreState.TotalCorrectCount,
            stageDefinition != null ? stageDefinition.GoalType : StageGoalType.Score,
            stageDefinition != null ? stageDefinition.DailyDateKey : string.Empty,
            stageDefinition != null ? stageDefinition.FixedSeed : 0u,
            stageDefinition != null ? stageDefinition.DailyTier : 0);
    }
}
