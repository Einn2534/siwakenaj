// Created: 2026-02-26
// Author: Einn

using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(LayoutElement))]
public class PreferredSizeByParentWidth : MonoBehaviour
{
    [Range(0.1f, 1.0f)] public float widthRatio = 0.88f;
    [Range(0.1f, 1.0f)] public float heightRatio = 0.72f;
    public float minWidth = 400f;
    public float maxWidth = 1000f;

    public float aspect = 0f;

    LayoutElement le;
    RectTransform referenceRt;

    void OnEnable()
    {
        le = GetComponent<LayoutElement>();
        referenceRt = get_reference_parent();
        Apply();
    }

    void Start()
    {
        if (!le) le = GetComponent<LayoutElement>();
        if (!referenceRt) referenceRt = get_reference_parent();
        Apply();
    }

    void OnRectTransformDimensionsChange() => Apply();

    void Apply()
    {
        if (!le) le = GetComponent<LayoutElement>();
        if (!referenceRt) referenceRt = get_reference_parent();
        if (!referenceRt) return;

        float resolvedAspect = aspect;
        if (resolvedAspect <= 0f)
        {
            Image img = GetComponentInChildren<Image>();
            if (img && img.sprite)
            {
                Rect spriteRect = img.sprite.rect;
                if (spriteRect.height > 0f)
                {
                    resolvedAspect = spriteRect.width / spriteRect.height;
                }
            }
        }

        if (resolvedAspect <= 0f)
        {
            resolvedAspect = 1f;
        }

        float parentW = referenceRt.rect.width;
        float parentH = referenceRt.rect.height;
        float widthByParent = parentW * widthRatio;
        float widthByHeight = parentH * heightRatio * resolvedAspect;
        float width = Mathf.Min(widthByParent, widthByHeight, maxWidth);

        if (widthByParent >= minWidth && widthByHeight >= minWidth)
        {
            width = Mathf.Max(width, minWidth);
        }

        le.preferredWidth = width;
        le.preferredHeight = width / resolvedAspect;

        update_scroll_padding(width);

        LayoutRebuilder.MarkLayoutForRebuild(referenceRt);
    }

    void update_scroll_padding(float childWidth)
    {
        RectTransform directParent = transform.parent as RectTransform;
        if (!directParent)
        {
            return;
        }

        HorizontalLayoutGroup layout = directParent.GetComponent<HorizontalLayoutGroup>();
        if (!layout || referenceRt == directParent)
        {
            return;
        }

        int sidePadding = Mathf.Max(0, Mathf.RoundToInt((referenceRt.rect.width - childWidth) * 0.5f));
        if (layout.padding.left == sidePadding && layout.padding.right == sidePadding)
        {
            return;
        }

        layout.padding.left = sidePadding;
        layout.padding.right = sidePadding;
        LayoutRebuilder.MarkLayoutForRebuild(directParent);
    }

    RectTransform get_reference_parent()
    {
        RectTransform directParent = transform.parent as RectTransform;
        if (!directParent)
        {
            return null;
        }

        if (directParent.GetComponent<ContentSizeFitter>() || directParent.GetComponent<HorizontalLayoutGroup>())
        {
            RectTransform viewport = directParent.parent as RectTransform;
            if (viewport)
            {
                return viewport;
            }
        }

        return directParent;
    }
}
