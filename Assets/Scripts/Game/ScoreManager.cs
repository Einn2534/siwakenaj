using UnityEngine;
using UnityEngine.Serialization;

public class ScoreManager : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("scoreLaneUi")]
    private ScoreLaneUI _scoreLaneUi;

    public ScoreState State { get; private set; }
    public int CurrentScore => State != null ? State.CurrentScore : 0;
    public int MissCount => State != null ? State.MissCount : 0;
    public int RemainingSuccessCount => State != null ? State.RemainingSuccessCount : 0;
    public int ComboCount => State != null ? State.ComboCount : 0;
    public int TotalCorrectCount => State != null ? State.TotalCorrectCount : 0;
    public bool IsFeverActive => State != null && State.IsFeverActive;
    public bool HasReachedTargetScore => State != null && State.HasReachedTargetScore;
    public bool HasReachedMissLimit => State != null && State.HasReachedMissLimit;

    public void Initialize(StageDefinition stageDefinition)
    {
        StageDefinition safeStageDefinition = stageDefinition ?? StageDefinition.CreateFallback(1);
        State = new ScoreState(
            safeStageDefinition.TargetScore,
            safeStageDefinition.MissLimit,
            safeStageDefinition.FeverComboThreshold);
        _scoreLaneUi?.ResetAll();
    }

    public int ApplySuccess(CarType laneType)
    {
        return ApplySuccess(laneType, 1);
    }

    public int ApplySuccess(CarType laneType, int scoreMultiplier)
    {
        if (State == null)
        {
            return 0;
        }

        int earnedScore = State.ApplySuccess(laneType, scoreMultiplier);
        _scoreLaneUi?.UpdateLane(laneType, State.GetCorrectCount(laneType));
        return earnedScore;
    }

    public void ApplyMiss()
    {
        State?.ApplyMiss();
    }

    public void ReviveFromContinue()
    {
        State?.ReviveFromContinue();
    }

    public int GetCorrectCount(CarType laneType)
    {
        return State != null ? State.GetCorrectCount(laneType) : 0;
    }
}
