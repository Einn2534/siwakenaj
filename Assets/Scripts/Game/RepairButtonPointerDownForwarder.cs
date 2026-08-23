using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class RepairButtonPointerDownForwarder : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, ICancelHandler
{
    private const float PressedScale = 0.94f;

    private LaneInputController _controller;
    private UnityEngine.UI.Button _button;
    private RectTransform _rectTransform;
    private Vector3 _baseScale = Vector3.one;
    private int _activePointerId;
    private bool _hasActivePointer;

    public void Configure(LaneInputController controller, UnityEngine.UI.Button button)
    {
        _controller = controller;
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

        _controller.PressRepairPointerDown(this);
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

    private void SetPressedVisual(bool pressed)
    {
        if (_rectTransform != null)
        {
            _rectTransform.localScale = pressed ? _baseScale * PressedScale : _baseScale;
        }
    }
}
