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

    public static int SelectEndless(int sourceStageNumber)
    {
        int safeStageNumber = StageNumberUtility.Normalize(sourceStageNumber);
        SessionState.SelectEndless(safeStageNumber);
        SaveService.SetSelectedStage(safeStageNumber);
        RememberLastEndless(safeStageNumber);
        return safeStageNumber;
    }

    public static int RememberLastStage(int stageNumber)
    {
        int safeStageNumber = StageNumberUtility.Normalize(stageNumber);
        SaveService.SetLastStage(safeStageNumber);
        SaveService.SetLastGameMode(GameMode.Stage);
        SaveService.Save();
        return safeStageNumber;
    }

    public static int RememberLastEndless(int sourceStageNumber)
    {
        int safeStageNumber = StageNumberUtility.Normalize(sourceStageNumber);
        SaveService.SetLastStage(safeStageNumber);
        SaveService.SetLastGameMode(GameMode.Endless);
        SaveService.Save();
        return safeStageNumber;
    }
}
