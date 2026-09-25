using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed partial class GameplayFlowPlayModeTests
{
    [UnityTest, Order(60)]
    public IEnumerator MissBoundary_CrossingWarningPauseAndInputGrace()
    {
        yield return PreparePowaMain();
        object spawner = FindSceneComponent("CarSpawner");
        object flow = FindSceneComponent("GameFlowController");
        object score = FindSceneComponent("ScoreManager");
        object view = FindSceneComponent("MissBoundaryView");
        Assert.That(view, Is.Not.Null);
        RectTransform root = (RectTransform)GetField(view, "_root");
        Assert.That(root.sizeDelta.y, Is.EqualTo(240f));
        Assert.That(root.GetComponent<Canvas>().sortingOrder, Is.EqualTo(21));
        Assert.That(root.GetComponentsInChildren<Graphic>().All(g => !g.raycastTarget), Is.True);
        Assert.That(AllCopy(root.gameObject), Is.Empty);

        Component car = (Component)SpawnFixedCar(spawner, "CompactCar");
        MoveToBoundaryDistance(car, spawner, .2f);
        yield return null;
        yield return null;
        Assert.That(GetProperty(view, "Proximity"), Is.EqualTo(0f));
        MoveToBoundaryDistance(car, spawner, .03f);
        yield return null;
        yield return null;
        Assert.That((float)GetProperty(view, "Proximity"), Is.EqualTo(.8f).Within(.001f));
        AssertScore(score, 0, 0);

        Assert.That(Invoke(flow, "PauseGame"), Is.True);
        float proximity = (float)GetProperty(view, "Proximity");
        Vector3 stoppedPosition = car.transform.position;
        yield return new WaitForSecondsRealtime(.12f);
        Assert.That(GetProperty(view, "Proximity"), Is.EqualTo(proximity));
        Assert.That(car.transform.position, Is.EqualTo(stoppedPosition));
        Invoke(flow, "ResumeGame");

        // A live layout change must update both the existing target and the fence.
        RectTransform zone = (RectTransform)GetProperty(spawner, "PlayZone");
        Vector2 originalSize = zone.sizeDelta;
        try
        {
            zone.sizeDelta = originalSize + new Vector2(-80f, 0f);
            Canvas.ForceUpdateCanvases();
            Invoke(view, "RefreshLayout");
            Assert.That(root.position.x, Is.EqualTo((float)GetProperty(car, "MissBoundaryX")).Within(.0001f));
        }
        finally
        {
            zone.sizeDelta = originalSize;
            Canvas.ForceUpdateCanvases();
        }

        MoveToBoundaryDistance(car, spawner, -.001f);
        Invoke(car, "Update"); // Real miss detection, event, despawn and scoring path.
        AssertScore(score, 0, 1);
        Assert.That(GetProperty(view, "FlashStrength"), Is.EqualTo(1f));
        Invoke(car, "Update");
        AssertScore(score, 0, 1);
        PointerDown(FindSceneComponent("LaneInputController"), "CompactCar");
        yield return null;
        yield return null;
        AssertScore(score, 0, 1);

        Invoke(flow, "PauseGame");
        float flash = (float)GetProperty(view, "FlashStrength");
        Assert.That(flash, Is.GreaterThan(0f));
        yield return new WaitForSecondsRealtime(.25f);
        Assert.That(GetProperty(view, "FlashStrength"), Is.EqualTo(flash));
        Invoke(flow, "ResumeGame");
        yield return new WaitForSeconds(.25f);
        Assert.That(GetProperty(view, "FlashStrength"), Is.EqualTo(0f));
        Assert.That(GetProperty(view, "Proximity"), Is.EqualTo(0f));
        PointerDown(FindSceneComponent("LaneInputController"), "CompactCar");
        yield return SettleInput();
        AssertScore(score, 0, 2);

        yield return LoadScene("Main");
        Assert.That(FindSceneComponents("MissBoundaryView").Count(), Is.EqualTo(1));
        Assert.That(GetProperty(FindSceneComponent("MissBoundaryView"), "FlashStrength"), Is.EqualTo(0f));
    }

    [UnityTest, Order(61)] public IEnumerator MissBoundary_VehiclesVisuals() => CheckBoundaryTheme(1);
    [UnityTest, Order(62)] public IEnumerator MissBoundary_HandsVisuals() => CheckBoundaryTheme(4);
    [UnityTest, Order(63)] public IEnumerator MissBoundary_ClocksVisuals() => CheckBoundaryTheme(8);
    [UnityTest, Order(64)] public IEnumerator MissBoundary_GreensVisuals() => CheckBoundaryTheme(11);

    [UnityTest, Order(65)]
    public IEnumerator MissBoundary_AllModesAndStoppedTutorialTarget()
    {
        PlayerPrefs.SetInt("StageClear_15", 1);
        foreach (string mode in new[] { "SelectStage", "SelectDaily", "SelectEndless" })
        {
            InvokeStatic(RequiredType("SessionState"), mode, mode == "SelectStage" ? 1 : 15);
            yield return PreparePowaMain();
            Assert.That(FindSceneComponents("MissBoundaryView").Count(), Is.EqualTo(1));
            object pause = FindSceneComponent("MainPauseMenuController");
            Invoke(pause, "OpenMenu");
            Invoke(pause, "OpenHowTo");
            Invoke(pause, "ReplayTutorial");
            yield return null;
            yield return WaitUntil(() => FindSceneComponent("TutorialController") != null
                && (bool)GetProperty(FindSceneComponent("TutorialController"), "IsRunning"),
                SceneTimeoutSeconds, "Tutorial did not start.");
            object tutorial = FindSceneComponent("TutorialController");
            Invoke(tutorial, "AdvanceDialogue");
            yield return WaitUntil(() => (bool)GetField(tutorial, "_isAwaitingInput"),
                SceneTimeoutSeconds, "Stopped practice target did not arrive.");
            Component car = (Component)GetField(tutorial, "_activeCar");
            object spawner = FindSceneComponent("CarSpawner");
            Assert.That(GetField(car, "_speedWorld"), Is.EqualTo(0f));
            MoveToBoundaryDistance(car, spawner, -.03f);
            Invoke(tutorial, "ClampGuidedCarToPlayZone");
            Assert.That((float)Invoke(car, "GetMissPointX"), Is.GreaterThan((float)GetProperty(car, "MissBoundaryX")));
            Assert.That(FindSceneComponents("MissBoundaryView").Count(), Is.EqualTo(1));
            Invoke(tutorial, "SkipTutorial");
            yield return null;
        }
    }

    private IEnumerator CheckBoundaryTheme(int stageNumber)
    {
        InvokeStatic(RequiredType("SessionState"), "SelectStage", stageNumber);
        yield return PreparePowaMain();
        object spawner = FindSceneComponent("CarSpawner");
        object view = FindSceneComponent("MissBoundaryView");
        Component car = (Component)SpawnFixedCar(spawner, "CompactCar");
        CaptureBoundary($"Stage{stageNumber:00}_Idle", car, .25f);
        CaptureBoundary($"Stage{stageNumber:00}_Near", car, .02f);
        yield return null; // Finish the synchronous captures before timing the miss flash.
        MoveToBoundaryDistance(car, spawner, -.001f);
        Invoke(car, "Update");
        Assert.That(GetProperty(FindSceneComponent("ScoreManager"), "MissCount"), Is.EqualTo(1));
        Assert.That(GetProperty(view, "FlashStrength"), Is.EqualTo(1f));
        yield return null; // Let Unity destroy the missed target before rendering evidence.
        Assert.That(car == null, Is.True);
        Assert.That((float)GetProperty(view, "FlashStrength"), Is.GreaterThan(0f));
        CaptureBoundary($"Stage{stageNumber:00}_Miss", null, 0f);
        yield return null;
    }

    private static void MoveToBoundaryDistance(Component car, object spawner, float widthRatio)
    {
        RectTransform zone = (RectTransform)GetProperty(spawner, "PlayZone");
        Vector3[] corners = new Vector3[4];
        zone.GetWorldCorners(corners);
        float missPointX = (float)Invoke(car, "GetMissPointX");
        float targetX = (float)GetProperty(car, "MissBoundaryX") + (corners[2].x - corners[0].x) * widthRatio;
        car.transform.position += Vector3.right * (targetX - missPointX);
    }

    private static void CaptureBoundary(string stateName, Component car, float distanceRatio)
    {
        Type capture = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("MissBoundaryCapture")).FirstOrDefault(t => t != null);
        Assert.That(capture, Is.Not.Null);
        capture.GetMethod("Capture", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { stateName, car, distanceRatio });
    }
}
