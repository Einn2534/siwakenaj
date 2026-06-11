using UnityEngine;

public static class StageNumberUtility
{
    public const int MinimumStageNumber = 1;

    public static int Normalize(int stageNumber)
    {
        return Mathf.Max(MinimumStageNumber, stageNumber);
    }

    public static int FromIndex(int stageIndex)
    {
        return Normalize(stageIndex + 1);
    }
}
