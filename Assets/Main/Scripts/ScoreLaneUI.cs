// Created: 2025-11-28
// Updated: drop animation + no rebuild + top-anchored layout
// Author: gpt-5.1-codex-max

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>車種ごとのアイコンを積み上げて表示する(追加時は上から落下)。</summary>
public class ScoreLaneUI : MonoBehaviour
{
    private const int DEFAULT_MAX_ROWS = 10;

    [SerializeField] RectTransform[] lanes;
    [SerializeField] GameObject iconPrefab;

    [SerializeField] int maxRows = DEFAULT_MAX_ROWS;

    // 1~maxRowsのときに使う高さの割合(小さいほど詰まる)
    [SerializeField, Range(0.1f, 1f)] float normalFillRatio = 0.6f;

    // レーン内の余白(上端基準で配置するため)
    [SerializeField] float topPadding = 20f;
    [SerializeField] float bottomPadding = 20f;

    // 落下演出
    [SerializeField] float dropStartOffset = 120f; // レーン上端からさらに上
    [SerializeField] float dropDuration = 0.25f;
    [SerializeField] AnimationCurve dropCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    readonly Dictionary<CarType, List<GameObject>> laneIcons = new();
    readonly Dictionary<CarType, int> laneCounts = new();

    public void reset_all()
    {
        foreach (var icons in laneIcons.Values)
        {
            foreach (var icon in icons)
            {
                if (icon) Destroy(icon);
            }
        }
        laneIcons.Clear();
        laneCounts.Clear();
    }

    public void update_lane(CarType laneType, int count)
    {
        ensure_lane_storage(laneType);

        int newCount = Mathf.Max(0, count);
        int prevCount = laneCounts.TryGetValue(laneType, out int v) ? v : 0;
        laneCounts[laneType] = newCount;

        int laneIndex = (int)laneType;
        if (lanes == null || laneIndex < 0 || laneIndex >= lanes.Length) return;

        RectTransform lane = lanes[laneIndex];
        if (!lane || !iconPrefab) return;

        List<GameObject> icons = laneIcons[laneType];

        // 必要数まで生成(使い回し)
        for (int i = icons.Count; i < newCount; i++)
        {
            icons.Add(Instantiate(iconPrefab, lane));
        }

        // 表示数を反映
        for (int i = 0; i < icons.Count; i++)
        {
            if (icons[i]) icons[i].SetActive(i < newCount);
        }

        if (newCount == 0) return;

        // 目標位置を計算して配置(上端基準)
        List<Vector2> targets = calc_targets(lane, newCount);

        // 追加が1個だけ増えたときだけ落下アニメ
        bool animateDrop = (newCount == prevCount + 1);

        for (int i = 0; i < newCount; i++)
        {
            RectTransform rect = icons[i].GetComponent<RectTransform>();
            if (!rect) continue;

            if (animateDrop && i == newCount - 1)
            {
                // 新規追加分だけ上から落とす
                float topY = lane.rect.height * 0.5f - topPadding;
                Vector2 from = new Vector2(0f, topY + dropStartOffset);
                Vector2 to = targets[i];

                rect.anchoredPosition = from;
                StartCoroutine(drop_to(rect, from, to, dropDuration));
            }
            else
            {
                rect.anchoredPosition = targets[i];
            }
        }
    }

    void ensure_lane_storage(CarType laneType)
    {
        if (!laneIcons.ContainsKey(laneType))
        {
            laneIcons[laneType] = new List<GameObject>();
        }
        if (!laneCounts.ContainsKey(laneType))
        {
            laneCounts[laneType] = 0;
        }
    }

    // 上端基準で配置:
    // - count<=maxRows: 上端付近の一部領域だけ使って詰める
    // - count>maxRows: 上端から下端までを均等圧縮
    List<Vector2> calc_targets(RectTransform lane, int count)
    {
        float height = lane.rect.height;

        float usableHeight = Mathf.Max(0f, height - topPadding - bottomPadding);
        float topY = height * 0.5f - topPadding;

        float step;
        float startY;

        if (count <= 1)
        {
            step = 0f;
            startY = topY;
        }
        else if (count <= maxRows)
        {
            float usedHeight = usableHeight * normalFillRatio;
            step = usedHeight / (maxRows - 1f);

            // 一番上が topY に合うように、必要分だけ下へ広げる
            startY = topY - step * (count - 1);
        }
        else
        {
            step = usableHeight / (count - 1f);

            // 圧縮時も一番上は topY に固定
            startY = topY - step * (count - 1);
        }

        var list = new List<Vector2>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(new Vector2(0f, startY + step * i));
        }
        return list;
    }

    IEnumerator drop_to(RectTransform rect, Vector2 from, Vector2 to, float duration)
    {
        float t = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = dropCurve != null ? dropCurve.Evaluate(p) : p;

            rect.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            yield return null;
        }

        rect.anchoredPosition = to;
    }
}
