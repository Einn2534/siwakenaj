using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LaneButtonPointerDownForwarder : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, ICancelHandler
{
    private LaneInputController _controller;
    private Button _button;
    private int _activePointerId;
    private bool _hasActivePointer;

    public CarType LaneType { get; private set; }
    public int LaneButtonIndex { get; private set; } = -1;

    public void Configure(LaneInputController controller, CarType laneType, int laneButtonIndex, Button button)
    {
        _controller = controller;
        LaneType = laneType;
        LaneButtonIndex = laneButtonIndex;
        _button = button;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (_controller == null || _button == null || !_button.isActiveAndEnabled || !_button.IsInteractable())
        {
            return;
        }

        _controller.PressLanePointerDown(LaneType, this);
        _activePointerId = eventData != null ? eventData.pointerId : 0;
        _hasActivePointer = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        int pointerId = eventData != null ? eventData.pointerId : 0;
        if (!_hasActivePointer || pointerId != _activePointerId)
        {
            return;
        }

        _hasActivePointer = false;
        _controller?.SuppressNextLaneClick(this);
    }

    public void OnCancel(BaseEventData eventData)
    {
        _hasActivePointer = false;
    }
}
