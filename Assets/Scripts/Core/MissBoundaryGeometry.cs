using UnityEngine;

public static class MissBoundaryGeometry
{
    public const float InsetRatio = 0.06f;
    public const float WarningDistanceRatio = 0.15f;
    public const float MissPointRatio = 0.6f;

    public static float GetBoundaryX(float leftEdgeX, float playZoneWidth)
    {
        return leftEdgeX + playZoneWidth * InsetRatio;
    }

    public static float GetMissPointX(float targetMinX, float targetWidth)
    {
        return targetMinX + targetWidth * MissPointRatio;
    }

    public static float GetProximity(float targetX, float boundaryX, float playZoneWidth)
    {
        if (playZoneWidth <= 0f) return 0f;
        return Mathf.Clamp01(1f - (targetX - boundaryX) / (playZoneWidth * WarningDistanceRatio));
    }
}
