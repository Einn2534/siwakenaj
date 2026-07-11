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
    public bool IsEndless => Mode == GameMode.Endless;

    public GameResultData(
        int stageNumber,
        bool isClear,
        int score,
        int missCount,
        int lightTruckCount,
        int compactCarCount,
        int sportsCarCount)
        : this(GameMode.Stage, stageNumber, isClear, score, missCount, lightTruckCount, compactCarCount, sportsCarCount)
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
        int sportsCarCount)
    {
        Mode = mode;
        StageNumber = stageNumber;
        IsClear = isClear;
        Score = score;
        MissCount = missCount;
        LightTruckCount = lightTruckCount;
        CompactCarCount = compactCarCount;
        SportsCarCount = sportsCarCount;
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
        return FromScoreState(GameMode.Stage, stageNumber, isClear, scoreState);
    }

    public static GameResultData FromScoreState(GameMode mode, int stageNumber, bool isClear, ScoreState scoreState)
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
            scoreState.GetCorrectCount(CarType.SportsCar));
    }
}
