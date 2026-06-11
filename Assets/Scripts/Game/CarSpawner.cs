using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CarSpawner : MonoBehaviour
{
    private const float MinimumIntervalSeconds = 0.2f;
    private const int MaxCarsOnScreen = 3;
    private const float SpawnMarginRatio = 0.6f;
    private const float MinSpawnGapRatio = 0.25f;
    private const float DefaultCarWidth = 1f;
    private const int CarSortingOrder = 20;

    [SerializeField]
    private CarVisualDatabase _visualDatabase;

    [SerializeField]
    private GameObject _carPrefab;

    [SerializeField, FormerlySerializedAs("playZone")]
    private RectTransform _playZone;

    [SerializeField, FormerlySerializedAs("spawnPoint")]
    private Transform _spawnPoint;

    private readonly List<CarController> _activeCars = new();
    private StageDefinition _stageDefinition;
    private Coroutine _spawnCoroutine;
    private bool _isSpawning;

    public event Action<CarController> CarMissed;

    public void Initialize(StageDefinition stageDefinition)
    {
        _stageDefinition = stageDefinition;
        _visualDatabase ??= CarVisualDatabase.LoadDefault();
        _isSpawning = false;
        _spawnCoroutine = null;
        CleanupNullCars();
    }

    public void StartSpawning()
    {
        if (_isSpawning)
        {
            return;
        }

        _isSpawning = true;
        _spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (!_isSpawning)
        {
            return;
        }

        _isSpawning = false;
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
    }

    public void StopAllCars()
    {
        CleanupNullCars();
        foreach (CarController car in _activeCars)
        {
            car.Stop();
        }
    }

    public CarController GetActiveCar()
    {
        CleanupNullCars();

        CarController best = null;
        float minX = float.PositiveInfinity;

        foreach (CarController car in _activeCars)
        {
            float x = car.GetMinX();
            if (x < minX)
            {
                minX = x;
                best = car;
            }
        }

        return best;
    }

    public void DespawnCar(CarController car)
    {
        if (car == null)
        {
            return;
        }

        UnregisterCar(car);
        car.Despawn();
    }

    private IEnumerator SpawnLoop()
    {
        while (_isSpawning)
        {
            SpawnIfPossible();

            float interval = _stageDefinition != null
                ? Mathf.Max(_stageDefinition.SpawnInterval, MinimumIntervalSeconds)
                : MinimumIntervalSeconds;
            yield return new WaitForSeconds(interval);
        }
    }

    private void SpawnIfPossible()
    {
        if (_stageDefinition == null)
        {
            return;
        }

        if (!TryGetPlayZoneWorldRect(out Rect playZoneWorldRect))
        {
            return;
        }

        CleanupNullCars();
        if (_activeCars.Count >= MaxCarsOnScreen)
        {
            return;
        }

        CarType? selectedType = SelectWeightedCarType();
        if (!selectedType.HasValue)
        {
            return;
        }

        float spawnZ = GetSpawnZ();
        Vector3 position = new(
            playZoneWorldRect.xMax,
            playZoneWorldRect.center.y,
            spawnZ);
        GameObject carObject = CreateCarObject(position);
        CarController car = carObject.GetComponent<CarController>();
        if (car == null)
        {
            Destroy(carObject);
            return;
        }

        float speedWorld = playZoneWorldRect.width * Mathf.Max(_stageDefinition.CarSpeed, 0f);
        car.Initialize(selectedType.Value, speedWorld, playZoneWorldRect.xMin, playZoneWorldRect.width);

        float carWidth = GetCarWidth(carObject);
        float spawnMarginX = carWidth * SpawnMarginRatio;
        float minSpawnGapX = carWidth * MinSpawnGapRatio;
        float spawnX = playZoneWorldRect.xMax + spawnMarginX;
        float spawnY = playZoneWorldRect.center.y;

        if (ShouldSkipSpawnForGap(spawnX, minSpawnGapX))
        {
            Destroy(carObject);
            return;
        }

        carObject.transform.position = new Vector3(
            spawnX,
            spawnY,
            spawnZ);
        RegisterCar(car);
    }

    private GameObject CreateCarObject(Vector3 position)
    {
        GameObject carObject = _carPrefab != null
            ? Instantiate(_carPrefab, position, Quaternion.identity)
            : new GameObject("Car");

        carObject.name = "Car";
        carObject.transform.SetPositionAndRotation(position, Quaternion.identity);

        SpriteRenderer spriteRenderer = carObject.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = carObject.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.sortingOrder = CarSortingOrder;

        if (!carObject.TryGetComponent(out CarVisualController _))
        {
            carObject.AddComponent<CarVisualController>();
        }

        if (!carObject.TryGetComponent(out CarController _))
        {
            carObject.AddComponent<CarController>();
        }

        return carObject;
    }

    private void RegisterCar(CarController car)
    {
        if (car == null)
        {
            return;
        }

        car.Missed += HandleCarMissed;
        car.Despawned += HandleCarDespawned;
        _activeCars.Add(car);
    }

    private void UnregisterCar(CarController car)
    {
        if (car == null)
        {
            return;
        }

        car.Missed -= HandleCarMissed;
        car.Despawned -= HandleCarDespawned;
        _activeCars.Remove(car);
    }

    private void HandleCarMissed(CarController car)
    {
        UnregisterCar(car);
        CarMissed?.Invoke(car);
        car.Despawn();
    }

    private void HandleCarDespawned(CarController car)
    {
        UnregisterCar(car);
    }

    private void CleanupNullCars()
    {
        _activeCars.RemoveAll(car => car == null);
    }

    private bool TryGetPlayZoneWorldRect(out Rect worldRect)
    {
        worldRect = new Rect();
        if (_playZone == null)
        {
            return false;
        }

        Vector3[] corners = new Vector3[4];
        _playZone.GetWorldCorners(corners);
        Vector3 bottomLeft = corners[0];
        Vector3 topRight = corners[2];
        worldRect = new Rect(bottomLeft.x, bottomLeft.y, topRight.x - bottomLeft.x, topRight.y - bottomLeft.y);
        return worldRect.width > 0f && worldRect.height > 0f;
    }

    private CarType? SelectWeightedCarType()
    {
        int lightTruckWeight = Mathf.Max(0, _stageDefinition.WeightLightTruck);
        int compactCarWeight = Mathf.Max(0, _stageDefinition.WeightCompactCar);
        int sportsCarWeight = Mathf.Max(0, _stageDefinition.WeightSportsCar);
        int totalWeight = lightTruckWeight + compactCarWeight + sportsCarWeight;
        if (totalWeight <= 0)
        {
            return null;
        }

        int roll = UnityEngine.Random.Range(0, totalWeight);
        if (roll < lightTruckWeight)
        {
            return CarType.LightTruck;
        }

        roll -= lightTruckWeight;
        if (roll < compactCarWeight)
        {
            return CarType.CompactCar;
        }

        return CarType.SportsCar;
    }

    private float GetCarWidth(GameObject target)
    {
        if (target == null)
        {
            return DefaultCarWidth;
        }

        if (BoundsHelper.TryGetBounds(target, out Bounds bounds))
        {
            return Mathf.Max(bounds.size.x, DefaultCarWidth);
        }

        return DefaultCarWidth;
    }

    private float GetSpawnZ()
    {
        return _spawnPoint != null ? _spawnPoint.position.z : transform.position.z;
    }

    private bool ShouldSkipSpawnForGap(float spawnX, float minSpawnGapX)
    {
        if (_activeCars.Count == 0)
        {
            return false;
        }

        float rightMostMaxX = float.NegativeInfinity;
        bool hasBounds = false;

        foreach (CarController car in _activeCars)
        {
            if (BoundsHelper.TryGetBounds(car.gameObject, out Bounds bounds))
            {
                rightMostMaxX = Mathf.Max(rightMostMaxX, bounds.max.x);
                hasBounds = true;
            }
            else
            {
                rightMostMaxX = Mathf.Max(rightMostMaxX, car.transform.position.x);
            }
        }

        if (!hasBounds)
        {
            rightMostMaxX = Mathf.Max(rightMostMaxX, spawnX - minSpawnGapX);
        }

        return (spawnX - rightMostMaxX) < minSpawnGapX;
    }
}
