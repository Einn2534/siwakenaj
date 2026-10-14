using System;
using UnityEngine;

/// <summary>Offline cache of a store-granted non-consumable purchase.</summary>
public static class AdRemovalEntitlement
{
    public const string ProductId = "remove_ads";

    // Editor purchases must never grant an entitlement in a production build.
    public static string PreferenceKey =>
#if UNITY_EDITOR
        "IAP_Editor_RemoveAds_v1";
#else
        "IAP_RemoveAds_v1";
#endif

    public static bool IsOwned => PlayerPrefs.GetInt(PreferenceKey, 0) == 1;
    public static event Action<bool> Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetListeners() => Changed = null;

    internal static void SetOwned(bool owned)
    {
        bool changed = IsOwned != owned;
        PlayerPrefs.SetInt(PreferenceKey, owned ? 1 : 0);
        // Persist the benefit before acknowledging the transaction with the store.
        PlayerPrefs.Save();
        if (changed) Changed?.Invoke(owned);
    }
}
