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
    public void FifteenStageAllocationAndChallengeModesAreExplicit()
    {
        ScriptableObject stages = Resources.Load<ScriptableObject>("StageDatabase");
        for (int n = 1; n <= 15; n++)
        {
            string expected = n <= 3 ? "くるま" : n <= 7 ? "じゃんけん" : n <= 10 ? "時計" : "葉野菜";
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
        Assert.That(CoreReflection.GetProperty<Sprite>(Theme(1, "Stage"), "CoveredSprite"), Is.Not.SameAs(parcel));
    }

    private static object Theme(int stage, string mode) => CoreReflection.CallStatic(CoreReflection.RequiredType("CarVisualDatabase"), "LoadForStage", stage, Enum.Parse(CoreReflection.RequiredType("GameMode"), mode));
}
