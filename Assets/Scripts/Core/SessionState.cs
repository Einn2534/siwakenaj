using UnityEngine;

public static class SessionState
{
    public static int SelectedStageNumber { get; private set; } = StageNumberUtility.MinimumStageNumber;
    public static GameResultData LastResult { get; private set; } = GameResultData.Empty(StageNumberUtility.MinimumStageNumber);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeFromSave()
    {
        SelectStage(SaveService.GetSelectedStage());
        LastResult = GameResultData.Empty(SelectedStageNumber);
    }

    public static void SelectStage(int stageNumber)
    {
        SelectedStageNumber = StageNumberUtility.Normalize(stageNumber);
    }

    public static void StoreResult(GameResultData result)
    {
        if (result == null)
        {
            return;
        }

        SelectStage(result.StageNumber);
        LastResult = result;
    }
}
