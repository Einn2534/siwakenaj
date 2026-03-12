using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LaneButtonLayoutController : MonoBehaviour
{
    private const float ButtonWidthRatio = 0.24f;
    private const float MinButtonWidth = 120f;
    private const float MaxButtonWidth = 300f;
    private const float HorizontalPaddingRatio = 0.03f;
    private const float MinHorizontalPadding = 12f;
    private const float MaxHorizontalPadding = 36f;
    private const float SpacingRatio = 0.02f;
    private const float MinSpacing = 8f;
    private const float MaxSpacing = 24f;
    private const float VerticalPaddingRatio = 0.06f;
    private const float MinVerticalPadding = 12f;
    private const float MaxVerticalPadding = 32f;
    private const float MinButtonAspect = 0.1f;
    private const float Epsilon = 0.5f;

    private RectTransform _cachedRectTransform;
    private HorizontalLayoutGroup _cachedLayoutGroup;

    private void OnEnable()
    {
        ApplyResponsiveLayout();
    }

    private void Start()
    {
        ApplyResponsiveLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyResponsiveLayout();
    }

    public void ApplyResponsiveLayout()
    {
        if (!TryGetComponent(out _cachedRectTransform) || !TryGetComponent(out _cachedLayoutGroup))
        {
            return;
        }

        List<RectTransform> buttonRects = GetDirectButtonRects();
        if (buttonRects.Count == 0)
        {
            return;
        }

        float parentWidth = _cachedRectTransform.rect.width;
        if (parentWidth <= 0f)
        {
            return;
        }

        float horizontalPadding = Mathf.Clamp(parentWidth * HorizontalPaddingRatio, MinHorizontalPadding, MaxHorizontalPadding);
        float spacing = Mathf.Clamp(parentWidth * SpacingRatio, MinSpacing, MaxSpacing);
        float referenceAspect = GetButtonAspect(buttonRects[0]);
        float availableWidth = parentWidth - (horizontalPadding * 2f) - (spacing * (buttonRects.Count - 1));
        float preferredWidth = Mathf.Min(parentWidth * ButtonWidthRatio, availableWidth / buttonRects.Count);
        float buttonWidth = Mathf.Clamp(preferredWidth, MinButtonWidth, MaxButtonWidth);
        buttonWidth = Mathf.Min(buttonWidth, availableWidth / buttonRects.Count);

        if (buttonWidth <= 0f)
        {
            return;
        }

        float buttonHeight = buttonWidth / referenceAspect;
        float verticalPadding = Mathf.Clamp(parentWidth * VerticalPaddingRatio, MinVerticalPadding, MaxVerticalPadding);
        float zoneHeight = buttonHeight + (verticalPadding * 2f);

        ApplyLayoutGroup(horizontalPadding, spacing);
        SetHeightIfNeeded(_cachedRectTransform, zoneHeight);

        foreach (RectTransform buttonRect in buttonRects)
        {
            SetWidthIfNeeded(buttonRect, buttonWidth);
            SetHeightIfNeeded(buttonRect, buttonHeight);
        }
    }

    private List<RectTransform> GetDirectButtonRects()
    {
        List<RectTransform> results = new();
        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i += 1)
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

    private float GetButtonAspect(RectTransform buttonRect)
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

        return Mathf.Max(width / height, MinButtonAspect);
    }

    private void ApplyLayoutGroup(float horizontalPadding, float spacing)
    {
        int roundedPadding = Mathf.RoundToInt(horizontalPadding);
        if (_cachedLayoutGroup.padding.left != roundedPadding || _cachedLayoutGroup.padding.right != roundedPadding)
        {
            _cachedLayoutGroup.padding.left = roundedPadding;
            _cachedLayoutGroup.padding.right = roundedPadding;
        }

        if (Mathf.Abs(_cachedLayoutGroup.spacing - spacing) > Epsilon)
        {
            _cachedLayoutGroup.spacing = spacing;
        }
    }

    private static void SetWidthIfNeeded(RectTransform target, float width)
    {
        if (!target || Mathf.Abs(target.rect.width - width) <= Epsilon)
        {
            return;
        }

        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }

    private static void SetHeightIfNeeded(RectTransform target, float height)
    {
        if (!target || Mathf.Abs(target.rect.height - height) <= Epsilon)
        {
            return;
        }

        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }
}
