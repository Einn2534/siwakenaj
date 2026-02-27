// Created: 2025-02-14
// Updated: 2026-02-26
// Author: Einn

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>スワイプ操作でページをスナップスクロールする。</summary>
public class SwipeSnapController : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    private const float SWIPE_THRESHOLD_RATIO = 0.2f;
    private const float SNAP_LERP_SPEED = 10f;

    [SerializeField]
    ScrollRect scrollRect;

    [SerializeField]
    RectTransform content;

    [SerializeField]
    int pageCount = 1;

    /// <summary>選択インデックスが変わったときに呼ばれるイベント。</summary>
    public event Action<int> onPageChanged;

    int currentIndex;
    bool isDragging;
    float dragStartNormalizedX;

    /// <summary>初回表示時にページ0へスナップする。</summary>
    void Start()
    {
        jump_to_index(0);
    }

    /// <summary>スナップアニメーションを毎フレーム更新する。</summary>
    void Update()
    {
        if (isDragging || pageCount <= 1)
        {
            return;
        }

        float target = get_normalized_x(currentIndex);
        scrollRect.horizontalNormalizedPosition = Mathf.Lerp(
            scrollRect.horizontalNormalizedPosition,
            target,
            Time.deltaTime * SNAP_LERP_SPEED);
    }

    /// <summary>ドラッグ開始位置を記録する。</summary>
    /// <param name="eventData">ポインタイベントデータ。</param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        dragStartNormalizedX = scrollRect.horizontalNormalizedPosition;
    }

    /// <summary>ドラッグ終了後にスナップ先を決定する。</summary>
    /// <param name="eventData">ポインタイベントデータ。</param>
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        float delta = scrollRect.horizontalNormalizedPosition - dragStartNormalizedX;
        float threshold = SWIPE_THRESHOLD_RATIO / Mathf.Max(1, pageCount - 1);

        int newIndex = currentIndex;
        if (delta > threshold && currentIndex < pageCount - 1)
        {
            newIndex = currentIndex + 1;
        }
        else if (delta < -threshold && currentIndex > 0)
        {
            newIndex = currentIndex - 1;
        }

        set_index(newIndex);
    }

    /// <summary>指定インデックスへスナップする（即座に移動）。</summary>
    /// <param name="index">移動先のインデックス。</param>
    public void jump_to_index(int index)
    {
        set_index(index);
        scrollRect.horizontalNormalizedPosition = get_normalized_x(currentIndex);
    }

    /// <summary>現在の選択インデックスを取得する。</summary>
    /// <returns>選択中インデックス。</returns>
    public int get_current_index()
    {
        return currentIndex;
    }

    /// <summary>インデックスを更新し、変更があればイベントを発火する。</summary>
    /// <param name="index">新しいインデックス。</param>
    void set_index(int index)
    {
        int clamped = Mathf.Clamp(index, 0, Mathf.Max(0, pageCount - 1));
        bool changed = clamped != currentIndex;
        currentIndex = clamped;

        if (changed)
        {
            onPageChanged?.Invoke(currentIndex);
        }
    }

    /// <summary>インデックスに対応する横方向の正規化座標を算出する。</summary>
    /// <param name="index">対象インデックス。</param>
    /// <returns>0〜1 の正規化値。</returns>
    float get_normalized_x(int index)
    {
        if (pageCount <= 1)
        {
            return 0f;
        }

        return (float)index / (pageCount - 1);
    }
}
