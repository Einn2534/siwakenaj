using UnityEngine;

public static class StarRatingUtility
{
    public const int MaxStars = 3;

    public static int Clamp(int starRating)
    {
        return Mathf.Clamp(starRating, 0, MaxStars);
    }

    public static int CalculateForResult(GameResultData result)
    {
        if (result == null || !result.IsClear)
        {
            return 0;
        }

        int rating = result.MissCount switch
        {
            <= 0 => 3,
            1 => 2,
            _ => 1
        };

        return Mathf.Clamp(rating, 1, MaxStars);
    }

    public static int CalculateForResult(GameResultData result, StageDefinition stageDefinition)
    {
        if (result == null || !result.IsClear || stageDefinition == null)
        {
            return 0;
        }

        float value = stageDefinition.MasteryMetric switch
        {
            MasteryMetric.Score => result.Score,
            MasteryMetric.MaxCombo => result.MaxCombo,
            MasteryMetric.ClearTime => result.PlayTimeSeconds,
            _ => result.MissCount
        };

        bool lowerIsBetter = stageDefinition.MasteryMetric == MasteryMetric.MissCount
            || stageDefinition.MasteryMetric == MasteryMetric.ClearTime;
        bool meetsTwo = lowerIsBetter
            ? value <= stageDefinition.TwoStarThreshold
            : value >= stageDefinition.TwoStarThreshold;
        bool meetsThree = lowerIsBetter
            ? value <= stageDefinition.ThreeStarThreshold
            : value >= stageDefinition.ThreeStarThreshold;

        return meetsThree ? 3 : meetsTwo ? 2 : 1;
    }
}
