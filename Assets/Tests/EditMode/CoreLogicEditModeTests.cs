using System;
using NUnit.Framework;

public sealed class CoreLogicEditModeTests
{
    [Test]
    public void StageNumberUtility_NormalizesRawNumbersAndIndexes()
    {
        Type utility = CoreReflection.RequiredType("StageNumberUtility");

        Assert.That(CoreReflection.CallStatic(utility, "Normalize", -10), Is.EqualTo(1));
        Assert.That(CoreReflection.CallStatic(utility, "Normalize", 4), Is.EqualTo(4));
        Assert.That(CoreReflection.CallStatic(utility, "FromIndex", -5), Is.EqualTo(1));
        Assert.That(CoreReflection.CallStatic(utility, "FromIndex", 2), Is.EqualTo(3));
    }

    [Test]
    public void StarRatingUtility_ClampsAndScoresClearResultsByMisses()
    {
        Type utility = CoreReflection.RequiredType("StarRatingUtility");
        Type resultType = CoreReflection.RequiredType("GameResultData");
        object perfectClear = CoreReflection.New(resultType, 1, true, 100, 0, 0, 0, 0);
        object oneMissClear = CoreReflection.New(resultType, 1, true, 90, 1, 0, 0, 0);
        object manyMissClear = CoreReflection.New(resultType, 1, true, 80, 4, 0, 0, 0);
        object failedResult = CoreReflection.New(resultType, 1, false, 50, 3, 0, 0, 0);

        Assert.That(CoreReflection.CallStatic(utility, "Clamp", -2), Is.Zero);
        Assert.That(CoreReflection.CallStatic(utility, "Clamp", 99), Is.EqualTo(3));
        Assert.That(CoreReflection.CallStatic(utility, "CalculateForResult", perfectClear), Is.EqualTo(3));
        Assert.That(CoreReflection.CallStatic(utility, "CalculateForResult", oneMissClear), Is.EqualTo(2));
        Assert.That(CoreReflection.CallStatic(utility, "CalculateForResult", manyMissClear), Is.EqualTo(1));
        Assert.That(CoreReflection.CallStatic(utility, "CalculateForResult", failedResult), Is.Zero);
        Assert.That(CoreReflection.CallStatic(utility, "CalculateForResult", new object[] { null }), Is.Zero);
    }

    [Test]
    public void JudgeEvaluator_MapsNullMatchAndMismatchToExpectedResults()
    {
        Type carType = CoreReflection.RequiredType("CarType");
        Type judgeResult = CoreReflection.RequiredType("JudgeResult");
        Type evaluator = CoreReflection.RequiredType("JudgeEvaluator");
        object lightTruck = CoreReflection.EnumValue(carType, "LightTruck");
        object compactCar = CoreReflection.EnumValue(carType, "CompactCar");

        Assert.That(
            CoreReflection.CallStatic(evaluator, "Evaluate", null, lightTruck),
            Is.EqualTo(CoreReflection.EnumValue(judgeResult, "NoCar")));
        Assert.That(
            CoreReflection.CallStatic(evaluator, "Evaluate", lightTruck, lightTruck),
            Is.EqualTo(CoreReflection.EnumValue(judgeResult, "Correct")));
        Assert.That(
            CoreReflection.CallStatic(evaluator, "Evaluate", compactCar, lightTruck),
            Is.EqualTo(CoreReflection.EnumValue(judgeResult, "WrongLane")));
    }

    [Test]
    public void ScoreState_ClampsConstructorArgumentsAndTracksLimitFlags()
    {
        Type scoreStateType = CoreReflection.RequiredType("ScoreState");

        object state = CoreReflection.New(scoreStateType, -10, -2);

        Assert.That(CoreReflection.GetProperty<int>(state, "TargetScore"), Is.Zero);
        Assert.That(CoreReflection.GetProperty<int>(state, "MissLimit"), Is.Zero);
        Assert.That(CoreReflection.GetProperty<bool>(state, "IsEndless"), Is.True);
        Assert.That(CoreReflection.GetProperty<bool>(state, "HasReachedTargetScore"), Is.False);
        Assert.That(CoreReflection.GetProperty<bool>(state, "HasReachedMissLimit"), Is.False);
    }

    [Test]
    public void ScoreState_EndlessModeHasNoTargetAndCanEndOnOneMiss()
    {
        Type carType = CoreReflection.RequiredType("CarType");
        Type scoreStateType = CoreReflection.RequiredType("ScoreState");
        object state = CoreReflection.New(scoreStateType, 0, 1);
        object lightTruck = CoreReflection.EnumValue(carType, "LightTruck");

        CoreReflection.Call(state, "ApplySuccess", lightTruck);
        CoreReflection.Call(state, "ApplySuccess", lightTruck);

        Assert.That(CoreReflection.GetProperty<int>(state, "CurrentScore"), Is.EqualTo(20));
        Assert.That(CoreReflection.GetProperty<int>(state, "RemainingSuccessCount"), Is.Zero);
        Assert.That(CoreReflection.GetProperty<bool>(state, "HasReachedTargetScore"), Is.False);
        Assert.That(CoreReflection.GetProperty<bool>(state, "HasReachedMissLimit"), Is.False);

        CoreReflection.Call(state, "ApplyMiss");

        Assert.That(CoreReflection.GetProperty<bool>(state, "HasReachedMissLimit"), Is.True);
    }

    [Test]
    public void ScoreState_ReviveFromContinueBacksMissesBelowLimitWithoutRestoringScore()
    {
        Type carType = CoreReflection.RequiredType("CarType");
        Type scoreStateType = CoreReflection.RequiredType("ScoreState");
        object endlessState = CoreReflection.New(scoreStateType, 0, 1);
        object regularState = CoreReflection.New(scoreStateType, 20, 3);
        object lightTruck = CoreReflection.EnumValue(carType, "LightTruck");

        CoreReflection.Call(endlessState, "ApplySuccess", lightTruck);
        CoreReflection.Call(endlessState, "ApplyMiss");
        CoreReflection.Call(endlessState, "ReviveFromContinue");

        Assert.That(CoreReflection.GetProperty<int>(endlessState, "CurrentScore"), Is.EqualTo(5));
        Assert.That(CoreReflection.GetProperty<int>(endlessState, "MissCount"), Is.Zero);
        Assert.That(CoreReflection.GetProperty<bool>(endlessState, "HasReachedMissLimit"), Is.False);

        CoreReflection.Call(regularState, "ApplyMiss");
        CoreReflection.Call(regularState, "ApplyMiss");
        CoreReflection.Call(regularState, "ApplyMiss");
        CoreReflection.Call(regularState, "ReviveFromContinue");

        Assert.That(CoreReflection.GetProperty<int>(regularState, "MissCount"), Is.EqualTo(2));
        Assert.That(CoreReflection.GetProperty<bool>(regularState, "HasReachedMissLimit"), Is.False);
    }

    [Test]
    public void ScoreState_AppliesSuccessesMissesAndPerLaneCounts()
    {
        Type carType = CoreReflection.RequiredType("CarType");
        Type scoreStateType = CoreReflection.RequiredType("ScoreState");
        object state = CoreReflection.New(scoreStateType, 20, 2);
        object lightTruck = CoreReflection.EnumValue(carType, "LightTruck");
        object compactCar = CoreReflection.EnumValue(carType, "CompactCar");

        CoreReflection.Call(state, "ApplyMiss");
        Assert.That(CoreReflection.GetProperty<int>(state, "CurrentScore"), Is.Zero);
        Assert.That(CoreReflection.GetProperty<int>(state, "MissCount"), Is.EqualTo(1));

        CoreReflection.Call(state, "ApplySuccess", lightTruck);
        CoreReflection.Call(state, "ApplySuccess", lightTruck);
        CoreReflection.Call(state, "ApplySuccess", compactCar);
        CoreReflection.Call(state, "ApplyMiss");

        Assert.That(CoreReflection.GetProperty<int>(state, "CurrentScore"), Is.EqualTo(25));
        Assert.That(CoreReflection.GetProperty<int>(state, "MissCount"), Is.EqualTo(2));
        Assert.That(CoreReflection.GetProperty<bool>(state, "HasReachedTargetScore"), Is.True);
        Assert.That(CoreReflection.GetProperty<bool>(state, "HasReachedMissLimit"), Is.True);
        Assert.That(CoreReflection.Call(state, "GetCorrectCount", lightTruck), Is.EqualTo(2));
        Assert.That(CoreReflection.Call(state, "GetCorrectCount", compactCar), Is.EqualTo(1));
    }

    [Test]
    public void ScoreState_ExpressAndFeverMultipliersStackAndMissResetsCombo()
    {
        Type carType = CoreReflection.RequiredType("CarType");
        Type scoreStateType = CoreReflection.RequiredType("ScoreState");
        object state = CoreReflection.New(scoreStateType, 100, 3, 3);
        object lightTruck = CoreReflection.EnumValue(carType, "LightTruck");

        Assert.That(CoreReflection.Call(state, "ApplySuccess", lightTruck, 2), Is.EqualTo(20));
        Assert.That(CoreReflection.Call(state, "ApplySuccess", lightTruck), Is.EqualTo(10));
        Assert.That(CoreReflection.Call(state, "ApplySuccess", lightTruck), Is.EqualTo(20));
        Assert.That(CoreReflection.GetProperty<int>(state, "CurrentScore"), Is.EqualTo(50));
        Assert.That(CoreReflection.GetProperty<int>(state, "ComboCount"), Is.EqualTo(3));
        Assert.That(CoreReflection.GetProperty<int>(state, "TotalCorrectCount"), Is.EqualTo(3));
        Assert.That(CoreReflection.GetProperty<bool>(state, "IsFeverActive"), Is.True);

        CoreReflection.Call(state, "ApplyMiss");

        Assert.That(CoreReflection.GetProperty<int>(state, "CurrentScore"), Is.EqualTo(45));
        Assert.That(CoreReflection.GetProperty<int>(state, "ComboCount"), Is.Zero);
        Assert.That(CoreReflection.GetProperty<bool>(state, "IsFeverActive"), Is.False);
    }

    [Test]
    public void CarModifierRules_DefineExpressCoveredAndBrokenBehavior()
    {
        Type modifierType = CoreReflection.RequiredType("CarModifier");
        Type rulesType = CoreReflection.RequiredType("CarModifierRules");
        object express = CoreReflection.EnumValue(modifierType, "Express");
        object covered = CoreReflection.EnumValue(modifierType, "Covered");
        object broken = CoreReflection.EnumValue(modifierType, "Broken");

        Assert.That(CoreReflection.CallStatic(rulesType, "GetScoreMultiplier", express), Is.EqualTo(2));
        Assert.That((float)CoreReflection.CallStatic(rulesType, "GetSpeedMultiplier", express), Is.EqualTo(1.55f).Within(0.001f));
        Assert.That(CoreReflection.CallStatic(rulesType, "StartsCovered", covered), Is.True);
        Assert.That(CoreReflection.CallStatic(rulesType, "RequiresRepair", broken), Is.True);
    }

    [Test]
    public void GameResultData_FromScoreStateCopiesScoreAndLaneCounts()
    {
        Type carType = CoreReflection.RequiredType("CarType");
        Type scoreStateType = CoreReflection.RequiredType("ScoreState");
        Type resultType = CoreReflection.RequiredType("GameResultData");
        object state = CoreReflection.New(scoreStateType, 30, 2);
        object lightTruck = CoreReflection.EnumValue(carType, "LightTruck");
        object sportsCar = CoreReflection.EnumValue(carType, "SportsCar");

        CoreReflection.Call(state, "ApplySuccess", lightTruck);
        CoreReflection.Call(state, "ApplySuccess", sportsCar);
        CoreReflection.Call(state, "ApplyMiss");

        object result = CoreReflection.CallStatic(resultType, "FromScoreState", 3, true, state);

        Assert.That(CoreReflection.GetProperty<int>(result, "StageNumber"), Is.EqualTo(3));
        Assert.That(CoreReflection.GetProperty<bool>(result, "IsClear"), Is.True);
        Assert.That(CoreReflection.GetProperty<int>(result, "Score"), Is.EqualTo(15));
        Assert.That(CoreReflection.GetProperty<int>(result, "MissCount"), Is.EqualTo(1));
        Assert.That(CoreReflection.Call(result, "GetCorrectCount", lightTruck), Is.EqualTo(1));
        Assert.That(CoreReflection.Call(result, "GetCorrectCount", sportsCar), Is.EqualTo(1));
    }

    [Test]
    public void GameResultData_FromScoreStateCopiesGameMode()
    {
        Type gameModeType = CoreReflection.RequiredType("GameMode");
        Type scoreStateType = CoreReflection.RequiredType("ScoreState");
        Type resultType = CoreReflection.RequiredType("GameResultData");
        object endlessMode = CoreReflection.EnumValue(gameModeType, "Endless");
        object state = CoreReflection.New(scoreStateType, 0, 1);

        object result = CoreReflection.CallStatic(resultType, "FromScoreState", endlessMode, 5, false, state);

        Assert.That(CoreReflection.GetProperty<object>(result, "Mode"), Is.EqualTo(endlessMode));
        Assert.That(CoreReflection.GetProperty<bool>(result, "IsEndless"), Is.True);
        Assert.That(CoreReflection.GetProperty<int>(result, "StageNumber"), Is.EqualTo(5));
    }

    [Test]
    public void GameResultData_FromNullScoreStateReturnsEmptyResultForStage()
    {
        Type resultType = CoreReflection.RequiredType("GameResultData");

        object result = CoreReflection.CallStatic(resultType, "FromScoreState", 7, true, null);

        Assert.That(CoreReflection.GetProperty<int>(result, "StageNumber"), Is.EqualTo(7));
        Assert.That(CoreReflection.GetProperty<bool>(result, "IsClear"), Is.False);
        Assert.That(CoreReflection.GetProperty<int>(result, "Score"), Is.Zero);
        Assert.That(CoreReflection.GetProperty<int>(result, "MissCount"), Is.Zero);
    }
}
