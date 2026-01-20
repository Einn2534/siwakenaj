// Created: 2025-02-14
// Author: gpt-5.2-codex

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>横スワイプのカードをスナップ移動させる。</summary>
public class SwipeSnapController : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    private const float DEFAULT_SNAP_SPEED = 10f;

    [SerializeField]
    ScrollRect scrollRect;

    [SerializeField]
    RectTransform content;

    [SerializeField]
    RectTransform[] pages;

    [SerializeField]
    float snapSpeed = DEFAULT_SNAP_SPEED;

    [SerializeField]
    UnityEvent<int> onIndexChanged;

    int targetIndex;
    bool hasInitialized;
    bool isDragging;

    /// <summary>初期化時にページ位置を設定する。</summary>
    void Start()
    {
        initialize_pages();
    }

    /// <summary>スナップ動作を更新する。</summary>
    void Update()
    {
        if (!hasInitialized || pages == null || pages.Length == 0)
        {
            return;
        }

        if (scrollRect && !isDragging)
        {
            float targetPosition = get_page_normalized_position(targetIndex);
            float newPosition = Mathf.Lerp(scrollRect.horizontalNormalizedPosition, targetPosition, Time.deltaTime * snapSpeed);
            scrollRect.horizontalNormalizedPosition = newPosition;
        }

        if (scrollRect && isDragging)
        {
            int nearestIndex = get_nearest_page_index();
            if (nearestIndex != targetIndex)
            {
                targetIndex = nearestIndex;
                onIndexChanged?.Invoke(targetIndex);
            }
        }
    }

    /// <summary>ドラッグ開始時にフラグを更新する。</summary>
    /// <param name="eventData">イベントデータ。</param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
    }

    /// <summary>ドラッグ終了時にフラグを更新する。</summary>
    /// <param name="eventData">イベントデータ。</param>
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }

    /// <summary>指定したインデックスへ移動する。</summary>
    /// <param name="index">移動先インデックス。</param>
    public void jump_to_index(int index)
    {
        initialize_pages();
        targetIndex = clamp_index(index);
        if (scrollRect)
        {
            scrollRect.horizontalNormalizedPosition = get_page_normalized_position(targetIndex);
        }

        onIndexChanged?.Invoke(targetIndex);
    }

    /// <summary>現在選択中のインデックスを取得する。</summary>
    /// <returns>選択中のインデックス。</returns>
    public int get_current_index()
    {
        return targetIndex;
    }

    /// <summary>ページ情報を初期化する。</summary>
    void initialize_pages()
    {
        if (hasInitialized)
        {
            return;
        }

        if (pages == null || pages.Length == 0)
        {
            hasInitialized = true;
            return;
        }

        targetIndex = clamp_index(targetIndex);
        if (scrollRect)
        {
            scrollRect.horizontalNormalizedPosition = get_page_normalized_position(targetIndex);
        }

        onIndexChanged?.Invoke(targetIndex);
        hasInitialized = true;
    }

    /// <summary>一番近いページのインデックスを取得する。</summary>
    /// <returns>最も近いページインデックス。</returns>
    int get_nearest_page_index()
    {
        if (scrollRect == null || pages == null || pages.Length == 0)
        {
            return 0;
        }

        float position = scrollRect.horizontalNormalizedPosition;
        float pageStep = get_page_step();
        int index = Mathf.RoundToInt(position / pageStep);
        return clamp_index(index);
    }

    /// <summary>ページ間のステップ値を取得する。</summary>
    /// <returns>0 から 1 の正規化ステップ。</returns>
    float get_page_step()
    {
        if (pages == null || pages.Length <= 1)
        {
            return 1f;
        }

        return 1f / (pages.Length - 1);
    }

    /// <summary>ページの正規化位置を取得する。</summary>
    /// <param name="index">ページインデックス。</param>
    /// <returns>0 から 1 の正規化位置。</returns>
    float get_page_normalized_position(int index)
    {
        return clamp_index(index) * get_page_step();
    }

    /// <summary>インデックスを範囲内に丸める。</summary>
    /// <param name="index">入力インデックス。</param>
    /// <returns>補正済みインデックス。</returns>
    int clamp_index(int index)
    {
        if (pages == null || pages.Length == 0)
        {
            return 0;
        }

        return Mathf.Clamp(index, 0, pages.Length - 1);
    }
}
