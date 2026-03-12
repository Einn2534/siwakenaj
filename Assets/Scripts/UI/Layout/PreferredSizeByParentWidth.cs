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

    private LayoutElement _layoutElement;
    private RectTransform _referenceRectTransform;

    private void OnEnable()
    {
        _layoutElement = GetComponent<LayoutElement>();
        _referenceRectTransform = GetReferenceParent();
        Apply();
    }

    private void Start()
    {
        if (_layoutElement == null)
        {
            _layoutElement = GetComponent<LayoutElement>();
        }

        if (_referenceRectTransform == null)
        {
            _referenceRectTransform = GetReferenceParent();
        }

        Apply();
    }

    private void OnRectTransformDimensionsChange()
    {
        Apply();
    }

    private void Apply()
    {
        if (_layoutElement == null)
        {
            _layoutElement = GetComponent<LayoutElement>();
        }

        if (_referenceRectTransform == null)
        {
            _referenceRectTransform = GetReferenceParent();
        }

        if (_referenceRectTransform == null)
        {
            return;
        }

        float resolvedAspect = aspect;
        if (resolvedAspect <= 0f)
        {
            Image image = GetComponentInChildren<Image>();
            if (image != null && image.sprite != null)
            {
                Rect spriteRect = image.sprite.rect;
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

        float parentWidth = _referenceRectTransform.rect.width;
        float parentHeight = _referenceRectTransform.rect.height;
        float widthByParent = parentWidth * widthRatio;
        float widthByHeight = parentHeight * heightRatio * resolvedAspect;
        float width = Mathf.Min(widthByParent, widthByHeight, maxWidth);

        if (widthByParent >= minWidth && widthByHeight >= minWidth)
        {
            width = Mathf.Max(width, minWidth);
        }

        _layoutElement.preferredWidth = width;
        _layoutElement.preferredHeight = width / resolvedAspect;

        UpdateScrollPadding(width);
        LayoutRebuilder.MarkLayoutForRebuild(_referenceRectTransform);
    }

    private void UpdateScrollPadding(float childWidth)
    {
        RectTransform directParent = transform.parent as RectTransform;
        if (directParent == null)
        {
            return;
        }

        HorizontalLayoutGroup layout = directParent.GetComponent<HorizontalLayoutGroup>();
        if (layout == null || _referenceRectTransform == directParent)
        {
            return;
        }

        int sidePadding = Mathf.Max(0, Mathf.RoundToInt((_referenceRectTransform.rect.width - childWidth) * 0.5f));
        if (layout.padding.left == sidePadding && layout.padding.right == sidePadding)
        {
            return;
        }

        layout.padding.left = sidePadding;
        layout.padding.right = sidePadding;
        LayoutRebuilder.MarkLayoutForRebuild(directParent);
    }

    private RectTransform GetReferenceParent()
    {
        RectTransform directParent = transform.parent as RectTransform;
        if (directParent == null)
        {
            return null;
        }

        if (directParent.GetComponent<ContentSizeFitter>() || directParent.GetComponent<HorizontalLayoutGroup>())
        {
            RectTransform viewport = directParent.parent as RectTransform;
            if (viewport != null)
            {
                return viewport;
            }
        }

        return directParent;
    }
}
