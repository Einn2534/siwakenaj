using System;
using UnityEngine;
using UnityEngine.Advertisements;

public class UnityAdsManager : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
{
    private const string SettingsResourcePath = "UnityAdsSettings";

    private static UnityAdsManager _instance;

    private UnityAdsSettings _settings;
    private string _interstitialAdUnitId;
    private bool _isInitialized;
    private bool _isLoading;
    private bool _isInterstitialLoaded;
    private Action _showCompletedCallback;

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
        if (!_isInitialized || !_isInterstitialLoaded || string.IsNullOrWhiteSpace(_interstitialAdUnitId))
        {
            LoadInterstitial();
            onComplete?.Invoke();
            return;
        }

        _showCompletedCallback = onComplete;
        _isInterstitialLoaded = false;
        Advertisement.Show(_interstitialAdUnitId, this);
    }

    public void OnInitializationComplete()
    {
        _isInitialized = true;
        LoadInterstitial();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogWarning($"Unity Ads initialization failed: {error} - {message}");
    }

    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        if (adUnitId == _interstitialAdUnitId)
        {
            _isLoading = false;
            _isInterstitialLoaded = true;
        }
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        if (adUnitId == _interstitialAdUnitId)
        {
            _isLoading = false;
            _isInterstitialLoaded = false;
        }

        Debug.LogWarning($"Unity Ads load failed: {adUnitId} - {error} - {message}");
    }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        Debug.LogWarning($"Unity Ads show failed: {adUnitId} - {error} - {message}");
        CompleteShow();
        LoadInterstitial();
    }

    public void OnUnityAdsShowStart(string adUnitId)
    {
    }

    public void OnUnityAdsShowClick(string adUnitId)
    {
    }

    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        CompleteShow();
        LoadInterstitial();
    }

    private void Initialize()
    {
        _settings = Resources.Load<UnityAdsSettings>(SettingsResourcePath);
        if (_settings == null)
        {
            LogConfigurationWarning("Unity Ads settings asset is missing. Create Resources/UnityAdsSettings and set the Game IDs.");
            return;
        }

        string gameId = GetGameId();
        _interstitialAdUnitId = GetInterstitialAdUnitId();
        if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(_interstitialAdUnitId))
        {
            LogConfigurationWarning("Unity Ads Game ID or interstitial Ad Unit ID is empty.");
            return;
        }

        Advertisement.Initialize(gameId, _settings.TestMode, this);
    }

    private void LoadInterstitial()
    {
        if (!_isInitialized || _isLoading || _isInterstitialLoaded || string.IsNullOrWhiteSpace(_interstitialAdUnitId))
        {
            return;
        }

        _isLoading = true;
        Advertisement.Load(_interstitialAdUnitId, this);
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

    private void CompleteShow()
    {
        Action callback = _showCompletedCallback;
        _showCompletedCallback = null;
        callback?.Invoke();
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
