using System;

[Serializable]
public class StageDefinition
{
    private const float EndlessDefaultCarSpeed = 0.92f;
    private const float EndlessDefaultSpawnInterval = 0.72f;

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
            StageNumber = StageNumberUtility.Normalize(stageNumber)
        };
    }

    public static StageDefinition CreateEndless(StageDefinition sourceStage = null)
    {
        return new StageDefinition
        {
            StageNumber = sourceStage != null
                ? StageNumberUtility.Normalize(sourceStage.StageNumber)
                : StageNumberUtility.MinimumStageNumber,
            TargetScore = 0,
            MissLimit = 1,
            CarSpeed = sourceStage != null && sourceStage.CarSpeed > 0f
                ? sourceStage.CarSpeed
                : EndlessDefaultCarSpeed,
            SpawnInterval = sourceStage != null && sourceStage.SpawnInterval > 0f
                ? sourceStage.SpawnInterval
                : EndlessDefaultSpawnInterval,
            WeightLightTruck = sourceStage != null ? sourceStage.WeightLightTruck : 1,
            WeightCompactCar = sourceStage != null ? sourceStage.WeightCompactCar : 1,
            WeightSportsCar = sourceStage != null ? sourceStage.WeightSportsCar : 1,
            IsImplemented = true
        };
    }
}
