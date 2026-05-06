using System;
using NUnit.Framework;

public sealed class CoreLogicEditModeTests
{
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
        Assert.That(CoreReflection.GetProperty<bool>(state, "HasReachedTargetScore"), Is.True);
        Assert.That(CoreReflection.GetProperty<bool>(state, "HasReachedMissLimit"), Is.True);
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
