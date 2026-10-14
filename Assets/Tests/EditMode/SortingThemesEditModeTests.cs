using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class SortingThemesEditModeTests
{
    [TestCase(1, "くるま", "トラック", "普通車", "スポーツカー", "台")]
    [TestCase(4, "じゃんけん", "グー", "チョキ", "パー", "回")]
    [TestCase(8, "時計", "3時", "6時", "9時", "個")]
    [TestCase(11, "葉野菜", "キャベツ", "レタス", "白菜", "個")]
    [TestCase(16, "豆類", "インゲン", "サヤエンドウ", "枝豆", "個")]
    [TestCase(17, "爬虫類", "トカゲ", "ヤモリ", "レオパ", "匹")]
    [TestCase(18, "ポーズ", "逆立ち", "三点倒立", "ブリッジ", "回")]
    [TestCase(19, "時代", "原始人", "侍", "現代人", "人")]
    [TestCase(20, "ひらがな", "め", "の", "ゆ", "文字")]
    public void NamedThemeHasThreeDistinctProductionSprites(int stageNumber, string title, string a, string b, string c, string unit)
    {
        object theme = Theme(stageNumber, "Stage");
        Assert.That(CoreReflection.GetProperty<string>(theme, "ThemeName"), Is.EqualTo(title));
        Assert.That(CoreReflection.GetProperty<string>(theme, "CountUnit"), Is.EqualTo(unit));
        HashSet<Sprite> sprites = new();
        string[] names = { a, b, c };
        for (int i = 0; i < 3; i++)
        {
            object type = Enum.ToObject(CoreReflection.RequiredType("CarType"), i);
            Assert.That(CoreReflection.Call(theme, "GetDisplayName", type), Is.EqualTo(names[i]));
            Sprite body = (Sprite)CoreReflection.Call(theme, "GetBodySprite", type);
            Sprite icon = (Sprite)CoreReflection.Call(theme, "GetIconSprite", type);
            Assert.That(body, Is.Not.Null);
            Assert.That(icon, Is.Not.Null);
            Assert.That(body.texture.width, Is.GreaterThanOrEqualTo(256));
            if (stageNumber != 1)
                Assert.That(Mathf.Max(body.bounds.size.x, body.bounds.size.y), Is.EqualTo(15f).Within(0.1f), "Clock and generated images must have the same readable world-space size.");
            Assert.That(sprites.Add(body), Is.True, "Each slot needs a distinct image, not a placeholder repeated three times.");
        }
        Assert.That(CoreReflection.GetProperty<Sprite>(theme, "CoveredSprite"), Is.Not.Null);
    }

    [Test]
    public void ThirtyStageAllocationAndChallengeModesAreExplicit()
    {
        ScriptableObject stages = Resources.Load<ScriptableObject>("StageDatabase");
        string[] chapterFour = { "豆類", "爬虫類", "ポーズ", "時代", "ひらがな" };
        for (int n = 1; n <= 30; n++)
        {
            string expected = n <= 3 ? "くるま" : n <= 7 ? "じゃんけん" : n <= 10 ? "時計"
                : n <= 15 ? "葉野菜" : chapterFour[(n - 16) % 5];
            object stage = CoreReflection.Call(stages, "GetStageDefinition", n);
            Assert.That(CoreReflection.GetField<object>(stage, "VisualTheme"), Is.SameAs(Theme(n, "Stage")));
            Assert.That(CoreReflection.GetProperty<string>(Theme(n, "Stage"), "ThemeName"), Is.EqualTo(expected));
            object endless = CoreReflection.Call(stages, "GetEndlessStageDefinition", n);
            Assert.That(CoreReflection.GetProperty<object>(endless, "Theme"), Is.SameAs(Theme(n, "Stage")));
            Assert.That(CoreReflection.GetProperty<string>(Theme(n, "Daily"), "ThemeName"), Is.EqualTo("くるま"));
        }
    }

    [Test]
    public void AllNonVehicleThemesHideIdentityBehindTheSameParcel()
    {
        Sprite parcel = CoreReflection.GetProperty<Sprite>(Theme(4, "Stage"), "CoveredSprite");
        Assert.That(CoreReflection.GetProperty<Sprite>(Theme(8, "Stage"), "CoveredSprite"), Is.SameAs(parcel));
        Assert.That(CoreReflection.GetProperty<Sprite>(Theme(11, "Stage"), "CoveredSprite"), Is.SameAs(parcel));
        foreach (int stage in new[] { 16, 17, 18, 19, 20 })
            Assert.That(CoreReflection.GetProperty<Sprite>(Theme(stage, "Stage"), "CoveredSprite"), Is.SameAs(parcel));
        Assert.That(CoreReflection.GetProperty<Sprite>(Theme(1, "Stage"), "CoveredSprite"), Is.Not.SameAs(parcel));
    }

    [Test]
    public void LateThemesReturnThreeTimesWithDifferentGoalsAndIncreasingPace()
    {
        ScriptableObject database = Resources.Load<ScriptableObject>("StageDatabase");
        for (int number = 16; number <= 30; number++)
        {
            object stage = CoreReflection.Call(database, "GetStageDefinition", number);
            Assert.That(CoreReflection.GetField<int>(stage, "StageNumber"), Is.EqualTo(number));
            Assert.That(CoreReflection.GetField<int>(stage, "ChapterNumber"), Is.EqualTo((number - 1) / 5 + 1));
            Assert.That(CoreReflection.GetField<int>(stage, "GoalValue"), Is.GreaterThanOrEqualTo(24));
            Assert.That(CoreReflection.GetField<int>(stage, "MissLimit"), Is.EqualTo(3));
            Assert.That(CoreReflection.GetField<bool>(stage, "AllowRewardedContinue"), Is.True);
            Assert.That(CoreReflection.GetField<float>(stage, "ExpressChance"), Is.GreaterThan(0f));
            Assert.That(CoreReflection.GetField<float>(stage, "CoveredChance"), Is.GreaterThan(0f));
            Assert.That(CoreReflection.GetField<float>(stage, "BrokenChance"), Is.GreaterThan(0f));
            Assert.That(CoreReflection.GetField<int>(stage, "RushEveryCorrect"), Is.GreaterThan(0));
            Assert.That(CoreReflection.GetField<int>(stage, "FeverComboThreshold"), Is.GreaterThan(0));
            Assert.That(CoreReflection.GetField<int>(stage, "WeightLightTruck"), Is.EqualTo(1));
            Assert.That(CoreReflection.GetField<int>(stage, "WeightCompactCar"), Is.EqualTo(1));
            Assert.That(CoreReflection.GetField<int>(stage, "WeightSportsCar"), Is.EqualTo(1));
            bool quota = CoreReflection.GetField<object>(stage, "GoalType").ToString() == "VehicleQuota";
            Assert.That(CoreReflection.GetField<object>(stage, "SpawnPattern").ToString(), Is.EqualTo(quota ? "BalancedBag" : "WeightedRandom"));
            if (quota)
            {
                int each = CoreReflection.GetField<int>(stage, "LightTruckQuota");
                Assert.That(each, Is.GreaterThanOrEqualTo(10));
                Assert.That(CoreReflection.GetField<int>(stage, "CompactCarQuota"), Is.EqualTo(each));
                Assert.That(CoreReflection.GetField<int>(stage, "SportsCarQuota"), Is.EqualTo(each));
                Assert.That(CoreReflection.GetField<int>(stage, "GoalValue"), Is.EqualTo(each * 3));
            }
            Assert.That(CoreReflection.GetField<object>(stage, "MasteryMetric").ToString(), Is.EqualTo("MissCount"));
            Assert.That(CoreReflection.GetField<float>(stage, "TwoStarThreshold"), Is.EqualTo(1f));
            Assert.That(CoreReflection.GetField<float>(stage, "ThreeStarThreshold"), Is.Zero);
            Type resultType = CoreReflection.RequiredType("GameResultData");
            Type rating = CoreReflection.RequiredType("StarRatingUtility");
            for (int misses = 0; misses <= 2; misses++)
            {
                object clear = CoreReflection.New(resultType, number, true, 200, misses, 4, 4, 4);
                Assert.That(CoreReflection.CallStatic(rating, "CalculateForResult", clear, stage),
                    Is.EqualTo(3 - misses), $"stage {number}, misses {misses}");
            }
            if (number >= 21)
            {
                object previous = CoreReflection.Call(database, "GetStageDefinition", number - 5);
                Assert.That(CoreReflection.GetField<object>(stage, "VisualTheme"), Is.SameAs(CoreReflection.GetField<object>(previous, "VisualTheme")));
                Assert.That(CoreReflection.GetField<float>(stage, "CarSpeed"), Is.GreaterThan(CoreReflection.GetField<float>(previous, "CarSpeed")));
                Assert.That(CoreReflection.GetField<float>(stage, "SpawnInterval"), Is.LessThan(CoreReflection.GetField<float>(previous, "SpawnInterval")));
            }
        }
        for (int first = 16; first <= 20; first++)
        {
            HashSet<string> goals = new();
            for (int number = first; number <= 30; number += 5)
                goals.Add(CoreReflection.GetField<object>(CoreReflection.Call(database, "GetStageDefinition", number), "GoalType").ToString());
            Assert.That(goals.Count, Is.GreaterThanOrEqualTo(2), $"theme at stage {first} needs distinct challenges");
            object basic = CoreReflection.Call(database, "GetStageDefinition", first);
            object final = CoreReflection.Call(database, "GetStageDefinition", first + 10);
            Assert.That(CoreReflection.GetField<object>(final, "GoalType"), Is.EqualTo(CoreReflection.GetField<object>(basic, "GoalType")));
            Assert.That(CoreReflection.GetField<int>(final, "GoalValue"), Is.GreaterThan(CoreReflection.GetField<int>(basic, "GoalValue")));
        }

        object next = CoreReflection.Call(database, "GetNextStageDefinition", 15);
        Assert.That(CoreReflection.GetField<int>(next, "StageNumber"), Is.EqualTo(16));
        Assert.That(CoreReflection.GetField<int>(CoreReflection.Call(database, "GetNextStageDefinition", 20), "StageNumber"), Is.EqualTo(21));
        Assert.That(CoreReflection.Call(database, "GetNextStageDefinition", 30), Is.Null);
        Func<int, bool> onlyFifteenCleared = number => number == 15;
        Assert.That(CoreReflection.Call(database, "IsStageUnlocked", 15, onlyFifteenCleared), Is.True);
        Assert.That(CoreReflection.Call(database, "IsStageUnlocked", 16, onlyFifteenCleared), Is.False);
    }

    private static object Theme(int stage, string mode) => CoreReflection.CallStatic(CoreReflection.RequiredType("CarVisualDatabase"), "LoadForStage", stage, Enum.Parse(CoreReflection.RequiredType("GameMode"), mode));
}
