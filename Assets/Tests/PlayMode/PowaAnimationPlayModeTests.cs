using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed partial class GameplayFlowPlayModeTests
{
    [UnityTest]
    public IEnumerator Powa_LatestFeedbackPauseResumeAndWinPriority()
    {
        yield return PreparePowaMain();
        object flow = FindSceneComponent("GameFlowController");
        object spawner = FindSceneComponent("CarSpawner");
        object input = FindSceneComponent("LaneInputController");
        Component player = (Component)FindSceneComponent("PlayerAnimationController");
        Animator animator = player.GetComponent<Animator>();
        SpriteRenderer artwork = player.transform.Find("Motion/Artwork").GetComponent<SpriteRenderer>();
        Assert.That(player.GetComponentsInChildren<SpriteRenderer>(), Has.Length.EqualTo(1), "One complete drawing includes the single staff, with no old cutout layers.");
        Assert.That(animator.runtimeAnimatorController.animationClips.Single(c => c.name == "Attack").length, Is.EqualTo(.8f).Within(.002f));
        Transform motion = player.transform.Find("Motion");
        float start = motion.localPosition.y;
        yield return new WaitForSeconds(.15f);
        Assert.That(Mathf.Abs(start - motion.localPosition.y), Is.GreaterThan(.001f), "Idle must float after camera layout updates.");

        SpawnFixedCar(spawner, "LightTruck"); PointerDown(input, "LightTruck");
        yield return SettlePowaInput();
        AssertState(animator, "Attack");
        SpawnFixedCar(spawner, "LightTruck"); PointerDown(input, "LightTruck");
        yield return SettlePowaInput();
        AssertState(animator, "Attack");
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.LessThan(.6f), "Repeat success must restart immediately.");
        Sprite anticipation = artwork.sprite;
        yield return new WaitForSeconds(.28f);
        Assert.That(artwork.sprite, Is.Not.EqualTo(anticipation), "Success must advance its drawings.");
        Assert.That(motion.localPosition.y, Is.GreaterThan(.3f), "The jump must survive Main layout's LateUpdate.");
        PointerDown(input, "SportsCar");
        yield return SettlePowaInput();
        AssertState(animator, "Damage");
        Assert.That((bool)GetProperty(player, "IsReactionPlaying"), Is.True);

        Assert.That(Invoke(flow, "PauseGame"), Is.EqualTo(true));
        float paused = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        Vector3 pausedPosition = motion.localPosition;
        Sprite pausedSprite = artwork.sprite;
        yield return new WaitForSecondsRealtime(.2f);
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.EqualTo(paused).Within(.001f));
        Assert.That(Vector3.Distance(motion.localPosition, pausedPosition), Is.LessThan(.001f));
        Assert.That(artwork.sprite, Is.EqualTo(pausedSprite));
        Invoke(flow, "ResumeGame");
        yield return new WaitForSeconds(.8f);
        AssertState(animator, "Idle");
        Assert.That((bool)GetProperty(player, "IsReactionPlaying"), Is.False);

        Invoke(player, "PlayWin"); Invoke(player, "PlayCry"); Invoke(player, "PlayHappy");
        AssertState(animator, "Win");
        yield return new WaitForSeconds(.5f);
        Assert.That(artwork.sprite.name, Does.StartWith("win-"));
        Assert.That(motion.localPosition.y, Is.GreaterThan(.3f));
        yield return new WaitForSeconds(.5f);
        AssertState(animator, "Win");
        Assert.That(artwork.sprite.name, Is.EqualTo("win-peak"), "The single staff is held in the final drawn pose.");
        Assert.That((bool)GetProperty(player, "IsReactionPlaying"), Is.False);
    }

    [UnityTest]
    public IEnumerator Powa_ClearStopsInputRecordsOnceWaitsThenRetries()
    {
        yield return PreparePowaMain();
        object flow = FindSceneComponent("GameFlowController");
        object spawner = FindSceneComponent("CarSpawner");
        object input = FindSceneComponent("LaneInputController");
        object score = FindSceneComponent("ScoreManager");
        Animator animator = ((Component)FindSceneComponent("PlayerAnimationController")).GetComponent<Animator>();
        int before = ResultEventCount();
        for (int i = 0; i < 12; i++)
        {
            SpawnFixedCar(spawner, "LightTruck"); PointerDown(input, "LightTruck");
            yield return SettlePowaInput();
        }
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Main"), "Result must wait for the win pose.");
        AssertState(animator, "Win");
        Assert.That(GetBool(flow, "IsPlaying"), Is.False);
        PointerDown(input, "SportsCar"); Invoke(flow, "FinishStage"); Invoke(flow, "FinalizeGameOver");
        yield return null;
        AssertScore(score, 120, 0);
        Assert.That(ResultEventCount() - before, Is.EqualTo(1));
        yield return WaitForScene("Result", 3f);
        AssertStoredResult(true, 120, 0);
        Assert.That(ResultEventCount() - before, Is.EqualTo(1));
        yield return RetryFromResult();
        Assert.That(((Component)FindSceneComponent("PlayerAnimationController")).GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Win"), Is.False);
    }

    [UnityTest]
    public IEnumerator Powa_FailureWaitsAndMissingAnimatorCannotBlockResult()
    {
        foreach (bool disconnect in new[] { false, true })
        {
            InvokeStatic(RequiredType("SessionState"), "SelectStage", 5);
            yield return PreparePowaMain();
            object flow = FindSceneComponent("GameFlowController");
            object input = FindSceneComponent("LaneInputController");
            Animator animator = ((Component)FindSceneComponent("PlayerAnimationController")).GetComponent<Animator>();
            if (disconnect) animator.runtimeAnimatorController = null;
            int before = ResultEventCount();
            PointerDown(input, "SportsCar");
            yield return SettlePowaInput();
            if (!disconnect)
            {
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Main"));
                AssertState(animator, "Damage");
                Invoke(flow, "FinalizeGameOver");
            }
            yield return WaitForScene("Result", 3f);
            AssertStoredResult(false, 0, 1, 5);
            Assert.That(ResultEventCount() - before, Is.EqualTo(1));
        }
    }

    [UnityTest]
    public IEnumerator Powa_ReviveCanReactAndReturnToIdle()
    {
        yield return PreparePowaMain();
        object flow = FindSceneComponent("GameFlowController");
        object input = FindSceneComponent("LaneInputController");
        Animator animator = ((Component)FindSceneComponent("PlayerAnimationController")).GetComponent<Animator>();
        for (int i = 0; i < 3; i++) { PointerDown(input, "SportsCar"); yield return SettlePowaInput(); }
        Assert.That(GetField(flow, "_currentState").ToString(), Is.EqualTo("ContinuePrompt"));
        // Complete the rewarded-ad callback without relying on a network ad in Editor tests.
        Invoke(flow, "OnRewardedContinueCompleted", true);
        AssertState(animator, "Attack");
        yield return WaitUntil(() => GetBool(flow, "IsPlaying"), 8, "Continue did not resume.");
        object spawner = FindSceneComponent("CarSpawner");
        Invoke(spawner, "StopSpawning"); Invoke(spawner, "DespawnAllCars");
        yield return new WaitForSeconds(.6f);
        AssertState(animator, "Idle");
        SpawnFixedCar(spawner, "CompactCar"); PointerDown(input, "CompactCar");
        yield return SettlePowaInput();
        AssertState(animator, "Attack");
    }

    private static IEnumerator PreparePowaMain()
    {
        yield return LoadScene("Main");
        object flow = FindSceneComponent("GameFlowController");
        yield return WaitUntil(() => GetBool(flow, "IsPlaying"), SceneTimeoutSeconds, "Main did not start.");
        Time.timeScale = 1;
        object spawner = FindSceneComponent("CarSpawner");
        Invoke(spawner, "StopSpawning"); Invoke(spawner, "DespawnAllCars");
        yield return null;
    }

    private static void AssertState(Animator animator, string name) =>
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName(name), Is.True, "Expected Powa state " + name);

    private static IEnumerator SettlePowaInput()
    {
        yield return null;
        yield return null;
        // Real Main input has an 80ms scaled cooldown. Keep feedback inside its clip.
        yield return new WaitForSeconds(.1f);
    }

    private static int ResultEventCount()
    {
        string path = (string)GetStaticProperty(RequiredType("LocalTelemetryService"), "OutputPath");
        return File.Exists(path) ? File.ReadLines(path).Count(line => line.Contains("\"EventName\":\"run_result\"")) : 0;
    }
}
