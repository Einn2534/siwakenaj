using System;
using NUnit.Framework;
using UnityEngine;

public sealed class StageDatabaseEditModeTests
{
    [Test]
    public void StageDefinition_CreateFallbackClampsStageNumber()
    {
        Type definitionType = CoreReflection.RequiredType("StageDefinition");

        object fallback = CoreReflection.CallStatic(definitionType, "CreateFallback", -4);

        Assert.That(CoreReflection.GetField<int>(fallback, "StageNumber"), Is.EqualTo(1));
        Assert.That(CoreReflection.GetField<int>(fallback, "TargetScore"), Is.EqualTo(100));
        Assert.That(CoreReflection.GetField<int>(fallback, "MissLimit"), Is.EqualTo(3));
    }

    [Test]
    public void StageDefinition_CreateEndlessUsesOneMissAndNoTarget()
    {
        Type definitionType = CoreReflection.RequiredType("StageDefinition");
        object source = Stage(definitionType, 4, 310, true);
        definitionType.GetField("MissLimit").SetValue(source, 3);
        definitionType.GetField("CarSpeed").SetValue(source, 0.98f);
        definitionType.GetField("SpawnInterval").SetValue(source, 0.68f);
        definitionType.GetField("ExpressChance").SetValue(source, 0.2f);
        definitionType.GetField("CoveredChance").SetValue(source, 0.15f);
        definitionType.GetField("BrokenChance").SetValue(source, 0.1f);
        definitionType.GetField("RushEveryCorrect").SetValue(source, 7);
        definitionType.GetField("FeverComboThreshold").SetValue(source, 6);

        object endless = CoreReflection.CallStatic(definitionType, "CreateEndless", source);

        Assert.That(CoreReflection.GetField<int>(endless, "StageNumber"), Is.EqualTo(4));
        Assert.That(CoreReflection.GetField<int>(endless, "TargetScore"), Is.Zero);
        Assert.That(CoreReflection.GetField<int>(endless, "MissLimit"), Is.EqualTo(1));
        Assert.That(CoreReflection.GetField<float>(endless, "CarSpeed"), Is.EqualTo(0.98f));
        Assert.That(CoreReflection.GetField<float>(endless, "SpawnInterval"), Is.EqualTo(0.68f));
        Assert.That(CoreReflection.GetField<float>(endless, "ExpressChance"), Is.EqualTo(0.2f));
        Assert.That(CoreReflection.GetField<float>(endless, "CoveredChance"), Is.EqualTo(0.15f));
        Assert.That(CoreReflection.GetField<float>(endless, "BrokenChance"), Is.EqualTo(0.1f));
        Assert.That(CoreReflection.GetField<int>(endless, "RushEveryCorrect"), Is.EqualTo(7));
        Assert.That(CoreReflection.GetField<int>(endless, "FeverComboThreshold"), Is.EqualTo(6));
    }

    [Test]
    public void StageDatabase_ReleasedStagesIntroduceGimmicksInOrder()
    {
        ScriptableObject database = Resources.Load<ScriptableObject>("StageDatabase");
        Assert.That(database, Is.Not.Null);

        object stageOne = CoreReflection.Call(database, "GetStageDefinition", 1);
        object stageTwo = CoreReflection.Call(database, "GetStageDefinition", 2);
        object stageThree = CoreReflection.Call(database, "GetStageDefinition", 3);
        object stageFour = CoreReflection.Call(database, "GetStageDefinition", 4);
        object stageFive = CoreReflection.Call(database, "GetStageDefinition", 5);

        Assert.That(CoreReflection.GetField<float>(stageOne, "ExpressChance"), Is.Zero);
        Assert.That(CoreReflection.GetField<float>(stageTwo, "ExpressChance"), Is.GreaterThan(0f));
        Assert.That(CoreReflection.GetField<float>(stageThree, "CoveredChance"), Is.GreaterThan(0f));
        Assert.That(CoreReflection.GetField<float>(stageFour, "BrokenChance"), Is.GreaterThan(0f));
        Assert.That(CoreReflection.GetField<int>(stageFive, "RushEveryCorrect"), Is.GreaterThan(0));
        Assert.That(CoreReflection.GetField<int>(stageFive, "FeverComboThreshold"), Is.GreaterThan(0));
    }

    [Test]
    public void StageDatabase_GetStageDefinitionMatchesSafeNumberOrFallsBack()
    {
        Type definitionType = CoreReflection.RequiredType("StageDefinition");
        object stageFive = Stage(definitionType, 5, 50, true);
        object stageTwo = Stage(definitionType, 2, 20, true);
        ScriptableObject database = Database(stageFive, null, stageTwo);

        object matched = CoreReflection.Call(database, "GetStageDefinition", 2);
        object clampedFallback = CoreReflection.Call(database, "GetStageDefinition", -3);

        Assert.That(CoreReflection.GetField<int>(matched, "StageNumber"), Is.EqualTo(2));
        Assert.That(CoreReflection.GetField<int>(clampedFallback, "StageNumber"), Is.EqualTo(5));
    }

    [Test]
    public void StageDatabase_EmptyDatabaseCreatesSafeFallbackStage()
    {
        Type databaseType = CoreReflection.RequiredType("StageDatabase");
        ScriptableObject database = ScriptableObject.CreateInstance(databaseType);

        object fallback = CoreReflection.Call(database, "GetStageDefinition", 0);

        Assert.That(CoreReflection.GetField<int>(fallback, "StageNumber"), Is.EqualTo(1));
    }

    [Test]
    public void StageDatabase_IndexMethodsFindMatchesAndReportMissingStages()
    {
        Type definitionType = CoreReflection.RequiredType("StageDefinition");
        ScriptableObject database = Database(
            Stage(definitionType, 1, 10, true),
            null,
            Stage(definitionType, 4, 40, true));

        object[] hitArgs = { 4, null };
        object[] missArgs = { 2, null };

        Assert.That(CoreReflection.Call(database, "GetStageIndex", 4), Is.EqualTo(2));
        Assert.That(CoreReflection.Call(database, "GetStageIndex", 9), Is.Zero);
        Assert.That(CoreReflection.Call(database, "TryGetStageIndex", hitArgs), Is.True);
        Assert.That(hitArgs[1], Is.EqualTo(2));
        Assert.That(CoreReflection.Call(database, "TryGetStageIndex", missArgs), Is.False);
        Assert.That(missArgs[1], Is.EqualTo(-1));
    }

    [Test]
    public void StageDatabase_GetNextStageDefinitionSkipsNullsAndRequiresCurrentStage()
    {
        Type definitionType = CoreReflection.RequiredType("StageDefinition");
        ScriptableObject database = Database(
            Stage(definitionType, 1, 10, true),
            null,
            Stage(definitionType, 4, 40, true));

        object next = CoreReflection.Call(database, "GetNextStageDefinition", 1);
        object missingCurrent = CoreReflection.Call(database, "GetNextStageDefinition", 2);

        Assert.That(CoreReflection.GetField<int>(next, "StageNumber"), Is.EqualTo(4));
        Assert.That(missingCurrent, Is.Null);
    }

    [Test]
    public void StageDatabase_IsStageUnlockedRequiresPreviousImplementedStageClear()
    {
        Type definitionType = CoreReflection.RequiredType("StageDefinition");
        ScriptableObject database = Database(
            Stage(definitionType, 1, 30, true),
            Stage(definitionType, 2, 60, false),
            Stage(definitionType, 3, 90, true));

        Assert.That(CoreReflection.Call(database, "IsStageUnlocked", 0, null), Is.True);
        Assert.That(CoreReflection.Call(database, "IsStageUnlocked", 1, new Func<int, int>(_ => 999)), Is.False);
        Assert.That(CoreReflection.Call(database, "IsStageUnlocked", 2, new Func<int, int>(_ => 29)), Is.False);
        Assert.That(CoreReflection.Call(database, "IsStageUnlocked", 2, new Func<int, int>(_ => 30)), Is.True);
        Assert.That(CoreReflection.Call(database, "GetRequiredClearStageNumber", 2), Is.EqualTo(1));
        Assert.That(CoreReflection.Call(database, "IsStageUnlocked", -1, new Func<int, int>(_ => 999)), Is.False);
        Assert.That(CoreReflection.Call(database, "GetRequiredClearStageNumber", 0), Is.Zero);
    }

    private static object Stage(Type definitionType, int stageNumber, int targetScore, bool isImplemented)
    {
        object stage = Activator.CreateInstance(definitionType);
        definitionType.GetField("StageNumber").SetValue(stage, stageNumber);
        definitionType.GetField("TargetScore").SetValue(stage, targetScore);
        definitionType.GetField("IsImplemented").SetValue(stage, isImplemented);
        return stage;
    }

    private static ScriptableObject Database(params object[] stages)
    {
        Type definitionType = CoreReflection.RequiredType("StageDefinition");
        Type databaseType = CoreReflection.RequiredType("StageDatabase");
        Array typedStages = Array.CreateInstance(definitionType, stages.Length);
        for (int i = 0; i < stages.Length; i += 1)
        {
            typedStages.SetValue(stages[i], i);
        }

        ScriptableObject database = ScriptableObject.CreateInstance(databaseType);
        CoreReflection.Call(database, "SetStages", typedStages);
        return database;
    }
}
