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
    public bool HasReachedTargetScore => CurrentScore >= TargetScore;
    public bool HasReachedMissLimit => MissCount >= MissLimit;

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

    public int GetCorrectCount(CarType laneType)
    {
        return _laneCounts.TryGetValue(laneType, out int count) ? count : 0;
    }

    private void IncrementLane(CarType laneType)
    {
        if (!_laneCounts.ContainsKey(laneType))
        {
            _laneCounts[laneType] = 0;
        }

        _laneCounts[laneType] += 1;
    }
}
