using System;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(CarVisualController))]
public class CarController : MonoBehaviour
{
    private const float MinimumSpeed = 0f;
    private const float MissMarginRatio = 0.02f;

    [SerializeField, FormerlySerializedAs("carType")]
    private CarType _carType;

    private CarVisualController _visualController;
    private float _speedWorld;
    private float _leftEdgeX;
    private float _missMarginX;
    private bool _hasMissLine;
    private bool _hasReportedMiss;
    private bool _isDespawning;
    private bool _isInitialized;

    public event Action<CarController> Missed;
    public event Action<CarController> Despawned;

    public CarType CarType => _carType;

    private void Awake()
    {
        _visualController = GetComponent<CarVisualController>();
    }

    public void Initialize(CarType carType, float speedWorld, float leftEdgeX, float playZoneWidth)
    {
        _carType = carType;
        _visualController ??= GetComponent<CarVisualController>();
        _visualController?.Apply(carType);
        _speedWorld = Mathf.Max(speedWorld, MinimumSpeed);
        _leftEdgeX = leftEdgeX;
        _missMarginX = playZoneWidth * MissMarginRatio;
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
