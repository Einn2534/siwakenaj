using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ScoreLaneUI : MonoBehaviour
{
    private const int DefaultMaxRows = 10;

    [Header("Lane / Prefab")]
    [SerializeField, FormerlySerializedAs("lanes")]
    private RectTransform[] _lanes;

    [SerializeField, FormerlySerializedAs("iconPrefab")]
    private GameObject _iconPrefab;

    [Header("Layout")]
    [SerializeField, Min(1), FormerlySerializedAs("maxRows")]
    private int _maxRows = DefaultMaxRows;

    [SerializeField, Range(0.1f, 1f), FormerlySerializedAs("normalFillRatio")]
    private float _normalFillRatio = 0.6f;

    [SerializeField, FormerlySerializedAs("topPadding")]
    private float _topPadding = 20f;

    [SerializeField, FormerlySerializedAs("bottomPadding")]
    private float _bottomPadding = 20f;

    [SerializeField, FormerlySerializedAs("leftPadding")]
    private float _leftPadding = 20f;

    [SerializeField, FormerlySerializedAs("rightPadding")]
    private float _rightPadding = 20f;

    [SerializeField, FormerlySerializedAs("columnSpacing")]
    private float _columnSpacing = 20f;

    [Header("Drop Animation")]
    [SerializeField, FormerlySerializedAs("dropStartOffset")]
    private float _dropStartOffset = 120f;

    [SerializeField, FormerlySerializedAs("dropDuration")]
    private float _dropDuration = 0.25f;

    [SerializeField, FormerlySerializedAs("dropCurve")]
    private AnimationCurve _dropCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private readonly Dictionary<CarType, List<GameObject>> _laneIcons = new();
    private readonly Dictionary<CarType, int> _laneCounts = new();
    private readonly Dictionary<RectTransform, Coroutine> _dropRoutines = new();
    private readonly List<Vector2> _tmpTargets = new(64);

    private bool _metricsValid;
    private float _iconWidth = 64f;
    private float _iconHeight = 64f;
    private Vector2 _iconPivot = new(0.5f, 0.5f);

    private void Awake()
    {
        CachePrefabMetrics();
    }

    private void OnEnable()
    {
        StartCoroutine(RefreshNextFrame());
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_maxRows < 1)
        {
            _maxRows = 1;
        }

        _metricsValid = false;
        CachePrefabMetrics();
    }
#endif

    public void RefreshAll()
    {
        if (_laneCounts.Count == 0)
        {
            return;
        }

        List<CarType> keys = new(_laneCounts.Keys);
        foreach (CarType laneType in keys)
        {
            ApplyLaneLayout(laneType, _laneCounts[laneType], false);
        }
    }

    public void ResetAll()
    {
        StopAllDrops();

        foreach (List<GameObject> list in _laneIcons.Values)
        {
            for (int i = 0; i < list.Count; i += 1)
            {
                if (list[i] != null)
                {
                    Destroy(list[i]);
                }
            }
        }

        _laneIcons.Clear();
        _laneCounts.Clear();
    }

    public void UpdateLane(CarType laneType, int count)
    {
        EnsureLaneStorage(laneType);

        int newCount = Mathf.Max(0, count);
        int previousCount = _laneCounts.TryGetValue(laneType, out int value) ? value : 0;
        _laneCounts[laneType] = newCount;
        bool animateDrop = newCount == previousCount + 1;
        ApplyLaneLayout(laneType, newCount, animateDrop);
    }

    private void ApplyLaneLayout(CarType laneType, int count, bool animateLast)
    {
        if (!TryGetLaneRect(laneType, out RectTransform lane) || lane == null || _iconPrefab == null)
        {
            return;
        }

        CachePrefabMetrics();

        if (!_laneIcons.TryGetValue(laneType, out List<GameObject> icons))
        {
            icons = new List<GameObject>();
            _laneIcons[laneType] = icons;
        }

        if (count <= 0)
        {
            for (int i = 0; i < icons.Count; i += 1)
            {
                if (icons[i] != null)
                {
                    icons[i].SetActive(false);
                }
            }

            return;
        }

        EnsureIconInstances(icons, lane, count);
        SetActiveIcons(icons, count);
        _tmpTargets.Clear();
        FillTargets(lane, count, _tmpTargets);

        for (int i = 0; i < count; i += 1)
        {
            GameObject icon = icons[i];
            if (icon == null)
            {
                continue;
            }

            RectTransform iconRect = icon.transform as RectTransform;
            if (iconRect == null)
            {
                continue;
            }

            Vector2 target = _tmpTargets[i];
            if (!(animateLast && i == count - 1))
            {
                StopDrop(iconRect);
                iconRect.anchoredPosition = target;
            }
            else
            {
                Vector2 from = target + Vector2.up * _dropStartOffset;
                iconRect.anchoredPosition = from;
                StartDrop(iconRect, from, target, _dropDuration);
            }
        }
    }

    private bool TryGetLaneRect(CarType laneType, out RectTransform lane)
    {
        lane = null;
        int index = (int)laneType;
        if (_lanes == null || index < 0 || index >= _lanes.Length)
        {
            return false;
        }

        lane = _lanes[index];
        return lane != null;
    }

    private void EnsureLaneStorage(CarType laneType)
    {
        if (!_laneIcons.ContainsKey(laneType))
        {
            _laneIcons[laneType] = new List<GameObject>();
        }

        if (!_laneCounts.ContainsKey(laneType))
        {
            _laneCounts[laneType] = 0;
        }
    }

    private void EnsureIconInstances(List<GameObject> icons, RectTransform parent, int count)
    {
        for (int i = icons.Count; i < count; i += 1)
        {
            icons.Add(Instantiate(_iconPrefab, parent));
        }
    }

    private static void SetActiveIcons(List<GameObject> icons, int activeCount)
    {
        for (int i = 0; i < icons.Count; i += 1)
        {
            if (icons[i] != null)
            {
                icons[i].SetActive(i < activeCount);
            }
        }
    }

    private void CachePrefabMetrics()
    {
        if (_metricsValid)
        {
            return;
        }

        _iconWidth = 64f;
        _iconHeight = 64f;
        _iconPivot = new Vector2(0.5f, 0.5f);

        if (_iconPrefab != null)
        {
            RectTransform prefabRect = _iconPrefab.GetComponent<RectTransform>();
            if (prefabRect != null)
            {
                _iconPivot = prefabRect.pivot;
                float width = prefabRect.rect.width;
                float height = prefabRect.rect.height;
                _iconWidth = width > 0f ? width : Mathf.Max(1f, prefabRect.sizeDelta.x);
                _iconHeight = height > 0f ? height : Mathf.Max(1f, prefabRect.sizeDelta.y);
            }
        }

        _metricsValid = true;
    }

    private void FillTargets(RectTransform lane, int count, List<Vector2> output)
    {
        Rect rect = lane.rect;
        float innerXMin = rect.xMin + _leftPadding;
        float innerXMax = rect.xMax - _rightPadding;
        float innerYMin = rect.yMin + _bottomPadding;
        float innerYMax = rect.yMax - _topPadding;
        float minX = innerXMin + _iconWidth * _iconPivot.x;
        float maxX = innerXMax - _iconWidth * (1f - _iconPivot.x);
        float minY = innerYMin + _iconHeight * _iconPivot.y;
        float maxY = innerYMax - _iconHeight * (1f - _iconPivot.y);

        if (maxX < minX)
        {
            maxX = minX;
        }

        if (maxY < minY)
        {
            maxY = minY;
        }

        float innerHeight = Mathf.Max(0f, maxY - minY);
        int rowsPerColumn = Mathf.Max(1, _maxRows);
        int columnCount = Mathf.CeilToInt(count / (float)rowsPerColumn);
        float verticalStep = 0f;

        if (rowsPerColumn >= 2)
        {
            float fitStep = innerHeight / (rowsPerColumn - 1f);
            verticalStep = fitStep * Mathf.Clamp01(_normalFillRatio);
            verticalStep = Mathf.Max(verticalStep, _iconHeight * 0.5f);
            if (verticalStep > fitStep)
            {
                verticalStep = fitStep;
            }
        }

        int usedRows = Mathf.Min(rowsPerColumn, count);
        float startY = minY;
        if (usedRows >= 2)
        {
            float topMostY = startY + (usedRows - 1) * verticalStep;
            if (topMostY > maxY)
            {
                startY -= topMostY - maxY;
            }
        }

        startY = Mathf.Clamp(startY, minY, maxY);

        float desiredColumnStep = _iconWidth + _columnSpacing;
        float columnStep = desiredColumnStep;
        float centerX = (minX + maxX) * 0.5f;

        if (columnCount <= 1)
        {
            columnStep = 0f;
        }
        else
        {
            float available = Mathf.Max(0f, maxX - minX);
            float fitStep = available > 0f ? available / (columnCount - 1f) : 0f;
            columnStep = Mathf.Min(desiredColumnStep, fitStep);
        }

        output.Capacity = Mathf.Max(output.Capacity, count);

        for (int i = 0; i < count; i += 1)
        {
            int column = i / rowsPerColumn;
            int row = i % rowsPerColumn;
            float span = (columnCount - 1) * columnStep;
            float firstX = centerX - span * 0.5f;
            float x = Mathf.Clamp(firstX + column * columnStep, minX, maxX);
            float y = Mathf.Clamp(startY + row * verticalStep, minY, maxY);
            output.Add(new Vector2(x, y));
        }
    }

    private IEnumerator RefreshNextFrame()
    {
        yield return null;
        RefreshAll();
    }

    private void StopAllDrops()
    {
        foreach (Coroutine coroutine in _dropRoutines.Values)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }

        _dropRoutines.Clear();
    }

    private void StopDrop(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        if (_dropRoutines.TryGetValue(rect, out Coroutine coroutine) && coroutine != null)
        {
            StopCoroutine(coroutine);
        }

        _dropRoutines.Remove(rect);
    }

    private void StartDrop(RectTransform rect, Vector2 from, Vector2 to, float duration)
    {
        if (rect == null)
        {
            return;
        }

        if (_dropRoutines.TryGetValue(rect, out Coroutine existing) && existing != null)
        {
            StopCoroutine(existing);
        }

        _dropRoutines[rect] = StartCoroutine(DropTo(rect, from, to, duration));
    }

    private IEnumerator DropTo(RectTransform rect, Vector2 from, Vector2 to, float duration)
    {
        float time = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (time < duration)
        {
            time += Time.deltaTime;
            float progress = Mathf.Clamp01(time / duration);
            float eased = _dropCurve != null ? _dropCurve.Evaluate(progress) : progress;

            if (rect != null)
            {
                rect.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            }

            yield return null;
        }

        if (rect != null)
        {
            rect.anchoredPosition = to;
        }

        _dropRoutines.Remove(rect);
    }
}
