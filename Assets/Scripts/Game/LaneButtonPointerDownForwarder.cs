using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LaneButtonPointerDownForwarder : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, ICancelHandler
{
    private const float PressedScale = 0.94f;

    private LaneInputController _controller;
    private Button _button;
    private RectTransform _rectTransform;
    private Vector3 _baseScale = Vector3.one;
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
        _rectTransform = button != null ? button.transform as RectTransform : transform as RectTransform;
        _baseScale = _rectTransform != null ? _rectTransform.localScale : Vector3.one;
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
        SetPressedVisual(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        int pointerId = eventData != null ? eventData.pointerId : 0;
        if (!_hasActivePointer || pointerId != _activePointerId)
        {
            return;
        }

        _hasActivePointer = false;
        SetPressedVisual(false);
        _controller?.SuppressNextLaneClick(this);
    }

    public void OnCancel(BaseEventData eventData)
    {
        _hasActivePointer = false;
        SetPressedVisual(false);
    }

    private void OnDisable()
    {
        _hasActivePointer = false;
        SetPressedVisual(false);
    }

    private void SetPressedVisual(bool isPressed)
    {
        if (_rectTransform == null)
        {
            _rectTransform = transform as RectTransform;
            _baseScale = _rectTransform != null ? _rectTransform.localScale : Vector3.one;
        }

        if (_rectTransform != null)
        {
            _rectTransform.localScale = isPressed ? _baseScale * PressedScale : _baseScale;
        }
    }
}
