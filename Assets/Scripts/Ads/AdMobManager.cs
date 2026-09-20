using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

public sealed class AdMobManager : MonoBehaviour
{
    private static AdMobManager _instance;
    private InterstitialAd _interstitial;
    private RewardedAd _rewarded;
    private bool _initializing, _initialized, _loadingInterstitial, _loadingRewarded, _showing, _privacyShowing;
    private float _nextInterstitial, _nextRewarded, _interstitialExpiry, _rewardedExpiry;
    private Action _interstitialComplete;
    private AdRewardCompletion _rewardCompletion;

    public static AdMobManager Instance { get { EnsureInstance(); return _instance; } }
    public bool PrivacyOptionsRequired => ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        if (_instance != null) return;
        _instance = new GameObject(nameof(AdMobManager)).AddComponent<AdMobManager>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        // Consent callbacks run before MobileAds.Initialize creates its executor.
        MobileAdsEventExecutor.Initialize();
#if UNITY_ANDROID && !UNITY_EDITOR
        ConsentInformation.Update(new ConsentRequestParameters(), error => OnMain(() =>
        {
            if (error != null) { Debug.LogWarning("AdMob consent: " + error.Message); TryInitialize(); return; }
            ConsentForm.LoadAndShowConsentFormIfRequired(formError => OnMain(() =>
            {
                if (formError != null) Debug.LogWarning("AdMob consent: " + formError.Message);
                TryInitialize();
            }));
        }));
#endif
    }

    private static void OnMain(Action action) => MobileAdsEventExecutor.ExecuteInUpdate(action);
    private void TryInitialize()
    {
        if (this == null || !ConsentInformation.CanRequestAds()) return;
        InitializeSdk();
    }

    private void InitializeSdk()
    {
        if (_initializing || _initialized) return;
        _initializing = true;
        MobileAds.SetRequestConfiguration(new RequestConfiguration
        {
            MaxAdContentRating = MaxAdContentRating.PG,
            TestDeviceIds = new System.Collections.Generic.List<string>(AdMobConfiguration.TestDeviceIds)
        });
        MobileAds.Initialize(status => OnMain(() =>
        {
            if (this == null) return;
            _initializing = false;
            _initialized = status != null;
            if (_initialized) { LoadInterstitial(); LoadRewarded(); }
        }));
    }

#if UNITY_EDITOR
    // Explicitly opt in to Google's Editor mock; regression runs never initialize an ad SDK.
    public void InitializeEditorTestAds() { if (AdMobConfiguration.UseTestAds) InitializeSdk(); }
#endif

    public void ShowPrivacyOptions()
    {
        if (!PrivacyOptionsRequired || _showing || _privacyShowing) return;
        _privacyShowing = true;
        _interstitial?.Destroy(); _interstitial = null;
        _rewarded?.Destroy(); _rewarded = null;
        ConsentForm.ShowPrivacyOptionsForm(error => OnMain(() =>
        {
            if (this == null) return;
            _privacyShowing = false;
            if (error != null) Debug.LogWarning("AdMob privacy options: " + error.Message);
            TryInitialize();
        }));
    }

    private bool CanRequest()
    {
#if UNITY_EDITOR
        return _initialized && !_privacyShowing;
#else
        return _initialized && !_privacyShowing && ConsentInformation.CanRequestAds();
#endif
    }

    private void Update()
    {
        if (!CanRequest() || _showing) return;
        if (_interstitial != null && Time.realtimeSinceStartup >= _interstitialExpiry) { _interstitial.Destroy(); _interstitial = null; }
        if (_rewarded != null && Time.realtimeSinceStartup >= _rewardedExpiry) { _rewarded.Destroy(); _rewarded = null; }
        LoadInterstitial();
        LoadRewarded();
    }

    private void LoadInterstitial()
    {
        if (!CanRequest() || _showing || _loadingInterstitial || _interstitial != null || Time.realtimeSinceStartup < _nextInterstitial) return;
        _loadingInterstitial = true;
        _nextInterstitial = Time.realtimeSinceStartup + 30f;
        InterstitialAd.Load(AdMobConfiguration.InterstitialId, new AdRequest(), (ad, error) => OnMain(() =>
        {
            if (this == null) { ad?.Destroy(); return; }
            _loadingInterstitial = false;
            if (error != null || ad == null) { ad?.Destroy(); return; }
            if (!CanRequest()) { ad.Destroy(); return; }
            _interstitial = ad;
            _interstitialExpiry = Time.realtimeSinceStartup + 3300f;
            ad.OnAdFullScreenContentClosed += () => OnMain(FinishInterstitial);
            ad.OnAdFullScreenContentFailed += error => OnMain(FinishInterstitial);
        }));
    }

    private void LoadRewarded()
    {
        if (!CanRequest() || _showing || _loadingRewarded || _rewarded != null || Time.realtimeSinceStartup < _nextRewarded) return;
        _loadingRewarded = true;
        _nextRewarded = Time.realtimeSinceStartup + 30f;
        RewardedAd.Load(AdMobConfiguration.RewardedId, new AdRequest(), (ad, error) => OnMain(() =>
        {
            if (this == null) { ad?.Destroy(); return; }
            _loadingRewarded = false;
            if (error != null || ad == null) { ad?.Destroy(); return; }
            if (!CanRequest()) { ad.Destroy(); return; }
            _rewarded = ad;
            _rewardedExpiry = Time.realtimeSinceStartup + 3300f;
            ad.OnAdFullScreenContentClosed += () => OnMain(() => FinishRewarded(false));
            ad.OnAdFullScreenContentFailed += error => OnMain(() => FinishRewarded(true));
        }));
    }

    public bool IsRewardedReady() => CanRequest() && !_showing && _rewarded != null && Time.realtimeSinceStartup < _rewardedExpiry && _rewarded.CanShowAd();

    public void ShowRewardedThenContinue(Action<bool> onComplete)
    {
        if (!IsRewardedReady()) { onComplete?.Invoke(false); return; }
        _showing = true;
        var completion = new AdRewardCompletion(onComplete);
        _rewardCompletion = completion;
        try { _rewarded.Show(reward => OnMain(completion.Earn)); }
        catch (Exception e) { Debug.LogWarning("AdMob rewarded: " + e.Message); FinishRewarded(true); }
    }

    public void ShowInterstitialThenContinue(Action onComplete)
    {
        if (!CanRequest() || _showing || _interstitial == null || Time.realtimeSinceStartup >= _interstitialExpiry || !_interstitial.CanShowAd())
        { onComplete?.Invoke(); return; }
        _showing = true;
        _interstitialComplete = onComplete;
        try { _interstitial.Show(); }
        catch (Exception e) { Debug.LogWarning("AdMob interstitial: " + e.Message); FinishInterstitial(); }
    }

    private void FinishInterstitial()
    {
        if (this == null || !_showing || _rewardCompletion != null) return;
        var callback = _interstitialComplete;
        _interstitialComplete = null;
        _showing = false;
        _interstitial?.Destroy(); _interstitial = null;
        _nextInterstitial = 0;
        callback?.Invoke();
    }

    private void FinishRewarded(bool failed)
    {
        if (this == null || _rewardCompletion == null) return;
        var completion = _rewardCompletion;
        _rewardCompletion = null;
        _showing = false;
        _rewarded?.Destroy(); _rewarded = null;
        _nextRewarded = 0;
        if (failed) completion.Fail(); else completion.Close();
    }

    private void OnDestroy()
    {
        if (_instance != this) return;
        _interstitial?.Destroy();
        _rewarded?.Destroy();
        _instance = null;
    }
}
