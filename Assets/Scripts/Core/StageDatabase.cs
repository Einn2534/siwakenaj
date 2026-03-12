using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageDatabase", menuName = "Siwakenja/Stage Database")]
public class StageDatabase : ScriptableObject
{
    [SerializeField]
    private StageDefinition[] _stages = Array.Empty<StageDefinition>();

    public IReadOnlyList<StageDefinition> Stages => _stages ?? Array.Empty<StageDefinition>();

    public StageDefinition GetStageDefinition(int stageNumber)
    {
        int safeStageNumber = Mathf.Max(1, stageNumber);
        foreach (StageDefinition stage in Stages)
        {
            if (stage != null && stage.StageNumber == safeStageNumber)
            {
                return stage;
            }
        }

        if (Stages.Count > 0 && Stages[0] != null)
        {
            return Stages[0];
        }

        return StageDefinition.CreateFallback(safeStageNumber);
    }

    public int GetStageIndex(int stageNumber)
    {
        int safeStageNumber = Mathf.Max(1, stageNumber);
        for (int i = 0; i < Stages.Count; i += 1)
        {
            if (Stages[i] != null && Stages[i].StageNumber == safeStageNumber)
            {
                return i;
            }
        }

        return 0;
    }

    public bool IsStageUnlocked(int stageIndex, Func<int, int> getBestScore)
    {
        if (stageIndex < 0 || stageIndex >= Stages.Count)
        {
            return false;
        }

        StageDefinition stage = Stages[stageIndex];
        if (stage == null || !stage.IsImplemented)
        {
            return false;
        }

        if (stage.StageNumber <= 1)
        {
            return true;
        }

        int previousIndex = Mathf.Clamp(stageIndex - 1, 0, Stages.Count - 1);
        StageDefinition previousStage = Stages[previousIndex];
        if (previousStage == null || !previousStage.IsImplemented)
        {
            return false;
        }

        return getBestScore(previousStage.StageNumber) >= previousStage.TargetScore;
    }

    public void SetStages(StageDefinition[] stages)
    {
        _stages = stages ?? Array.Empty<StageDefinition>();
    }
}
