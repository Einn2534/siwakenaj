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

    public bool TryGetStageIndex(int stageNumber, out int stageIndex)
    {
        int safeStageNumber = Mathf.Max(1, stageNumber);
        for (int i = 0; i < Stages.Count; i += 1)
        {
            if (Stages[i] != null && Stages[i].StageNumber == safeStageNumber)
            {
                stageIndex = i;
                return true;
            }
        }

        stageIndex = -1;
        return false;
    }

    public StageDefinition GetNextStageDefinition(int stageNumber)
    {
        if (!TryGetStageIndex(stageNumber, out int stageIndex))
        {
            return null;
        }

        for (int i = stageIndex + 1; i < Stages.Count; i += 1)
        {
            if (Stages[i] != null)
            {
                return Stages[i];
            }
        }

        return null;
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

        StageDefinition requiredStage = GetRequiredClearStage(stageIndex);
        if (requiredStage == null)
        {
            return false;
        }

        return getBestScore != null && getBestScore(requiredStage.StageNumber) >= requiredStage.TargetScore;
    }

    public int GetRequiredClearStageNumber(int stageIndex)
    {
        StageDefinition requiredStage = GetRequiredClearStage(stageIndex);
        return requiredStage != null ? Mathf.Max(1, requiredStage.StageNumber) : 0;
    }

    public void SetStages(StageDefinition[] stages)
    {
        _stages = stages ?? Array.Empty<StageDefinition>();
    }

    private StageDefinition GetRequiredClearStage(int stageIndex)
    {
        if (stageIndex <= 0 || stageIndex >= Stages.Count)
        {
            return null;
        }

        for (int i = stageIndex - 1; i >= 0; i -= 1)
        {
            StageDefinition previousStage = Stages[i];
            if (previousStage != null && previousStage.IsImplemented)
            {
                return previousStage;
            }
        }

        return null;
    }
}
