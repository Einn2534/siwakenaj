using System;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(CarVisualController))]
public class CarController : MonoBehaviour
{
    private const float MinimumSpeed = 0f;
    private const float MissMarginRatio = 0.02f;
    private const float CoveredRevealLineRatio = 0.48f;

    [SerializeField, FormerlySerializedAs("carType")]
    private CarType _carType;

    private CarVisualController _visualController;
    private CarModifier _modifier;
    private float _speedWorld;
    private float _leftEdgeX;
    private float _missMarginX;
    private float _coveredRevealX;
    private bool _hasMissLine;
    private bool _hasReportedMiss;
    private bool _isDespawning;
    private bool _isInitialized;

    public event Action<CarController> Missed;
    public event Action<CarController> Despawned;
    public event Action<CarController> Revealed;

    public CarType CarType => _carType;
    public CarModifier Modifier => _modifier;
    public bool IsCovered => _modifier == CarModifier.Covered;
    public bool IsRevealed { get; private set; }
    public bool RequiresRepair => CarModifierRules.RequiresRepair(_modifier);
    public int ScoreMultiplier => CarModifierRules.GetScoreMultiplier(_modifier);

    private void Awake()
    {
        _visualController = GetComponent<CarVisualController>();
    }

    public void Initialize(CarType carType, float speedWorld, float leftEdgeX, float playZoneWidth)
    {
        Initialize(carType, CarModifier.Normal, speedWorld, leftEdgeX, playZoneWidth);
    }

    public void Initialize(CarType carType, CarModifier modifier, float speedWorld, float leftEdgeX, float playZoneWidth)
    {
        _carType = carType;
        _modifier = modifier;
        _visualController ??= GetComponent<CarVisualController>();
        IsRevealed = !CarModifierRules.StartsCovered(modifier);
        _visualController?.Apply(carType, modifier, IsRevealed);
        _speedWorld = Mathf.Max(speedWorld * CarModifierRules.GetSpeedMultiplier(modifier), MinimumSpeed);
        _leftEdgeX = leftEdgeX;
        _missMarginX = playZoneWidth * MissMarginRatio;
        _coveredRevealX = leftEdgeX + (playZoneWidth * CoveredRevealLineRatio);
        _hasMissLine = true;
        _hasReportedMiss = false;
        _isDespawning = false;
        _isInitialized = true;
    }

    public void Stop()
    {
        _speedWorld = 0f;
    }

    public float GetMinX()
    {
        if (BoundsHelper.TryGetBounds(gameObject, out Bounds bounds))
        {
            return bounds.min.x;
        }

        return transform.position.x;
    }

    public void Despawn()
    {
        if (_isDespawning)
        {
            return;
        }

        _isDespawning = true;
        Despawned?.Invoke(this);
        Destroy(gameObject);
    }

    public void Reveal()
    {
        if (IsRevealed)
        {
            return;
        }

        IsRevealed = true;
        _visualController?.Reveal(_carType, _modifier);
        Revealed?.Invoke(this);
    }

    private void Update()
    {
        if (!_isInitialized)
        {
            return;
        }

        if (_speedWorld > 0f)
        {
            transform.position += Vector3.left * (_speedWorld * Time.deltaTime);
        }

        if (!IsRevealed && GetMinX() <= _coveredRevealX)
        {
            Reveal();
        }

        if (_hasMissLine && !_hasReportedMiss && IsOutOfPlayZone())
        {
            _hasReportedMiss = true;
            Missed?.Invoke(this);
        }
    }

    private bool IsOutOfPlayZone()
    {
        return GetMinX() < (_leftEdgeX - _missMarginX);
    }
}
