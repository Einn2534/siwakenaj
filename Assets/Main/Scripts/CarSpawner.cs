// Created: 2025-11-28
// Updated: 複数台同時スポーン対応
// Author: gpt-5.1-codex-max + user

using System.Collections;
using UnityEngine;

/// <summary>
/// 一定間隔で車をランダム生成する。
/// 画面上には複数台同時に存在してよい。
/// 判定対象の車は get_active_car() で「一番左まで進んでいる車」を返す。
/// </summary>
public class CarSpawner : MonoBehaviour
{
    private const float MINIMUM_INTERVAL_SECONDS = 0.2f;

    [SerializeField]
    GameObject[] carPrefabs;

    [SerializeField]
    float spawnIntervalSeconds = 1f;

    [SerializeField]
    Transform spawnPoint;

    [SerializeField]
    int maxCarsOnScreen = 3; // 同時に画面に出してよい最大台数

    bool isSpawning;
    Coroutine spawnCoroutine;

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

        // すでに画面にある車の数を数える
        CarController[] existingCars = FindObjectsOfType<CarController>();
        if (existingCars.Length >= maxCarsOnScreen)
        {
            return;
        }

        int index = Random.Range(0, carPrefabs.Length);
        Vector3 position = spawnPoint ? spawnPoint.position : transform.position;
        Instantiate(carPrefabs[index], position, Quaternion.identity);
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

            float x = car.transform.position.x;
            // x が小さいほど左にあるとみなす
            if (x < minX)
            {
                minX = x;
                best = car;
            }
        }

        return best;
    }

    /// <summary>インスペクタ値の簡易バリデーション。</summary>
    void OnValidate()
    {
        spawnIntervalSeconds = Mathf.Max(spawnIntervalSeconds, MINIMUM_INTERVAL_SECONDS);
        maxCarsOnScreen = Mathf.Max(1, maxCarsOnScreen);
    }
}
