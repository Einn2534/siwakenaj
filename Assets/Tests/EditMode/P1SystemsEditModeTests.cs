using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class P1SystemsEditModeTests
{
    [Test]
    public void ExpressTutorialOnlyStartsForUnseenStageThreeAndIgnoresLegacyHintSeen()
    {
        Dictionary<string, (bool, int)> original = BackupInts(new[] { "ExpressTutorial_Seen", "GimmickHintSeen_Express" });
        Type session = CoreReflection.RequiredType("SessionState");
        Type launch = CoreReflection.RequiredType("TutorialLaunchService");
        string originalMode = session.GetProperty("SelectedGameMode").GetValue(null).ToString();
        object originalStage = session.GetProperty("SelectedStageNumber").GetValue(null);
        try
        {
            PlayerPrefs.DeleteKey("ExpressTutorial_Seen");
            PlayerPrefs.SetInt("GimmickHintSeen_Express", 1);
            CoreReflection.CallStatic(session, "SelectStage", 3);
            Assert.That(CoreReflection.CallStatic(launch, "ShouldStartExpressTutorial", 3), Is.EqualTo(true));
            Assert.That(CoreReflection.CallStatic(launch, "ShouldStartExpressTutorial", 1), Is.EqualTo(false));
            Assert.That(CoreReflection.CallStatic(launch, "ShouldStartExpressTutorial", 4), Is.EqualTo(false));
            CoreReflection.CallStatic(session, "SelectDaily", 3);
            Assert.That(CoreReflection.CallStatic(launch, "ShouldStartExpressTutorial", 3), Is.EqualTo(false));
            CoreReflection.CallStatic(session, "SelectEndless", 3);
            Assert.That(CoreReflection.CallStatic(launch, "ShouldStartExpressTutorial", 3), Is.EqualTo(false));
            CoreReflection.CallStatic(session, "SelectStage", 3);
            CoreReflection.CallStatic(CoreReflection.RequiredType("SaveService"), "SetExpressTutorialSeen");
            Assert.That(CoreReflection.CallStatic(launch, "ShouldStartExpressTutorial", 3), Is.EqualTo(false));
        }
        finally
        {
            string restoreMethod = originalMode == "Daily" ? "SelectDaily" : originalMode == "Endless" ? "SelectEndless" : "SelectStage";
            CoreReflection.CallStatic(session, restoreMethod, originalStage);
            RestoreInts(original);
        }
    }

    [Test]
    public void StageDatabase_ContainsFourChaptersAndAllFiveGoalTypes()
    {
        ScriptableObject database = Resources.Load<ScriptableObject>("StageDatabase");
        Assert.That(database, Is.Not.Null);
        IList stages = (IList)CoreReflection.GetProperty<object>(database, "Stages");
        Assert.That(stages.Count, Is.EqualTo(20));

        HashSet<string> goalTypes = new();
        for (int i = 0; i < stages.Count; i += 1)
        {
            object stage = stages[i];
            Assert.That(GetField<int>(stage, "StageNumber"), Is.EqualTo(i + 1));
            Assert.That(GetField<int>(stage, "ChapterNumber"), Is.EqualTo((i / 5) + 1));
            Assert.That(GetField<string>(stage, "DisplayName"), Is.Not.Empty);
            Assert.That(GetField<string>(stage, "MissionText"), Is.Not.Empty);
            string expectedSpawnPattern = i + 1 is 2 or 7 or 17
                ? "BalancedBag"
                : i + 1 is 3 or 6 or 11 ? "FixedIntroThenBag" : "WeightedRandom";
            Assert.That(GetField<object>(stage, "SpawnPattern").ToString(), Is.EqualTo(expectedSpawnPattern), $"stage {i + 1}");
            goalTypes.Add(GetField<object>(stage, "GoalType").ToString());
        }

        Assert.That(goalTypes, Is.EquivalentTo(new[] { "Score", "TotalCorrect", "Combo", "VehicleQuota", "Survival" }));
    }

    [Test]
    public void ObjectiveEvaluator_AllFiveGoalsClearAndEachFailsAtMissLimit()
    {
        Action<object> score = stage => SetField(stage, "GoalValue", 20);
        Action<object> totalCorrect = stage => SetField(stage, "GoalValue", 2);
        Action<object> combo = stage => SetField(stage, "GoalValue", 2);
        Action<object> quota = stage =>
        {
            SetField(stage, "LightTruckQuota", 1);
            SetField(stage, "CompactCarQuota", 1);
            SetField(stage, "SportsCarQuota", 1);
        };
        Action<object> survival = stage => SetField(stage, "SurvivalSeconds", 10f);

        AssertGoalClears("Score", score, state => ApplySuccess(state, "LightTruck", 2));
        AssertGoalClears("TotalCorrect", totalCorrect, state => ApplySuccess(state, "LightTruck", 2));
        AssertGoalClears("Combo", combo, state => ApplySuccess(state, "LightTruck", 2));
        AssertGoalClears("VehicleQuota", quota, state =>
        {
            ApplySuccess(state, "LightTruck", 1);
            ApplySuccess(state, "CompactCar", 1);
            ApplySuccess(state, "SportsCar", 1);
        });
        AssertGoalClears("Survival", survival, null, 10f);

        AssertGoalFails("Score", score);
        AssertGoalFails("TotalCorrect", totalCorrect);
        AssertGoalFails("Combo", combo);
        AssertGoalFails("VehicleQuota", quota);
        AssertGoalFails("Survival", survival);
    }

    [Test]
    public void ObjectiveEvaluator_PausedSurvivalTimeDoesNotAdvanceAndContinueRulesStayConsistent()
    {
        object stage = NewStage("Survival", 0);
        SetField(stage, "SurvivalSeconds", 10f);
        object state = NewScoreState(stage);
        object evaluator = NewEvaluator(stage, state, "Stage");

        CoreReflection.Call(evaluator, "Tick", 5f, false);
        Assert.That(CoreReflection.GetProperty<float>(evaluator, "ElapsedSeconds"), Is.Zero);
        CoreReflection.Call(evaluator, "Tick", 5f, true);
        Assert.That(CoreReflection.GetProperty<float>(evaluator, "ElapsedSeconds"), Is.EqualTo(5f));

        SetField(stage, "AllowRewardedContinue", false);
        CoreReflection.Call(state, "ApplyMiss");
        CoreReflection.Call(state, "ApplyMiss");
        CoreReflection.Call(state, "ApplyMiss");
        Assert.That(CoreReflection.GetProperty<bool>(evaluator, "CanOfferRewardedContinue"), Is.False);

        object dailyStage = NewStage("Survival", 0);
        SetField(dailyStage, "AllowRewardedContinue", true);
        object dailyState = NewScoreState(dailyStage);
        object dailyEvaluator = NewEvaluator(dailyStage, dailyState, "Daily");
        CoreReflection.Call(dailyState, "ApplyMiss");
        CoreReflection.Call(dailyState, "ApplyMiss");
        CoreReflection.Call(dailyState, "ApplyMiss");
        Assert.That(CoreReflection.GetProperty<bool>(dailyEvaluator, "CanOfferRewardedContinue"), Is.False);

        ScriptableObject database = Resources.Load<ScriptableObject>("StageDatabase");
        object firstExam = CoreReflection.Call(database, "GetStageDefinition", 5);
        Assert.That(GetField<int>(firstExam, "MissLimit"), Is.EqualTo(2));
        Assert.That(GetField<bool>(firstExam, "AllowRewardedContinue"), Is.False);
    }

    [Test]
    public void BalancedBagAndFixedIntroPreventQuotaStarvationAndIntroduceGimmickWithinThree()
    {
        Type sequenceType = CoreReflection.RequiredType("DeterministicSpawnSequence");
        object quotaStage = NewStage("VehicleQuota", 12);
        SetField(quotaStage, "SpawnPattern", Enum.Parse(CoreReflection.RequiredType("SpawnPattern"), "BalancedBag"));
        SetField(quotaStage, "LightTruckQuota", 4);
        SetField(quotaStage, "CompactCarQuota", 4);
        SetField(quotaStage, "SportsCarQuota", 4);
        object sequence = CoreReflection.New(sequenceType, quotaStage, 123u);
        Dictionary<string, int> counts = new();
        for (int i = 0; i < 12; i += 1)
        {
            object instruction = CoreReflection.Call(sequence, "Next");
            string car = GetField<object>(instruction, "CarType").ToString();
            counts[car] = counts.TryGetValue(car, out int count) ? count + 1 : 1;
        }

        Assert.That(counts["LightTruck"], Is.EqualTo(4));
        Assert.That(counts["CompactCar"], Is.EqualTo(4));
        Assert.That(counts["SportsCar"], Is.EqualTo(4));

        ScriptableObject database = Resources.Load<ScriptableObject>("StageDatabase");
        foreach (int stageNumber in new[] { 3, 6, 11 })
        {
            object stage = CoreReflection.Call(database, "GetStageDefinition", stageNumber);
            object introSequence = CoreReflection.New(sequenceType, stage, 7u);
            bool foundGimmick = false;
            for (int i = 0; i < 3; i += 1)
            {
                object instruction = CoreReflection.Call(introSequence, "Next");
                foundGimmick |= GetField<object>(instruction, "Modifier").ToString() != "Normal";
            }

            Assert.That(foundGimmick, Is.True, $"Stage {stageNumber} must introduce its gimmick within three cars.");
        }
    }

    [Test]
    public void SpawnSequence_PeekDoesNotConsumeDeferredInstruction()
    {
        Type sequenceType = CoreReflection.RequiredType("DeterministicSpawnSequence");
        object stage = NewStage("Score", 100);
        SetField(stage, "SpawnPattern", Enum.Parse(CoreReflection.RequiredType("SpawnPattern"), "FixedIntroThenBag"));
        Array intro = Array.CreateInstance(CoreReflection.RequiredType("SpawnInstruction"), 1);
        object introInstruction = CoreReflection.New(
            CoreReflection.RequiredType("SpawnInstruction"),
            Enum.Parse(CoreReflection.RequiredType("CarType"), "SportsCar"),
            Enum.Parse(CoreReflection.RequiredType("CarModifier"), "Broken"));
        intro.SetValue(introInstruction, 0);
        SetField(stage, "IntroSpawns", intro);

        object sequence = CoreReflection.New(sequenceType, stage, 77u);
        object firstAttempt = CoreReflection.Call(sequence, "Peek");
        object deferredAttempt = CoreReflection.Call(sequence, "Peek");
        Assert.That(GetField<object>(firstAttempt, "CarType").ToString(), Is.EqualTo("SportsCar"));
        Assert.That(GetField<object>(firstAttempt, "Modifier").ToString(), Is.EqualTo("Broken"));
        Assert.That(GetField<object>(deferredAttempt, "CarType").ToString(), Is.EqualTo("SportsCar"));
        Assert.That(GetField<object>(deferredAttempt, "Modifier").ToString(), Is.EqualTo("Broken"));

        CoreReflection.Call(sequence, "Commit");
        object afterSuccessfulSpawn = CoreReflection.Call(sequence, "Peek");
        Assert.That(GetField<object>(afterSuccessfulSpawn, "Modifier").ToString(), Is.Not.EqualTo("Broken"));
    }

    [Test]
    public void LocalTelemetry_RecordsAllRequiredMeasurementFields()
    {
        Type telemetryType = CoreReflection.RequiredType("LocalTelemetryService");
        PropertyInfo outputPathProperty = telemetryType.GetProperty("OutputPath", BindingFlags.Public | BindingFlags.Static);
        Assert.That(outputPathProperty, Is.Not.Null);
        string outputPath = (string)outputPathProperty.GetValue(null);
        bool existed = File.Exists(outputPath);
        long originalLength = existed ? new FileInfo(outputPath).Length : 0L;

        object result = NewResult("Stage", 4, false, 120, 3, 2, 3, 4, 19.5f, 7, 9, "Combo", string.Empty, 0u, 0);
        try
        {
            CoreReflection.CallStatic(telemetryType, "RecordResult", result, true);
            CoreReflection.CallStatic(telemetryType, "RecordRetry", result);
            CoreReflection.CallStatic(telemetryType, "RecordRewardedContinueSelected", result);

            string appended;
            using (FileStream stream = new(outputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                stream.Seek(originalLength, SeekOrigin.Begin);
                using StreamReader reader = new(stream);
                appended = reader.ReadToEnd();
            }

            StringAssert.Contains("\"EventName\":\"run_result\"", appended);
            StringAssert.Contains("\"FirstAttempt\":true", appended);
            StringAssert.Contains("\"Cleared\":false", appended);
            StringAssert.Contains("\"FailureReason\":\"miss_limit\"", appended);
            StringAssert.Contains("\"PlayTimeSeconds\":19.5", appended);
            StringAssert.Contains("\"MissCount\":3", appended);
            StringAssert.Contains("\"MaxCombo\":7", appended);
            StringAssert.Contains("\"EventName\":\"retry\"", appended);
            StringAssert.Contains("\"Retried\":true", appended);
            StringAssert.Contains("\"EventName\":\"rewarded_continue_selected\"", appended);
            StringAssert.Contains("\"RewardedContinueSelected\":true", appended);
        }
        finally
        {
            if (existed)
            {
                using FileStream restore = new(outputPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
                restore.SetLength(originalLength);
            }
            else if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Test]
    public void DailyV1_SameDateAndTierProduceIdenticalSettingsAndFirstHundredCars()
    {
        Type dailyType = CoreReflection.RequiredType("DailyChallengeService");
        object first = CoreReflection.CallStatic(dailyType, "Create", "20260824", 3, 15);
        object second = CoreReflection.CallStatic(dailyType, "Create", "20260824", 3, 15);
        Assert.That(JsonUtility.ToJson(first), Is.EqualTo(JsonUtility.ToJson(second)));
        Assert.That(GetField<string>(first, "DailyDateKey"), Is.EqualTo(GetField<string>(second, "DailyDateKey")));
        Assert.That(GetField<int>(first, "DailyTier"), Is.EqualTo(GetField<int>(second, "DailyTier")));
        Assert.That(GetField<uint>(first, "FixedSeed"), Is.EqualTo(GetField<uint>(second, "FixedSeed")));
        foreach (string field in new[] { "CarSpeed", "SpawnInterval", "ExpressChance", "CoveredChance", "BrokenChance" })
        {
            Assert.That(GetField<float>(first, field), Is.EqualTo(GetField<float>(second, field)).Within(0.000001f));
        }

        Type sequenceType = CoreReflection.RequiredType("DeterministicSpawnSequence");
        object firstSequence = CoreReflection.New(sequenceType, first, GetField<uint>(first, "FixedSeed"));
        object secondSequence = CoreReflection.New(sequenceType, second, GetField<uint>(second, "FixedSeed"));
        for (int i = 0; i < 100; i += 1)
        {
            object a = CoreReflection.Call(firstSequence, "Next");
            object b = CoreReflection.Call(secondSequence, "Next");
            Assert.That(GetField<object>(a, "CarType"), Is.EqualTo(GetField<object>(b, "CarType")), $"car {i}");
            Assert.That(GetField<object>(a, "Modifier"), Is.EqualTo(GetField<object>(b, "Modifier")), $"modifier {i}");
        }
    }

    [Test]
    public void DailyV1_UsesJstDateAndProgressionTier()
    {
        Type dailyType = CoreReflection.RequiredType("DailyChallengeService");
        Assert.That(CoreReflection.CallStatic(dailyType, "GetJstDateKey", new DateTime(2026, 8, 23, 15, 0, 0, DateTimeKind.Utc)), Is.EqualTo("20260824"));
        Assert.That(CoreReflection.CallStatic(dailyType, "GetTier", 5), Is.EqualTo(1));
        Assert.That(CoreReflection.CallStatic(dailyType, "GetTier", 10), Is.EqualTo(2));
        Assert.That(CoreReflection.CallStatic(dailyType, "GetTier", 15), Is.EqualTo(3));

        object tierOne = CoreReflection.CallStatic(dailyType, "Create", "20260824", 1, 5);
        object tierTwo = CoreReflection.CallStatic(dailyType, "Create", "20260824", 2, 10);
        object tierThree = CoreReflection.CallStatic(dailyType, "Create", "20260824", 3, 15);
        Assert.That(GetField<float>(tierOne, "CoveredChance"), Is.Zero);
        Assert.That(GetField<float>(tierTwo, "CoveredChance"), Is.GreaterThan(0f));
        Assert.That(GetField<float>(tierTwo, "BrokenChance"), Is.Zero);
        Assert.That(GetField<float>(tierThree, "BrokenChance"), Is.GreaterThan(0f));
        Assert.That(GetField<float>(tierThree, "ExpressChance") + GetField<float>(tierThree, "CoveredChance") + GetField<float>(tierThree, "BrokenChance"), Is.LessThanOrEqualTo(0.550001f));
    }

    [Test]
    public void DailySave_RolloverResetsDailyBestButPreservesAndAdvancesStreak()
    {
        const string key = "DailyChallengeSaveDataV1";
        bool existed = PlayerPrefs.HasKey(key);
        string original = PlayerPrefs.GetString(key, string.Empty);
        try
        {
            PlayerPrefs.DeleteKey(key);
            Type saveType = CoreReflection.RequiredType("DailyChallengeSaveData");
            Type dailyType = CoreReflection.RequiredType("DailyChallengeService");
            object data = CoreReflection.New(saveType);
            SetField(data, "CurrentDateKey", "20260823");
            SetField(data, "BestScore", 500);
            SetField(data, "BestMaxCombo", 20);
            SetField(data, "CompletedToday", true);
            SetField(data, "LastCompletedDateKey", "20260823");
            SetField(data, "CurrentStreak", 2);
            CoreReflection.CallStatic(dailyType, "Save", data);

            object rolled = CoreReflection.CallStatic(dailyType, "LoadForDate", "20260824");
            Assert.That(GetField<int>(rolled, "BestScore"), Is.Zero);
            Assert.That(GetField<int>(rolled, "BestMaxCombo"), Is.Zero);
            Assert.That(GetField<bool>(rolled, "CompletedToday"), Is.False);
            Assert.That(GetField<int>(rolled, "CurrentStreak"), Is.EqualTo(2));

            object persisted = CoreReflection.CallStatic(dailyType, "LoadForDate", "20260824");
            Assert.That(GetField<string>(persisted, "CurrentDateKey"), Is.EqualTo("20260824"));

            object firstClear = NewResult("Daily", 5, true, 420, 0, 12, 10, 8, 60f, 18, 30, "Survival", "20260824", 42u, 1);
            object firstSaved = CoreReflection.CallStatic(dailyType, "RecordResult", firstClear);
            Assert.That(GetField<int>(firstSaved, "BestScore"), Is.EqualTo(420));
            Assert.That(GetField<int>(firstSaved, "BestMaxCombo"), Is.EqualTo(18));
            Assert.That(GetField<bool>(firstSaved, "CompletedToday"), Is.True);
            Assert.That(GetField<int>(firstSaved, "CurrentStreak"), Is.EqualTo(3));

            object repeated = CoreReflection.CallStatic(dailyType, "RecordResult", firstClear);
            Assert.That(GetField<int>(repeated, "CurrentStreak"), Is.EqualTo(3));

            object nextClear = NewResult("Daily", 5, true, 510, 1, 14, 11, 9, 60f, 22, 34, "Survival", "20260825", 84u, 1);
            object nextSaved = CoreReflection.CallStatic(dailyType, "RecordResult", nextClear);
            Assert.That(GetField<bool>(nextSaved, "CompletedToday"), Is.True);
            Assert.That(GetField<int>(nextSaved, "CurrentStreak"), Is.EqualTo(4));
        }
        finally
        {
            RestoreString(key, existed, original);
        }
    }

    [Test]
    public void SaveMigration_PreservesLegacyBestAndStarsWhileAddingExplicitClear()
    {
        string[] keys = { "BestScore_Stage1", "StarRating_Stage1", "StageClear_1", "ProgressMigrationVersion" };
        Dictionary<string, (bool, int)> original = BackupInts(keys);
        try
        {
            PlayerPrefs.SetInt("BestScore_Stage1", 60);
            PlayerPrefs.SetInt("StarRating_Stage1", 2);
            PlayerPrefs.DeleteKey("StageClear_1");
            PlayerPrefs.DeleteKey("ProgressMigrationVersion");
            ScriptableObject database = Resources.Load<ScriptableObject>("StageDatabase");
            CoreReflection.CallStatic(CoreReflection.RequiredType("SaveService"), "EnsureProgressMigration", database);
            Assert.That(PlayerPrefs.GetInt("StageClear_1", 0), Is.EqualTo(1));
            Assert.That(PlayerPrefs.GetInt("BestScore_Stage1", 0), Is.EqualTo(60));
            Assert.That(PlayerPrefs.GetInt("StarRating_Stage1", 0), Is.EqualTo(2));
        }
        finally
        {
            RestoreInts(original);
        }
    }

    [Test]
    public void StageMasteryAndSaveRecordsUsePerStageMetricsAndNeverRegressBests()
    {
        Type starUtility = CoreReflection.RequiredType("StarRatingUtility");
        object missStage = NewStage("TotalCorrect", 10);
        SetField(missStage, "MasteryMetric", EnumValue("MasteryMetric", "MissCount"));
        SetField(missStage, "TwoStarThreshold", 1f);
        SetField(missStage, "ThreeStarThreshold", 0f);
        object scoreStage = NewStage("Score", 100);
        SetField(scoreStage, "MasteryMetric", EnumValue("MasteryMetric", "Score"));
        SetField(scoreStage, "TwoStarThreshold", 150f);
        SetField(scoreStage, "ThreeStarThreshold", 200f);
        object comboStage = NewStage("Combo", 10);
        SetField(comboStage, "MasteryMetric", EnumValue("MasteryMetric", "MaxCombo"));
        SetField(comboStage, "TwoStarThreshold", 12f);
        SetField(comboStage, "ThreeStarThreshold", 18f);
        object timeStage = NewStage("Survival", 20);
        SetField(timeStage, "MasteryMetric", EnumValue("MasteryMetric", "ClearTime"));
        SetField(timeStage, "TwoStarThreshold", 22f);
        SetField(timeStage, "ThreeStarThreshold", 18f);

        Assert.That(CoreReflection.CallStatic(starUtility, "CalculateForResult", NewResult("Stage", 1, true, 100, 0, 4, 3, 3, 24f, 10, 10, "TotalCorrect", string.Empty, 0u, 0), missStage), Is.EqualTo(3));
        Assert.That(CoreReflection.CallStatic(starUtility, "CalculateForResult", NewResult("Stage", 2, true, 210, 2, 4, 3, 3, 24f, 10, 10, "Score", string.Empty, 0u, 0), scoreStage), Is.EqualTo(3));
        Assert.That(CoreReflection.CallStatic(starUtility, "CalculateForResult", NewResult("Stage", 3, true, 150, 1, 7, 6, 5, 24f, 18, 18, "Combo", string.Empty, 0u, 0), comboStage), Is.EqualTo(3));
        Assert.That(CoreReflection.CallStatic(starUtility, "CalculateForResult", NewResult("Stage", 4, true, 150, 1, 7, 6, 5, 18f, 12, 18, "Survival", string.Empty, 0u, 0), timeStage), Is.EqualTo(3));

        ScriptableObject database = Resources.Load<ScriptableObject>("StageDatabase");
        AssertScoreMasteryCanReachThreeStars(database, starUtility, 3, 17, 190);
        AssertScoreMasteryCanReachThreeStars(database, starUtility, 10, 29, 310);

        string[] intKeys = { "BestScore_Stage15", "StarRating_Stage15", "StageClear_15" };
        Dictionary<string, (bool, int)> originalInts = BackupInts(intKeys);
        const string timeKey = "BestTime_Stage15";
        bool timeExisted = PlayerPrefs.HasKey(timeKey);
        float originalTime = PlayerPrefs.GetFloat(timeKey, 0f);
        try
        {
            PlayerPrefs.DeleteKey("BestScore_Stage15");
            PlayerPrefs.DeleteKey("StarRating_Stage15");
            PlayerPrefs.DeleteKey("StageClear_15");
            PlayerPrefs.DeleteKey(timeKey);
            Type saveService = CoreReflection.RequiredType("SaveService");
            CoreReflection.CallStatic(saveService, "SetBestScore", 15, 500);
            CoreReflection.CallStatic(saveService, "SetBestScore", 15, 200);
            CoreReflection.CallStatic(saveService, "SetStarRating", 15, 3);
            CoreReflection.CallStatic(saveService, "SetStarRating", 15, 1);
            CoreReflection.CallStatic(saveService, "SetBestTime", 15, 25f);
            CoreReflection.CallStatic(saveService, "SetBestTime", 15, 31f);
            Assert.That(CoreReflection.CallStatic(saveService, "GetBestScore", 15), Is.EqualTo(500));
            Assert.That(CoreReflection.CallStatic(saveService, "GetStarRating", 15), Is.EqualTo(3));
            Assert.That(CoreReflection.CallStatic(saveService, "GetBestTime", 15), Is.EqualTo(25f));
        }
        finally
        {
            RestoreInts(originalInts);
            if (timeExisted) PlayerPrefs.SetFloat(timeKey, originalTime); else PlayerPrefs.DeleteKey(timeKey);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void InterstitialGate_AllowsOnlyAfterTwoMinutesOrThreeCompletionsAndNeverForDaily()
    {
        string[] keys = { "Interstitial_CompletedSinceLast" };
        Dictionary<string, (bool, int)> originalInts = BackupInts(keys);
        const string ticksKey = "Interstitial_LastShownUtcTicks";
        bool ticksExisted = PlayerPrefs.HasKey(ticksKey);
        string originalTicks = PlayerPrefs.GetString(ticksKey, string.Empty);
        try
        {
            DateTime now = new(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc);
            Type gate = CoreReflection.RequiredType("InterstitialGate");
            PlayerPrefs.SetInt("Interstitial_CompletedSinceLast", 0);
            PlayerPrefs.SetString(ticksKey, now.AddSeconds(-119).Ticks.ToString());
            Assert.That(CoreReflection.CallStatic(gate, "CanShow", EnumValue("GameMode", "Stage"), now), Is.False);
            PlayerPrefs.SetString(ticksKey, now.AddSeconds(-120).Ticks.ToString());
            Assert.That(CoreReflection.CallStatic(gate, "CanShow", EnumValue("GameMode", "Stage"), now), Is.True);
            PlayerPrefs.SetString(ticksKey, now.Ticks.ToString());
            PlayerPrefs.SetInt("Interstitial_CompletedSinceLast", 3);
            Assert.That(CoreReflection.CallStatic(gate, "CanShow", EnumValue("GameMode", "Stage"), now), Is.True);
            Assert.That(CoreReflection.CallStatic(gate, "CanShow", EnumValue("GameMode", "Daily"), now), Is.False);
        }
        finally
        {
            RestoreInts(originalInts);
            RestoreString(ticksKey, ticksExisted, originalTicks);
        }
    }

    private static void AssertGoalClears(string goalName, Action<object> configure, Action<object> play, float tickSeconds = 0f)
    {
        object stage = NewStage(goalName, 0);
        configure(stage);
        object state = NewScoreState(stage);
        object evaluator = NewEvaluator(stage, state, "Stage");
        play?.Invoke(state);
        if (tickSeconds > 0f)
        {
            CoreReflection.Call(evaluator, "Tick", tickSeconds, true);
        }

        Assert.That(CoreReflection.GetProperty<object>(evaluator, "Outcome").ToString(), Is.EqualTo("Cleared"), goalName);
    }

    private static void AssertGoalFails(string goalName, Action<object> configure)
    {
        object stage = NewStage(goalName, 0);
        configure(stage);
        object state = NewScoreState(stage);
        object evaluator = NewEvaluator(stage, state, "Stage");
        for (int i = 0; i < 3; i += 1)
        {
            CoreReflection.Call(state, "ApplyMiss");
        }

        Assert.That(CoreReflection.GetProperty<object>(evaluator, "Outcome").ToString(), Is.EqualTo("Failed"), goalName);
    }

    private static void AssertScoreMasteryCanReachThreeStars(ScriptableObject database, Type starUtility, int stageNumber, int normalCorrectCount, int finalScore)
    {
        object stage = CoreReflection.Call(database, "GetStageDefinition", stageNumber);
        object state = NewScoreState(stage);
        object evaluator = NewEvaluator(stage, state, "Stage");
        ApplySuccess(state, "LightTruck", normalCorrectCount);
        CoreReflection.Call(state, "ApplySuccess", EnumValue("CarType", "LightTruck"), 2);
        Assert.That(CoreReflection.GetProperty<int>(state, "CurrentScore"), Is.EqualTo(finalScore), $"stage {stageNumber}");
        Assert.That(CoreReflection.GetProperty<object>(evaluator, "Outcome").ToString(), Is.EqualTo("Cleared"), $"stage {stageNumber}");

        object result = NewResult("Stage", stageNumber, true, finalScore, 0, normalCorrectCount + 1, 0, 0, 20f, normalCorrectCount + 1, normalCorrectCount + 1, "Score", string.Empty, 0u, 0);
        Assert.That(CoreReflection.CallStatic(starUtility, "CalculateForResult", result, stage), Is.EqualTo(3), $"stage {stageNumber}");
    }

    private static object NewStage(string goalName, int goalValue)
    {
        object stage = CoreReflection.New(CoreReflection.RequiredType("StageDefinition"));
        SetField(stage, "GoalType", Enum.Parse(CoreReflection.RequiredType("StageGoalType"), goalName));
        SetField(stage, "GoalValue", goalValue);
        SetField(stage, "TargetScore", goalName == "Score" ? goalValue : 0);
        SetField(stage, "MissLimit", 3);
        return stage;
    }

    private static object NewScoreState(object stage)
    {
        return CoreReflection.New(
            CoreReflection.RequiredType("ScoreState"),
            GetField<int>(stage, "TargetScore"),
            GetField<int>(stage, "MissLimit"),
            GetField<int>(stage, "FeverComboThreshold"));
    }

    private static object NewEvaluator(object stage, object state, string mode)
    {
        return CoreReflection.New(CoreReflection.RequiredType("StageObjectiveEvaluator"), stage, state, EnumValue("GameMode", mode));
    }

    private static object NewResult(
        string mode,
        int stageNumber,
        bool isClear,
        int score,
        int misses,
        int lightTrucks,
        int compactCars,
        int sportsCars,
        float playTime,
        int maxCombo,
        int totalCorrect,
        string goalType,
        string dailyDateKey,
        uint seed,
        int dailyTier)
    {
        return CoreReflection.New(
            CoreReflection.RequiredType("GameResultData"),
            EnumValue("GameMode", mode),
            stageNumber,
            isClear,
            score,
            misses,
            lightTrucks,
            compactCars,
            sportsCars,
            playTime,
            maxCombo,
            totalCorrect,
            EnumValue("StageGoalType", goalType),
            dailyDateKey,
            seed,
            dailyTier);
    }

    private static void ApplySuccess(object state, string carType, int count)
    {
        object value = EnumValue("CarType", carType);
        for (int i = 0; i < count; i += 1)
        {
            CoreReflection.Call(state, "ApplySuccess", value);
        }
    }

    private static object EnumValue(string typeName, string value)
    {
        return Enum.Parse(CoreReflection.RequiredType(typeName), value);
    }

    private static T GetField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{fieldName}");
        return (T)field.GetValue(target);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{fieldName}");
        field.SetValue(target, value);
    }

    private static Dictionary<string, (bool Existed, int Value)> BackupInts(IEnumerable<string> keys)
    {
        Dictionary<string, (bool, int)> result = new();
        foreach (string key in keys)
        {
            result[key] = (PlayerPrefs.HasKey(key), PlayerPrefs.GetInt(key, 0));
        }

        return result;
    }

    private static void RestoreInts(Dictionary<string, (bool Existed, int Value)> values)
    {
        foreach (KeyValuePair<string, (bool Existed, int Value)> pair in values)
        {
            if (pair.Value.Existed)
            {
                PlayerPrefs.SetInt(pair.Key, pair.Value.Value);
            }
            else
            {
                PlayerPrefs.DeleteKey(pair.Key);
            }
        }

        PlayerPrefs.Save();
    }

    private static void RestoreString(string key, bool existed, string value)
    {
        if (existed)
        {
            PlayerPrefs.SetString(key, value);
        }
        else
        {
            PlayerPrefs.DeleteKey(key);
        }

        PlayerPrefs.Save();
    }
}
