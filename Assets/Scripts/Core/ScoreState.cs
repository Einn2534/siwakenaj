using System.Collections.Generic;
using UnityEngine;

public class ScoreState
{
    private const int ScorePerCorrect = 10;
    private const int ScorePerMiss = -5;

    private readonly Dictionary<CarType, int> _laneCounts = new();

    public int TargetScore { get; }
    public int MissLimit { get; }
    public int FeverComboThreshold { get; }
    public int CurrentScore { get; private set; }
    public int MissCount { get; private set; }
    public int ComboCount { get; private set; }
    public int TotalCorrectCount { get; private set; }
    public bool IsEndless => TargetScore <= 0;
    public bool IsFeverActive => FeverComboThreshold > 0 && ComboCount >= FeverComboThreshold;
    public int FeverScoreMultiplier => IsFeverActive ? 2 : 1;
    public int RemainingSuccessCount => IsEndless ? 0 : Mathf.Max(0, Mathf.CeilToInt((TargetScore - CurrentScore) / (float)ScorePerCorrect));
    public bool HasReachedTargetScore => !IsEndless && CurrentScore >= TargetScore;
    public bool HasReachedMissLimit => MissLimit > 0 && MissCount >= MissLimit;

    public ScoreState(int targetScore, int missLimit)
        : this(targetScore, missLimit, 0)
    {
    }

    public ScoreState(int targetScore, int missLimit, int feverComboThreshold)
    {
        TargetScore = Mathf.Max(0, targetScore);
        MissLimit = Mathf.Max(0, missLimit);
        FeverComboThreshold = Mathf.Max(0, feverComboThreshold);
    }

    public int ApplySuccess(CarType laneType)
    {
        return ApplySuccess(laneType, 1);
    }

    public int ApplySuccess(CarType laneType, int scoreMultiplier)
    {
        ComboCount += 1;
        TotalCorrectCount += 1;
        int safeScoreMultiplier = Mathf.Max(1, scoreMultiplier);
        int earnedScore = ScorePerCorrect * safeScoreMultiplier * FeverScoreMultiplier;
        CurrentScore += earnedScore;
        IncrementLane(laneType);
        return earnedScore;
    }

    public void ApplyMiss()
    {
        CurrentScore = Mathf.Max(0, CurrentScore + ScorePerMiss);
        MissCount += 1;
        ComboCount = 0;
    }

    public void ReviveFromContinue()
    {
        MissCount = Mathf.Max(0, MissLimit - 1);
        ComboCount = 0;
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
