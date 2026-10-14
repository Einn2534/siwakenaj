using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed partial class GameplayFlowPlayModeTests
{
    [UnityTest]
    public IEnumerator TitleAdRemovalEntryOpensPurchaseUpdatesOwnershipAndReturnsToTitle()
    {
        yield return LoadScene("Title");
        yield return null;
        UnityEngine.UI.Button entry = FindSceneObject("AdRemovalPurchaseButton")?.GetComponent<UnityEngine.UI.Button>();
        Assert.That(entry, Is.Not.Null.And.Property("interactable").True);
        Assert.That(GetButtonText(entry), Does.Contain("広告を削除").And.Contain("買い切り"));
        Assert.That(FindSceneObject("HowToButton"), Is.Null);
        entry.onClick.Invoke();
        yield return null;
        object title = FindSceneComponent("TitleController");
        object panel = FindSceneComponent("AdRemovalPurchasePanel");
        Assert.That(panel, Is.Not.Null);
        GameObject sheet = (GameObject)GetField(panel, "_sheet");
        Assert.That(sheet.activeInHierarchy, Is.True);
        Assert.That(((GameObject)GetField(title, "_howToPanel")).activeSelf, Is.False);
        var purchase = (UnityEngine.UI.Button)GetField(panel, "_buy");
        RequiredType("AdRemovalEntitlement").GetMethod("SetOwned", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Invoke(null, new object[] { true });
        Assert.That(GetButtonText(entry), Does.Contain("広告削除済み").And.Contain("購入済み"));
        Assert.That(purchase.interactable, Is.False, "An owned product must not offer a second purchase.");
        UnityEngine.UI.Button close = sheet.transform.Find("CloseButton").GetComponent<UnityEngine.UI.Button>();
        Assert.That(GetButtonText(close), Is.EqualTo("タイトルにもどる"));
        close.onClick.Invoke();
        yield return null;
        Assert.That(((GameObject)GetField(title, "_settingsPanel")).activeSelf, Is.False);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Title"));
        entry.onClick.Invoke();
        yield return null;
        Assert.That(sheet.activeInHierarchy, Is.True);
        Assert.That(((GameObject)GetField(title, "_settingsPanel")).GetComponents(RequiredType("AdRemovalPurchasePanel")), Has.Length.EqualTo(1));
        close.onClick.Invoke();
        // The existing settings route must still return to settings.
        Invoke(title, "OnSettingsOpen");
        Invoke(panel, "Open");
        Assert.That(GetButtonText(close), Is.EqualTo("設定にもどる"));
        close.onClick.Invoke();
        Assert.That(((GameObject)GetField(title, "_settingsPanel")).activeSelf, Is.True);
        Invoke(title, "OnSettingsClose");
    }

    [UnityTest]
    public IEnumerator FailedResultDoesNotConsumeInterstitialEligibility()
    {
        string lastShown = DateTime.UtcNow.AddMinutes(-10).Ticks.ToString();
        PlayerPrefs.SetString("Interstitial_LastShownUtcTicks", lastShown);
        PlayerPrefs.SetInt("Interstitial_CompletedSinceLast", 2);
        yield return LoadScene("Main");
        object flow = FindSceneComponent("GameFlowController");
        yield return WaitUntil(() => GetBool(flow, "IsPlaying"), SceneTimeoutSeconds, "Gameplay did not start.");
        object spawner = FindSceneComponent("CarSpawner");
        object lane = FindSceneComponent("LaneInputController");
        Invoke(spawner, "StopSpawning");
        Invoke(spawner, "DespawnAllCars");
        for (int i = 0; i < 3; i++)
        {
            PointerDown(lane, "LightTruck");
            yield return SettleInput();
        }
        object prompt = FindSceneComponent("ContinuePromptController");
        Assert.That(prompt, Is.Not.Null);
        ((UnityEngine.UI.Button)GetField(prompt, "_giveUpButton")).onClick.Invoke();
        yield return WaitForScene("Result", SceneTimeoutSeconds);
        object result = FindSceneComponent("ResultController");
        yield return WaitUntil(() => (bool)GetField(result, "_areResultActionsUnlocked"), ResultUnlockTimeoutSeconds, "Failed result did not unlock.");
        Assert.That(PlayerPrefs.GetInt("Interstitial_CompletedSinceLast"), Is.EqualTo(2));
        Assert.That(PlayerPrefs.GetString("Interstitial_LastShownUtcTicks"), Is.EqualTo(lastShown));
    }

    [UnityTest]
    public IEnumerator PurchasedAdRemoval_RevivesWithoutSdkOrNetwork_OncePerPlay()
    {
        yield return LoadScene("Main");
        object gameFlow = FindSceneComponent("GameFlowController");
        object laneInput = FindSceneComponent("LaneInputController");
        object spawner = FindSceneComponent("CarSpawner");
        yield return WaitUntil(() => GetBool(gameFlow, "IsPlaying"), SceneTimeoutSeconds, "Gameplay did not start.");
        Invoke(spawner, "StopSpawning");
        Invoke(spawner, "DespawnAllCars");
        for (int i = 0; i < 3; i++)
        {
            PointerDown(laneInput, "LightTruck");
            yield return SettleInput();
        }
        object prompt = FindSceneComponent("ContinuePromptController");
        Assert.That(prompt, Is.Not.Null);
        Assert.That(GetField(gameFlow, "_currentState").ToString(), Is.EqualTo("ContinuePrompt"));
        var button = (UnityEngine.UI.Button)GetField(prompt, "_continueButton");
        Assert.That(button.GetComponentInChildren<TMPro.TMP_Text>().text, Is.EqualTo("広告を見て復活"));
        // A deferred payment can complete while this offer is already open.
        RequiredType("AdRemovalEntitlement").GetMethod("SetOwned", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Invoke(null, new object[] { true });
        Assert.That(button.GetComponentInChildren<TMPro.TMP_Text>().text, Is.EqualTo("広告なしで復活"));
        button.onClick.Invoke();
        yield return WaitUntil(() => GetBool(gameFlow, "IsPlaying"), SceneTimeoutSeconds, "Paid revival did not resume gameplay.");
        AssertScore(FindSceneComponent("ScoreManager"), 0, 2);
        Invoke(spawner, "StopSpawning");
        Invoke(spawner, "DespawnAllCars");
        // The existing revival rule restores one remaining life.
        PointerDown(laneInput, "LightTruck");
        yield return SettleInput();
        yield return WaitForScene("Result", SceneTimeoutSeconds);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Result"), "A second revival must not be offered.");
    }

    [UnityTest]
    public IEnumerator PurchasedAdRemoval_SuppressesInterstitialAndSdkFallbackReturnsExactlyOnce()
    {
        PlayerPrefs.SetInt("IAP_Editor_RemoveAds_v1", 1);
        PlayerPrefs.SetInt("Interstitial_CompletedSinceLast", 2);
        PlayerPrefs.SetString("Interstitial_LastShownUtcTicks", DateTime.UtcNow.AddMinutes(-10).Ticks.ToString());
        object stageMode = EnumValue("GameMode", "Stage");
        Assert.That(InvokeStatic(RequiredType("InterstitialGate"), "CanShow", stageMode, DateTime.UtcNow), Is.EqualTo(false));
        object manager = GetStaticProperty(RequiredType("AdMobManager"), "Instance");
        int interstitialCallbacks = 0, rewardedCallbacks = 0;
        bool completed = false;
        Invoke(manager, "ShowInterstitialThenContinue", (Action)(() => interstitialCallbacks++));
        Invoke(manager, "ShowRewardedThenContinue", (Action<bool>)(value => { rewardedCallbacks++; completed = value; }));
        yield return null;
        Assert.That(interstitialCallbacks, Is.EqualTo(1));
        Assert.That(rewardedCallbacks, Is.EqualTo(1));
        Assert.That(completed, Is.True);
        Assert.That(GetField(manager, "_initialized"), Is.EqualTo(false));
        Assert.That(GetField(manager, "_interstitial"), Is.Null);
        Assert.That(GetField(manager, "_rewarded"), Is.Null);
    }

    [UnityTest]
    public IEnumerator PurchasedAdRemoval_DoesNotUnlockRevivalInExamOrDaily()
    {
        PlayerPrefs.SetInt("IAP_Editor_RemoveAds_v1", 1);
        foreach (bool daily in new[] { false, true })
        {
            InvokeStatic(RequiredType("SessionState"), daily ? "SelectDaily" : "SelectStage", 5);
            yield return LoadScene("Main");
            object gameFlow = FindSceneComponent("GameFlowController");
            yield return WaitUntil(() => GetBool(gameFlow, "IsPlaying"), SceneTimeoutSeconds, "Gameplay did not start.");
            object spawner = FindSceneComponent("CarSpawner");
            object laneInput = FindSceneComponent("LaneInputController");
            Invoke(spawner, "StopSpawning");
            Invoke(spawner, "DespawnAllCars");
            yield return null;
            int misses = daily ? 3 : 2;
            for (int i = 0; i < misses; i++)
            {
                PointerDown(laneInput, "LightTruck");
                yield return SettleInput();
            }
            yield return WaitForScene("Result", SceneTimeoutSeconds);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Result"));
        }
    }
}
