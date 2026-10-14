using System;
using UnityEngine;

public static class InterstitialGate
{
    public const double MinimumSeconds = 180d;
    public const int MinimumCompletedStages = 2;
    private const string LastShownUtcTicksKey = "Interstitial_LastShownUtcTicks";
    private const string CompletedSinceLastKey = "Interstitial_CompletedSinceLast";

    public static void RecordCompletedRun()
    {
        // Give the first advertisement the same cooldown as later ones.
        if (!TryReadCooldownStart(out _))
        {
            PlayerPrefs.SetString(LastShownUtcTicksKey, DateTime.UtcNow.Ticks.ToString());
        }
        PlayerPrefs.SetInt(CompletedSinceLastKey, Mathf.Max(0, PlayerPrefs.GetInt(CompletedSinceLastKey, 0)) + 1);
        PlayerPrefs.Save();
    }

    public static bool CanShow(GameMode mode, DateTime utcNow)
    {
        if (AdRemovalEntitlement.IsOwned || mode != GameMode.Stage)
        {
            return false;
        }

        int completed = Mathf.Max(0, PlayerPrefs.GetInt(CompletedSinceLastKey, 0));
        if (completed < MinimumCompletedStages || !TryReadCooldownStart(out DateTime cooldownStart))
        {
            return false;
        }

        return (utcNow.ToUniversalTime() - cooldownStart).TotalSeconds >= MinimumSeconds;
    }

    private static bool TryReadCooldownStart(out DateTime cooldownStart)
    {
        cooldownStart = default;
        if (!long.TryParse(PlayerPrefs.GetString(LastShownUtcTicksKey, "0"), out long ticks)
            || ticks <= 0L || ticks > DateTime.MaxValue.Ticks) return false;
        cooldownStart = new DateTime(ticks, DateTimeKind.Utc);
        return true;
    }

    public static void RecordShown(DateTime utcNow)
    {
        PlayerPrefs.SetString(LastShownUtcTicksKey, utcNow.ToUniversalTime().Ticks.ToString());
        PlayerPrefs.SetInt(CompletedSinceLastKey, 0);
        PlayerPrefs.Save();
    }
}
