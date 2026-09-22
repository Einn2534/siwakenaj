using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed partial class GameplayFlowPlayModeTests
{
    [UnityTest, Order(30)] public IEnumerator VehicleThemeRealFlow() => CheckThemeFlow(1, "くるま");
    [UnityTest, Order(31)] public IEnumerator HandsThemeRealFlow() => CheckThemeFlow(4, "じゃんけん");
    [UnityTest, Order(32)] public IEnumerator ClockThemeRealFlow() => CheckThemeFlow(8, "時計");
    [UnityTest, Order(33)] public IEnumerator GreensThemeRealFlow() => CheckThemeFlow(11, "葉野菜");

    private IEnumerator CheckThemeFlow(int stageNumber, string title)
    {
        for (int i = 1; i < stageNumber; i++) PlayerPrefs.SetInt($"StageClear_{i}", 1);
        InvokeStatic(RequiredType("StageSelectionService"), "SelectStage", stageNumber);
        yield return LoadScene("StageSelect");
        object select = FindSceneComponent("StageSelectController");
        Assert.That(Invoke(select, "GetSelectedStageNumber"), Is.EqualTo(stageNumber));
        CaptureTheme($"Stage{stageNumber:00}_Select");
        Invoke(select, "OnHowToOpen");
        Assert.That(AllCopy(GetField(select, "_howToOverlay") as GameObject), Does.Contain(title));
        CaptureTheme($"Stage{stageNumber:00}_SelectGuide");
        Invoke(select, "OnHowToClose");
        // Enter through the real stage selection button handler.
        Invoke(select, "OnPlayPressed");
        yield return WaitForScene("Main", SceneTimeoutSeconds);
        object flow = FindSceneComponent("GameFlowController");
        yield return WaitUntil(() => GetBool(flow, "IsPlaying"), SceneTimeoutSeconds, "Theme Main did not start");
        object spawner = FindSceneComponent("CarSpawner");
        object lane = FindSceneComponent("LaneInputController");
        object theme = InvokeStatic(RequiredType("CarVisualDatabase"), "LoadSelected");
        Assert.That(GetProperty(theme, "ThemeName"), Is.EqualTo(title));
        Invoke(spawner, "StopSpawning");
        Invoke(spawner, "DespawnAllCars");
        yield return null;
        AssertThemeButtons(theme);
        string[] types = { "LightTruck", "CompactCar", "SportsCar" };
        foreach (string type in types)
        {
            Component car = (Component)SpawnFixedCar(spawner, type);
            Assert.That(car.GetComponent<SpriteRenderer>().sprite, Is.SameAs(Invoke(theme, "GetBodySprite", EnumValue("CarType", type))));
        }
        CaptureTheme($"Stage{stageNumber:00}_Main");
        Invoke(spawner, "DespawnAllCars");
        yield return null;
        SpawnFixedCar(spawner, "LightTruck");
        PointerDown(lane, "CompactCar");
        yield return SettleInput();
        Assert.That(GetProperty(FindSceneComponent("ScoreManager"), "MissCount"), Is.EqualTo(1));
        PointerDown(lane, "LightTruck");
        yield return SettleInput();
        Assert.That(GetProperty(FindSceneComponent("ScoreManager"), "TotalCorrectCount"), Is.EqualTo(1));
        foreach (string type in types.Skip(1))
        {
            SpawnFixedCar(spawner, type);
            PointerDown(lane, type);
            yield return SettleInput();
        }
        Assert.That(GetProperty(FindSceneComponent("ScoreManager"), "TotalCorrectCount"), Is.EqualTo(3));
        IDictionary collected = (IDictionary)GetField(FindSceneComponent("ScoreLaneUI"), "_laneIcons");
        foreach (DictionaryEntry entry in collected)
            foreach (GameObject icon in (IList)entry.Value)
                foreach (Image graphic in icon.GetComponentsInChildren<Image>())
                {
                    Assert.That(graphic.sprite, Is.SameAs(Invoke(theme, "GetIconSprite", entry.Key)));
                    if (stageNumber != 1) Assert.That(graphic.preserveAspect, Is.True, "Collected theme icons must not be stretched to the vehicle aspect ratio.");
                }
        CaptureTheme($"Stage{stageNumber:00}_Collected");

        object pause = FindSceneComponent("MainPauseMenuController");
        Invoke(pause, "OpenMenu");
        Invoke(pause, "OpenHowTo");
        yield return null;
        GameObject guide = (GameObject)GetField(pause, "_howToPanelRoot");
        string guideCopy = AllCopy(guide);
        foreach (string type in types) Assert.That(guideCopy, Does.Contain((string)Invoke(theme, "GetDisplayName", EnumValue("CarType", type))));
        Assert.That(guideCopy, Does.Contain((string)GetProperty(theme, "RepairAction")));
        Assert.That(guideCopy, Does.Contain("FEVER").And.Contain("RUSH"));
        if (stageNumber != 1) Assert.That(guideCopy, Does.Not.Contain("故障車").And.Not.Contain("覆面車"));
        CaptureTheme($"Stage{stageNumber:00}_HowTo");
        Invoke(pause, "CloseHowTo");
        Invoke(pause, "ResumeGame");
        yield return CompleteCurrentStageThroughRealInput(flow, stageNumber);
        yield return WaitForScene("Result", SceneTimeoutSeconds);
        object resultController = FindSceneComponent("ResultController");
        yield return WaitUntil(() => (bool)GetField(resultController, "_areResultActionsUnlocked"), 6f, "Result did not unlock");
        string resultCopy = AllCopy(((Component)resultController).gameObject);
        foreach (string type in types) Assert.That(resultCopy, Does.Contain((string)Invoke(theme, "GetDisplayName", EnumValue("CarType", type))));
        foreach (string field in new[] { "_lightTruckIcon", "_compactCarIcon", "_sportsCarIcon" })
        {
            Image icon = (Image)GetField(resultController, field);
            int index = field == "_lightTruckIcon" ? 0 : field == "_compactCarIcon" ? 1 : 2;
            Assert.That(icon.sprite, Is.SameAs(Invoke(theme, "GetIconSprite", EnumValue("CarType", types[index]))));
        }
        Assert.That(PlayerPrefs.GetInt($"StageClear_{stageNumber}"), Is.EqualTo(1));
        CaptureTheme($"Stage{stageNumber:00}_Result");
        yield return RetryFromResult(stageNumber);
        flow = FindSceneComponent("GameFlowController");
        yield return WaitUntil(() => GetBool(flow, "IsPlaying"), SceneTimeoutSeconds, "Retry did not start");
        AssertThemeButtons(theme);
        Invoke(FindSceneComponent("CarSpawner"), "StopSpawning");
        Invoke(FindSceneComponent("CarSpawner"), "DespawnAllCars");
        // Replay the tutorial through the pause action and retain this theme/stage.
        pause = FindSceneComponent("MainPauseMenuController");
        Invoke(pause, "OpenMenu");
        Invoke(pause, "OpenHowTo");
        Invoke(pause, "ReplayTutorial");
        yield return null;
        yield return WaitForScene("Main", SceneTimeoutSeconds);
        yield return WaitUntil(() => FindSceneComponent("TutorialController") != null && (bool)GetProperty(FindSceneComponent("TutorialController"), "IsRunning"), 6f, "Theme tutorial did not start");
        object tutorial = FindSceneComponent("TutorialController");
        yield return WaitUntil(() => GetField(tutorial, "_activeCar") != null && (bool)GetField(tutorial, "_isAwaitingInput"), 6f, "Guided themed object not ready for input");
        Component guided = (Component)GetField(tutorial, "_activeCar");
        Assert.That(guided.GetComponent<SpriteRenderer>().sprite, Is.SameAs(Invoke(theme, "GetBodySprite", EnumValue("CarType", "LightTruck"))));
        Assert.That(GetTextComponentValue((Component)GetField(tutorial, "_instructionText")), Does.Contain((string)Invoke(theme, "GetDisplayName", EnumValue("CarType", "LightTruck"))));
        CaptureTheme($"Stage{stageNumber:00}_Tutorial");
        PointerDown(FindSceneComponent("LaneInputController"), "LightTruck");
        yield return SettleInput();
        Assert.That(GetField(tutorial, "_completedCarCount"), Is.EqualTo(1));
        Invoke(tutorial, "SkipTutorial");
        yield return WaitUntil(() => !(bool)GetProperty(tutorial, "IsRunning"), 5f, "Tutorial did not return");
        Assert.That(GetStaticProperty(RequiredType("SessionState"), "SelectedStageNumber"), Is.EqualTo(stageNumber));
    }

    [UnityTest, Order(34)]
    public IEnumerator ThemeBoundariesNextAndModeSwitchesResolveFreshData()
    {
        foreach (int stageNumber in new[] { 3, 7, 10 })
        {
            InvokeStatic(RequiredType("SessionState"), "SelectStage", stageNumber);
            yield return LoadScene("Main");
            object flow = FindSceneComponent("GameFlowController");
            yield return WaitUntil(() => GetBool(flow, "IsPlaying"), SceneTimeoutSeconds, "Boundary stage did not start");
            yield return CompleteCurrentStageThroughRealInput(flow, stageNumber);
            yield return WaitForScene("Result", SceneTimeoutSeconds);
            object result = FindSceneComponent("ResultController");
            yield return WaitUntil(() => (bool)GetField(result, "_areResultActionsUnlocked"), 6f, "Boundary result locked");
            Invoke(result, "OnNextStagePressed");
            yield return WaitForScene("Main", SceneTimeoutSeconds);
            flow = FindSceneComponent("GameFlowController");
            yield return WaitUntil(() => GetBool(flow, "IsPlaying"), SceneTimeoutSeconds, "Next theme did not start");
            Assert.That(GetStaticProperty(RequiredType("SessionState"), "SelectedStageNumber"), Is.EqualTo(stageNumber + 1));
            AssertThemeButtons(InvokeStatic(RequiredType("CarVisualDatabase"), "LoadSelected"));
        }
        PlayerPrefs.SetInt("StageClear_15", 1);
        foreach (string mode in new[] { "SelectEndless", "SelectDaily", "SelectStage" })
        {
            InvokeStatic(RequiredType("SessionState"), mode, mode == "SelectStage" ? 1 : 15);
            yield return LoadScene("Main");
            object flow = FindSceneComponent("GameFlowController");
            yield return WaitUntil(() => GetBool(flow, "IsPlaying"), SceneTimeoutSeconds, "Mode did not start");
            object theme = InvokeStatic(RequiredType("CarVisualDatabase"), "LoadSelected");
            Assert.That(GetProperty(theme, "ThemeName"), Is.EqualTo(mode == "SelectEndless" ? "葉野菜" : "くるま"));
            AssertThemeButtons(theme);
        }
    }

    [UnityTest, Order(35)]
    public IEnumerator QuotaLabelsAndThemedFailureKeepTheirNames()
    {
        foreach (int stageNumber in new[] { 2, 7, 8, 11 })
        {
            InvokeStatic(RequiredType("SessionState"), "SelectStage", stageNumber);
            yield return LoadScene("Main");
            object flow = FindSceneComponent("GameFlowController");
            yield return WaitUntil(() => GetBool(flow, "IsPlaying"), SceneTimeoutSeconds, "Quota/failure stage did not start");
            object spawner = FindSceneComponent("CarSpawner");
            Invoke(spawner, "StopSpawning");
            Invoke(spawner, "DespawnAllCars");
            yield return null;
            if (stageNumber == 2 || stageNumber == 7) CaptureTheme($"Stage{stageNumber:00}_Quota");
            object theme = InvokeStatic(RequiredType("CarVisualDatabase"), "LoadSelected");
            object lane = FindSceneComponent("LaneInputController");
            int missLimit = (int)GetProperty(GetProperty(FindSceneComponent("ScoreManager"), "State"), "MissLimit");
            for (int i = 0; i < missLimit; i++)
            {
                PointerDown(lane, "LightTruck");
                yield return SettleInput();
            }
            object prompt = FindSceneComponent("ContinuePromptController");
            Assert.That(prompt, Is.Not.Null);
            Invoke(GetProperty(GetField(prompt, "_giveUpButton"), "onClick"), "Invoke");
            yield return WaitForScene("Result", SceneTimeoutSeconds);
            AssertStoredResult(isClear: false, expectedScore: 0, expectedMisses: missLimit, expectedStage: stageNumber);
            object result = FindSceneComponent("ResultController");
            string copy = AllCopy(((Component)result).gameObject);
            foreach (string type in new[] { "LightTruck", "CompactCar", "SportsCar" })
                Assert.That(copy, Does.Contain((string)Invoke(theme, "GetDisplayName", EnumValue("CarType", type))));
        }
    }

    [UnityTest, Order(36)]
    public IEnumerator DailyAndEndlessTutorialsReturnToTheirRealModes()
    {
        PlayerPrefs.SetInt("StageClear_15", 1);
        foreach (string mode in new[] { "Daily", "Endless" })
        {
            InvokeStatic(RequiredType("SessionState"), "Select" + mode, 15);
            yield return LoadScene("Main");
            object flow = FindSceneComponent("GameFlowController");
            yield return WaitUntil(() => GetBool(flow, "IsPlaying"), SceneTimeoutSeconds, "Mode did not start");
            int sourceStage = (int)GetStaticProperty(RequiredType("SessionState"), "SelectedStageNumber");
            object theme = InvokeStatic(RequiredType("CarVisualDatabase"), "LoadSelected");
            object pause = FindSceneComponent("MainPauseMenuController");
            Invoke(pause, "OpenMenu");
            Invoke(pause, "OpenHowTo");
            Invoke(pause, "ReplayTutorial");
            yield return null;
            yield return WaitForScene("Main", SceneTimeoutSeconds);
            yield return WaitUntil(() => FindSceneComponent("TutorialController") != null && (bool)GetProperty(FindSceneComponent("TutorialController"), "IsRunning"), 6f, "Mode replay did not start");
            object tutorial = FindSceneComponent("TutorialController");
            if (mode == "Daily") Invoke(tutorial, "SkipTutorial");
            else
            {
                float deadline = Time.realtimeSinceStartup + 35f;
                while ((bool)GetProperty(tutorial, "IsRunning") && Time.realtimeSinceStartup < deadline)
                {
                    object car = GetField(tutorial, "_activeCar");
                    if ((bool)GetField(tutorial, "_isAwaitingInput") && car != null)
                    {
                        PointerDown(FindSceneComponent("LaneInputController"), GetProperty(car, "CarType").ToString());
                        yield return SettleInput();
                    }
                    else
                    {
                        if (GetField(tutorial, "_state").ToString() == "Graduation")
                        {
                            Component graduation = (Component)GetField(tutorial, "_graduationRoot");
                            Assert.That(AllCopy(graduation.gameObject), Does.Contain("エンドレス"));
                            graduation.GetComponent<Button>().onClick.Invoke();
                        }
                        else Invoke(tutorial, "AdvanceDialogue");
                        yield return null;
                    }
                }
                Assert.That(GetField(tutorial, "_completedCarCount"), Is.EqualTo(6));
            }
            flow = FindSceneComponent("GameFlowController");
            yield return WaitUntil(() => GetBool(flow, "IsPlaying") && !(bool)GetProperty(tutorial, "IsRunning"), 6f, "Mode tutorial did not return to gameplay");
            Assert.That(GetStaticProperty(RequiredType("SessionState"), "SelectedGameMode").ToString(), Is.EqualTo(mode));
            Assert.That(GetStaticProperty(RequiredType("SessionState"), "SelectedStageNumber"), Is.EqualTo(sourceStage));
            AssertThemeButtons(theme);
            object score = FindSceneComponent("ScoreManager");
            AssertScore(score, 0, 0);
            Assert.That(GetProperty(score, "TotalCorrectCount"), Is.EqualTo(0));
            object evaluator = GetProperty(score, "ObjectiveEvaluator");
            Assert.That(GetField(evaluator, "_mode").ToString(), Is.EqualTo(mode));
            object stage = GetField(flow, "_currentStageDefinition");
            if (mode == "Daily")
            {
                Assert.That(GetField(stage, "SurvivalSeconds"), Is.EqualTo(60f));
                Assert.That((float)GetProperty(evaluator, "ElapsedSeconds"), Is.LessThan(2f));
            }
            else
            {
                Assert.That(GetProperty(GetProperty(score, "ProgressSnapshot"), "TargetValue"), Is.EqualTo(0));
                Assert.That(GetProperty(score, "ObjectiveOutcome").ToString(), Is.EqualTo("Playing"));
            }
        }
    }

    private static void AssertThemeButtons(object theme)
    {
        object visuals = FindSceneComponent("LaneButtonVisualController");
        object input = FindSceneComponent("LaneInputController");
        foreach (string lane in new[] { "A", "B", "C" })
        {
            object type = GetProperty(input, "Lane" + lane + "Type");
            Component graphic = (Component)GetField(visuals, "_lane" + lane + "Image");
            Sprite expected = (Sprite)Invoke(theme, "GetIconSprite", type);
            if (graphic is Image image) Assert.That(image.sprite, Is.SameAs(expected));
            else Assert.That(((RawImage)graphic).texture, Is.SameAs(expected.texture));
            Assert.That(AllCopy(graphic.transform.parent.gameObject), Does.Contain((string)Invoke(theme, "GetDisplayName", type)));
        }
    }

    private static string AllCopy(GameObject root) => string.Join("\n", root.GetComponentsInChildren<Component>(true).Select(GetTextComponentValue).Where(s => !string.IsNullOrWhiteSpace(s)));

    private static void CaptureTheme(string name)
    {
        Type capture = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("RegressionCaptureRunner")).FirstOrDefault(t => t != null);
        Assert.That(capture, Is.Not.Null);
        capture.GetMethod("CaptureCurrentTheme", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { name });
    }
}
