using UnityEngine;

public static class SessionState
{
    private const int DefaultStageNumber = 1;

    public static int SelectedStageNumber { get; private set; } = DefaultStageNumber;
    public static GameResultData LastResult { get; private set; } = GameResultData.Empty(DefaultStageNumber);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeFromSave()
    {
        SelectStage(SaveService.GetSelectedStage());
        LastResult = GameResultData.Empty(SelectedStageNumber);
    }

    public static void SelectStage(int stageNumber)
    {
        SelectedStageNumber = stageNumber < DefaultStageNumber ? DefaultStageNumber : stageNumber;
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
