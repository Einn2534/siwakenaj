// Created: 2025-11-28
// Author: gpt-5.1-codex-max

using System.Collections.Generic;
using UnityEngine;

/// <summary>Displays stacked icons for each lane with compression.</summary>
public class ScoreLaneUI : MonoBehaviour
{
    private const int DEFAULT_MAX_ROWS = 10;

    [SerializeField]
    RectTransform[] lanes;

    [SerializeField]
    GameObject iconPrefab;

    [SerializeField]
    int maxRows = DEFAULT_MAX_ROWS;

    readonly Dictionary<CarType, List<GameObject>> laneIcons = new();

    /// <summary>Clears all lane icons.</summary>
    public void reset_all()
    {
        foreach (List<GameObject> icons in laneIcons.Values)
        {
            foreach (GameObject icon in icons)
            {
                Destroy(icon);
            }
        }

        laneIcons.Clear();
    }

    /// <summary>Updates the displayed icons for a specific lane.</summary>
    /// <param name="laneType">Target lane identifier.</param>
    /// <param name="count">Number of icons to display.</param>
    public void update_lane(CarType laneType, int count)
    {
        ensure_lane_storage(laneType);
        rebuild_lane(laneType, count);
    }

    /// <summary>Ensures dictionary storage exists for a lane.</summary>
    /// <param name="laneType">Lane identifier to prepare.</param>
    void ensure_lane_storage(CarType laneType)
    {
        if (!laneIcons.ContainsKey(laneType))
        {
            laneIcons[laneType] = new List<GameObject>();
        }
    }

    /// <summary>Rebuilds the icon stack with compressed spacing.</summary>
    /// <param name="laneType">Lane identifier to draw.</param>
    /// <param name="count">Desired icon count.</param>
    void rebuild_lane(CarType laneType, int count)
    {
        List<GameObject> icons = laneIcons[laneType];
        foreach (GameObject icon in icons)
        {
            Destroy(icon);
        }

        icons.Clear();

        int laneIndex = (int)laneType;

        if (lanes == null || laneIndex < 0 || laneIndex >= lanes.Length)
        {
            return;
        }

        RectTransform lane = lanes[laneIndex];
        if (!lane || !iconPrefab)
        {
            return;
        }

        float clampedCount = Mathf.Max(count, 0);
        float rowCount = Mathf.Max(1f, Mathf.Min(clampedCount, maxRows));
        float height = lane.rect.height;
        float step = rowCount <= 1f ? 0f : height / (rowCount - 1f);
        float startY = -height * 0.5f;

        for (int i = 0; i < clampedCount; i++)
        {
            GameObject icon = Instantiate(iconPrefab, lane);
            RectTransform rect = icon.GetComponent<RectTransform>();
            float ratio = rowCount <= 1f ? 0f : Mathf.Min(i, rowCount - 1f);
            rect.anchoredPosition = new Vector2(0f, startY + step * ratio);
            icons.Add(icon);
        }
    }
}
