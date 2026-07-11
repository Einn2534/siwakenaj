using UnityEngine;

public static class TutorialLaunchService
{
    public const int TutorialStageNumber = 1;

    private static bool _replayRequested;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        _replayRequested = false;
    }

    public static void RequestReplay()
    {
        _replayRequested = true;
    }

    public static bool ShouldStartTutorial(int stageNumber)
    {
        if (ConsumeReplayRequest())
        {
            return true;
        }

        return StageNumberUtility.Normalize(stageNumber) == TutorialStageNumber
            && !SaveService.GetTutorialCompleted()
            && !SaveService.GetTutorialSkipped();
    }

    public static bool ConsumeReplayRequest()
    {
        bool wasRequested = _replayRequested;
        _replayRequested = false;
        return wasRequested;
    }

    public static void MarkCompleted()
    {
        SaveService.SetTutorialCompleted(true);
        SaveService.SetTutorialSkipped(false);
        SaveService.Save();
    }

    public static void MarkSkipped()
    {
        SaveService.SetTutorialSkipped(true);
        SaveService.Save();
    }
}
