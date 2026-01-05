// ScoreLaneUI.cs（差し替え用）
// そのまま丸ごとコピペして使えます。
// 既存の ScoreManager が reset_all / update_lane を呼んでいても動くよう、互換メソッドも入れてあります。

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>車種ごとのアイコンを積み上げて表示する(追加時は上から落下)。溢れたら2列目(以降も列追加)。</summary>
public class ScoreLaneUI : MonoBehaviour
{
    private const int DEFAULT_MAX_ROWS = 10;

    [Header("Lane / Prefab")]
    [SerializeField] private RectTransform[] lanes;
    [SerializeField] private GameObject iconPrefab;

    [Header("Layout")]
    [SerializeField, Min(1)] private int maxRows = DEFAULT_MAX_ROWS;

    // 1~maxRowsのときに使う高さの割合(小さいほど詰まる)
    [SerializeField, Range(0.1f, 1f)] private float normalFillRatio = 0.6f;

    // レーン内の余白
    [SerializeField] private float topPadding = 20f;
    [SerializeField] private float bottomPadding = 20f;
    [SerializeField] private float leftPadding = 20f;
    [SerializeField] private float rightPadding = 20f;

    // 列間隔(アイコン幅 + これ)
    [SerializeField] private float columnSpacing = 20f;

    // 2列目以降を右に伸ばす(true推奨:既存が動かない)
    [SerializeField] private bool overflowToRight = true;

    [Header("Spacing Tuning")]
    // 着地点を上に持ち上げる（+で上へ）
    [SerializeField] private float stackLift = 60f;


    [Header("Drop Animation")]
    [SerializeField] private float dropStartOffset = 120f; // 目標位置の上方向オフセット
    [SerializeField] private float dropDuration = 0.25f;
    [SerializeField] private AnimationCurve dropCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private readonly Dictionary<CarType, List<GameObject>> laneIcons = new();
    private readonly Dictionary<CarType, int> laneCounts = new();

    // 落下中のコルーチン(同一Rectに多重に走るのを防ぐ)
    private readonly Dictionary<RectTransform, Coroutine> dropRoutines = new();

    // Prefab寸法キャッシュ
    private bool prefabMetricsValid;
    private float prefabW = 64f;
    private float prefabH = 64f;
    private Vector2 prefabPivot = new Vector2(0.5f, 0.5f);

    private void Awake()
    {
        InvalidatePrefabMetrics();
        CachePrefabMetrics();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxRows < 1) maxRows = 1;
        InvalidatePrefabMetrics();
        CachePrefabMetrics();
    }
#endif

    // 互換用（既存コードがsnake_caseでも動く）
    public void reset_all() => ResetAll();
    public void update_lane(CarType laneType, int count) => UpdateLane(laneType, count);

    public void ResetAll()
    {
        StopAllCoroutines();
        dropRoutines.Clear();

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

    public void UpdateLane(CarType laneType, int count)
    {
        EnsureLaneStorage(laneType);

        int newCount = Mathf.Max(0, count);
        int prevCount = laneCounts.TryGetValue(laneType, out int v) ? v : 0;
        laneCounts[laneType] = newCount;

        int laneIndex = (int)laneType;
        if (lanes == null || laneIndex < 0 || laneIndex >= lanes.Length) return;

        RectTransform lane = lanes[laneIndex];
        if (!lane || !iconPrefab) return;

        CachePrefabMetrics();

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

        // 目標位置を計算して配置(溢れたら2列目以降)
        List<Vector2> targets = CalcTargets(lane, newCount);

        // 追加が1個だけ増えたときだけ落下アニメ
        bool animateDrop = (newCount == prevCount + 1);

        for (int i = 0; i < newCount; i++)
        {
            RectTransform rect = icons[i].GetComponent<RectTransform>();
            if (!rect) continue;

            Vector2 to = targets[i];

            if (animateDrop && i == newCount - 1)
            {
                Vector2 from = to + Vector2.up * dropStartOffset;
                rect.anchoredPosition = from;
                StartDrop(rect, from, to, dropDuration);
            }
            else
            {
                rect.anchoredPosition = to;
            }
        }
    }

    private void EnsureLaneStorage(CarType laneType)
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

    private void InvalidatePrefabMetrics()
    {
        prefabMetricsValid = false;
    }

    private void CachePrefabMetrics()
    {
        if (prefabMetricsValid) return;

        prefabW = 64f;
        prefabH = 64f;
        prefabPivot = new Vector2(0.5f, 0.5f);

        if (iconPrefab != null)
        {
            RectTransform pr = iconPrefab.GetComponent<RectTransform>();
            if (pr != null)
            {
                prefabPivot = pr.pivot;

                float w = pr.rect.width;
                float h = pr.rect.height;

                // Prefabのrectが0になるケース対策(sizeDeltaも見る)
                prefabW = (w > 0f) ? w : Mathf.Max(1f, pr.sizeDelta.x);
                prefabH = (h > 0f) ? h : Mathf.Max(1f, pr.sizeDelta.y);
            }
        }

        prefabMetricsValid = true;
    }

    // レーンの内側境界(アイコンpivotが置けるmin/max)を計算
    private void GetInnerPivotBounds(RectTransform lane, out float minX, out float maxX, out float minY, out float maxY)
    {
        float w = lane.rect.width;
        float h = lane.rect.height;

        // laneローカル座標: pivotが原点
        float leftEdge = -lane.pivot.x * w;
        float rightEdge = (1f - lane.pivot.x) * w;
        float bottomEdge = -lane.pivot.y * h;
        float topEdge = (1f - lane.pivot.y) * h;

        float innerLeft = leftEdge + leftPadding;
        float innerRight = rightEdge - rightPadding;
        float innerBottom = bottomEdge + bottomPadding;
        float innerTop = topEdge - topPadding;

        // アイコンがはみ出ないよう、pivot位置として許される範囲に変換
        minX = innerLeft + prefabW * prefabPivot.x;
        maxX = innerRight - prefabW * (1f - prefabPivot.x);

        minY = innerBottom + prefabH * prefabPivot.y;
        maxY = innerTop - prefabH * (1f - prefabPivot.y);

        // 逆転を防ぐ
        if (maxX < minX) maxX = minX;
        if (maxY < minY) maxY = minY;
    }

    // 下端基準で積み上げ、溢れたら列を増やす(2列目以降も可)
    // - 縦が詰みすぎる場合は「行数を減らして列へ逃がす」ので、近すぎ問題が起きにくい
    private List<Vector2> CalcTargets(RectTransform lane, int count)
    {
        GetInnerPivotBounds(lane, out float minX, out float maxX, out float minY, out float maxY);

        float innerHeight = Mathf.Max(0f, maxY - minY);

        // 最小ステップ(重なり防止)
        float minStep = prefabH;

        // 高さから「最小ステップを保てる最大行数」を計算（+1は両端含む）
        int maxRowsByHeight = (minStep > 0f)
            ? Mathf.FloorToInt(innerHeight / minStep) + 1
            : maxRows;

        // 実際に使う行数（入らないなら行数を減らして列へ）
        int rowsPerCol = Mathf.Clamp(maxRowsByHeight, 1, maxRows);

        int colCount = Mathf.CeilToInt(count / (float)rowsPerCol);

        // 基本ステップ(詰め具合)
        float baseStep = 0f;
        if (rowsPerCol >= 2)
        {
            float usedHeight = innerHeight * Mathf.Clamp01(normalFillRatio);
            baseStep = usedHeight / (rowsPerCol - 1f);
        }

        // 物理的に入る最大ステップ
        float maxStep = (rowsPerCol >= 2) ? (innerHeight / (rowsPerCol - 1f)) : 0f;

        // 実際に使うステップ（最小間隔優先、ただし上限はmaxStep）
        float rowStep = (rowsPerCol >= 2)
            ? Mathf.Clamp(Mathf.Max(baseStep, minStep), 0f, maxStep)
            : 0f;

        // 積み上げ開始Y（countが少ない時はその分だけで上端判定）
        int maxRowUsed = Mathf.Min(rowsPerCol - 1, count - 1);

        float startY = minY + stackLift;
        if (maxRowUsed >= 1)
        {
            float topMostY = startY + rowStep * maxRowUsed;
            if (topMostY > maxY)
            {
                startY = maxY - rowStep * maxRowUsed;
            }
        }
        startY = Mathf.Max(startY, minY);

        // 横方向: 列ステップ(必要なら詰める)
        float colStep = prefabW + columnSpacing;
        float widthAvail = Mathf.Max(0f, maxX - minX);

        if (colCount >= 2)
        {
            float need = (colCount - 1) * colStep;
            if (need > widthAvail && widthAvail > 0f)
            {
                colStep = widthAvail / (colCount - 1f);
            }
        }

        var list = new List<Vector2>(count);

        for (int i = 0; i < count; i++)
        {
            int col = i / rowsPerCol; // 0,1,2...
            int row = i % rowsPerCol; // 0..rowsPerCol-1

            float x;
            if (overflowToRight)
            {
                // 1列目は左寄せ固定。列が増えても既存が動きにくい
                x = minX + col * colStep;
            }
            else
            {
                // 内側幅の中心基準で左右に広げる(列が増えると既存が動く)
                float centerX = (minX + maxX) * 0.5f;
                x = centerX + (col - (colCount - 1) * 0.5f) * colStep;
            }

            float y = startY + rowStep * row;

            list.Add(new Vector2(x, y));
        }

        return list;
    }

    private void StartDrop(RectTransform rect, Vector2 from, Vector2 to, float duration)
    {
        if (dropRoutines.TryGetValue(rect, out var running) && running != null)
        {
            StopCoroutine(running);
        }
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

            rect.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            yield return null;
        }

        rect.anchoredPosition = to;
        dropRoutines.Remove(rect);
    }
}
