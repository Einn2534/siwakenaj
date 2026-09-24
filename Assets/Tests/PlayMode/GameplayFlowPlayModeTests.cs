using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed partial class GameplayFlowPlayModeTests
{
    private const float SceneTimeoutSeconds = 12f;
    private const float ResultUnlockTimeoutSeconds = 5f;
    private const float TestTimeScale = 20f;
    private const int CampaignStageCount = 20;

    private static readonly string[] BasePlayerPrefsKeys =
    {
        "BGM_On",
        "SE_On",
        "Vibration_On",
        "HowTo_Shown",
        "Tutorial_Completed",
        "Tutorial_Skipped",
        "ExpressTutorial_Seen",
        "SelectedStage",
        "LastStage",
        "LastGameMode",
        "BestScore_Stage1",
        "StarRating_Stage1",
        "ProgressMigrationVersion",
        "Interstitial_CompletedSinceLast",
        "GimmickHintSeen_Express",
        "GimmickHintSeen_Covered",
        "GimmickHintSeen_Broken"
    };
    private static readonly string[] PlayerPrefsKeys = BuildIntPlayerPrefsKeys();
    private static readonly string[] StringPlayerPrefsKeys =
    {
        "DailyChallengeSaveDataV1",
        "Interstitial_LastShownUtcTicks"
    };
    private static readonly string[] FloatPlayerPrefsKeys = BuildFloatPlayerPrefsKeys();

    private readonly Dictionary<string, SavedIntPreference> _savedPreferences = new();
    private readonly Dictionary<string, SavedStringPreference> _savedStringPreferences = new();
    private readonly Dictionary<string, SavedFloatPreference> _savedFloatPreferences = new();
    private string _telemetryPath;
    private long _telemetryOriginalLength;
    private bool _telemetryFileExisted;

    [OneTimeSetUp]
    public void SavePlayerPreferences()
    {
        foreach (string key in PlayerPrefsKeys)
        {
            _savedPreferences[key] = new SavedIntPreference(PlayerPrefs.HasKey(key), PlayerPrefs.GetInt(key));
        }

        foreach (string key in StringPlayerPrefsKeys)
        {
            _savedStringPreferences[key] = new SavedStringPreference(PlayerPrefs.HasKey(key), PlayerPrefs.GetString(key, string.Empty));
        }

        foreach (string key in FloatPlayerPrefsKeys)
        {
            _savedFloatPreferences[key] = new SavedFloatPreference(PlayerPrefs.HasKey(key), PlayerPrefs.GetFloat(key));
        }

        Type telemetryType = RequiredType("LocalTelemetryService");
        _telemetryPath = (string)GetStaticProperty(telemetryType, "OutputPath");
        _telemetryFileExisted = File.Exists(_telemetryPath);
        _telemetryOriginalLength = _telemetryFileExisted ? new FileInfo(_telemetryPath).Length : 0L;
    }

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.SetInt("BGM_On", 0);
        PlayerPrefs.SetInt("SE_On", 0);
        PlayerPrefs.SetInt("Vibration_On", 0);
        PlayerPrefs.SetInt("HowTo_Shown", 1);
        PlayerPrefs.SetInt("Tutorial_Completed", 1);
        PlayerPrefs.SetInt("Tutorial_Skipped", 0);
        PlayerPrefs.SetInt("ExpressTutorial_Seen", 1);
        PlayerPrefs.SetInt("SelectedStage", 1);
        PlayerPrefs.SetInt("LastStage", 1);
        PlayerPrefs.SetInt("LastGameMode", 0);
        PlayerPrefs.SetInt("BestScore_Stage1", 0);
        PlayerPrefs.SetInt("StarRating_Stage1", 0);
        PlayerPrefs.DeleteKey("DailyChallengeSaveDataV1");
        PlayerPrefs.DeleteKey("Interstitial_LastShownUtcTicks");
        PlayerPrefs.SetInt("Interstitial_CompletedSinceLast", 0);
        PlayerPrefs.DeleteKey("ProgressMigrationVersion");
        PlayerPrefs.DeleteKey("GimmickHintSeen_Express");
        PlayerPrefs.DeleteKey("GimmickHintSeen_Covered");
        PlayerPrefs.DeleteKey("GimmickHintSeen_Broken");
        for (int i = 1; i <= CampaignStageCount; i += 1)
        {
            PlayerPrefs.DeleteKey($"StageClear_{i}");
            PlayerPrefs.DeleteKey($"BestTime_Stage{i}");
            PlayerPrefs.SetInt($"BestScore_Stage{i}", 0);
            PlayerPrefs.SetInt($"StarRating_Stage{i}", 0);
        }
        PlayerPrefs.Save();

        InvokeStatic(RequiredType("SessionState"), "SelectStage", 1);
        Time.timeScale = TestTimeScale;
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
    }

    [OneTimeTearDown]
    public void RestorePlayerPreferences()
    {
        foreach (KeyValuePair<string, SavedIntPreference> pair in _savedPreferences)
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

        foreach (KeyValuePair<string, SavedStringPreference> pair in _savedStringPreferences)
        {
            if (pair.Value.Existed)
            {
                PlayerPrefs.SetString(pair.Key, pair.Value.Value);
            }
            else
            {
                PlayerPrefs.DeleteKey(pair.Key);
            }
        }

        foreach (KeyValuePair<string, SavedFloatPreference> pair in _savedFloatPreferences)
        {
            if (pair.Value.Existed)
            {
                PlayerPrefs.SetFloat(pair.Key, pair.Value.Value);
            }
            else
            {
                PlayerPrefs.DeleteKey(pair.Key);
            }
        }

        PlayerPrefs.Save();

        if (_telemetryFileExisted && File.Exists(_telemetryPath))
        {
            using FileStream restore = new(_telemetryPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            restore.SetLength(_telemetryOriginalLength);
        }
        else if (!_telemetryFileExisted && File.Exists(_telemetryPath))
        {
            File.Delete(_telemetryPath);
        }
    }

    [UnityTest]
    [Order(1)]
    public IEnumerator PointerDownScenarios_Clear_Result_AndRetryCompleteEndToEnd()
    {
        yield return LoadScene("Main");
        object gameFlow = FindSceneComponent("GameFlowController");
        yield return WaitUntil(() => GetBool(gameFlow, "IsPlaying"), SceneTimeoutSeconds, "Main did not enter Playing state.");

        object scoreManager = FindSceneComponent("ScoreManager");
        object carSpawner = FindSceneComponent("CarSpawner");
        object laneInput = FindSceneComponent("LaneInputController");
        Assert.That(scoreManager, Is.Not.Null);
        Assert.That(carSpawner, Is.Not.Null);
        Assert.That(laneInput, Is.Not.Null);
        UnityEngine.UI.Button earlyStageRepairButton = FindRepairButton();
        Assert.That(
            earlyStageRepairButton.gameObject.activeInHierarchy,
            Is.False,
            "Repair button should stay hidden on stages that cannot spawn broken cars.");

        Invoke(carSpawner, "StopSpawning");
        Invoke(carSpawner, "DespawnAllCars");
        yield return null;

        AssertSpawnerLimit(carSpawner);
        Invoke(carSpawner, "DespawnAllCars");
        yield return null;

        PointerDown(laneInput, "LightTruck");
        yield return SettleInput();
        AssertScore(scoreManager, score: 0, misses: 1);

        object mistappedCar = SpawnFixedCar(carSpawner, "CompactCar");
        PointerDown(laneInput, "LightTruck");
        yield return SettleInput();
        AssertScore(scoreManager, score: 0, misses: 2);

        PointerDown(laneInput, "LightTruck");
        yield return SettleInput();
        AssertScore(scoreManager, score: 0, misses: 2);

        Invoke(gameFlow, "OnCarMissed", mistappedCar);
        yield return null;
        AssertScore(scoreManager, score: 0, misses: 2);
        Invoke(carSpawner, "DespawnAllCars");
        yield return null;

        SpawnFixedCar(carSpawner, "LightTruck");
        PointerDown(laneInput, "LightTruck");
        yield return SettleInput();
        AssertScore(scoreManager, score: 10, misses: 2);
        Assert.That(Invoke(scoreManager, "GetCorrectCount", EnumValue("CarType", "LightTruck")), Is.EqualTo(1));

        SpawnFixedCar(carSpawner, "SportsCar");
        PointerDown(laneInput, "LightTruck");
        PointerDown(laneInput, "SportsCar");
        yield return SettleInput();
        AssertScore(scoreManager, score: 20, misses: 2);
        Assert.That(Invoke(scoreManager, "GetCorrectCount", EnumValue("CarType", "SportsCar")), Is.EqualTo(1),
            "The last Pointer Down in the frame should win and only one answer should be processed.");

        for (int i = 0; i < 10; i += 1)
        {
            SpawnFixedCar(carSpawner, "CompactCar");
            PointerDown(laneInput, "CompactCar");
            yield return SettleInput();
        }

        yield return WaitForScene("Result", SceneTimeoutSeconds);
        AssertStoredResult(isClear: true, expectedScore: 120, expectedMisses: 2);
        yield return RetryFromResult();
    }

    [UnityTest]
    [Order(2)]
    public IEnumerator MissLimit_GiveUp_Result_AndRetryCompleteEndToEnd()
    {
        yield return LoadScene("Main");
        object gameFlow = FindSceneComponent("GameFlowController");
        yield return WaitUntil(() => GetBool(gameFlow, "IsPlaying"), SceneTimeoutSeconds, "Main did not enter Playing state.");

        object scoreManager = FindSceneComponent("ScoreManager");
        object carSpawner = FindSceneComponent("CarSpawner");
        object laneInput = FindSceneComponent("LaneInputController");
        Invoke(carSpawner, "StopSpawning");
        Invoke(carSpawner, "DespawnAllCars");
        yield return null;

        for (int i = 0; i < 3; i += 1)
        {
            PointerDown(laneInput, "LightTruck");
            yield return SettleInput();
        }

        AssertScore(scoreManager, score: 0, misses: 3);
        object continuePrompt = FindSceneComponent("ContinuePromptController");
        Assert.That(continuePrompt, Is.Not.Null, "The miss limit should open the continue prompt before game over.");
        object giveUpButton = GetField(continuePrompt, "_giveUpButton");
        object onClick = GetProperty(giveUpButton, "onClick");
        Invoke(onClick, "Invoke");

        yield return WaitForScene("Result", SceneTimeoutSeconds);
        AssertStoredResult(isClear: false, expectedScore: 0, expectedMisses: 3);
        yield return RetryFromResult();
    }

    [UnityTest]
    [Order(3)]
    public IEnumerator ExpressCoveredAndBrokenCarsUseTheirDedicatedRules()
    {
        InvokeStatic(RequiredType("SessionState"), "SelectStage", 11);
        yield return LoadScene("Main");
        object gameFlow = FindSceneComponent("GameFlowController");
        yield return WaitUntil(() => GetBool(gameFlow, "IsPlaying"), SceneTimeoutSeconds, "Main did not enter Playing state.");

        object scoreManager = FindSceneComponent("ScoreManager");
        object carSpawner = FindSceneComponent("CarSpawner");
        object laneInput = FindSceneComponent("LaneInputController");
        Invoke(carSpawner, "StopSpawning");
        Invoke(carSpawner, "DespawnAllCars");
        yield return null;

        UnityEngine.UI.Button repairButton = FindRepairButton();
        Assert.That(repairButton, Is.Not.Null);
        Assert.That(repairButton.gameObject.activeInHierarchy, Is.True, "Repair button should remain visible throughout a repair stage.");
        Assert.That(repairButton.interactable, Is.False, "Repair button should be disabled when there is no active car.");

        SpawnFixedCar(carSpawner, "LightTruck", "Express");
        Assert.That(repairButton.interactable, Is.False, "Repair button should reject non-broken cars.");
        RepairPointerDown();
        yield return SettleInput();
        AssertScore(scoreManager, score: 0, misses: 0);

        PointerDown(laneInput, "LightTruck");
        yield return SettleInput();
        AssertScore(scoreManager, score: 20, misses: 0);
        Assert.That(repairButton.interactable, Is.False);

        object coveredCar = SpawnFixedCar(carSpawner, "CompactCar", "Covered");
        Assert.That(GetProperty(coveredCar, "IsRevealed"), Is.False);
        Sprite vehicleCover = Resources.Load<Sprite>("Gameplay/covered_vehicle");
        Assert.That(vehicleCover, Is.Not.Null, "The original vehicle cover must remain available.");
        Assert.That(vehicleCover.bounds.size.x, Is.EqualTo(9.9f).Within(0.05f));
        object theme = InvokeStatic(RequiredType("CarVisualDatabase"), "LoadSelected");
        Sprite coveredSprite = (Sprite)GetProperty(theme, "CoveredSprite");
        Assert.That(coveredSprite, Is.Not.Null);
        Assert.That(coveredSprite, Is.Not.SameAs(vehicleCover), "Stage 11 uses the neutral parcel for vegetables.");
        SpriteRenderer coveredRenderer = (coveredCar as Component)?.GetComponent<SpriteRenderer>();
        Assert.That(coveredRenderer, Is.Not.Null);
        Assert.That(coveredRenderer.sprite, Is.SameAs(coveredSprite), "Covered objects should use their theme's neutral silhouette.");
        PointerDown(laneInput, "CompactCar");
        yield return SettleInput();
        AssertScore(scoreManager, score: 15, misses: 1);

        Invoke(coveredCar, "Reveal");
        Assert.That(coveredRenderer.sprite, Is.Not.SameAs(coveredSprite), "Reveal should restore the actual vehicle sprite.");
        Assert.That(coveredRenderer.sprite, Is.SameAs(Invoke(theme, "GetBodySprite", EnumValue("CarType", "CompactCar"))));
        PointerDown(laneInput, "CompactCar");
        yield return SettleInput();
        AssertScore(scoreManager, score: 25, misses: 1);

        SpawnFixedCar(carSpawner, "SportsCar", "Broken");
        Assert.That(repairButton.interactable, Is.True, "Repair button should only enable for the active broken car.");
        RepairPointerDown();
        yield return SettleInput();
        AssertScore(scoreManager, score: 35, misses: 1);
        Assert.That(repairButton.interactable, Is.False, "Repair button should disable immediately after resolving the broken car.");
    }

    [UnityTest]
    [Order(4)]
    public IEnumerator StageFourteenRushSpawnsAThreeCarProcessionAndShowsFeverHud()
    {
        InvokeStatic(RequiredType("SessionState"), "SelectStage", 14);
        yield return LoadScene("Main");
        object gameFlow = FindSceneComponent("GameFlowController");
        yield return WaitUntil(() => GetBool(gameFlow, "IsPlaying"), SceneTimeoutSeconds, "Main did not enter Playing state.");

        object carSpawner = FindSceneComponent("CarSpawner");
        object gimmickHud = FindSceneComponent("GimmickHudController");
        object progressHud = FindSceneComponent("StageProgressHudController");
        Assert.That(carSpawner, Is.Not.Null);
        Assert.That(gimmickHud, Is.Not.Null);
        Assert.That(progressHud, Is.Not.Null);

        Canvas gimmickCanvas = (gimmickHud as Component)?.GetComponentInChildren<Canvas>(true);
        Canvas progressCanvas = FindSceneCanvas("StageProgressHudCanvas");
        Assert.That(gimmickCanvas, Is.Not.Null);
        Assert.That(progressCanvas, Is.Not.Null);
        Assert.That(gimmickCanvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceCamera));
        Assert.That(progressCanvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceCamera));
        Assert.That(gimmickCanvas.worldCamera, Is.SameAs(Camera.main));
        Assert.That(progressCanvas.worldCamera, Is.SameAs(Camera.main));

        RectTransform comboRoot = GetField(gimmickHud, "_comboRoot") as RectTransform;
        RectTransform repairRoot = GetField(gimmickHud, "_repairRoot") as RectTransform;
        Assert.That(comboRoot, Is.Not.Null);
        Assert.That(repairRoot, Is.Not.Null);
        Assert.That(comboRoot.gameObject.activeInHierarchy, Is.True, "Stage 14 should expose the fever combo HUD.");
        Assert.That(comboRoot.rect.width, Is.GreaterThan(0f));
        Assert.That(comboRoot.rect.height, Is.GreaterThan(0f));

        Canvas.ForceUpdateCanvases();
        Rect repairScreenRect = GetScreenRect(repairRoot, gimmickCanvas.worldCamera);
        Component laneInput = FindSceneComponent("LaneInputController") as Component;
        Assert.That(laneInput, Is.Not.Null);
        UnityEngine.UI.Button[] laneButtons = laneInput.GetComponentsInChildren<UnityEngine.UI.Button>(true)
            .Where(button => button.transform.parent == laneInput.transform)
            .ToArray();
        Assert.That(laneButtons, Has.Length.EqualTo(3));
        foreach (UnityEngine.UI.Button laneButton in laneButtons)
        {
            Rect laneScreenRect = GetScreenRect(laneButton.transform as RectTransform, Camera.main);
            Assert.That(
                repairScreenRect.Overlaps(laneScreenRect),
                Is.False,
                $"Repair button overlaps lane button '{laneButton.name}'.");
        }

        Invoke(carSpawner, "StopSpawning");
        Invoke(carSpawner, "DespawnAllCars");
        Invoke(carSpawner, "StartSpawning");
        Assert.That(Invoke(carSpawner, "TryStartRush"), Is.EqualTo(true));

        yield return WaitUntil(
            () => (int)GetProperty(carSpawner, "LastRushSpawnCount") == 3,
            2f,
            "Rush should send all three cars instead of dropping attempts blocked by the safe spawn gap.");

        Invoke(carSpawner, "StopSpawning");
        Invoke(carSpawner, "DespawnAllCars");
    }

    [UnityTest]
    [Order(5)]
    public IEnumerator TitleCampaignRouteAndChapterTabsUseRealSceneControls()
    {
        PlayerPrefs.SetInt("StageClear_5", 1);
        PlayerPrefs.Save();
        yield return LoadScene("Title");
        yield return null;

        UnityEngine.UI.Button campaign = FindSceneObject("StageSelectButton")?.GetComponent<UnityEngine.UI.Button>();
        UnityEngine.UI.Button daily = FindSceneObject("DailyButton")?.GetComponent<UnityEngine.UI.Button>();
        UnityEngine.UI.Button endless = FindSceneObject("EndlessButton")?.GetComponent<UnityEngine.UI.Button>();
        Assert.That(campaign, Is.Not.Null);
        Assert.That(daily, Is.Not.Null);
        Assert.That(endless, Is.Not.Null);
        Assert.That(GetButtonText(campaign), Does.Contain("おしごと"));
        Assert.That(GetButtonText(campaign), Does.Contain("20 STAGES"));
        Assert.That(GetButtonText(daily), Does.Contain("デイリー"));
        Assert.That(GetButtonText(endless), Does.Contain("エンドレス"));

        campaign.onClick.Invoke();
        yield return WaitForScene("StageSelect", SceneTimeoutSeconds);
        yield return WaitUntil(() => FindSceneObject("ChapterTabs") != null, 3f, "Chapter tabs were not created.");
        GameObject chapterTabs = FindSceneObject("ChapterTabs");
        Assert.That(chapterTabs.GetComponentsInChildren<UnityEngine.UI.Button>(true), Has.Length.EqualTo(4));

        object controller = FindSceneComponent("StageSelectController");
        Invoke(controller, "SelectChapter", 3);
        yield return null;
        Assert.That(Invoke(controller, "GetSelectedStageNumber"), Is.EqualTo(11));
        Component[] cards = FindSceneComponents("StageCardView").Where(component => component.gameObject.activeInHierarchy).ToArray();
        Assert.That(cards, Has.Length.EqualTo(5));
        Assert.That(cards.Select(card => GetTextComponentValue(GetField(card, "_statusText"))).Any(text => text.Contains("おしごと10")), Is.True);

        Invoke(controller, "SelectChapter", 1);
        yield return null;
        UnityEngine.UI.Button playButton = GetField(controller, "_playButton") as UnityEngine.UI.Button;
        Assert.That(playButton, Is.Not.Null.And.Property("interactable").True);
        playButton.onClick.Invoke();
        yield return WaitForScene("Main", SceneTimeoutSeconds);
        object gameFlow = FindSceneComponent("GameFlowController");
        yield return WaitUntil(() => GetBool(gameFlow, "IsPlaying"), SceneTimeoutSeconds, "Campaign route did not reach Main.");
        object spawner = FindSceneComponent("CarSpawner");
        object laneInput = FindSceneComponent("LaneInputController");
        Invoke(spawner, "StopSpawning");
        Invoke(spawner, "DespawnAllCars");
        for (int i = 0; i < 12; i += 1)
        {
            SpawnFixedCar(spawner, "LightTruck");
            PointerDown(laneInput, "LightTruck");
            yield return SettleInput();
        }

        yield return WaitForScene("Result", SceneTimeoutSeconds);
        Assert.That(PlayerPrefs.GetInt("StageClear_1", 0), Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetInt("BestScore_Stage1", 0), Is.EqualTo(120));
        object resultController = FindSceneComponent("ResultController");
        yield return WaitUntil(
            () => (bool)GetField(resultController, "_areResultActionsUnlocked"),
            ResultUnlockTimeoutSeconds,
            "Campaign result actions did not unlock.");
        Invoke(resultController, "OnNextStagePressed");
        yield return WaitForScene("Main", SceneTimeoutSeconds);
        Assert.That(GetStaticProperty(RequiredType("SessionState"), "SelectedStageNumber"), Is.EqualTo(2));
    }

    [UnityTest]
    [Order(6)]
    public IEnumerator DailyTitleRouteRunsSixtySecondsWithoutContinueAndRetries()
    {
        PlayerPrefs.SetInt("StageClear_5", 1);
        PlayerPrefs.Save();
        yield return LoadScene("Title");
        yield return null;
        UnityEngine.UI.Button daily = FindSceneObject("DailyButton")?.GetComponent<UnityEngine.UI.Button>();
        Assert.That(daily, Is.Not.Null.And.Property("interactable").True);
        daily.onClick.Invoke();
        yield return WaitForScene("Main", SceneTimeoutSeconds);

        object gameFlow = FindSceneComponent("GameFlowController");
        yield return WaitUntil(() => GetBool(gameFlow, "IsPlaying"), SceneTimeoutSeconds, "Daily Main did not enter Playing.");
        Assert.That(GetStaticProperty(RequiredType("SessionState"), "SelectedGameMode").ToString(), Is.EqualTo("Daily"));
        object stage = GetField(gameFlow, "_currentStageDefinition");
        Assert.That(GetField(stage, "MissLimit"), Is.EqualTo(3));
        Assert.That(GetField(stage, "SurvivalSeconds"), Is.EqualTo(60f));
        Assert.That(GetField(stage, "AllowRewardedContinue"), Is.EqualTo(false));
        object pauseMenu = FindSceneComponent("MainPauseMenuController");
        Assert.That(pauseMenu, Is.Not.Null);
        Invoke(pauseMenu, "OpenMenu");
        yield return null;
        Assert.That(GetTextComponentValue(GetField(pauseMenu, "_stageText")), Is.EqualTo("デイリー"));
        Invoke(pauseMenu, "RetryStage");
        yield return WaitForScene("Main", SceneTimeoutSeconds);
        gameFlow = FindSceneComponent("GameFlowController");
        yield return WaitUntil(() => GetBool(gameFlow, "IsPlaying"), SceneTimeoutSeconds, "Daily pause retry did not reach Main.");
        Assert.That(GetStaticProperty(RequiredType("SessionState"), "SelectedGameMode").ToString(), Is.EqualTo("Daily"));
        Invoke(FindSceneComponent("CarSpawner"), "StopSpawning");

        yield return WaitForScene("Result", 6f);
        object dailyResult = GetStaticProperty(RequiredType("SessionState"), "LastResult");
        Assert.That(GetProperty(dailyResult, "IsDaily"), Is.EqualTo(true));
        Assert.That(GetProperty(dailyResult, "IsClear"), Is.EqualTo(true));
        Assert.That((float)GetProperty(dailyResult, "PlayTimeSeconds"), Is.GreaterThanOrEqualTo(60f));
        object dailySave = InvokeStatic(
            RequiredType("DailyChallengeService"),
            "LoadForDate",
            GetProperty(dailyResult, "DailyDateKey"));
        Assert.That(GetField(dailySave, "CompletedToday"), Is.EqualTo(true));
        yield return RetryFromResult(5);
        Assert.That(GetStaticProperty(RequiredType("SessionState"), "SelectedGameMode").ToString(), Is.EqualTo("Daily"));
    }

    [UnityTest]
    [Order(7)]
    public IEnumerator EndlessTitleRouteUsesHighestClearedStageAndOffersOneContinue()
    {
        PlayerPrefs.SetInt("StageClear_5", 1);
        PlayerPrefs.Save();
        yield return LoadScene("Title");
        yield return null;
        UnityEngine.UI.Button endless = FindSceneObject("EndlessButton")?.GetComponent<UnityEngine.UI.Button>();
        Assert.That(endless, Is.Not.Null.And.Property("interactable").True);
        endless.onClick.Invoke();
        yield return WaitForScene("Main", SceneTimeoutSeconds);
        object gameFlow = FindSceneComponent("GameFlowController");
        yield return WaitUntil(() => GetBool(gameFlow, "IsPlaying"), SceneTimeoutSeconds, "Endless Main did not enter Playing.");
        object stage = GetField(gameFlow, "_currentStageDefinition");
        Assert.That(GetField(stage, "StageNumber"), Is.EqualTo(5));
        Assert.That(GetStaticProperty(RequiredType("SessionState"), "SelectedGameMode").ToString(), Is.EqualTo("Endless"));
        object progressHud = FindSceneComponent("StageProgressHudController");
        Assert.That(CountActiveSlots(progressHud, "_missOrbs"), Is.EqualTo(1));

        object spawner = FindSceneComponent("CarSpawner");
        Invoke(spawner, "StopSpawning");
        Invoke(spawner, "DespawnAllCars");
        PointerDown(FindSceneComponent("LaneInputController"), "LightTruck");
        yield return SettleInput();
        object continuePrompt = FindSceneComponent("ContinuePromptController");
        Assert.That(continuePrompt, Is.Not.Null, "Endless should offer its one rewarded continue.");
        object giveUpButton = GetField(continuePrompt, "_giveUpButton");
        Invoke(GetProperty(giveUpButton, "onClick"), "Invoke");
        yield return WaitForScene("Result", SceneTimeoutSeconds);
        Assert.That(GetProperty(GetStaticProperty(RequiredType("SessionState"), "LastResult"), "IsEndless"), Is.EqualTo(true));
        Assert.That(CountActiveSlots(FindSceneComponent("ResultController"), "_missOrbImages"), Is.EqualTo(1));
    }

    [UnityTest]
    [Order(8)]
    public IEnumerator SurvivalStagePauseFreezesObjectiveClockInRealMainScene()
    {
        InvokeStatic(RequiredType("SessionState"), "SelectStage", 8);
        yield return LoadScene("Main");
        object gameFlow = FindSceneComponent("GameFlowController");
        yield return WaitUntil(() => GetBool(gameFlow, "IsPlaying"), SceneTimeoutSeconds, "Stage 8 did not enter Playing.");
        Invoke(FindSceneComponent("CarSpawner"), "StopSpawning");
        object scoreManager = FindSceneComponent("ScoreManager");
        object evaluator = GetProperty(scoreManager, "ObjectiveEvaluator");
        Assert.That(Invoke(gameFlow, "PauseGame"), Is.EqualTo(true));
        float pausedAt = (float)GetProperty(evaluator, "ElapsedSeconds");
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That((float)GetProperty(evaluator, "ElapsedSeconds"), Is.EqualTo(pausedAt).Within(0.001f));
        Invoke(gameFlow, "ResumeGame");
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That((float)GetProperty(evaluator, "ElapsedSeconds"), Is.GreaterThan(pausedAt));
    }

    [UnityTest]
    [Order(9)]
    public IEnumerator AllFifteenStagesClearThroughRealMainAndResultScenesInSequence()
    {
        ScriptableObject database = Resources.Load<ScriptableObject>("StageDatabase");
        Func<int, bool> getCleared = stageNumber => PlayerPrefs.GetInt($"StageClear_{stageNumber}", 0) == 1;
        for (int stageNumber = 1; stageNumber <= 15; stageNumber += 1)
        {
            Assert.That(Invoke(database, "IsStageUnlocked", stageNumber - 1, getCleared), Is.True, $"Stage {stageNumber} should unlock in sequence.");
            InvokeStatic(RequiredType("SessionState"), "SelectStage", stageNumber);
            yield return LoadScene("Main");
            object gameFlow = FindSceneComponent("GameFlowController");
            yield return WaitUntil(() => GetBool(gameFlow, "IsPlaying"), SceneTimeoutSeconds, $"Stage {stageNumber} did not enter Playing.");

            if (stageNumber is 1 or 5 or 10)
            {
                int expectedMissSlots = stageNumber == 5 ? 2 : 3;
                Assert.That(CountActiveSlots(FindSceneComponent("StageProgressHudController"), "_missOrbs"), Is.EqualTo(expectedMissSlots), $"stage {stageNumber}");
            }

            yield return CompleteCurrentStageThroughRealInput(gameFlow, stageNumber);
            if (SceneManager.GetActiveScene().name != "Result")
            {
                yield return WaitForScene("Result", SceneTimeoutSeconds);
            }

            object result = GetStaticProperty(RequiredType("SessionState"), "LastResult");
            Assert.That(GetProperty(result, "StageNumber"), Is.EqualTo(stageNumber));
            Assert.That(GetProperty(result, "IsClear"), Is.EqualTo(true), $"Stage {stageNumber} should clear through Main.");
            Assert.That(getCleared(stageNumber), Is.True, $"Stage {stageNumber} clear should persist in Result.");
        }

        Assert.That(Enumerable.Range(1, 15).All(getCleared), Is.True);
    }

    [UnityTest]
    [Order(10)]
    public IEnumerator LateTapImmediatelyAfterPassDoesNotAddAnotherMiss()
    {
        yield return LoadScene("Main");
        object gameFlow = FindSceneComponent("GameFlowController");
        yield return WaitUntil(() => GetBool(gameFlow, "IsPlaying"), SceneTimeoutSeconds, "Main did not enter Playing state.");

        object scoreManager = FindSceneComponent("ScoreManager");
        object carSpawner = FindSceneComponent("CarSpawner");
        object laneInput = FindSceneComponent("LaneInputController");
        Invoke(carSpawner, "StopSpawning");
        Invoke(carSpawner, "DespawnAllCars");
        yield return null;

        object passedCar = SpawnFixedCar(carSpawner, "CompactCar");
        Invoke(carSpawner, "HandleCarMissed", passedCar);
        AssertScore(scoreManager, score: 0, misses: 1);

        PointerDown(laneInput, "CompactCar");
        yield return SettleInput();
        AssertScore(scoreManager, score: 0, misses: 1);

        yield return new WaitForSecondsRealtime(0.2f);
        PointerDown(laneInput, "CompactCar");
        yield return SettleInput();
        AssertScore(scoreManager, score: 0, misses: 2);
    }

    [UnityTest]
    [Order(11)]
    public IEnumerator GimmickHintsAppearOnlyOnFirstEncounter()
    {
        yield return LoadScene("Main");
        object gameFlow = FindSceneComponent("GameFlowController");
        yield return WaitUntil(() => GetBool(gameFlow, "IsPlaying"), SceneTimeoutSeconds, "Main did not enter Playing state.");

        object carSpawner = FindSceneComponent("CarSpawner");
        object laneInput = FindSceneComponent("LaneInputController");
        object gimmickHud = FindSceneComponent("GimmickHudController");
        object stage = GetField(gameFlow, "_currentStageDefinition");
        RectTransform messageRoot = GetField(gimmickHud, "_messageRoot") as RectTransform;
        Invoke(carSpawner, "StopSpawning");
        Invoke(carSpawner, "DespawnAllCars");
        yield return null;

        Invoke(gimmickHud, "ShowModifierHint", EnumValue("CarModifier", "Express"));
        Assert.That(messageRoot.gameObject.activeSelf, Is.True);
        Invoke(gimmickHud, "StopEffects");
        Invoke(gimmickHud, "Initialize", stage, laneInput);
        Invoke(gimmickHud, "ShowModifierHint", EnumValue("CarModifier", "Express"));
        Assert.That(messageRoot.gameObject.activeSelf, Is.False, "Express hint should not repeat on another stage entry.");

        Invoke(gimmickHud, "ShowModifierHint", EnumValue("CarModifier", "Covered"));
        Assert.That(messageRoot.gameObject.activeSelf, Is.True);
        Invoke(gimmickHud, "StopEffects");
        Invoke(gimmickHud, "Initialize", stage, laneInput);
        Invoke(gimmickHud, "ShowModifierHint", EnumValue("CarModifier", "Covered"));
        Assert.That(messageRoot.gameObject.activeSelf, Is.False, "Covered hint should not repeat on another stage entry.");
    }

    [UnityTest]
    [Order(12)]
    public IEnumerator PauseHowToShowsCurrentRulesAndReturnsToThePauseMenu()
    {
        yield return LoadScene("Main");
        object gameFlow = FindSceneComponent("GameFlowController");
        yield return WaitUntil(() => GetBool(gameFlow, "IsPlaying"), SceneTimeoutSeconds, "Main did not enter Playing state.");

        object pauseMenu = FindSceneComponent("MainPauseMenuController");
        Assert.That(pauseMenu, Is.Not.Null);
        Invoke(pauseMenu, "OpenMenu");
        yield return null;
        Invoke(pauseMenu, "OpenHowTo");
        yield return null;

        GameObject howToRoot = GetField(pauseMenu, "_howToPanelRoot") as GameObject;
        Assert.That(howToRoot, Is.Not.Null.And.Property("activeInHierarchy").True);
        string howToCopy = string.Join("\n", howToRoot.GetComponentsInChildren<Component>(true)
            .Select(GetTextComponentValue)
            .Where(value => !string.IsNullOrWhiteSpace(value)));

        Assert.That(howToCopy, Does.Contain("しごとの手引き"));
        Assert.That(howToCopy, Does.Contain("右から来る車"));
        Assert.That(howToCopy, Does.Contain("車がいない時もMISS"));
        Assert.That(howToCopy, Does.Contain("故障車"));
        Assert.That(howToCopy, Does.Contain("整備へ送る"));
        Assert.That(howToCopy, Does.Contain("覆面車"));
        Assert.That(howToCopy, Does.Contain("? が消えるまで待つ"));
        Assert.That(howToCopy, Does.Contain("急送車"));
        Assert.That(howToCopy, Does.Contain("得点が ×2"));
        Assert.That(howToCopy, Does.Contain("COMBO"));
        Assert.That(howToCopy, Does.Contain("FEVER"));
        Assert.That(howToCopy, Does.Contain("大名行列（RUSH）は3台連続"));
        Assert.That(howToCopy, Does.Not.Contain("COMING CAR"));

        UnityEngine.UI.Button tutorialButton = GetField(pauseMenu, "_howToTutorialButton") as UnityEngine.UI.Button;
        UnityEngine.UI.Button closeButton = GetField(pauseMenu, "_howToCloseButton") as UnityEngine.UI.Button;
        Assert.That(tutorialButton, Is.Not.Null.And.Property("interactable").True);
        Assert.That(closeButton, Is.Not.Null.And.Property("interactable").True);
        Assert.That(GetButtonText(tutorialButton), Does.Contain("チュートリアル"));
        Assert.That(GetButtonText(closeButton), Does.Contain("わかった"));
        UnityEngine.UI.Image closeButtonImage = closeButton.targetGraphic as UnityEngine.UI.Image;
        Assert.That(closeButtonImage, Is.Not.Null);
        Assert.That(closeButtonImage.type, Is.EqualTo(UnityEngine.UI.Image.Type.Simple));
        Assert.That(closeButtonImage.preserveAspect, Is.True);
        float buttonAspect = (closeButton.transform as RectTransform).rect.width / (closeButton.transform as RectTransform).rect.height;
        float spriteAspect = closeButtonImage.sprite.rect.width / closeButtonImage.sprite.rect.height;
        Assert.That(buttonAspect, Is.EqualTo(spriteAspect).Within(0.01f));

        RectTransform paper = howToRoot.GetComponentsInChildren<RectTransform>(true)
            .Single(rect => rect.name == "InstructionPaper");
        Assert.That(paper.rect.width, Is.GreaterThan(0f));
        Assert.That(paper.rect.height, Is.GreaterThan(0f));

        Invoke(pauseMenu, "CloseHowTo");
        yield return null;
        Assert.That(howToRoot.activeSelf, Is.False);
        Assert.That((GetField(pauseMenu, "_menuPanelRoot") as GameObject)?.activeSelf, Is.True);
        Invoke(pauseMenu, "ResumeGame");
    }

    [UnityTest]
    [Order(13)]
    public IEnumerator TitleHowToCloseButtonKeepsItsArtworkAspect()
    {
        yield return LoadScene("Title");
        object howTo = FindSceneComponent("HowToOverlayController");
        Assert.That(howTo, Is.Not.Null);
        Invoke(howTo, "ShowOverlay");
        yield return null;

        UnityEngine.UI.Button closeButton = GetField(howTo, "_closeButton") as UnityEngine.UI.Button;
        Assert.That(closeButton, Is.Not.Null);
        UnityEngine.UI.Image closeImage = closeButton.targetGraphic as UnityEngine.UI.Image;
        Assert.That(closeImage, Is.Not.Null);
        Assert.That(closeImage.type, Is.EqualTo(UnityEngine.UI.Image.Type.Simple));
        Assert.That(closeImage.preserveAspect, Is.True);

        RectTransform closeRect = closeButton.transform as RectTransform;
        float buttonAspect = closeRect.rect.width / closeRect.rect.height;
        float spriteAspect = closeImage.sprite.rect.width / closeImage.sprite.rect.height;
        Assert.That(buttonAspect, Is.EqualTo(spriteAspect).Within(0.01f));
    }

    [UnityTest]
    [Order(14)]
    public IEnumerator TutorialHighlightsTheTruckButtonDuringTheFirstGuidedStep()
    {
        PlayerPrefs.SetInt("Tutorial_Completed", 0);
        PlayerPrefs.SetInt("Tutorial_Skipped", 0);
        PlayerPrefs.Save();
        InvokeStatic(RequiredType("TutorialLaunchService"), "RequestReplay");

        yield return LoadScene("Main");
        yield return WaitUntil(
            () => FindSceneComponent("TutorialController") != null,
            SceneTimeoutSeconds,
            "Tutorial controller was not installed.");

        object tutorial = FindSceneComponent("TutorialController");
        yield return WaitUntil(
            () => (bool)GetProperty(tutorial, "IsRunning"),
            SceneTimeoutSeconds,
            "Tutorial did not start.");

        object laneInput = FindSceneComponent("LaneInputController");
        UnityEngine.UI.Button truckButton = GetField(laneInput, "_laneAButton") as UnityEngine.UI.Button;
        UnityEngine.UI.Button compactButton = GetField(laneInput, "_laneBButton") as UnityEngine.UI.Button;
        UnityEngine.UI.Button sportsButton = GetField(laneInput, "_laneCButton") as UnityEngine.UI.Button;
        Assert.That(truckButton, Is.Not.Null, "The scene's truck button was not resolved at runtime.");
        Assert.That(compactButton, Is.Not.Null);
        Assert.That(sportsButton, Is.Not.Null);

        Invoke(tutorial, "AdvanceDialogue");
        yield return WaitUntil(
            () => GetProperty(tutorial, "State").ToString() == "GuidedTruck"
                && (bool)GetField(tutorial, "_isAwaitingInput"),
            SceneTimeoutSeconds,
            "Tutorial did not reach the guided truck input step.");

        object focusGuide = FindSceneComponent("TutorialFocusGuideView");
        Assert.That(focusGuide, Is.Not.Null);
        RectTransform guideRoot = GetField(focusGuide, "_root") as RectTransform;
        Assert.That(guideRoot, Is.Not.Null);
        Assert.That(guideRoot.gameObject.activeInHierarchy, Is.True);
        Assert.That(GetField(focusGuide, "_targetButton"), Is.SameAs(truckButton.transform));
        Assert.That(guideRoot.Find("CorrectRing")?.gameObject.activeSelf, Is.True);
        Assert.That(guideRoot.Find("WitchPointer")?.gameObject.activeSelf, Is.True);
        Assert.That(truckButton.interactable, Is.True);
        Assert.That(compactButton.interactable, Is.False);
        Assert.That(sportsButton.interactable, Is.False);

        Invoke(tutorial, "SkipTutorial");
        yield return null;
    }

    [UnityTest]
    [Order(15)]
    public IEnumerator ExpressTutorialTeachesStoppedAndMovingCarsBeforeStartingStageThree()
    {
        PlayerPrefs.DeleteKey("ExpressTutorial_Seen");
        PlayerPrefs.SetInt("GimmickHintSeen_Express", 1);
        InvokeStatic(RequiredType("SessionState"), "SelectStage", 3);
        Time.timeScale = 1f;
        yield return LoadScene("Main");

        object tutorial = FindSceneComponent("TutorialController");
        object gameFlow = FindSceneComponent("GameFlowController");
        object spawner = FindSceneComponent("CarSpawner");
        object scoreManager = FindSceneComponent("ScoreManager");
        object laneInput = FindSceneComponent("LaneInputController");
        Assert.That(tutorial, Is.Not.Null, "The old brief hint must not suppress the new tutorial.");
        Assert.That((bool)GetProperty(tutorial, "IsRunning"), Is.True);
        Assert.That((bool)GetField(spawner, "_isSpawning"), Is.False);

        Assert.That(Invoke(gameFlow, "PauseGame"), Is.EqualTo(true));
        yield return new WaitForSecondsRealtime(2.2f);
        Assert.That(GetProperty(tutorial, "State").ToString(), Is.EqualTo("ExpressIntroduction"),
            "Dialogue should wait while the game is paused.");
        Invoke(gameFlow, "ResumeGame");

        yield return WaitUntil(
            () => GetProperty(tutorial, "State").ToString() == "GuidedExpress"
                && (bool)GetField(tutorial, "_isAwaitingInput"),
            SceneTimeoutSeconds, "The explanations did not automatically reach guided practice.");
        object stoppedCar = GetField(tutorial, "_activeCar");
        Assert.That(GetProperty(stoppedCar, "Modifier").ToString(), Is.EqualTo("Express"));
        Assert.That(GetField(stoppedCar, "_speedWorld"), Is.EqualTo(0f));
        Assert.That(GetProperty(stoppedCar, "ScoreMultiplier"), Is.EqualTo(2));
        SpriteRenderer carBody = ((Component)stoppedCar).GetComponent<SpriteRenderer>();
        Renderer marker = ((Component)stoppedCar).transform.Find("ModifierBadge").GetComponent<Renderer>();
        Assert.That(marker.bounds.size.y, Is.GreaterThanOrEqualTo(carBody.bounds.size.y * 0.4f),
            "The express marker must be readable relative to the car.");
        Assert.That(Camera.main.WorldToViewportPoint(carBody.bounds.min).x, Is.GreaterThan(0f));
        Assert.That(Camera.main.WorldToViewportPoint(carBody.bounds.max).x, Is.LessThan(1f),
            "The stopped practice car must be fully visible.");
        Assert.That(GetField(FindSceneComponent("TutorialFocusGuideView"), "_targetButton"),
            Is.SameAs(((Component)GetField(laneInput, "_laneAButton")).transform));
        yield return new WaitForSecondsRealtime(2.2f);
        Assert.That(GetField(tutorial, "_completedCarCount"), Is.EqualTo(0),
            "Practice must require a real lane input, even after two seconds.");
        AssertScore(scoreManager, 0, 0);

        PointerDown(laneInput, "LightTruck");
        yield return SettleInput();
        Assert.That(GetField(tutorial, "_completedCarCount"), Is.EqualTo(1));
        Assert.That(GetTextComponentValue(GetField(tutorial, "_instructionText")), Does.Contain("2倍"));
        Assert.That(PlayerPrefs.GetInt("ExpressTutorial_Seen", 0), Is.Zero);

        yield return WaitUntil(
            () => GetProperty(tutorial, "State").ToString() == "ExpressPractice"
                && (bool)GetField(tutorial, "_isAwaitingInput"),
            SceneTimeoutSeconds, "The moving-car practice did not start.");
        object movingCar = GetField(tutorial, "_activeCar");
        Assert.That(GetProperty(movingCar, "Modifier").ToString(), Is.EqualTo("Express"));
        Assert.That((float)GetField(movingCar, "_speedWorld"), Is.GreaterThan(0f));
        PointerDown(laneInput, "LightTruck");
        yield return SettleInput();
        yield return WaitUntil(() => (bool)GetField(tutorial, "_isAwaitingInput"),
            SceneTimeoutSeconds, "A mistake did not allow another practice attempt.");
        AssertScore(scoreManager, 0, 0);
        Assert.That(GetField(tutorial, "_completedCarCount"), Is.EqualTo(1));
        Assert.That(GetProperty(GetField(tutorial, "_activeCar"), "Modifier").ToString(), Is.EqualTo("Express"));

        // The input controller ignores taps inside its short debounce window.
        yield return new WaitForSecondsRealtime(0.1f);
        string practiceCarType = GetProperty(GetField(tutorial, "_activeCar"), "CarType").ToString();
        PointerDown(laneInput, practiceCarType);
        yield return SettleInput();
        Assert.That(GetField(tutorial, "_completedCarCount"), Is.EqualTo(2));
        yield return WaitUntil(() => !(bool)GetProperty(tutorial, "IsRunning"),
            SceneTimeoutSeconds, "Completing practice did not start the regular stage.");
        yield return null;
        Assert.That(GetField(GetField(gameFlow, "_currentStageDefinition"), "StageNumber"), Is.EqualTo(3));
        AssertScore(scoreManager, 0, 0);
        Assert.That(PlayerPrefs.GetInt("ExpressTutorial_Seen", 0), Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetInt("Tutorial_Skipped", 0), Is.Zero);

        yield return LoadScene("Main");
        Assert.That(FindSceneComponent("TutorialController"), Is.Null,
            "The completed express tutorial should not repeat on retry.");
    }

    [UnityTest]
    [Order(16)]
    public IEnumerator SkippingExpressTutorialStartsStageThreeWithoutSkippingTheBasicTutorial()
    {
        PlayerPrefs.DeleteKey("ExpressTutorial_Seen");
        PlayerPrefs.SetInt("Tutorial_Completed", 0);
        PlayerPrefs.SetInt("Tutorial_Skipped", 0);
        InvokeStatic(RequiredType("SessionState"), "SelectStage", 3);
        yield return LoadScene("Main");
        object tutorial = FindSceneComponent("TutorialController");
        object gameFlow = FindSceneComponent("GameFlowController");
        Assert.That(tutorial, Is.Not.Null);
        Invoke(tutorial, "SkipTutorial");
        yield return WaitUntil(() => !(bool)GetProperty(tutorial, "IsRunning") && GetBool(gameFlow, "IsPlaying"),
            SceneTimeoutSeconds, "Skipping the express tutorial did not resume stage three.");
        Assert.That(GetField(GetField(gameFlow, "_currentStageDefinition"), "StageNumber"), Is.EqualTo(3));
        Assert.That(PlayerPrefs.GetInt("ExpressTutorial_Seen", 0), Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetInt("Tutorial_Completed", 0), Is.Zero);
        Assert.That(PlayerPrefs.GetInt("Tutorial_Skipped", 0), Is.Zero);
        AssertScore(FindSceneComponent("ScoreManager"), 0, 0);
    }

    private static IEnumerator CompleteCurrentStageThroughRealInput(object gameFlow, int stageNumber)
    {
        object stage = GetField(gameFlow, "_currentStageDefinition");
        object scoreManager = FindSceneComponent("ScoreManager");
        object spawner = FindSceneComponent("CarSpawner");
        object laneInput = FindSceneComponent("LaneInputController");
        Invoke(spawner, "StopSpawning");
        Invoke(spawner, "DespawnAllCars");
        yield return null;

        string goalType = GetField(stage, "GoalType").ToString();
        int safety = 0;
        if (goalType == "Survival")
        {
            yield return WaitForScene("Result", SceneTimeoutSeconds);
            yield break;
        }

        if (goalType == "VehicleQuota")
        {
            foreach (string carType in new[] { "LightTruck", "CompactCar", "SportsCar" })
            {
                int target = GetField(stage, carType + "Quota") is int value ? value : 0;
                while (SceneManager.GetActiveScene().name == "Main"
                    && (int)Invoke(scoreManager, "GetCorrectCount", EnumValue("CarType", carType)) < target)
                {
                    Assert.That(SpawnFixedCar(spawner, carType), Is.Not.Null, $"Stage {stageNumber} could not spawn {carType}.");
                    PointerDown(laneInput, carType);
                    yield return SettleInput();
                    Assert.That(++safety, Is.LessThan(100), $"Stage {stageNumber} quota did not converge.");
                }
            }

            yield break;
        }

        int targetValue = GetField(stage, "GoalValue") is int goal ? goal : 0;
        while (SceneManager.GetActiveScene().name == "Main")
        {
            int current = goalType == "Score"
                ? (int)GetProperty(scoreManager, "CurrentScore")
                : goalType == "Combo"
                    ? (int)GetProperty(scoreManager, "ComboCount")
                    : (int)GetProperty(scoreManager, "TotalCorrectCount");
            if (current >= targetValue)
            {
                break;
            }

            Assert.That(SpawnFixedCar(spawner, "LightTruck"), Is.Not.Null, $"Stage {stageNumber} could not spawn a playable car.");
            PointerDown(laneInput, "LightTruck");
            yield return SettleInput();
            Assert.That(++safety, Is.LessThan(100), $"Stage {stageNumber} objective did not converge.");
        }
    }

    private static IEnumerator RetryFromResult(int expectedStageNumber = 1)
    {
        object resultController = FindSceneComponent("ResultController");
        Assert.That(resultController, Is.Not.Null);
        yield return WaitUntil(
            () => (bool)GetField(resultController, "_areResultActionsUnlocked"),
            ResultUnlockTimeoutSeconds,
            "Result actions did not unlock after the presentation and disabled ad stub.");

        Invoke(resultController, "OnRetryPressed");
        yield return WaitForScene("Main", SceneTimeoutSeconds);

        Type sessionState = RequiredType("SessionState");
        Assert.That(GetStaticProperty(sessionState, "SelectedStageNumber"), Is.EqualTo(expectedStageNumber));
        Assert.That(FindSceneComponent("GameFlowController"), Is.Not.Null, "Retry should load a fresh Main flow.");
    }

    private static void AssertSpawnerLimit(object carSpawner)
    {
        Assert.That(SpawnFixedCar(carSpawner, "LightTruck"), Is.Not.Null);
        Assert.That(SpawnFixedCar(carSpawner, "CompactCar"), Is.Not.Null);
        Assert.That(SpawnFixedCar(carSpawner, "SportsCar"), Is.Not.Null);
        Assert.That(SpawnFixedCar(carSpawner, "LightTruck"), Is.Null, "At most three cars may be active.");
    }

    private static object SpawnFixedCar(object carSpawner, string carTypeName)
    {
        return Invoke(carSpawner, "SpawnFixedCar", EnumValue("CarType", carTypeName), 0f);
    }

    private static object SpawnFixedCar(object carSpawner, string carTypeName, string modifierName)
    {
        return Invoke(
            carSpawner,
            "SpawnFixedCar",
            EnumValue("CarType", carTypeName),
            0f,
            EnumValue("CarModifier", modifierName));
    }

    private static void RepairPointerDown()
    {
        object gimmickHud = FindSceneComponent("GimmickHudController");
        Assert.That(gimmickHud, Is.Not.Null);
        Type forwarderType = RequiredType("RepairButtonPointerDownForwarder");
        Component hudComponent = gimmickHud as Component;
        Component forwarder = hudComponent.GetComponentsInChildren(forwarderType, true).FirstOrDefault();
        Assert.That(forwarder, Is.Not.Null, "Repair Pointer Down forwarder was not configured.");
        Invoke(forwarder, "OnPointerDown", new object[] { null });
    }

    private static UnityEngine.UI.Button FindRepairButton()
    {
        object gimmickHud = FindSceneComponent("GimmickHudController");
        Assert.That(gimmickHud, Is.Not.Null);
        return (gimmickHud as Component)
            ?.GetComponentsInChildren<UnityEngine.UI.Button>(true)
            .Single(button => button.name == "RepairButton");
    }

    private static void PointerDown(object laneInput, string carTypeName)
    {
        Type forwarderType = RequiredType("LaneButtonPointerDownForwarder");
        Component laneInputComponent = laneInput as Component;
        Assert.That(laneInputComponent, Is.Not.Null);
        Component[] forwarders = laneInputComponent.GetComponentsInChildren(forwarderType, true);
        object expectedLane = EnumValue("CarType", carTypeName);
        object forwarder = forwarders.FirstOrDefault(candidate => Equals(GetProperty(candidate, "LaneType"), expectedLane));
        Assert.That(forwarder, Is.Not.Null, $"Pointer Down forwarder for {carTypeName} was not configured.");
        Invoke(forwarder, "OnPointerDown", new object[] { null });
    }

    private static IEnumerator SettleInput()
    {
        yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(0.02f);
    }

    private static IEnumerator LoadScene(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        Assert.That(operation, Is.Not.Null, $"Could not start loading scene {sceneName}.");
        while (!operation.isDone)
        {
            yield return null;
        }

        yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName));
    }

    private static IEnumerator WaitForScene(string sceneName, float timeoutSeconds)
    {
        yield return WaitUntil(
            () => SceneManager.GetActiveScene().name == sceneName && SceneManager.GetActiveScene().isLoaded,
            timeoutSeconds,
            $"Scene {sceneName} was not loaded.");
        yield return null;
    }

    private static IEnumerator WaitUntil(Func<bool> predicate, float timeoutSeconds, string failureMessage)
    {
        float deadline = Time.realtimeSinceStartup + timeoutSeconds;
        while (!predicate() && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        Assert.That(predicate(), Is.True, failureMessage);
    }

    private static void AssertScore(object scoreManager, int score, int misses)
    {
        Assert.That(GetProperty(scoreManager, "CurrentScore"), Is.EqualTo(score));
        Assert.That(GetProperty(scoreManager, "MissCount"), Is.EqualTo(misses));
    }

    private static int CountActiveSlots(object target, string fieldName)
    {
        return ((IEnumerable)GetField(target, fieldName))
            .Cast<Component>()
            .Count(component => component != null && component.gameObject.activeSelf);
    }

    private static Rect GetScreenRect(RectTransform rectTransform, Camera camera)
    {
        Assert.That(rectTransform, Is.Not.Null);
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Vector2 first = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        float xMin = first.x;
        float yMin = first.y;
        float xMax = first.x;
        float yMax = first.y;
        for (int i = 1; i < corners.Length; i += 1)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
            xMin = Mathf.Min(xMin, point.x);
            yMin = Mathf.Min(yMin, point.y);
            xMax = Mathf.Max(xMax, point.x);
            yMax = Mathf.Max(yMax, point.y);
        }

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static void AssertStoredResult(bool isClear, int expectedScore, int expectedMisses, int expectedStage = 1)
    {
        object result = GetStaticProperty(RequiredType("SessionState"), "LastResult");
        Assert.That(result, Is.Not.Null);
        Assert.That(GetProperty(result, "IsClear"), Is.EqualTo(isClear));
        Assert.That(GetProperty(result, "StageNumber"), Is.EqualTo(expectedStage));
        Assert.That(GetProperty(result, "Score"), Is.EqualTo(expectedScore));
        Assert.That(GetProperty(result, "MissCount"), Is.EqualTo(expectedMisses));
    }

    private static bool GetBool(object target, string methodName)
    {
        return target != null && (bool)Invoke(target, methodName);
    }

    private static object FindSceneComponent(string typeName)
    {
        Type type = RequiredType(typeName);
        foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(type))
        {
            if (candidate is Component component && component.gameObject.scene.IsValid() && component.gameObject.scene.isLoaded)
            {
                return component;
            }
        }

        return null;
    }

    private static IEnumerable<Component> FindSceneComponents(string typeName)
    {
        Type type = RequiredType(typeName);
        return Resources.FindObjectsOfTypeAll(type)
            .OfType<Component>()
            .Where(component => component.gameObject.scene.IsValid() && component.gameObject.scene.isLoaded);
    }

    private static GameObject FindSceneObject(string objectName)
    {
        return Resources.FindObjectsOfTypeAll<Transform>()
            .FirstOrDefault(transform => transform != null
                && transform.gameObject.scene.IsValid()
                && transform.gameObject.scene.isLoaded
                && transform.name == objectName)
            ?.gameObject;
    }

    private static string GetButtonText(UnityEngine.UI.Button button)
    {
        return string.Join(" ", button.GetComponentsInChildren<Component>(true)
            .Select(GetTextComponentValue)
            .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string GetTextComponentValue(object component)
    {
        if (component == null)
        {
            return string.Empty;
        }

        PropertyInfo property = component.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
        return property != null && property.PropertyType == typeof(string)
            ? property.GetValue(component) as string ?? string.Empty
            : string.Empty;
    }

    private static Canvas FindSceneCanvas(string objectName)
    {
        return Resources.FindObjectsOfTypeAll<Canvas>()
            .FirstOrDefault(canvas => canvas != null
                && canvas.gameObject.scene.IsValid()
                && canvas.gameObject.scene.isLoaded
                && canvas.gameObject.name == objectName);
    }

    private static Type RequiredType(string typeName)
    {
        Type type = Type.GetType(typeName + ", Assembly-CSharp")
            ?? AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "Assembly-CSharp")
                ?.GetType(typeName);
        Assert.That(type, Is.Not.Null, $"Expected {typeName} to exist in Assembly-CSharp.");
        return type;
    }

    private static object EnumValue(string typeName, string value)
    {
        return Enum.Parse(RequiredType(typeName), value);
    }

    private static object InvokeStatic(Type type, string methodName, params object[] args)
    {
        MethodInfo method = FindMethod(type, methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, args);
        return method.Invoke(null, args);
    }

    private static object Invoke(object target, string methodName, params object[] args)
    {
        Assert.That(target, Is.Not.Null, $"Cannot invoke {methodName} on a null target.");
        MethodInfo method = FindMethod(target.GetType(), methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, args);
        return method.Invoke(target, args);
    }

    private static MethodInfo FindMethod(Type type, string methodName, BindingFlags flags, object[] args)
    {
        MethodInfo method = type.GetMethods(flags)
            .FirstOrDefault(candidate =>
            {
                if (candidate.Name != methodName)
                {
                    return false;
                }

                ParameterInfo[] parameters = candidate.GetParameters();
                if (parameters.Length != args.Length)
                {
                    return false;
                }

                for (int i = 0; i < parameters.Length; i += 1)
                {
                    if (args[i] != null && !parameters[i].ParameterType.IsInstanceOfType(args[i]))
                    {
                        return false;
                    }
                }

                return true;
            });
        Assert.That(method, Is.Not.Null, $"Missing method {type.Name}.{methodName}.");
        return method;
    }

    private static object GetProperty(object target, string propertyName)
    {
        Assert.That(target, Is.Not.Null);
        PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(property, Is.Not.Null, $"Missing property {target.GetType().Name}.{propertyName}.");
        return property.GetValue(target);
    }

    private static object GetStaticProperty(Type type, string propertyName)
    {
        PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(property, Is.Not.Null, $"Missing static property {type.Name}.{propertyName}.");
        return property.GetValue(null);
    }

    private static object GetField(object target, string fieldName)
    {
        Assert.That(target, Is.Not.Null);
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{fieldName}.");
        return field.GetValue(target);
    }

    private readonly struct SavedIntPreference
    {
        public SavedIntPreference(bool existed, int value)
        {
            Existed = existed;
            Value = value;
        }

        public bool Existed { get; }
        public int Value { get; }
    }

    private readonly struct SavedStringPreference
    {
        public SavedStringPreference(bool existed, string value)
        {
            Existed = existed;
            Value = value;
        }

        public bool Existed { get; }
        public string Value { get; }
    }

    private readonly struct SavedFloatPreference
    {
        public SavedFloatPreference(bool existed, float value)
        {
            Existed = existed;
            Value = value;
        }

        public bool Existed { get; }
        public float Value { get; }
    }

    private static string[] BuildIntPlayerPrefsKeys()
    {
        List<string> keys = new(BasePlayerPrefsKeys);
        for (int i = 1; i <= CampaignStageCount; i += 1)
        {
            keys.Add($"BestScore_Stage{i}");
            keys.Add($"StarRating_Stage{i}");
            keys.Add($"StageClear_{i}");
        }

        foreach (string mode in new[] { "0", "1", "2" })
        {
            for (int i = 1; i <= CampaignStageCount; i += 1)
            {
                keys.Add($"Telemetry_Attempted_{mode}_{i}");
            }
        }

        return keys.Distinct().ToArray();
    }

    private static string[] BuildFloatPlayerPrefsKeys()
    {
        return Enumerable.Range(1, CampaignStageCount).Select(stage => $"BestTime_Stage{stage}").ToArray();
    }
}
