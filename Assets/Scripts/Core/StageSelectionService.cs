public static class StageSelectionService
{
    public static int SelectStage(int stageNumber)
    {
        int safeStageNumber = StageNumberUtility.Normalize(stageNumber);
        SessionState.SelectStage(safeStageNumber);
        SaveService.SetSelectedStage(safeStageNumber);
        RememberLastStage(safeStageNumber);
        return safeStageNumber;
    }

    public static int RememberLastStage(int stageNumber)
    {
        int safeStageNumber = StageNumberUtility.Normalize(stageNumber);
        SaveService.SetLastStage(safeStageNumber);
        SaveService.Save();
        return safeStageNumber;
    }
}
