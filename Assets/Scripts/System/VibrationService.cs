using UnityEngine;

public static class VibrationService
{
    private const float TerminalEventCooldownSeconds = 1.25f;

    private static float _lastTerminalEventTime = -TerminalEventCooldownSeconds;

    public static bool IsEnabled => SaveService.GetVibrationOn();

    public static void PlayCorrect()
    {
        Vibrate();
    }

    public static void PlayMiss()
    {
        Vibrate();
    }

    public static void PlayClear()
    {
        VibrateTerminalEvent();
    }

    public static void PlayGameOver()
    {
        VibrateTerminalEvent();
    }

    public static void Stop()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
            using AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            if (currentActivity == null)
            {
                return;
            }

            using AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            vibrator?.Call("cancel");
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"Failed to stop vibration: {exception.Message}");
        }
#endif
    }

    private static void VibrateTerminalEvent()
    {
        float now = Time.unscaledTime;
        if (now - _lastTerminalEventTime < TerminalEventCooldownSeconds)
        {
            return;
        }

        _lastTerminalEventTime = now;
        Vibrate();
    }

    private static void Vibrate()
    {
        if (!SaveService.GetVibrationOn())
        {
            Stop();
            return;
        }

#if UNITY_EDITOR
        return;
#else
        try
        {
            Handheld.Vibrate();
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"Failed to play vibration: {exception.Message}");
        }
#endif
    }
}
