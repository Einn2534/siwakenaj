using UnityEngine;

public static class BoundsHelper
{
    public static bool TryGetBounds(GameObject target, out Bounds bounds)
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
