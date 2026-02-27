// Created: 2026-02-27
// Author: Codex

using UnityEngine;

/// <summary>RectTransform を Screen.safeArea に合わせて調整する。</summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform targetRect;
    private Rect lastSafeArea = Rect.zero;
    private Vector2Int lastScreenSize = Vector2Int.zero;

    /// <summary>コンポーネント参照を初期化しセーフエリアを適用する。</summary>
    private void Awake()
    {
        targetRect = GetComponent<RectTransform>();
        apply_safe_area();
    }

    /// <summary>画面サイズや safeArea 変化を監視し必要時のみ再適用する。</summary>
    private void Update()
    {
        if (is_safe_area_changed())
        {
            apply_safe_area();
        }
    }

    /// <summary>RectTransform のアンカーを現在の Screen.safeArea に合わせる。</summary>
    private void apply_safe_area()
    {
        Rect safeArea = Screen.safeArea;
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        targetRect.anchorMin = anchorMin;
        targetRect.anchorMax = anchorMax;
        targetRect.offsetMin = Vector2.zero;
        targetRect.offsetMax = Vector2.zero;

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }

    /// <summary>safeArea または画面サイズが前回値から変化したかを判定する。</summary>
    /// <returns>変化していれば true、していなければ false。</returns>
    private bool is_safe_area_changed()
    {
        if (lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
        {
            return true;
        }

        return lastSafeArea != Screen.safeArea;
    }
}
