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
    public bool HasReachedTargetScore => State != null && State.HasReachedTargetScore;
    public bool HasReachedMissLimit => State != null && State.HasReachedMissLimit;

    public void Initialize(StageDefinition stageDefinition)
    {
        StageDefinition safeStageDefinition = stageDefinition ?? StageDefinition.CreateFallback(1);
        State = new ScoreState(safeStageDefinition.TargetScore, safeStageDefinition.MissLimit);
        _scoreLaneUi?.ResetAll();
    }

    public void ApplySuccess(CarType laneType)
    {
        if (State == null)
        {
            return;
        }

        State.ApplySuccess(laneType);
        _scoreLaneUi?.UpdateLane(laneType, State.GetCorrectCount(laneType));
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
