using System;
using UnityEngine;

[Serializable]
public sealed class DailyChallengeSaveData
{
    public string CurrentDateKey;
    public int BestScore;
    public int BestMaxCombo;
    public bool CompletedToday;
    public string LastCompletedDateKey;
    public int CurrentStreak;
}

public static class DailyChallengeService
{
    public const string Version = "daily-v1";
    public const float DurationSeconds = 60f;
    public const int MissLimit = 3;
    private const string SaveKey = "DailyChallengeSaveDataV1";
    private static readonly TimeSpan JstOffset = TimeSpan.FromHours(9d);

    public static string GetJstDateKey(DateTime utcNow)
    {
        DateTime normalizedUtc = utcNow.Kind == DateTimeKind.Utc ? utcNow : utcNow.ToUniversalTime();
        return normalizedUtc.Add(JstOffset).ToString("yyyyMMdd");
    }

    public static int GetTier(int highestClearedStageNumber)
    {
        return highestClearedStageNumber >= 15 ? 3 : highestClearedStageNumber >= 10 ? 2 : 1;
    }

    public static StageDefinition CreateForToday(int highestClearedStageNumber)
    {
        return Create(GetJstDateKey(DateTime.UtcNow), GetTier(highestClearedStageNumber), highestClearedStageNumber);
    }

    public static StageDefinition Create(string dateKey, int tier, int sourceStageNumber)
    {
        int safeTier = Mathf.Clamp(tier, 1, 3);
        string safeDateKey = string.IsNullOrWhiteSpace(dateKey) ? "19700101" : dateKey;
        string seedSource = $"{Version}:{safeDateKey}:tier{safeTier}";
        uint seed = StableHash.Fnv1a(seedSource);
        DeterministicRandom random = new(seed);

        StageDefinition stage = new()
        {
            StageNumber = StageNumberUtility.Normalize(sourceStageNumber),
            ChapterNumber = safeTier,
            DisplayName = "本日の仕分け",
            MissionText = "60秒でハイスコア",
            GoalType = StageGoalType.Survival,
            GoalValue = 60,
            TargetScore = 0,
            SurvivalSeconds = DurationSeconds,
            MissLimit = MissLimit,
            AllowRewardedContinue = false,
            WeightLightTruck = 4,
            WeightCompactCar = 4,
            WeightSportsCar = 4,
            SpawnPattern = SpawnPattern.BalancedBag,
            UseFixedSeed = true,
            FixedSeed = seed,
            MasteryMetric = MasteryMetric.Score,
            DailyDateKey = safeDateKey,
            DailyTier = safeTier,
            IsImplemented = true
        };

        if (safeTier == 1)
        {
            stage.CarSpeed = random.Range(0.78f, 0.86f);
            stage.SpawnInterval = random.Range(0.76f, 0.90f);
            stage.ExpressChance = random.Range(0.10f, 0.18f);
        }
        else if (safeTier == 2)
        {
            stage.CarSpeed = random.Range(0.82f, 0.94f);
            stage.SpawnInterval = random.Range(0.66f, 0.80f);
            stage.ExpressChance = random.Range(0.10f, 0.18f);
            stage.CoveredChance = random.Range(0.10f, 0.18f);
        }
        else
        {
            stage.CarSpeed = random.Range(0.88f, 1.02f);
            stage.SpawnInterval = random.Range(0.56f, 0.72f);
            stage.ExpressChance = random.Range(0.10f, 0.18f);
            stage.CoveredChance = random.Range(0.10f, 0.18f);
            stage.BrokenChance = random.Range(0.10f, 0.18f);
            stage.RushEveryCorrect = random.Range(7, 11);
            stage.FeverComboThreshold = random.Range(6, 9);
        }

        ClampCombinedGimmickChance(stage, 0.55f);
        return stage;
    }

    public static DailyChallengeSaveData LoadForDate(string dateKey)
    {
        DailyChallengeSaveData data = LoadRaw();
        string safeDateKey = string.IsNullOrWhiteSpace(dateKey) ? "19700101" : dateKey;
        if (!string.Equals(data.CurrentDateKey, safeDateKey, StringComparison.Ordinal))
        {
            data.CurrentDateKey = safeDateKey;
            data.BestScore = 0;
            data.BestMaxCombo = 0;
            data.CompletedToday = false;
            Save(data);
        }

        return data;
    }

    public static DailyChallengeSaveData RecordResult(GameResultData result)
    {
        if (result == null || !result.IsDaily)
        {
            return LoadForDate(GetJstDateKey(DateTime.UtcNow));
        }

        DailyChallengeSaveData data = LoadForDate(result.DailyDateKey);
        data.BestScore = Mathf.Max(data.BestScore, result.Score);
        data.BestMaxCombo = Mathf.Max(data.BestMaxCombo, result.MaxCombo);
        if (result.IsClear && !data.CompletedToday)
        {
            data.CurrentStreak = IsPreviousDate(data.LastCompletedDateKey, result.DailyDateKey)
                ? Mathf.Max(1, data.CurrentStreak + 1)
                : 1;
            data.LastCompletedDateKey = result.DailyDateKey;
            data.CompletedToday = true;
        }

        Save(data);
        return data;
    }

    public static void Save(DailyChallengeSaveData data)
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data ?? new DailyChallengeSaveData()));
        PlayerPrefs.Save();
    }

    private static DailyChallengeSaveData LoadRaw()
    {
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new DailyChallengeSaveData();
        }

        try
        {
            return JsonUtility.FromJson<DailyChallengeSaveData>(json) ?? new DailyChallengeSaveData();
        }
        catch (ArgumentException)
        {
            return new DailyChallengeSaveData();
        }
    }

    private static bool IsPreviousDate(string previousDateKey, string currentDateKey)
    {
        return DateTime.TryParseExact(previousDateKey, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime previous)
            && DateTime.TryParseExact(currentDateKey, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime current)
            && previous.Date.AddDays(1d) == current.Date;
    }

    private static void ClampCombinedGimmickChance(StageDefinition stage, float maximum)
    {
        float total = stage.ExpressChance + stage.CoveredChance + stage.BrokenChance;
        if (total <= maximum || total <= 0f)
        {
            return;
        }

        float scale = maximum / total;
        stage.ExpressChance *= scale;
        stage.CoveredChance *= scale;
        stage.BrokenChance *= scale;
    }
}
