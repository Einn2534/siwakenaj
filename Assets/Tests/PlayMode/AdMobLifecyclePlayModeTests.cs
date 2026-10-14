using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.TestTools;

public class AdMobLifecyclePlayModeTests
{
    private const string AdRemovalKey = "IAP_Editor_RemoveAds_v1";
    private bool _hadAdRemovalKey;
    private int _savedAdRemoval;

    [SetUp]
    public void SetUp()
    {
        _hadAdRemovalKey = UnityEngine.PlayerPrefs.HasKey(AdRemovalKey);
        _savedAdRemoval = UnityEngine.PlayerPrefs.GetInt(AdRemovalKey);
        UnityEngine.PlayerPrefs.DeleteKey(AdRemovalKey);
    }

    [TearDown]
    public void TearDown()
    {
        if (_hadAdRemovalKey) UnityEngine.PlayerPrefs.SetInt(AdRemovalKey, _savedAdRemoval);
        else UnityEngine.PlayerPrefs.DeleteKey(AdRemovalKey);
        UnityEngine.PlayerPrefs.Save();
    }

    [UnityTest]
    public IEnumerator CallbacksRunBeforeSdkInitialization_AndUnavailableAdsDoNotBlock()
    {
        Type managerType = Type.GetType("AdMobManager, Assembly-CSharp");
        Assert.That(managerType, Is.Not.Null);
        object manager = managerType.GetProperty("Instance").GetValue(null);
        Type executor = Type.GetType("GoogleMobileAds.Common.MobileAdsEventExecutor, GoogleMobileAds.Common");
        Assert.That(executor, Is.Not.Null);
        Assert.That((bool)executor.GetMethod("IsActive").Invoke(null, null), Is.True);
        bool callbackRan = false;
        executor.GetMethod("ExecuteInUpdate").Invoke(null, new object[] { (Action)(() => callbackRan = true) });
        yield return null;
        yield return null;
        Assert.That(callbackRan, Is.True, "Consent callbacks must work before SDK initialization.");
        int interstitialCalls = 0, rewardedCalls = 0;
        bool earned = true;
        managerType.GetMethod("ShowInterstitialThenContinue").Invoke(manager, new object[] { (Action)(() => interstitialCalls++) });
        managerType.GetMethod("ShowRewardedThenContinue").Invoke(manager, new object[] { (Action<bool>)(value => { rewardedCalls++; earned = value; }) });
        Assert.That(interstitialCalls, Is.EqualTo(1));
        Assert.That(rewardedCalls, Is.EqualTo(1));
        Assert.That(earned, Is.False);
    }
}
