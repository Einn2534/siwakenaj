using System.Collections.Generic;
using UnityEngine;

public class ScoreState
{
    private const int ScorePerCorrect = 10;
    private const int ScorePerMiss = -5;

    private readonly Dictionary<CarType, int> _laneCounts = new();

    public int TargetScore { get; }
    public int MissLimit { get; }
    public int CurrentScore { get; private set; }
    public int MissCount { get; private set; }
    public bool IsEndless => TargetScore <= 0;
    public int RemainingSuccessCount => IsEndless ? 0 : Mathf.Max(0, Mathf.CeilToInt((TargetScore - CurrentScore) / (float)ScorePerCorrect));
    public bool HasReachedTargetScore => !IsEndless && CurrentScore >= TargetScore;
    public bool HasReachedMissLimit => MissLimit > 0 && MissCount >= MissLimit;

    public ScoreState(int targetScore, int missLimit)
    {
        TargetScore = Mathf.Max(0, targetScore);
        MissLimit = Mathf.Max(0, missLimit);
    }

    public void ApplySuccess(CarType laneType)
    {
        CurrentScore += ScorePerCorrect;
        IncrementLane(laneType);
    }

    public void ApplyMiss()
    {
        CurrentScore = Mathf.Max(0, CurrentScore + ScorePerMiss);
        MissCount += 1;
    }

    public void ReviveFromContinue()
    {
        MissCount = Mathf.Max(0, MissLimit - 1);
    }

    public int GetCorrectCount(CarType laneType)
    {
        return _laneCounts.TryGetValue(laneType, out int count) ? count : 0;
    }

    private void IncrementLane(CarType laneType)
    {
        _laneCounts.TryGetValue(laneType, out int count);
        _laneCounts[laneType] = count + 1;
    }
}
