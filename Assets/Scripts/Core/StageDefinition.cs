using System;
using UnityEngine;

public enum StageGoalType
{
    Score,
    TotalCorrect,
    Combo,
    VehicleQuota,
    Survival
}

public enum SpawnPattern
{
    WeightedRandom,
    BalancedBag,
    FixedIntroThenBag
}

public enum MasteryMetric
{
    MissCount,
    Score,
    MaxCombo,
    ClearTime
}

[Serializable]
public sealed class SpawnInstruction
{
    public CarType CarType;
    public CarModifier Modifier;

    public SpawnInstruction()
    {
    }

    public SpawnInstruction(CarType carType, CarModifier modifier)
    {
        CarType = carType;
        Modifier = modifier;
    }

    public SpawnInstruction Copy()
    {
        return new SpawnInstruction(CarType, Modifier);
    }
}

[Serializable]
public class StageDefinition
{
    private const float EndlessDefaultCarSpeed = 0.92f;
    private const float EndlessDefaultSpawnInterval = 0.72f;

    [Header("Identity")]
    public int StageNumber = 1;
    public int ChapterNumber = 1;
    public string DisplayName = "仕分け開始";
    public string MissionText = "100点へ到達";
    public CarVisualDatabase VisualTheme;
    public CarVisualDatabase Theme => VisualTheme != null ? VisualTheme : CarVisualDatabase.LoadDefault();

    [Header("Objective")]
    public StageGoalType GoalType = StageGoalType.Score;
    public int GoalValue = 100;
    public int TargetScore = 100;
    public int LightTruckQuota;
    public int CompactCarQuota;
    public int SportsCarQuota;
    public float SurvivalSeconds;
    public int MissLimit = 3;
    public bool AllowRewardedContinue = true;

    [Header("Spawning")]
    public float CarSpeed = 0.7f;
    public float SpawnInterval = 1f;
    public int WeightLightTruck = 1;
    public int WeightCompactCar = 1;
    public int WeightSportsCar = 1;
    [Range(0f, 1f)] public float ExpressChance;
    [Range(0f, 1f)] public float CoveredChance;
    [Range(0f, 1f)] public float BrokenChance;
    [Min(0)] public int RushEveryCorrect;
    [Min(0)] public int FeverComboThreshold;
    public SpawnPattern SpawnPattern = SpawnPattern.WeightedRandom;
    public SpawnInstruction[] IntroSpawns = Array.Empty<SpawnInstruction>();
    public bool UseFixedSeed;
    public uint FixedSeed;

    [Header("Mastery")]
    public MasteryMetric MasteryMetric = MasteryMetric.MissCount;
    public float TwoStarThreshold = 1f;
    public float ThreeStarThreshold;

    [Header("Compatibility")]
    public int LegacyClearScore;
    public bool IsImplemented = true;

    [NonSerialized] public string DailyDateKey;
    [NonSerialized] public int DailyTier;

    public int EffectiveGoalValue
    {
        get
        {
            if (GoalType == StageGoalType.Survival)
            {
                return Mathf.CeilToInt(Mathf.Max(0f, SurvivalSeconds));
            }

            int value = GoalValue;
            if (GoalType == StageGoalType.Score && value <= 0)
            {
                value = TargetScore;
            }

            return Mathf.Max(0, value);
        }
    }

    public int TotalVehicleQuota => Mathf.Max(0, LightTruckQuota)
        + Mathf.Max(0, CompactCarQuota)
        + Mathf.Max(0, SportsCarQuota);

    public static StageDefinition CreateFallback(int stageNumber)
    {
        return new StageDefinition
        {
            StageNumber = StageNumberUtility.Normalize(stageNumber)
        };
    }

    public static StageDefinition CreateEndless(StageDefinition sourceStage = null)
    {
        StageDefinition source = sourceStage ?? CreateFallback(StageNumberUtility.MinimumStageNumber);
        return new StageDefinition
        {
            StageNumber = StageNumberUtility.Normalize(source.StageNumber),
            ChapterNumber = Mathf.Clamp(source.ChapterNumber, 1, 3),
            VisualTheme = source.VisualTheme,
            DisplayName = "エンドレス",
            MissionText = "1ミスまで自己ベストへ挑戦",
            GoalType = StageGoalType.Score,
            GoalValue = 0,
            TargetScore = 0,
            MissLimit = 1,
            AllowRewardedContinue = true,
            CarSpeed = source.CarSpeed > 0f ? source.CarSpeed : EndlessDefaultCarSpeed,
            SpawnInterval = source.SpawnInterval > 0f ? source.SpawnInterval : EndlessDefaultSpawnInterval,
            WeightLightTruck = source.WeightLightTruck,
            WeightCompactCar = source.WeightCompactCar,
            WeightSportsCar = source.WeightSportsCar,
            ExpressChance = source.ExpressChance,
            CoveredChance = source.CoveredChance,
            BrokenChance = source.BrokenChance,
            RushEveryCorrect = source.RushEveryCorrect,
            FeverComboThreshold = source.FeverComboThreshold,
            SpawnPattern = SpawnPattern.WeightedRandom,
            MasteryMetric = MasteryMetric.Score,
            IsImplemented = true
        };
    }
}
