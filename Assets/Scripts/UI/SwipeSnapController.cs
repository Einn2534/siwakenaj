using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class SwipeSnapController : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    private const float SwipeThresholdRatio = 0.2f;
    private const float SnapLerpSpeed = 10f;
    private const float SnapEpsilon = 0.001f;

    [SerializeField, FormerlySerializedAs("scrollRect")]
    private ScrollRect _scrollRect;

    [SerializeField, FormerlySerializedAs("content")]
    private RectTransform _content;

    [SerializeField, FormerlySerializedAs("pageCount")]
    private int _pageCount = 1;

    private int _currentIndex;
    private bool _isDragging;
    private float _dragStartNormalizedX;
    private RectTransform _viewportRect;

    public event Action<int> OnPageChanged;

    private void Start()
    {
        ResolveReferences();
        RebuildLayout();
        JumpToIndex(0);
    }

    private void Update()
    {
        if (_isDragging || GetPageCount() <= 1 || _scrollRect == null)
        {
            return;
        }

        float target = GetNormalizedX(_currentIndex);
        float next = Mathf.Lerp(_scrollRect.horizontalNormalizedPosition, target, Time.deltaTime * SnapLerpSpeed);

        if (Mathf.Abs(next - target) <= SnapEpsilon)
        {
            next = target;
        }

        _scrollRect.horizontalNormalizedPosition = next;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        _dragStartNormalizedX = _scrollRect.horizontalNormalizedPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;

        int pageCount = GetPageCount();
        if (pageCount <= 0)
        {
            return;
        }

        float delta = _scrollRect.horizontalNormalizedPosition - _dragStartNormalizedX;
        float threshold = SwipeThresholdRatio / Mathf.Max(1, pageCount - 1);
        int newIndex = _currentIndex;

        if (delta > threshold && _currentIndex < pageCount - 1)
        {
            newIndex = _currentIndex + 1;
        }
        else if (delta < -threshold && _currentIndex > 0)
        {
            newIndex = _currentIndex - 1;
        }

        SetIndex(newIndex);
    }

    public void JumpToIndex(int index)
    {
        ResolveReferences();
        RebuildLayout();
        SetIndex(index);

        if (_scrollRect != null)
        {
            _scrollRect.horizontalNormalizedPosition = GetNormalizedX(_currentIndex);
        }
    }

    public int GetCurrentIndex()
    {
        return _currentIndex;
    }

    public RectTransform GetContent()
    {
        ResolveReferences();
        return _content;
    }

    public void Refresh()
    {
        ResolveReferences();
        RebuildLayout();
        SetIndex(_currentIndex);

        if (_scrollRect != null)
        {
            _scrollRect.horizontalNormalizedPosition = GetNormalizedX(_currentIndex);
        }
    }

    private void SetIndex(int index)
    {
        int clamped = Mathf.Clamp(index, 0, Mathf.Max(0, GetPageCount() - 1));
        bool changed = clamped != _currentIndex;
        _currentIndex = clamped;

        if (changed)
        {
            OnPageChanged?.Invoke(_currentIndex);
        }
    }

    private float GetNormalizedX(int index)
    {
        ResolveReferences();
        int pageCount = GetPageCount();

        if (pageCount <= 1 || _content == null || _viewportRect == null)
        {
            return 0f;
        }

        int clampedIndex = Mathf.Clamp(index, 0, pageCount - 1);
        RectTransform child = GetPageRect(clampedIndex);
        if (child == null)
        {
            return 0f;
        }

        float scrollableWidth = _content.rect.width - _viewportRect.rect.width;
        if (scrollableWidth <= 0f)
        {
            return 0f;
        }

        Bounds childBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(_content, child);
        float targetOffset = childBounds.center.x - (_viewportRect.rect.width * 0.5f);
        return Mathf.Clamp01(targetOffset / scrollableWidth);
    }

    private void ResolveReferences()
    {
        if (_scrollRect == null)
        {
            _scrollRect = GetComponent<ScrollRect>();
        }

        if (_content == null && _scrollRect != null)
        {
            _content = _scrollRect.content;
        }

        if (_viewportRect == null && _scrollRect != null)
        {
            _viewportRect = _scrollRect.viewport ? _scrollRect.viewport : _scrollRect.transform as RectTransform;
        }
    }

    private void RebuildLayout()
    {
        if (_scrollRect == null || _content == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        if (_viewportRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_viewportRect);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
    }

    private int GetPageCount()
    {
        ResolveReferences();

        if (_content == null)
        {
            return Mathf.Max(1, _pageCount);
        }

        int count = 0;
        for (int i = 0; i < _content.childCount; i += 1)
        {
            if (_content.GetChild(i).gameObject.activeSelf)
            {
                count += 1;
            }
        }

        return count;
    }

    private RectTransform GetPageRect(int pageIndex)
    {
        if (_content == null || pageIndex < 0)
        {
            return null;
        }

        int activeIndex = 0;
        for (int i = 0; i < _content.childCount; i += 1)
        {
            RectTransform child = _content.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf)
            {
                continue;
            }

            if (activeIndex == pageIndex)
            {
                return child;
            }

            activeIndex += 1;
        }

        return null;
    }
}
