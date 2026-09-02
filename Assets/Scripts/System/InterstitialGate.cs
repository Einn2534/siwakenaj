using System;
using UnityEngine;

public static class InterstitialGate
{
    public const double MinimumSeconds = 120d;
    public const int MinimumCompletedStages = 3;
    private const string LastShownUtcTicksKey = "Interstitial_LastShownUtcTicks";
    private const string CompletedSinceLastKey = "Interstitial_CompletedSinceLast";

    public static void RecordCompletedRun()
    {
        PlayerPrefs.SetInt(CompletedSinceLastKey, Mathf.Max(0, PlayerPrefs.GetInt(CompletedSinceLastKey, 0)) + 1);
        PlayerPrefs.Save();
    }

    public static bool CanShow(GameMode mode, DateTime utcNow)
    {
        if (mode == GameMode.Daily)
        {
            return false;
        }

        int completed = Mathf.Max(0, PlayerPrefs.GetInt(CompletedSinceLastKey, 0));
        if (completed >= MinimumCompletedStages)
        {
            return true;
        }

        long lastTicks = 0L;
        long.TryParse(PlayerPrefs.GetString(LastShownUtcTicksKey, "0"), out lastTicks);
        if (lastTicks <= 0L)
        {
            return false;
        }

        DateTime lastShown = new(lastTicks, DateTimeKind.Utc);
        return (utcNow.ToUniversalTime() - lastShown).TotalSeconds >= MinimumSeconds;
    }

    public static void RecordShown(DateTime utcNow)
    {
        PlayerPrefs.SetString(LastShownUtcTicksKey, utcNow.ToUniversalTime().Ticks.ToString());
        PlayerPrefs.SetInt(CompletedSinceLastKey, 0);
        PlayerPrefs.Save();
    }
}
