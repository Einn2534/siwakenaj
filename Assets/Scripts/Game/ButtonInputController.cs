// Created: 2025-05-07
// Updated: 2026-03-13
// Author: Einn

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>UIボタンの入力を判定処理へ中継する。</summary>
public class ButtonInputController : MonoBehaviour
{
    private const float INPUT_COOLDOWN_SECONDS = 0.08f;
    private const float INITIAL_LAST_INPUT_TIME = -1f;
    private const int INITIAL_FRAME = -1;
    private const float BUTTON_WIDTH_RATIO = 0.24f;
    private const float MIN_BUTTON_WIDTH = 120f;
    private const float MAX_BUTTON_WIDTH = 300f;
    private const float HORIZONTAL_PADDING_RATIO = 0.03f;
    private const float MIN_HORIZONTAL_PADDING = 12f;
    private const float MAX_HORIZONTAL_PADDING = 36f;
    private const float SPACING_RATIO = 0.02f;
    private const float MIN_SPACING = 8f;
    private const float MAX_SPACING = 24f;
    private const float VERTICAL_PADDING_RATIO = 0.06f;
    private const float MIN_VERTICAL_PADDING = 12f;
    private const float MAX_VERTICAL_PADDING = 32f;
    private const float MIN_BUTTON_ASPECT = 0.1f;
    private const float EPSILON = 0.5f;

    [SerializeField]
    JudgeController judgeController;

    [SerializeField]
    CarSpawner carSpawner;

    [SerializeField]
    CarType laneAType = CarType.LightTruck;

    [SerializeField]
    CarType laneBType = CarType.CompactCar;

    [SerializeField]
    CarType laneCType = CarType.SportsCar;

    [SerializeField]
    GameController gameController;

    float lastInputTime = INITIAL_LAST_INPUT_TIME;
    int pendingFrame = INITIAL_FRAME;
    CarType pendingLaneType;
    Coroutine pendingCoroutine;
    RectTransform cachedRectTransform;
    HorizontalLayoutGroup cachedLayoutGroup;

    void OnEnable()
    {
        apply_responsive_layout();
    }

    void Start()
    {
        apply_responsive_layout();
    }

    void OnRectTransformDimensionsChange()
    {
        apply_responsive_layout();
    }

    /// <summary>1番目のボタンが押された際の処理。</summary>
    public void press_lane_a()
    {
        handle_press(laneAType);
    }

    /// <summary>2番目のボタンが押された際の処理。</summary>
    public void press_lane_b()
    {
        handle_press(laneBType);
    }

    /// <summary>3番目のボタンが押された際の処理。</summary>
    public void press_lane_c()
    {
        handle_press(laneCType);
    }

    /// <summary>現在の車と入力された車種を判定ロジックへ渡す。</summary>
    /// <param name="laneType">押下されたボタンに対応する車種。</param>
    void handle_press(CarType laneType)
    {
        if (!judgeController || !carSpawner)
        {
            return;
        }

        if (!is_playing())
        {
            return;
        }

        if (Time.time - lastInputTime < INPUT_COOLDOWN_SECONDS)
        {
            return;
        }

        pendingLaneType = laneType;
        pendingFrame = Time.frameCount;

        if (pendingCoroutine == null)
        {
            pendingCoroutine = StartCoroutine(process_pending_input());
        }
    }

    /// <summary>同一フレーム内の最後の入力のみを判定に渡す。</summary>
    /// <returns>コルーチン。</returns>
    IEnumerator process_pending_input()
    {
        int frame = pendingFrame;
        yield return new WaitForEndOfFrame();

        if (frame == pendingFrame)
        {
            if (is_playing() && judgeController && carSpawner)
            {
                judgeController.judge(carSpawner.get_active_car(), pendingLaneType);
                lastInputTime = Time.time;
            }
        }

        pendingCoroutine = null;
    }

    /// <summary>ゲームがプレイ中かどうかを確認する。</summary>
    /// <returns>プレイ中なら true。</returns>
    bool is_playing()
    {
        return gameController != null && gameController.is_playing();
    }

    void apply_responsive_layout()
    {
        if (!TryGetComponent(out cachedRectTransform))
        {
            return;
        }

        if (!TryGetComponent(out cachedLayoutGroup))
        {
            return;
        }

        List<RectTransform> buttonRects = get_direct_button_rects();
        if (buttonRects.Count == 0)
        {
            return;
        }

        float parentWidth = cachedRectTransform.rect.width;
        if (parentWidth <= 0f)
        {
            return;
        }

        float horizontalPadding = Mathf.Clamp(parentWidth * HORIZONTAL_PADDING_RATIO, MIN_HORIZONTAL_PADDING, MAX_HORIZONTAL_PADDING);
        float spacing = Mathf.Clamp(parentWidth * SPACING_RATIO, MIN_SPACING, MAX_SPACING);

        float referenceAspect = get_button_aspect(buttonRects[0]);
        float availableWidth = parentWidth - (horizontalPadding * 2f) - (spacing * (buttonRects.Count - 1));
        float preferredWidth = Mathf.Min(parentWidth * BUTTON_WIDTH_RATIO, availableWidth / buttonRects.Count);
        float buttonWidth = Mathf.Clamp(preferredWidth, MIN_BUTTON_WIDTH, MAX_BUTTON_WIDTH);

        // Clamp again after the minimum/maximum pass so the total row width never exceeds the container.
        buttonWidth = Mathf.Min(buttonWidth, availableWidth / buttonRects.Count);
        if (buttonWidth <= 0f)
        {
            return;
        }

        float buttonHeight = buttonWidth / referenceAspect;
        float verticalPadding = Mathf.Clamp(parentWidth * VERTICAL_PADDING_RATIO, MIN_VERTICAL_PADDING, MAX_VERTICAL_PADDING);
        float zoneHeight = buttonHeight + (verticalPadding * 2f);

        apply_layout_group(horizontalPadding, spacing);
        set_height_if_needed(cachedRectTransform, zoneHeight);

        foreach (RectTransform buttonRect in buttonRects)
        {
            set_width_if_needed(buttonRect, buttonWidth);
            set_height_if_needed(buttonRect, buttonHeight);
        }
    }

    List<RectTransform> get_direct_button_rects()
    {
        List<RectTransform> results = new();
        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (!child || !child.TryGetComponent(out Button _))
            {
                continue;
            }

            if (child is RectTransform rectTransform)
            {
                results.Add(rectTransform);
            }
        }

        return results;
    }

    float get_button_aspect(RectTransform buttonRect)
    {
        if (!buttonRect)
        {
            return 1f;
        }

        float height = buttonRect.rect.height;
        if (height <= 0f)
        {
            height = buttonRect.sizeDelta.y;
        }

        if (height <= 0f)
        {
            return 1f;
        }

        float width = buttonRect.rect.width;
        if (width <= 0f)
        {
            width = buttonRect.sizeDelta.x;
        }

        return Mathf.Max(width / height, MIN_BUTTON_ASPECT);
    }

    void apply_layout_group(float horizontalPadding, float spacing)
    {
        int roundedPadding = Mathf.RoundToInt(horizontalPadding);
        if (cachedLayoutGroup.padding.left != roundedPadding || cachedLayoutGroup.padding.right != roundedPadding)
        {
            cachedLayoutGroup.padding.left = roundedPadding;
            cachedLayoutGroup.padding.right = roundedPadding;
        }

        if (Mathf.Abs(cachedLayoutGroup.spacing - spacing) > EPSILON)
        {
            cachedLayoutGroup.spacing = spacing;
        }
    }

    static void set_width_if_needed(RectTransform target, float width)
    {
        if (!target || Mathf.Abs(target.rect.width - width) <= EPSILON)
        {
            return;
        }

        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }

    static void set_height_if_needed(RectTransform target, float height)
    {
        if (!target || Mathf.Abs(target.rect.height - height) <= EPSILON)
        {
            return;
        }

        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }
}
