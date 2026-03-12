using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(LaneButtonLayoutController))]
public class LaneInputController : MonoBehaviour
{
    private const float InputCooldownSeconds = 0.08f;
    private const float InitialLastInputTime = -1f;
    private const int InitialFrame = -1;

    [SerializeField, FormerlySerializedAs("laneAType")]
    private CarType _laneAType = CarType.LightTruck;

    [SerializeField, FormerlySerializedAs("laneBType")]
    private CarType _laneBType = CarType.CompactCar;

    [SerializeField, FormerlySerializedAs("laneCType")]
    private CarType _laneCType = CarType.SportsCar;

    [SerializeField, FormerlySerializedAs("gameController")]
    private GameFlowController _gameFlowController;

    private float _lastInputTime = InitialLastInputTime;
    private int _pendingFrame = InitialFrame;
    private CarType _pendingLaneType;
    private Coroutine _pendingCoroutine;

    private void Awake()
    {
        if (!TryGetComponent(out LaneButtonLayoutController _))
        {
            gameObject.AddComponent<LaneButtonLayoutController>();
        }
    }

    public void PressLaneA()
    {
        HandlePress(_laneAType);
    }

    public void PressLaneB()
    {
        HandlePress(_laneBType);
    }

    public void PressLaneC()
    {
        HandlePress(_laneCType);
    }

    private void HandlePress(CarType laneType)
    {
        if (_gameFlowController == null || !_gameFlowController.IsPlaying())
        {
            return;
        }

        if (Time.time - _lastInputTime < InputCooldownSeconds)
        {
            return;
        }

        _pendingLaneType = laneType;
        _pendingFrame = Time.frameCount;

        if (_pendingCoroutine == null)
        {
            _pendingCoroutine = StartCoroutine(ProcessPendingInput());
        }
    }

    private IEnumerator ProcessPendingInput()
    {
        int frame = _pendingFrame;
        yield return new WaitForEndOfFrame();

        if (frame == _pendingFrame && _gameFlowController != null && _gameFlowController.IsPlaying())
        {
            _gameFlowController.HandleLaneInput(_pendingLaneType);
            _lastInputTime = Time.time;
        }

        _pendingCoroutine = null;
    }
}
