using System;
using UnityEngine;
using UnityEngine.Advertisements;

public class UnityAdsManager : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
{
    private const string SettingsResourcePath = "UnityAdsSettings";

    private static UnityAdsManager _instance;

    private UnityAdsSettings _settings;
    private string _interstitialAdUnitId;
    private string _rewardedAdUnitId;
    private bool _isInitialized;
    private bool _isInterstitialLoading;
    private bool _isRewardedLoading;
    private bool _isInterstitialLoaded;
    private bool _isRewardedLoaded;
    private Action _showCompletedCallback;
    private Action<bool> _rewardedCompletedCallback;

    public static UnityAdsManager Instance
    {
        get
        {
            EnsureInstance();
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        if (_instance != null)
        {
            return;
        }

        GameObject root = new(nameof(UnityAdsManager));
        DontDestroyOnLoad(root);
        _instance = root.AddComponent<UnityAdsManager>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    public void ShowInterstitialThenContinue(Action onComplete)
    {
        if (!CanShowInterstitial())
        {
            LoadInterstitial();
            onComplete?.Invoke();
            return;
        }

        _showCompletedCallback = onComplete;
        _isInterstitialLoaded = false;
        Advertisement.Show(_interstitialAdUnitId, this);
    }

    public void ShowRewardedThenContinue(Action<bool> onComplete)
    {
        if (!CanShowRewarded())
        {
            LoadRewarded();
            onComplete?.Invoke(false);
            return;
        }

        _rewardedCompletedCallback = onComplete;
        _isRewardedLoaded = false;
        Advertisement.Show(_rewardedAdUnitId, this);
    }

    public bool IsRewardedReady()
    {
        return CanShowRewarded();
    }

    public void OnInitializationComplete()
    {
        _isInitialized = true;
        LoadInterstitial();
        LoadRewarded();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        _isInitialized = false;
        _isInterstitialLoading = false;
        _isRewardedLoading = false;
        _isInterstitialLoaded = false;
        _isRewardedLoaded = false;
        Debug.LogWarning($"Unity Ads initialization failed: {error} - {message}");
    }

    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        if (adUnitId == _interstitialAdUnitId)
        {
            _isInterstitialLoading = false;
            _isInterstitialLoaded = true;
        }
        else if (adUnitId == _rewardedAdUnitId)
        {
            _isRewardedLoading = false;
            _isRewardedLoaded = true;
        }
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        if (adUnitId == _interstitialAdUnitId)
        {
            _isInterstitialLoading = false;
            _isInterstitialLoaded = false;
        }
        else if (adUnitId == _rewardedAdUnitId)
        {
            _isRewardedLoading = false;
            _isRewardedLoaded = false;
        }

        Debug.LogWarning($"Unity Ads load failed: {adUnitId} - {error} - {message}");
    }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        if (adUnitId == _interstitialAdUnitId)
        {
            Debug.LogWarning($"Unity Ads show failed: {adUnitId} - {error} - {message}");
            CompleteShow();
            LoadInterstitial();
            return;
        }

        if (adUnitId == _rewardedAdUnitId)
        {
            Debug.LogWarning($"Unity Ads rewarded show failed: {adUnitId} - {error} - {message}");
            CompleteRewarded(false);
            LoadRewarded();
            return;
        }
    }

    public void OnUnityAdsShowStart(string adUnitId)
    {
    }

    public void OnUnityAdsShowClick(string adUnitId)
    {
    }

    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        if (adUnitId == _interstitialAdUnitId)
        {
            CompleteShow();
            LoadInterstitial();
            return;
        }

        if (adUnitId == _rewardedAdUnitId)
        {
            CompleteRewarded(showCompletionState == UnityAdsShowCompletionState.COMPLETED);
            LoadRewarded();
        }
    }

    private void Initialize()
    {
#if UNITY_EDITOR
        LogConfigurationWarning("Unity Ads is disabled in the Editor so automated regression runs never contact the ads service.");
        return;
#else
        _settings = Resources.Load<UnityAdsSettings>(SettingsResourcePath);
        if (_settings == null)
        {
            LogConfigurationWarning("Unity Ads settings asset is missing. Create Resources/UnityAdsSettings and set the Game IDs.");
            return;
        }

        if (!_settings.AdsEnabled)
        {
            LogConfigurationWarning("Unity Ads is disabled in UnityAdsSettings.");
            return;
        }

        if (!Advertisement.isSupported)
        {
            LogConfigurationWarning("Unity Ads is not supported on this platform.");
            return;
        }

        string gameId = GetGameId()?.Trim();
        _interstitialAdUnitId = GetInterstitialAdUnitId()?.Trim();
        _rewardedAdUnitId = GetRewardedAdUnitId()?.Trim();
        if (string.IsNullOrWhiteSpace(gameId) ||
            string.IsNullOrWhiteSpace(_interstitialAdUnitId) ||
            string.IsNullOrWhiteSpace(_rewardedAdUnitId))
        {
            LogConfigurationWarning("Unity Ads Game ID, interstitial Ad Unit ID, or rewarded Ad Unit ID is empty.");
            return;
        }

        if (Advertisement.isInitialized)
        {
            _isInitialized = true;
            LoadInterstitial();
            LoadRewarded();
            return;
        }

        Advertisement.Initialize(gameId, _settings.TestMode, this);
#endif
    }

    private void LoadInterstitial()
    {
        if (!_isInitialized || _isInterstitialLoading || _isInterstitialLoaded || string.IsNullOrWhiteSpace(_interstitialAdUnitId))
        {
            return;
        }

        _isInterstitialLoading = true;
        Advertisement.Load(_interstitialAdUnitId, this);
    }

    private void LoadRewarded()
    {
        if (!_isInitialized || _isRewardedLoading || _isRewardedLoaded || string.IsNullOrWhiteSpace(_rewardedAdUnitId))
        {
            return;
        }

        _isRewardedLoading = true;
        Advertisement.Load(_rewardedAdUnitId, this);
    }

    private bool CanShowInterstitial()
    {
        return _isInitialized &&
            _isInterstitialLoaded &&
            !Advertisement.isShowing &&
            _showCompletedCallback == null &&
            _rewardedCompletedCallback == null &&
            !string.IsNullOrWhiteSpace(_interstitialAdUnitId);
    }

    private bool CanShowRewarded()
    {
        return _isInitialized &&
            _isRewardedLoaded &&
            !Advertisement.isShowing &&
            _showCompletedCallback == null &&
            _rewardedCompletedCallback == null &&
            !string.IsNullOrWhiteSpace(_rewardedAdUnitId);
    }

    private string GetGameId()
    {
#if UNITY_IOS
        return _settings.IosGameId;
#else
        return _settings.AndroidGameId;
#endif
    }

    private string GetInterstitialAdUnitId()
    {
#if UNITY_IOS
        return _settings.IosInterstitialAdUnitId;
#else
        return _settings.AndroidInterstitialAdUnitId;
#endif
    }

    private string GetRewardedAdUnitId()
    {
#if UNITY_IOS
        return _settings.IosRewardedAdUnitId;
#else
        return _settings.AndroidRewardedAdUnitId;
#endif
    }

    private void CompleteShow()
    {
        Action callback = _showCompletedCallback;
        _showCompletedCallback = null;
        callback?.Invoke();
    }

    private void CompleteRewarded(bool wasCompleted)
    {
        Action<bool> callback = _rewardedCompletedCallback;
        _rewardedCompletedCallback = null;
        callback?.Invoke(wasCompleted);
    }

    private static void LogConfigurationWarning(string message)
    {
#if UNITY_EDITOR
        Debug.Log(message);
#else
        Debug.LogWarning(message);
#endif
    }
}
