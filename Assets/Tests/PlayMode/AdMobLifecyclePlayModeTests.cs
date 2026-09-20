using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.TestTools;

public class AdMobLifecyclePlayModeTests
{
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
