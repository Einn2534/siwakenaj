// Created: 2025-11-28
// Updated: 2025-12-01
// Author: gpt-5.1-codex-max + user

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一定間隔で車をランダム生成する。
/// 画面上には複数台同時に存在してよい。
/// 判定対象の車は get_active_car() で「一番左まで進んでいる車」を返す。
/// </summary>
public class CarSpawner : MonoBehaviour
{
    private const float MINIMUM_INTERVAL_SECONDS = 0.2f;
    private const int MAX_CARS_ON_SCREEN = 3;
    private const float SPAWN_MARGIN_RATIO = 0.6f;
    private const float MIN_SPAWN_GAP_RATIO = 0.25f;
    private const float DEFAULT_CAR_WIDTH = 1f;

    [SerializeField]
    // 車プレハブ一覧。
    GameObject[] carPrefabs;

    [SerializeField]
    // PlayZoneのUI矩形。
    RectTransform playZone;

    [SerializeField]
    // スポーン時のZ座標参照点。
    Transform spawnPoint;

    // スポーン間隔（秒）。
    float spawnIntervalSeconds = 1f;
    // 車の速度（PlayZone幅/秒）。
    float carSpeed = 1f;
    // ライトトラック重み。
    int weightLightTruck = 1;
    // コンパクトカー重み。
    int weightCompactCar = 1;
    // スポーツカー重み。
    int weightSportsCar = 1;

    // スポーン中フラグ。
    bool isSpawning;
    // スポーンループ用コルーチン。
    Coroutine spawnCoroutine;

    /// <summary>ステージ設定を反映する。</summary>
    /// <param name="stageConfig">ステージ設定。</param>
    public void apply_stage_config(StageConfig stageConfig)
    {
        if (stageConfig == null)
        {
            return;
        }

        spawnIntervalSeconds = Mathf.Max(stageConfig.spawnInterval, MINIMUM_INTERVAL_SECONDS);
        carSpeed = Mathf.Max(stageConfig.carSpeed, 0f);
        weightLightTruck = Mathf.Max(0, stageConfig.weightLightTruck);
        weightCompactCar = Mathf.Max(0, stageConfig.weightCompactCar);
        weightSportsCar = Mathf.Max(0, stageConfig.weightSportsCar);
    }

    /// <summary>スポーンを開始する。</summary>
    public void start_spawning()
    {
        if (isSpawning) return;

        isSpawning = true;
        spawnCoroutine = StartCoroutine(spawn_loop());
    }

    /// <summary>スポーンを停止する。</summary>
    public void stop_spawning()
    {
        if (!isSpawning) return;

        isSpawning = false;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    /// <summary>既存車の移動を停止する。</summary>
    public void stop_all_cars()
    {
        CarController[] cars = FindObjectsOfType<CarController>();
        foreach (var car in cars)
        {
            if (car != null)
            {
                car.set_speed_world(0f);
            }
        }
    }

    /// <summary>スポーン間隔ごとに生成処理を回すループ。</summary>
    IEnumerator spawn_loop()
    {
        while (isSpawning)
        {
            spawn_if_possible();

            float interval = Mathf.Max(spawnIntervalSeconds, MINIMUM_INTERVAL_SECONDS);
            yield return new WaitForSeconds(interval);
        }
    }

    /// <summary>条件を満たしていれば車を1台スポーンする。</summary>
    void spawn_if_possible()
    {
        if (carPrefabs == null || carPrefabs.Length == 0)
        {
            return;
        }

        if (!try_get_play_zone_world_rect(out Rect playZoneWorldRect))
        {
            return;
        }

        CarController[] existingCars = FindObjectsOfType<CarController>();
        if (existingCars.Length >= MAX_CARS_ON_SCREEN)
        {
            return;
        }

        CarType? selectedType = select_weighted_car_type();
        if (!selectedType.HasValue)
        {
            return;
        }

        List<GameObject> candidates = get_prefabs_by_type(selectedType.Value);
        if (candidates.Count == 0)
        {
            return;
        }

        GameObject prefab = candidates[Random.Range(0, candidates.Count)];
        float carWidth = get_car_width(prefab);
        float spawnMarginX = carWidth * SPAWN_MARGIN_RATIO;
        float minSpawnGapX = carWidth * MIN_SPAWN_GAP_RATIO;

        float spawnX = playZoneWorldRect.xMax + spawnMarginX;
        float spawnY = playZoneWorldRect.center.y;

        if (should_skip_spawn_for_gap(existingCars, spawnX, minSpawnGapX))
        {
            return;
        }

        Vector3 position = new Vector3(spawnX, spawnY, transform.position.z);
        if (spawnPoint)
        {
            position.z = spawnPoint.position.z;
        }

        GameObject carObject = Instantiate(prefab, position, Quaternion.identity);
        CarController car = carObject.GetComponent<CarController>();
        if (car != null)
        {
            float speedWorld = playZoneWorldRect.width * carSpeed;
            car.set_speed_world(speedWorld);
            car.set_miss_line(playZoneWorldRect.xMin, playZoneWorldRect.width);
        }
    }

    /// <summary>
    /// 判定対象となる「一番左に進んでいる車」を返す。
    /// 車が1台もなければ null を返す。
    /// </summary>
    public CarController get_active_car()
    {
        CarController[] cars = FindObjectsOfType<CarController>();
        if (cars == null || cars.Length == 0)
        {
            return null;
        }

        CarController best = null;
        float minX = float.PositiveInfinity;

        foreach (var car in cars)
        {
            if (!car) continue;

            float x = car.get_min_x();
            if (x < minX)
            {
                minX = x;
                best = car;
            }
        }

        return best;
    }

    /// <summary>インスペクタ入力を最低値に補正する。</summary>
    void OnValidate()
    {
        spawnIntervalSeconds = Mathf.Max(spawnIntervalSeconds, MINIMUM_INTERVAL_SECONDS);
    }

    /// <summary>PlayZone のワールド矩形を取得する。</summary>
    /// <param name="worldRect">取得したワールド矩形。</param>
    /// <returns>取得に成功した場合 true。</returns>
    bool try_get_play_zone_world_rect(out Rect worldRect)
    {
        worldRect = new Rect();
        if (!playZone)
        {
            return false;
        }

        Vector3[] corners = new Vector3[4];
        playZone.GetWorldCorners(corners);
        Vector3 bottomLeft = corners[0];
        Vector3 topRight = corners[2];

        worldRect = new Rect(
            bottomLeft.x,
            bottomLeft.y,
            topRight.x - bottomLeft.x,
            topRight.y - bottomLeft.y);
        return worldRect.width > 0f && worldRect.height > 0f;
    }

    /// <summary>重みに応じて車種を抽選する。</summary>
    /// <returns>抽選された車種。全重みが0なら null。</returns>
    CarType? select_weighted_car_type()
    {
        int totalWeight = weightLightTruck + weightCompactCar + weightSportsCar;
        if (totalWeight <= 0)
        {
            return null;
        }

        int roll = Random.Range(0, totalWeight);
        if (roll < weightLightTruck)
        {
            return CarType.LightTruck;
        }

        roll -= weightLightTruck;
        if (roll < weightCompactCar)
        {
            return CarType.CompactCar;
        }

        return CarType.SportsCar;
    }

    /// <summary>指定車種のプレハブを抽出する。</summary>
    /// <param name="carType">抽出対象の車種。</param>
    /// <returns>対象プレハブのリスト。</returns>
    List<GameObject> get_prefabs_by_type(CarType carType)
    {
        List<GameObject> results = new();
        foreach (var prefab in carPrefabs)
        {
            if (prefab == null) continue;

            CarController car = prefab.GetComponent<CarController>();
            if (car != null && car.get_car_type() == carType)
            {
                results.Add(prefab);
            }
        }

        return results;
    }

    /// <summary>プレハブの車幅を取得する。</summary>
    /// <param name="prefab">車プレハブ。</param>
    /// <returns>車幅。</returns>
    float get_car_width(GameObject prefab)
    {
        if (prefab == null)
        {
            return DEFAULT_CAR_WIDTH;
        }

        if (try_get_bounds(prefab, out Bounds bounds))
        {
            return Mathf.Max(bounds.size.x, DEFAULT_CAR_WIDTH);
        }

        return DEFAULT_CAR_WIDTH;
    }

    /// <summary>最右車両との距離を見てスポーンを延期すべきか判定する。</summary>
    /// <param name="existingCars">既存の車両一覧。</param>
    /// <param name="spawnX">次のスポーン位置X。</param>
    /// <param name="minSpawnGapX">最低スポーン間隔。</param>
    /// <returns>延期する場合 true。</returns>
    bool should_skip_spawn_for_gap(CarController[] existingCars, float spawnX, float minSpawnGapX)
    {
        if (existingCars == null || existingCars.Length == 0)
        {
            return false;
        }

        float rightMostMaxX = float.NegativeInfinity;
        bool hasBounds = false;

        foreach (var car in existingCars)
        {
            if (!car) continue;

            if (try_get_bounds(car.gameObject, out Bounds bounds))
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

    /// <summary>Collider2D か Renderer を優先して bounds を取得する。</summary>
    /// <param name="target">対象オブジェクト。</param>
    /// <param name="bounds">取得した bounds。</param>
    /// <returns>取得に成功した場合 true。</returns>
    bool try_get_bounds(GameObject target, out Bounds bounds)
    {
        bounds = new Bounds();
        if (target == null)
        {
            return false;
        }

        Collider2D collider = target.GetComponentInChildren<Collider2D>();
        if (collider != null)
        {
            bounds = collider.bounds;
            return true;
        }

        Renderer renderer = target.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            bounds = renderer.bounds;
            return true;
        }

        return false;
    }
}
