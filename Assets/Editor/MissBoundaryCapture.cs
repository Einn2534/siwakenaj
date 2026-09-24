using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

// Captures the live Main scene used by the boundary PlayMode tests.
public static class MissBoundaryCapture
{
    public static void Capture(string stateName, CarController car, float distanceRatio)
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Boundary evidence requires Play mode.");
        string output = Environment.GetEnvironmentVariable("SIWAKENJA_MISS_BOUNDARY_OUTPUT");
        if (string.IsNullOrWhiteSpace(output)) output = "Deliverables/MissBoundary";
        Directory.CreateDirectory(output);
        Vector2Int[] sizes = { new(1080, 1920), new(1170, 2532), new(1440, 3200) };
        Rect[] safeAreas = { new(0, 0, 1080, 1920), new(0, 102, 1170, 2289), new(0, 120, 1440, 2960) };
        CarSpawner spawner = UnityEngine.Object.FindFirstObjectByType<CarSpawner>();
        MissBoundaryView view = UnityEngine.Object.FindFirstObjectByType<MissBoundaryView>();
        RectTransform fence = (RectTransform)typeof(MissBoundaryView).GetField("_root", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
        Vector3 originalPosition = car != null ? car.transform.position : Vector3.zero;
        try
        {
            for (int i = 0; i < sizes.Length; i++)
            {
                Vector2Int size = sizes[i];
                Rect safe = safeAreas[i];
                SafeAreaFitter.SetEditorSimulation(size, safe);
                string name = $"{stateName}_{size.x}x{size.y}";
                PowaScreenshotCapture.CaptureSceneForRegression(SceneManager.GetActiveScene(), Path.Combine(output, name + ".png"), size.x, size.y, camera =>
                {
                    RefreshSafeAreas();
                    Canvas.ForceUpdateCanvases();
                    if (!spawner.TryGetPlayZoneWorldRect(out Rect zone)) throw new InvalidOperationException("Missing PlayZone.");
                    float boundaryX = MissBoundaryGeometry.GetBoundaryX(zone.xMin, zone.width);
                    if (car != null)
                    {
                        Vector3 position = car.transform.position;
                        position.x += boundaryX + zone.width * distanceRatio - car.GetMinX();
                        position.y = zone.center.y;
                        car.transform.position = position;
                        if (Mathf.Abs(car.MissBoundaryX - boundaryX) > .0001f) throw new InvalidOperationException("Target boundary did not resize.");
                    }
                    view.RefreshLayout();
                    typeof(MissBoundaryView).GetMethod("RefreshAppearance", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
                    if (Mathf.Abs(fence.position.x - boundaryX) > .0001f) throw new InvalidOperationException("Fence and miss boundary disagree.");
                    Vector3[] corners = new Vector3[4];
                    fence.GetWorldCorners(corners);
                    foreach (Vector3 corner in corners)
                    {
                        Vector2 screenPoint = camera.WorldToScreenPoint(corner);
                        if (!safe.Contains(screenPoint)) throw new InvalidOperationException("Fence outside safe area: " + screenPoint);
                    }
                    foreach (UnityEngine.UI.Graphic graphic in fence.GetComponentsInChildren<UnityEngine.UI.Graphic>())
                        if (graphic.raycastTarget) throw new InvalidOperationException("Fence blocks input.");
                });
            }
        }
        finally
        {
            SafeAreaFitter.ClearEditorSimulation();
            if (car != null) car.transform.position = originalPosition;
            RefreshSafeAreas();
            Canvas.ForceUpdateCanvases();
            view.RefreshLayout();
        }
    }

    private static void RefreshSafeAreas()
    {
        foreach (SafeAreaFitter fitter in UnityEngine.Object.FindObjectsByType<SafeAreaFitter>(FindObjectsSortMode.None)) fitter.Refresh();
        foreach (MainSafeAreaLayout layout in UnityEngine.Object.FindObjectsByType<MainSafeAreaLayout>(FindObjectsSortMode.None)) layout.ApplyNow();
    }
}
