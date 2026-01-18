// ScoreLaneUI.cs（simple / drop-last）
// 目的: 下から積む。maxRowsで溢れたら右に列追加。最後の1個だけ落下。

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    // Prefab metrics
    private bool metricsValid;
    private float iconW = 64f;
    private float iconH = 64f;
    private Vector2 iconPivot = new(0.5f, 0.5f);

    private void Awake()
    {
        CachePrefabMetrics();
    }

    private void OnEnable()
    {
        StartCoroutine(RefreshNextFrame());
    }

    private IEnumerator RefreshNextFrame()
    {
        yield return null;
        RefreshAll();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxRows < 1) maxRows = 1;
        metricsValid = false;
        CachePrefabMetrics();
    }
#endif

    public void RefreshAll()
    {
        if (laneCounts.Count == 0) return;

        var keys = new List<CarType>(laneCounts.Keys);
        foreach (var t in keys)
        {
            int count = laneCounts.TryGetValue(t, out var c) ? c : 0;
            ApplyLaneLayout(t, count, animateLast: false);
        }
    }

    public void ResetAll()
    {
        StopAllDrops();

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

    public void UpdateLane(CarType laneType, int count)
    {
        EnsureLaneStorage(laneType);

        int newCount = Mathf.Max(0, count);
        int prevCount = laneCounts.TryGetValue(laneType, out var v) ? v : 0;
        laneCounts[laneType] = newCount;

        bool animateDrop = (newCount == prevCount + 1);
        ApplyLaneLayout(laneType, newCount, animateDrop);
    }

    private void ApplyLaneLayout(CarType laneType, int count, bool animateLast)
    {
        if (!TryGetLaneRect(laneType, out var lane) || lane == null || iconPrefab == null)
            return;

        CachePrefabMetrics();

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

        EnsureIconInstances(icons, lane, count);
        SetActiveIcons(icons, count);

        tmpTargets.Clear();
        FillTargets(lane, count, tmpTargets);

        for (int i = 0; i < count; i++)
        {
            var go = icons[i];
            if (!go) continue;

            var rt = go.transform as RectTransform;
            if (!rt) continue;

            Vector2 to = tmpTargets[i];

            if (!(animateLast && i == count - 1))
            {
                StopDrop(rt);
                rt.anchoredPosition = to;
            }
            else
            {
                Vector2 from = to + Vector2.up * dropStartOffset;
                rt.anchoredPosition = from;
                StartDrop(rt, from, to, dropDuration);
            }
        }
    }

    private bool TryGetLaneRect(CarType laneType, out RectTransform lane)
    {
        lane = null;
        int idx = (int)laneType;
        if (lanes == null || idx < 0 || idx >= lanes.Length) return false;
        lane = lanes[idx];
        return lane != null;
    }

    private void EnsureLaneStorage(CarType laneType)
    {
        if (!laneIcons.ContainsKey(laneType))
            laneIcons[laneType] = new List<GameObject>();
        if (!laneCounts.ContainsKey(laneType))
            laneCounts[laneType] = 0;
    }

    private void EnsureIconInstances(List<GameObject> icons, RectTransform parent, int needCount)
    {
        for (int i = icons.Count; i < needCount; i++)
            icons.Add(Instantiate(iconPrefab, parent));
    }

    private void SetActiveIcons(List<GameObject> icons, int activeCount)
    {
        for (int i = 0; i < icons.Count; i++)
            if (icons[i]) icons[i].SetActive(i < activeCount);
    }

    private void CachePrefabMetrics()
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

    // -------------------------
    // Layout (simple)
    // -------------------------

    private void FillTargets(RectTransform lane, int count, List<Vector2> output)
    {
        // lane.rect は「laneローカル座標」での矩形（pivotを含んだ xMin/xMax を返す）
        Rect r = lane.rect;

        float innerXMin = r.xMin + leftPadding;
        float innerXMax = r.xMax - rightPadding;
        float innerYMin = r.yMin + bottomPadding;
        float innerYMax = r.yMax - topPadding;

        // アイコンのpivot点が置ける範囲（はみ出し防止）
        float minX = innerXMin + iconW * iconPivot.x;
        float maxX = innerXMax - iconW * (1f - iconPivot.x);
        float minY = innerYMin + iconH * iconPivot.y;
        float maxY = innerYMax - iconH * (1f - iconPivot.y);

        if (maxX < minX) maxX = minX;
        if (maxY < minY) maxY = minY;

        float innerWidth = Mathf.Max(0f, maxX - minX);
        float innerHeight = Mathf.Max(0f, maxY - minY);

        // 行数（基本 maxRows 固定）
        int rowsPerCol = Mathf.Max(1, maxRows);
        int colCount = Mathf.CeilToInt(count / (float)rowsPerCol);

        // 縦ステップ: まず「入る最大」を基準にしてから fillRatio で詰める
        float vStep = 0f;
        if (rowsPerCol >= 2)
        {
            float fitStep = innerHeight / (rowsPerCol - 1f);
            vStep = fitStep * Mathf.Clamp01(normalFillRatio);

            // ただし詰めすぎると見た目が潰れるので最低限は確保（最終的に入らないならfit優先）
            float minStep = iconH * 0.5f;
            vStep = Mathf.Max(vStep, minStep);

            // 入らない場合は fitStep に戻す（=必ず枠内に収める）
            if (vStep > fitStep) vStep = fitStep;
        }

        // 使う行数（最後の列は満たないことがあるが、縦位置計算自体は rowsPerCol でOK）
        // ただし count が少ないときは上に行きすぎないように調整
        int usedRows = Mathf.Min(rowsPerCol, count);

        // 下から積む開始Y（topを超えるなら下げる）
        float startY = minY;
        if (usedRows >= 2)
        {
            float topMostY = startY + (usedRows - 1) * vStep;
            if (topMostY > maxY)
                startY -= (topMostY - maxY);
        }
        startY = Mathf.Clamp(startY, minY, maxY);

        // 横ステップ
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

            float x;
            float span = (colCount - 1) * colStep;
            float firstX = centerX - span * 0.5f;
            x = firstX + col * colStep;

            float y = startY + row * vStep;

            // 安全クランプ（極端に小さいレーン対策）
            x = Mathf.Clamp(x, minX, maxX);
            y = Mathf.Clamp(y, minY, maxY);

            output.Add(new Vector2(x, y));
        }
    }

    // -------------------------
    // Drop animation
    // -------------------------

    private void StopAllDrops()
    {
        foreach (var kv in dropRoutines)
        {
            if (kv.Value != null) StopCoroutine(kv.Value);
        }
        dropRoutines.Clear();
    }

    private void StopDrop(RectTransform rect)
    {
        if (!rect) return;

        if (dropRoutines.TryGetValue(rect, out var c) && c != null)
            StopCoroutine(c);

        dropRoutines.Remove(rect);
    }

    private void StartDrop(RectTransform rect, Vector2 from, Vector2 to, float duration)
    {
        if (!rect) return;

        if (dropRoutines.TryGetValue(rect, out var c) && c != null)
            StopCoroutine(c);

        dropRoutines[rect] = StartCoroutine(DropTo(rect, from, to, duration));
    }

    private IEnumerator DropTo(RectTransform rect, Vector2 from, Vector2 to, float duration)
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
