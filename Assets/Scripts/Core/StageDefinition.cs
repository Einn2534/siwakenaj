using System;
using UnityEngine;

[Serializable]
public class StageDefinition
{
    public int StageNumber = 1;
    public int TargetScore = 100;
    public int MissLimit = 3;
    public float CarSpeed = 0.7f;
    public float SpawnInterval = 1f;
    public int WeightLightTruck = 1;
    public int WeightCompactCar = 1;
    public int WeightSportsCar = 1;
    public bool IsImplemented = true;

    public static StageDefinition CreateFallback(int stageNumber)
    {
        return new StageDefinition
        {
            StageNumber = Mathf.Max(1, stageNumber)
        };
    }
}
