using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

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

    [SerializeField]
    private Button _laneAButton;

    [SerializeField]
    private Button _laneBButton;

    [SerializeField]
    private Button _laneCButton;

    private float _lastInputTime = InitialLastInputTime;
    private int _pendingFrame = InitialFrame;
    private CarType _pendingLaneType;
    private Coroutine _pendingCoroutine;
    private Coroutine _clearSuppressedClicksCoroutine;
    private int _suppressLaneAClickCount;
    private int _suppressLaneBClickCount;
    private int _suppressLaneCClickCount;

    private enum LaneButtonId
    {
        LaneA,
        LaneB,
        LaneC
    }

    public CarType LaneAType => _laneAType;
    public CarType LaneBType => _laneBType;
    public CarType LaneCType => _laneCType;

    private void Awake()
    {
        if (!TryGetComponent(out LaneButtonLayoutController _))
        {
            gameObject.AddComponent<LaneButtonLayoutController>();
        }

        ConfigurePointerDownForwarders();
    }

    private void OnEnable()
    {
        ConfigurePointerDownForwarders();
    }

    private void OnDisable()
    {
        ResetSuppressedClicks();
    }

    public void PressLaneA()
    {
        HandleClick(LaneButtonId.LaneA);
    }

    public void PressLaneB()
    {
        HandleClick(LaneButtonId.LaneB);
    }

    public void PressLaneC()
    {
        HandleClick(LaneButtonId.LaneC);
    }

    internal void PressLanePointerDown(CarType laneType, LaneButtonPointerDownForwarder source)
    {
        if (source == null || !TryGetLaneButtonId(source, out _))
        {
            return;
        }

        HandlePress(laneType);
    }

    internal void SuppressNextLaneClick(LaneButtonPointerDownForwarder source)
    {
        if (source != null && TryGetLaneButtonId(source, out LaneButtonId laneButtonId))
        {
            AddSuppressedClick(laneButtonId);
        }
    }

    private void HandleClick(LaneButtonId laneButtonId)
    {
        if (ConsumeSuppressedClick(laneButtonId))
        {
            return;
        }

        HandlePress(GetLaneType(laneButtonId));
    }

    private bool HandlePress(CarType laneType)
    {
        if (_gameFlowController == null || !_gameFlowController.IsPlaying())
        {
            return false;
        }

        if (Time.time - _lastInputTime < InputCooldownSeconds)
        {
            return false;
        }

        _pendingLaneType = laneType;
        _pendingFrame = Time.frameCount;

        if (_pendingCoroutine == null)
        {
            _pendingCoroutine = StartCoroutine(ProcessPendingInput());
        }

        return true;
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

    private void ConfigurePointerDownForwarders()
    {
        int configuredButtonCount = 0;

        ConfigureButtonForwarder(_laneAButton, LaneButtonId.LaneA);
        ConfigureButtonForwarder(_laneBButton, LaneButtonId.LaneB);
        ConfigureButtonForwarder(_laneCButton, LaneButtonId.LaneC);
        configuredButtonCount += _laneAButton != null ? 1 : 0;
        configuredButtonCount += _laneBButton != null ? 1 : 0;
        configuredButtonCount += _laneCButton != null ? 1 : 0;

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (TryResolveLaneButtonId(button, out LaneButtonId laneButtonId))
            {
                ConfigureButtonForwarder(button, laneButtonId);
                configuredButtonCount += 1;
            }
        }

        if (configuredButtonCount == 0)
        {
            ConfigureDirectChildFallback(buttons);
        }
    }

    private void ConfigureDirectChildFallback(Button[] buttons)
    {
        if (buttons == null || buttons.Length != 3)
        {
            return;
        }

        for (int i = 0; i < buttons.Length; i += 1)
        {
            if (buttons[i] == null || buttons[i].transform.parent != transform)
            {
                return;
            }
        }

        ConfigureButtonForwarder(buttons[0], LaneButtonId.LaneA);
        ConfigureButtonForwarder(buttons[1], LaneButtonId.LaneB);
        ConfigureButtonForwarder(buttons[2], LaneButtonId.LaneC);
    }

    private void ConfigureButtonForwarder(Button button, LaneButtonId laneButtonId)
    {
        if (button == null)
        {
            return;
        }

        LaneButtonPointerDownForwarder forwarder = button.GetComponent<LaneButtonPointerDownForwarder>();
        if (forwarder == null)
        {
            forwarder = button.gameObject.AddComponent<LaneButtonPointerDownForwarder>();
        }

        forwarder.Configure(this, GetLaneType(laneButtonId), (int)laneButtonId, button);
    }

    private bool TryResolveLaneButtonId(Button button, out LaneButtonId laneButtonId)
    {
        laneButtonId = default;
        if (button == null)
        {
            return false;
        }

        if (button == _laneAButton)
        {
            laneButtonId = LaneButtonId.LaneA;
            return true;
        }

        if (button == _laneBButton)
        {
            laneButtonId = LaneButtonId.LaneB;
            return true;
        }

        if (button == _laneCButton)
        {
            laneButtonId = LaneButtonId.LaneC;
            return true;
        }

        int eventCount = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < eventCount; i += 1)
        {
            if (button.onClick.GetPersistentTarget(i) != this)
            {
                continue;
            }

            switch (button.onClick.GetPersistentMethodName(i))
            {
                case nameof(PressLaneA):
                    laneButtonId = LaneButtonId.LaneA;
                    return true;
                case nameof(PressLaneB):
                    laneButtonId = LaneButtonId.LaneB;
                    return true;
                case nameof(PressLaneC):
                    laneButtonId = LaneButtonId.LaneC;
                    return true;
            }
        }

        return false;
    }

    private bool TryGetLaneButtonId(LaneButtonPointerDownForwarder source, out LaneButtonId laneButtonId)
    {
        laneButtonId = default;
        if (source == null)
        {
            return false;
        }

        if (source.LaneButtonIndex < 0 || source.LaneButtonIndex > (int)LaneButtonId.LaneC)
        {
            return false;
        }

        laneButtonId = (LaneButtonId)source.LaneButtonIndex;
        return true;
    }

    private CarType GetLaneType(LaneButtonId laneButtonId)
    {
        return laneButtonId switch
        {
            LaneButtonId.LaneA => _laneAType,
            LaneButtonId.LaneB => _laneBType,
            LaneButtonId.LaneC => _laneCType,
            _ => _laneAType
        };
    }

    private void AddSuppressedClick(LaneButtonId laneButtonId)
    {
        switch (laneButtonId)
        {
            case LaneButtonId.LaneA:
                _suppressLaneAClickCount += 1;
                break;
            case LaneButtonId.LaneB:
                _suppressLaneBClickCount += 1;
                break;
            case LaneButtonId.LaneC:
                _suppressLaneCClickCount += 1;
                break;
        }

        if (_clearSuppressedClicksCoroutine == null)
        {
            _clearSuppressedClicksCoroutine = StartCoroutine(ClearSuppressedClicksAtEndOfFrame());
        }
    }

    private bool ConsumeSuppressedClick(LaneButtonId laneButtonId)
    {
        switch (laneButtonId)
        {
            case LaneButtonId.LaneA when _suppressLaneAClickCount > 0:
                _suppressLaneAClickCount -= 1;
                return true;
            case LaneButtonId.LaneB when _suppressLaneBClickCount > 0:
                _suppressLaneBClickCount -= 1;
                return true;
            case LaneButtonId.LaneC when _suppressLaneCClickCount > 0:
                _suppressLaneCClickCount -= 1;
                return true;
            default:
                return false;
        }
    }

    private IEnumerator ClearSuppressedClicksAtEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        ResetSuppressedClicks();
    }

    private void ResetSuppressedClicks()
    {
        _suppressLaneAClickCount = 0;
        _suppressLaneBClickCount = 0;
        _suppressLaneCClickCount = 0;
        _clearSuppressedClicksCoroutine = null;
    }
}
