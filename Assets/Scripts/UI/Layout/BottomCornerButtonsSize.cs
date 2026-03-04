// Created: 2026-02-27
// Author: Einn

using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class BottomCornerButtonsSizer : MonoBehaviour
{
    [Header("Targets (LayoutElement)")]
    public LayoutElement leftButton;
    public LayoutElement rightButton;
    public LayoutElement leftIcon;
    public LayoutElement rightIcon;

    [Header("Size ratios (by parent width)")]
    [Range(0.10f, 0.30f)] public float buttonRatio = 0.20f; // 当たり判定の一辺
    [Range(0.06f, 0.20f)] public float iconRatio   = 0.12f; // アイコンの一辺

    [Header("Clamps (reference px)")]
    public float buttonMin = 180f;
    public float buttonMax = 260f;
    public float iconMin   = 96f;
    public float iconMax   = 150f;

    void OnEnable() => Apply();
    void OnRectTransformDimensionsChange() => Apply();

    void Apply()
    {
        var rt = transform as RectTransform;
        if (!rt) return;

        float w = rt.rect.width;

        float buttonSize = Mathf.Clamp(w * buttonRatio, buttonMin, buttonMax);
        float iconSize   = Mathf.Clamp(w * iconRatio,   iconMin,   iconMax);

        SetSquare(leftButton, buttonSize);
        SetSquare(rightButton, buttonSize);
        SetSquare(leftIcon, iconSize);
        SetSquare(rightIcon, iconSize);

        LayoutRebuilder.MarkLayoutForRebuild(rt);
    }

    static void SetSquare(LayoutElement le, float size)
    {
        if (!le) return;
        le.preferredWidth  = size;
        le.preferredHeight = size;
        le.minWidth  = size;   // 触れる最低保証
        le.minHeight = size;
    }
}