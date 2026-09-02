using UnityEngine;

public enum GameMode
{
    Stage,
    Endless,
    Daily
}

public static class SessionState
{
    public static GameMode SelectedGameMode { get; private set; } = GameMode.Stage;
    public static int SelectedStageNumber { get; private set; } = StageNumberUtility.MinimumStageNumber;
    public static GameResultData LastResult { get; private set; } = GameResultData.Empty(GameMode.Stage, StageNumberUtility.MinimumStageNumber);
    public static bool IsEndlessMode => SelectedGameMode == GameMode.Endless;
    public static bool IsDailyMode => SelectedGameMode == GameMode.Daily;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeFromSave()
    {
        int selectedStageNumber = SaveService.GetSelectedStage();
        if (SaveService.GetLastGameMode() == GameMode.Endless)
        {
            SelectEndless(selectedStageNumber);
        }
        else if (SaveService.GetLastGameMode() == GameMode.Daily)
        {
            SelectDaily(selectedStageNumber);
        }
        else
        {
            SelectStage(selectedStageNumber);
        }

        LastResult = GameResultData.Empty(SelectedGameMode, SelectedStageNumber);
    }

    public static void SelectStage(int stageNumber)
    {
        SelectedGameMode = GameMode.Stage;
        SelectedStageNumber = StageNumberUtility.Normalize(stageNumber);
    }

    public static void SelectEndless(int sourceStageNumber)
    {
        SelectedGameMode = GameMode.Endless;
        SelectedStageNumber = StageNumberUtility.Normalize(sourceStageNumber);
    }

    public static void SelectDaily(int sourceStageNumber)
    {
        SelectedGameMode = GameMode.Daily;
        SelectedStageNumber = StageNumberUtility.Normalize(sourceStageNumber);
    }

    public static void StoreResult(GameResultData result)
    {
        if (result == null)
        {
            return;
        }

        if (result.Mode == GameMode.Endless)
        {
            SelectEndless(result.StageNumber);
        }
        else if (result.Mode == GameMode.Daily)
        {
            SelectDaily(result.StageNumber);
        }
        else
        {
            SelectStage(result.StageNumber);
        }

        LastResult = result;
    }
}
