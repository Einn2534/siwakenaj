using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed partial class GameplayFlowPlayModeTests
{
    [UnityTest, Order(17)]
    public IEnumerator CoveredTutorialTeachesWaitingBeforeSortingAndResetsForStageSix()
    {
        PlayerPrefs.DeleteKey("CoveredTutorial_Seen");
        PlayerPrefs.SetInt("GimmickHintSeen_Covered", 1);
        InvokeStatic(RequiredType("SessionState"), "SelectStage", 6);
        Time.timeScale = 1f;
        yield return LoadScene("Main");

        object tutorial = FindSceneComponent("TutorialController");
        object flow = FindSceneComponent("GameFlowController");
        object spawner = FindSceneComponent("CarSpawner");
        object score = FindSceneComponent("ScoreManager");
        object laneInput = FindSceneComponent("LaneInputController");
        Assert.That(tutorial, Is.Not.Null, "The old hint must not suppress interactive practice.");
        Assert.That(GetField(spawner, "_isSpawning"), Is.EqualTo(false));
        Assert.That(GetTextComponentValue(GetField(tutorial, "_instructionText")), Does.Contain("ひみつ便"));

        object stateBeforePause = GetProperty(tutorial, "State");
        Assert.That(Invoke(flow, "PauseGame"), Is.EqualTo(true));
        yield return new WaitForSecondsRealtime(2.2f);
        Assert.That(GetProperty(tutorial, "State"), Is.EqualTo(stateBeforePause));
        Invoke(flow, "ResumeGame");
        Invoke(tutorial, "AdvanceDialogue");
        yield return WaitUntil(() => GetTextComponentValue(GetField(tutorial, "_instructionText")).Contains("見える前"),
            SceneTimeoutSeconds, "The covered-object explanation did not appear.");
        object hidden = GetField(tutorial, "_activeCar");
        Assert.That(GetProperty(hidden, "IsRevealed"), Is.EqualTo(false));
        Assert.That(GetField(hidden, "_speedWorld"), Is.EqualTo(0f));
        Invoke(tutorial, "AdvanceDialogue");
        yield return WaitUntil(() => (bool)GetField(tutorial, "_isAwaitingInput"),
            SceneTimeoutSeconds, "The guided waiting step did not begin.");
        Assert.That(GetProperty(hidden, "IsRevealed"), Is.EqualTo(false));
        Invoke(tutorial, "ReinforceCorrectButton");
        Assert.That(GetField(FindSceneComponent("TutorialFocusGuideView"), "_targetButton"), Is.Null,
            "The correct button must not be exposed before the object is revealed.");
        CaptureTheme("Stage06_CoveredTutorial_Hidden");

        // Pausing must freeze both the guided movement and lane input.
        Invoke(flow, "PauseGame");
        Vector3 pausedPosition = ((Component)hidden).transform.position;
        Invoke(tutorial, "HandleLaneInput", EnumValue("CarType", "LightTruck"));
        yield return new WaitForSecondsRealtime(0.15f);
        Assert.That(((Component)hidden).transform.position, Is.EqualTo(pausedPosition));
        Assert.That(GetField(tutorial, "_tutorialMissCount"), Is.EqualTo(0));
        Invoke(flow, "ResumeGame");

        PointerDown(laneInput, "LightTruck");
        yield return SettleInput();
        Assert.That(GetField(tutorial, "_completedCarCount"), Is.EqualTo(0),
            "Even a matching button is wrong before the reveal.");
        Assert.That(GetField(tutorial, "_tutorialMissCount"), Is.EqualTo(1));
        AssertScore(score, 0, 0);

        yield return WaitUntil(() => GetField(tutorial, "_activeCar") != null
            && (bool)GetProperty(GetField(tutorial, "_activeCar"), "IsRevealed")
            && (bool)GetField(tutorial, "_isAwaitingInput"),
            SceneTimeoutSeconds, "The guided example did not reveal naturally after retry.");
        object revealed = GetField(tutorial, "_activeCar");
        Assert.That(GetField(revealed, "_speedWorld"), Is.EqualTo(0f));
        Assert.That(GetField(FindSceneComponent("TutorialFocusGuideView"), "_targetButton"), Is.Not.Null);
        yield return new WaitForSecondsRealtime(2.2f);
        Assert.That(GetField(tutorial, "_completedCarCount"), Is.EqualTo(0), "Practice must wait for a correct input.");
        CaptureTheme("Stage06_CoveredTutorial_Revealed");
        PointerDown(laneInput, "LightTruck");
        yield return SettleInput();
        Assert.That(GetField(tutorial, "_completedCarCount"), Is.EqualTo(1));

        yield return WaitForCoveredPractice(tutorial);
        object moving = GetField(tutorial, "_activeCar");
        Assert.That(GetProperty(moving, "IsRevealed"), Is.EqualTo(false));
        Assert.That((float)GetField(moving, "_speedWorld"), Is.GreaterThan(0f));
        yield return WaitUntil(() => (bool)GetProperty(moving, "IsRevealed"),
            SceneTimeoutSeconds, "The practice object did not reveal at the normal line.");
        PointerDown(laneInput, "SportsCar");
        yield return SettleInput();
        Assert.That(GetField(tutorial, "_completedCarCount"), Is.EqualTo(1));
        AssertScore(score, 0, 0);

        yield return WaitForCoveredPractice(tutorial);
        // Let this attempt pass the miss boundary, then verify another covered attempt appears.
        moving = GetField(tutorial, "_activeCar");
        yield return WaitUntil(() => GetField(tutorial, "_activeCar") != moving,
            SceneTimeoutSeconds, "A missed object did not allow another attempt.");
        yield return WaitForCoveredPractice(tutorial);
        moving = GetField(tutorial, "_activeCar");
        Assert.That(GetProperty(moving, "IsRevealed"), Is.EqualTo(false));
        yield return WaitUntil(() => (bool)GetProperty(moving, "IsRevealed"),
            SceneTimeoutSeconds, "The replacement object did not reveal.");
        PointerDown(laneInput, "CompactCar");
        yield return SettleInput();
        Assert.That(GetField(tutorial, "_completedCarCount"), Is.EqualTo(2));
        yield return WaitUntil(() => !(bool)GetProperty(tutorial, "IsRunning"),
            SceneTimeoutSeconds, "Covered practice did not return to the regular stage.");
        Assert.That(GetField(GetField(flow, "_currentStageDefinition"), "StageNumber"), Is.EqualTo(6));
        AssertScore(score, 0, 0);
        Assert.That(PlayerPrefs.GetInt("CoveredTutorial_Seen", 0), Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetInt("Tutorial_Skipped", 0), Is.Zero);

        yield return LoadScene("Main");
        Assert.That(FindSceneComponent("TutorialController"), Is.Null, "The completed tutorial should not repeat on retry.");
    }

    [UnityTest, Order(18)]
    public IEnumerator CoveredTutorialCanBeSkippedAndReplayedFromStageSixHowTo()
    {
        PlayerPrefs.DeleteKey("CoveredTutorial_Seen");
        PlayerPrefs.SetInt("Tutorial_Completed", 0);
        PlayerPrefs.SetInt("Tutorial_Skipped", 0);
        InvokeStatic(RequiredType("SessionState"), "SelectStage", 6);
        yield return LoadScene("Main");
        object tutorial = FindSceneComponent("TutorialController");
        object flow = FindSceneComponent("GameFlowController");
        Invoke(tutorial, "SkipTutorial");
        yield return WaitUntil(() => !(bool)GetProperty(tutorial, "IsRunning") && GetBool(flow, "IsPlaying"),
            SceneTimeoutSeconds, "Skipping did not resume stage six.");
        Assert.That(PlayerPrefs.GetInt("CoveredTutorial_Seen", 0), Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetInt("Tutorial_Completed", 0), Is.Zero);
        Assert.That(PlayerPrefs.GetInt("Tutorial_Skipped", 0), Is.Zero);
        AssertScore(FindSceneComponent("ScoreManager"), 0, 0);

        object pause = FindSceneComponent("MainPauseMenuController");
        Invoke(pause, "OpenHowTo");
        Invoke(pause, "ReplayTutorial");
        yield return WaitUntil(() => FindSceneComponent("TutorialController") != null
            && (bool)GetProperty(FindSceneComponent("TutorialController"), "IsRunning"),
            SceneTimeoutSeconds, "How To did not replay the covered tutorial.");
        tutorial = FindSceneComponent("TutorialController");
        Assert.That(GetProperty(tutorial, "State").ToString(), Is.EqualTo("CoveredIntroduction"));
        Invoke(tutorial, "SkipTutorial");
        yield return WaitUntil(() => !(bool)GetProperty(tutorial, "IsRunning"), SceneTimeoutSeconds, "Replay did not finish.");
        Assert.That(GetStaticProperty(RequiredType("SessionState"), "SelectedStageNumber"), Is.EqualTo(6));
    }

    private static IEnumerator WaitForCoveredPractice(object tutorial)
    {
        yield return WaitUntil(() => GetProperty(tutorial, "State").ToString() == "CoveredPractice"
            && (bool)GetField(tutorial, "_isAwaitingInput") && GetField(tutorial, "_activeCar") != null,
            SceneTimeoutSeconds, "The moving covered practice did not start.");
    }
}
