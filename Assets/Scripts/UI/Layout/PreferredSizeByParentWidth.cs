// Created: 2026-02-26
// Author: Einn

using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(LayoutElement))]
public class PreferredSizeByParentWidth : MonoBehaviour
{
    [Range(0.1f, 1.0f)] public float widthRatio = 0.88f; // 親幅の何割
    public float minWidth = 400f;
    public float maxWidth = 1000f;

    // ロゴのアスペクト(幅/高さ)。0なら子ImageのSpriteから自動取得
    public float aspect = 0f;

    LayoutElement le;
    RectTransform parentRt;

    void OnEnable()
    {
        le = GetComponent<LayoutElement>();
        parentRt = transform.parent as RectTransform;
        Apply();
    }

    void OnRectTransformDimensionsChange() => Apply();

    void Apply()
    {
        if (!le) le = GetComponent<LayoutElement>();
        if (!parentRt) parentRt = transform.parent as RectTransform;
        if (!parentRt) return;

        float parentW = parentRt.rect.width;
        float w = Mathf.Clamp(parentW * widthRatio, minWidth, maxWidth);

        float a = aspect;
        if (a <= 0f)
        {
            var img = GetComponentInChildren<Image>();
            if (img && img.sprite)
            {
                var r = img.sprite.rect;
                if (r.height > 0) a = r.width / r.height;
            }
        }
        if (a <= 0f) a = 1f;

        le.preferredWidth  = w;
        le.preferredHeight = w / a;

        // レイアウト更新を促進（Editor/実機で反映が遅い時に効く）
        LayoutRebuilder.MarkLayoutForRebuild(parentRt);
    }
}