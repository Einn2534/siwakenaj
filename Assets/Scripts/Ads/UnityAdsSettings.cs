using UnityEngine;

[CreateAssetMenu(fileName = "UnityAdsSettings", menuName = "Siwakenja/Unity Ads Settings")]
public class UnityAdsSettings : ScriptableObject
{
    [SerializeField]
    private bool _adsEnabled = true;

    [SerializeField]
    private string _androidGameId = string.Empty;

    [SerializeField]
    private string _iosGameId = string.Empty;

    [SerializeField]
    private string _androidInterstitialAdUnitId = "Interstitial_Android";

    [SerializeField]
    private string _iosInterstitialAdUnitId = "Interstitial_iOS";

    [SerializeField]
    private string _androidRewardedAdUnitId = "Rewarded_Android";

    [SerializeField]
    private string _iosRewardedAdUnitId = "Rewarded_iOS";

    [SerializeField]
    private bool _testMode = true;

    public bool AdsEnabled => _adsEnabled;
    public string AndroidGameId => _androidGameId;
    public string IosGameId => _iosGameId;
    public string AndroidInterstitialAdUnitId => _androidInterstitialAdUnitId;
    public string IosInterstitialAdUnitId => _iosInterstitialAdUnitId;
    public string AndroidRewardedAdUnitId => _androidRewardedAdUnitId;
    public string IosRewardedAdUnitId => _iosRewardedAdUnitId;
    public bool TestMode => _testMode;
}
