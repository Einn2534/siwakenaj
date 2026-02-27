// Created: 2025-02-14
// Updated: 2026-02-26
// Author: Einn

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>レーンUIにアイコンを積み上げて表示する。</summary>
public class ScoreLaneUI : MonoBehaviour
{
    private const int DEFAULT_MAX_ROWS = 10;

    [Header("Lane / Prefab")]
    [SerializeField] private RectTransform[] lanes;
    [SerializeField] private GameObject iconPrefab;

    [Header("Layout")]
    [SerializeField, Min(1)] private int maxRows = DEFAULT_MAX_ROWS;

    [Tooltip("1~maxRowsのときに使う縦の詰め具合(1=最大まで使用 / 小さいほど詰める)")]
    [SerializeField, Range(0.1f, 1f)] private float normalFillRatio = 0.6f;

    [SerializeField] private float topPadding = 20f;
    [SerializeField] private float bottomPadding = 20f;
    [SerializeField] private float leftPadding = 20f;
    [SerializeField] private float rightPadding = 20f;

    [SerializeField] private float columnSpacing = 20f;

    [Header("Drop Animation")]
    [SerializeField] private float dropStartOffset = 120f;
    [SerializeField] private float dropDuration = 0.25f;
    [SerializeField] private AnimationCurve dropCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private readonly Dictionary<CarType, List<GameObject>> laneIcons = new();
    private readonly Dictionary<CarType, int> laneCounts = new();
    private readonly Dictionary<RectTransform, Coroutine> dropRoutines = new();

    private readonly List<Vector2> tmpTargets = new(64);

    private bool metricsValid;
    private float iconW = 64f;
    private float iconH = 64f;
    private Vector2 iconPivot = new(0.5f, 0.5f);

    /// <summary>初期化時にプレハブのメトリクスをキャッシュする。</summary>
    private void Awake()
    {
        cache_prefab_metrics();
    }

    /// <summary>有効化時に次フレームでレイアウトを更新する。</summary>
    private void OnEnable()
    {
        StartCoroutine(refresh_next_frame());
    }

#if UNITY_EDITOR
    /// <summary>インスペクタ変更時にメトリクスを再計算する。</summary>
    private void OnValidate()
    {
        if (maxRows < 1) maxRows = 1;
        metricsValid = false;
        cache_prefab_metrics();
    }
#endif

    /// <summary>全レーンの表示を再配置する。</summary>
    public void refresh_all()
    {
        if (laneCounts.Count == 0) return;

        var keys = new List<CarType>(laneCounts.Keys);
        foreach (var t in keys)
        {
            int count = laneCounts.TryGetValue(t, out var c) ? c : 0;
            apply_lane_layout(t, count, animateLast: false);
        }
    }

    /// <summary>全レーンをリセットする。</summary>
    public void reset_all()
    {
        stop_all_drops();

        foreach (var list in laneIcons.Values)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i]) Destroy(list[i]);
            }
        }

        laneIcons.Clear();
        laneCounts.Clear();
    }

    /// <summary>指定レーンのアイコン数を更新する。</summary>
    /// <param name="laneType">更新対象の車種。</param>
    /// <param name="count">新しいアイコン数。</param>
    public void update_lane(CarType laneType, int count)
    {
        ensure_lane_storage(laneType);

        int newCount = Mathf.Max(0, count);
        int prevCount = laneCounts.TryGetValue(laneType, out var v) ? v : 0;
        laneCounts[laneType] = newCount;

        bool animateDrop = (newCount == prevCount + 1);
        apply_lane_layout(laneType, newCount, animateDrop);
    }

    /// <summary>レーンのレイアウトを適用する。</summary>
    /// <param name="laneType">対象の車種。</param>
    /// <param name="count">アイコン数。</param>
    /// <param name="animateLast">最後のアイコンに落下アニメーションを適用するか。</param>
    private void apply_lane_layout(CarType laneType, int count, bool animateLast)
    {
        if (!try_get_lane_rect(laneType, out var lane) || lane == null || iconPrefab == null)
            return;

        cache_prefab_metrics();

        if (!laneIcons.TryGetValue(laneType, out var icons))
        {
            icons = new List<GameObject>();
            laneIcons[laneType] = icons;
        }

        if (count <= 0)
        {
            for (int i = 0; i < icons.Count; i++)
                if (icons[i]) icons[i].SetActive(false);
            return;
        }

        ensure_icon_instances(icons, lane, count);
        set_active_icons(icons, count);

        tmpTargets.Clear();
        fill_targets(lane, count, tmpTargets);

        for (int i = 0; i < count; i++)
        {
            var go = icons[i];
            if (!go) continue;

            var rt = go.transform as RectTransform;
            if (!rt) continue;

            Vector2 to = tmpTargets[i];

            if (!(animateLast && i == count - 1))
            {
                stop_drop(rt);
                rt.anchoredPosition = to;
            }
            else
            {
                Vector2 from = to + Vector2.up * dropStartOffset;
                rt.anchoredPosition = from;
                start_drop(rt, from, to, dropDuration);
            }
        }
    }

    /// <summary>車種に対応するレーンRectTransformを取得する。</summary>
    /// <param name="laneType">対象の車種。</param>
    /// <param name="lane">取得したレーン。</param>
    /// <returns>取得できた場合 true。</returns>
    private bool try_get_lane_rect(CarType laneType, out RectTransform lane)
    {
        lane = null;
        int idx = (int)laneType;
        if (lanes == null || idx < 0 || idx >= lanes.Length) return false;
        lane = lanes[idx];
        return lane != null;
    }

    /// <summary>レーン用の内部ストレージを確保する。</summary>
    /// <param name="laneType">対象の車種。</param>
    private void ensure_lane_storage(CarType laneType)
    {
        if (!laneIcons.ContainsKey(laneType))
            laneIcons[laneType] = new List<GameObject>();
        if (!laneCounts.ContainsKey(laneType))
            laneCounts[laneType] = 0;
    }

    /// <summary>必要数のアイコンインスタンスを生成する。</summary>
    /// <param name="icons">既存のアイコンリスト。</param>
    /// <param name="parent">親RectTransform。</param>
    /// <param name="needCount">必要なアイコン数。</param>
    private void ensure_icon_instances(List<GameObject> icons, RectTransform parent, int needCount)
    {
        for (int i = icons.Count; i < needCount; i++)
            icons.Add(Instantiate(iconPrefab, parent));
    }

    /// <summary>アクティブなアイコン数を設定する。</summary>
    /// <param name="icons">アイコンリスト。</param>
    /// <param name="activeCount">有効にするアイコン数。</param>
    private void set_active_icons(List<GameObject> icons, int activeCount)
    {
        for (int i = 0; i < icons.Count; i++)
            if (icons[i]) icons[i].SetActive(i < activeCount);
    }

    /// <summary>プレハブのサイズとピボットをキャッシュする。</summary>
    private void cache_prefab_metrics()
    {
        if (metricsValid) return;

        iconW = 64f;
        iconH = 64f;
        iconPivot = new Vector2(0.5f, 0.5f);

        if (iconPrefab != null)
        {
            var pr = iconPrefab.GetComponent<RectTransform>();
            if (pr != null)
            {
                iconPivot = pr.pivot;

                float w = pr.rect.width;
                float h = pr.rect.height;

                iconW = (w > 0f) ? w : Mathf.Max(1f, pr.sizeDelta.x);
                iconH = (h > 0f) ? h : Mathf.Max(1f, pr.sizeDelta.y);
            }
        }

        metricsValid = true;
    }

    /// <summary>レーン内のアイコン配置位置を算出する。</summary>
    /// <param name="lane">対象レーン。</param>
    /// <param name="count">アイコン数。</param>
    /// <param name="output">出力先リスト。</param>
    private void fill_targets(RectTransform lane, int count, List<Vector2> output)
    {
        Rect r = lane.rect;

        float innerXMin = r.xMin + leftPadding;
        float innerXMax = r.xMax - rightPadding;
        float innerYMin = r.yMin + bottomPadding;
        float innerYMax = r.yMax - topPadding;

        float minX = innerXMin + iconW * iconPivot.x;
        float maxX = innerXMax - iconW * (1f - iconPivot.x);
        float minY = innerYMin + iconH * iconPivot.y;
        float maxY = innerYMax - iconH * (1f - iconPivot.y);

        if (maxX < minX) maxX = minX;
        if (maxY < minY) maxY = minY;

        float innerHeight = Mathf.Max(0f, maxY - minY);

        int rowsPerCol = Mathf.Max(1, maxRows);
        int colCount = Mathf.CeilToInt(count / (float)rowsPerCol);

        float vStep = 0f;
        if (rowsPerCol >= 2)
        {
            float fitStep = innerHeight / (rowsPerCol - 1f);
            vStep = fitStep * Mathf.Clamp01(normalFillRatio);

            float minStep = iconH * 0.5f;
            vStep = Mathf.Max(vStep, minStep);

            if (vStep > fitStep) vStep = fitStep;
        }

        int usedRows = Mathf.Min(rowsPerCol, count);

        float startY = minY;
        if (usedRows >= 2)
        {
            float topMostY = startY + (usedRows - 1) * vStep;
            if (topMostY > maxY)
                startY -= (topMostY - maxY);
        }
        startY = Mathf.Clamp(startY, minY, maxY);

        float desiredColStep = iconW + columnSpacing;
        float colStep = desiredColStep;

        float centerX = (minX + maxX) * 0.5f;

        if (colCount <= 1)
        {
            colStep = 0f;
        }
        else
        {
            float avail = Mathf.Max(0f, maxX - minX);
            float fitStep = (avail > 0f) ? (avail / (colCount - 1f)) : 0f;
            colStep = Mathf.Min(desiredColStep, fitStep);
        }

        output.Capacity = Mathf.Max(output.Capacity, count);

        for (int i = 0; i < count; i++)
        {
            int col = i / rowsPerCol;
            int row = i % rowsPerCol;

            float span = (colCount - 1) * colStep;
            float firstX = centerX - span * 0.5f;
            float x = firstX + col * colStep;

            float y = startY + row * vStep;

            x = Mathf.Clamp(x, minX, maxX);
            y = Mathf.Clamp(y, minY, maxY);

            output.Add(new Vector2(x, y));
        }
    }

    /// <summary>次フレームで全レーンをリフレッシュする。</summary>
    /// <returns>コルーチン。</returns>
    private IEnumerator refresh_next_frame()
    {
        yield return null;
        refresh_all();
    }

    /// <summary>全ての落下アニメーションを停止する。</summary>
    private void stop_all_drops()
    {
        foreach (var kv in dropRoutines)
        {
            if (kv.Value != null) StopCoroutine(kv.Value);
        }
        dropRoutines.Clear();
    }

    /// <summary>指定RectTransformの落下アニメーションを停止する。</summary>
    /// <param name="rect">対象のRectTransform。</param>
    private void stop_drop(RectTransform rect)
    {
        if (!rect) return;

        if (dropRoutines.TryGetValue(rect, out var c) && c != null)
            StopCoroutine(c);

        dropRoutines.Remove(rect);
    }

    /// <summary>落下アニメーションを開始する。</summary>
    /// <param name="rect">対象のRectTransform。</param>
    /// <param name="from">開始位置。</param>
    /// <param name="to">着地位置。</param>
    /// <param name="duration">アニメーション時間。</param>
    private void start_drop(RectTransform rect, Vector2 from, Vector2 to, float duration)
    {
        if (!rect) return;

        if (dropRoutines.TryGetValue(rect, out var c) && c != null)
            StopCoroutine(c);

        dropRoutines[rect] = StartCoroutine(drop_to(rect, from, to, duration));
    }

    /// <summary>落下アニメーションのコルーチン。</summary>
    /// <param name="rect">対象のRectTransform。</param>
    /// <param name="from">開始位置。</param>
    /// <param name="to">着地位置。</param>
    /// <param name="duration">アニメーション時間。</param>
    /// <returns>コルーチン。</returns>
    private IEnumerator drop_to(RectTransform rect, Vector2 from, Vector2 to, float duration)
    {
        float t = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = (dropCurve != null) ? dropCurve.Evaluate(p) : p;

            if (rect) rect.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            yield return null;
        }

        if (rect) rect.anchoredPosition = to;
        dropRoutines.Remove(rect);
    }
}
