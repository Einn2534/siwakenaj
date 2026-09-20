public static class AdMobConfiguration
{
    // Production units; the owner's validation device still receives test inventory.
    public static readonly bool UseTestAds = false;
    public static readonly string[] TestDeviceIds = { "741B535B2B07019778519CA2C9D7E7EF" };
    public const string AppId = "ca-app-pub-7699373274594279~5747435059";
    public const string ProductionRewardedId = "ca-app-pub-7699373274594279/7395558641";
    public const string ProductionInterstitialId = "ca-app-pub-7699373274594279/5809451135";
    public static string RewardedId => UseTestAds ? "ca-app-pub-3940256099942544/5224354917" : ProductionRewardedId;
    public static string InterstitialId => UseTestAds ? "ca-app-pub-3940256099942544/1033173712" : ProductionInterstitialId;
}
