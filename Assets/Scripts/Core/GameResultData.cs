public class GameResultData
{
    public int StageNumber { get; }
    public bool IsClear { get; }
    public int Score { get; }
    public int MissCount { get; }
    public int LightTruckCount { get; }
    public int CompactCarCount { get; }
    public int SportsCarCount { get; }

    public GameResultData(
        int stageNumber,
        bool isClear,
        int score,
        int missCount,
        int lightTruckCount,
        int compactCarCount,
        int sportsCarCount)
    {
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
        return new GameResultData(stageNumber, false, 0, 0, 0, 0, 0);
    }

    public static GameResultData FromScoreState(int stageNumber, bool isClear, ScoreState scoreState)
    {
        if (scoreState == null)
        {
            return Empty(stageNumber);
        }

        return new GameResultData(
            stageNumber,
            isClear,
            scoreState.CurrentScore,
            scoreState.MissCount,
            scoreState.GetCorrectCount(CarType.LightTruck),
            scoreState.GetCorrectCount(CarType.CompactCar),
            scoreState.GetCorrectCount(CarType.SportsCar));
    }
}
