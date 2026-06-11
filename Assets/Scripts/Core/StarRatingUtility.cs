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
}
