using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SortingThemeInstaller
{
    private const string Art = "Assets/Art/Sprites/SortingThemes/";
    private const string Data = "Assets/Resources/SortingThemes/";

    [MenuItem("Tools/Siwakenja/Sorting Themes/Install")]
    public static void Install()
    {
        Directory.CreateDirectory(Data);
        AssetDatabase.Refresh();
        foreach (string path in Directory.GetFiles(Art, "*.png"))
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            // Unity preserves source-space sprite size when reducing the texture.
            // Normalize original dimensions so generated art and clock drawings match.
            importer.spritePixelsPerUnit = Mathf.Max(sourceWidth, sourceHeight) / 15f;
            importer.maxTextureSize = 512;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
        CarVisualDatabase hands = CreateTheme("Hands", "じゃんけん", "手", "回", "けがした手", "手当てする",
            new[] { "グー", "チョキ", "パー" }, new[] { "rock", "scissors", "paper" });
        CarVisualDatabase clocks = CreateTheme("Clocks", "時計", "時計", "個", "故障した時計", "修理へ送る",
            new[] { "3時", "6時", "9時" }, new[] { "clock-3", "clock-6", "clock-9" });
        CarVisualDatabase greens = CreateTheme("Greens", "葉野菜", "野菜", "個", "傷んだ野菜", "取り除く",
            new[] { "キャベツ", "レタス", "白菜" }, new[] { "cabbage", "lettuce", "napa" });
        CarVisualDatabase beans = CreateTheme("Beans", "豆類", "豆", "個", "傷んだ豆", "取り除く",
            new[] { "インゲン", "サヤエンドウ", "枝豆" }, new[] { "green-bean", "snow-pea", "edamame" });
        CarVisualDatabase reptiles = CreateTheme("Reptiles", "爬虫類", "生き物", "匹", "具合の悪い生き物", "保護へ送る",
            new[] { "トカゲ", "ヤモリ", "レオパ" }, new[] { "lizard", "gecko", "leopard-gecko" });
        CarVisualDatabase poses = CreateTheme("Poses", "ポーズ", "ポーズ", "回", "くずれたポーズ", "やり直す",
            new[] { "逆立ち", "三点倒立", "ブリッジ" }, new[] { "handstand", "tripod-headstand", "bridge-pose" });
        CarVisualDatabase eras = CreateTheme("Eras", "時代", "人", "人", "疲れた人", "休ませる",
            new[] { "原始人", "侍", "現代人" }, new[] { "ancient-person", "samurai", "modern-person" });
        CarVisualDatabase hiragana = CreateTheme("Hiragana", "ひらがな", "文字", "文字", "かすれた文字", "書き直す",
            new[] { "め", "の", "ゆ" }, new[] { "hiragana-me", "hiragana-no", "hiragana-yu" });
        StageDatabase stages = Resources.Load<StageDatabase>("StageDatabase");
        foreach (StageDefinition stage in stages.Stages)
        {
            if (stage == null || stage.StageNumber > 15) continue;
            stage.VisualTheme = stage.StageNumber <= 3 ? CarVisualDatabase.LoadDefault()
                : stage.StageNumber <= 7 ? hands : stage.StageNumber <= 10 ? clocks : greens;
            stage.MissionText = stage.MissionText.Replace("車種", "種類").Replace("台", stage.Theme.CountUnit);
            if (stage.StageNumber == 11) stage.DisplayName = "野菜の仕分け";
            if (stage.StageNumber == 12) stage.DisplayName = "傷みを見抜け";
        }
        List<StageDefinition> definitions = new(stages.Stages);
        AddStageIfMissing(stages, definitions, 16, "豆の見分け", "12個を仕分けろ", beans,
            StageGoalType.TotalCorrect, 12, 0.68f, 1.05f);
        AddStageIfMissing(stages, definitions, 17, "いきもの図鑑", "各種類を4匹ずつ仕分けろ", reptiles,
            StageGoalType.VehicleQuota, 12, 0.70f, 1.00f);
        AddStageIfMissing(stages, definitions, 18, "ポーズ判定", "10コンボへ到達しろ", poses,
            StageGoalType.Combo, 10, 0.72f, 0.95f);
        AddStageIfMissing(stages, definitions, 19, "時代をこえて", "30秒間生き残れ", eras,
            StageGoalType.Survival, 30, 0.74f, 0.90f);
        AddStageIfMissing(stages, definitions, 20, "ひらがな検定", "180点へ到達しろ", hiragana,
            StageGoalType.Score, 180, 0.76f, 0.85f);
        stages.SetStages(definitions.ToArray());
        EditorUtility.SetDirty(stages);
        AssetDatabase.SaveAssets();
        Debug.Log("Sorting themes installed: 20 stages across four chapters.");
    }

    private static void AddStageIfMissing(StageDatabase database, List<StageDefinition> definitions,
        int number, string name, string mission, CarVisualDatabase theme, StageGoalType goal,
        int goalValue, float speed, float interval)
    {
        if (database.TryGetStageIndex(number, out _)) return;
        definitions.Add(new StageDefinition
        {
            StageNumber = number,
            ChapterNumber = 4,
            DisplayName = name,
            MissionText = mission,
            VisualTheme = theme,
            GoalType = goal,
            GoalValue = goalValue,
            TargetScore = goal == StageGoalType.Score ? goalValue : 0,
            LightTruckQuota = goal == StageGoalType.VehicleQuota ? 4 : 0,
            CompactCarQuota = goal == StageGoalType.VehicleQuota ? 4 : 0,
            SportsCarQuota = goal == StageGoalType.VehicleQuota ? 4 : 0,
            SurvivalSeconds = goal == StageGoalType.Survival ? goalValue : 0,
            MissLimit = 3,
            AllowRewardedContinue = true,
            CarSpeed = speed,
            SpawnInterval = interval,
            WeightLightTruck = 1,
            WeightCompactCar = 1,
            WeightSportsCar = 1,
            ExpressChance = 0,
            CoveredChance = 0,
            BrokenChance = 0,
            RushEveryCorrect = 0,
            FeverComboThreshold = 0,
            SpawnPattern = goal == StageGoalType.VehicleQuota ? SpawnPattern.BalancedBag : SpawnPattern.WeightedRandom,
            MasteryMetric = MasteryMetric.MissCount,
            TwoStarThreshold = 1,
            ThreeStarThreshold = 0,
            IsImplemented = true
        });
    }

    private static CarVisualDatabase CreateTheme(string id, string title, string noun, string unit, string broken, string repair, string[] names, string[] sprites)
    {
        string path = Data + id + ".asset";
        CarVisualDatabase theme = AssetDatabase.LoadAssetAtPath<CarVisualDatabase>(path);
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<CarVisualDatabase>();
            AssetDatabase.CreateAsset(theme, path);
        }
        SerializedObject data = new(theme);
        data.FindProperty("_themeName").stringValue = title;
        data.FindProperty("_noun").stringValue = noun;
        data.FindProperty("_countUnit").stringValue = unit;
        data.FindProperty("_brokenName").stringValue = broken;
        data.FindProperty("_repairAction").stringValue = repair;
        data.FindProperty("_isVehicleTheme").boolValue = false;
        data.FindProperty("_coveredSprite").objectReferenceValue = LoadSprite("mystery-parcel");
        SerializedProperty entries = data.FindProperty("_entries");
        entries.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("_carType").enumValueIndex = i;
            entry.FindPropertyRelative("_displayName").stringValue = names[i];
            entry.FindPropertyRelative("_bodySprite").objectReferenceValue = LoadSprite(sprites[i]);
            entry.FindPropertyRelative("_iconSprite").objectReferenceValue = LoadSprite(sprites[i]);
        }
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(theme);
        return theme;
    }

    private static Sprite LoadSprite(string name)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png");
        return sprite != null ? sprite : throw new InvalidOperationException("Missing theme sprite: " + name);
    }
}
