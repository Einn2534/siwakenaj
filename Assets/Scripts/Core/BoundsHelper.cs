// Created: 2026-03-05
// Author: Einn

using UnityEngine;

/// <summary>Collider2D または Renderer から Bounds を取得するユーティリティ。</summary>
public static class BoundsHelper
{
    /// <summary>対象オブジェクトの子から Collider2D → Renderer の順で Bounds を取得する。</summary>
    /// <param name="target">対象オブジェクト。</param>
    /// <param name="bounds">取得した Bounds。</param>
    /// <returns>取得に成功した場合 true。</returns>
    public static bool try_get_bounds(GameObject target, out Bounds bounds)
    {
        bounds = new Bounds();
        if (target == null)
        {
            return false;
        }

        Collider2D collider = target.GetComponentInChildren<Collider2D>();
        if (collider != null)
        {
            bounds = collider.bounds;
            return true;
        }

        Renderer renderer = target.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            bounds = renderer.bounds;
            return true;
        }

        return false;
    }
}
