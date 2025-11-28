// Created: 2025-11-28
// Author: gpt-5.1-codex-max

using System.Collections;
using UnityEngine;

/// <summary>一定間隔で車をランダム生成する。</summary>
public class CarSpawner : MonoBehaviour
{
    private const float MINIMUM_INTERVAL_SECONDS = 0.2f;

    [SerializeField]
    GameObject[] carPrefabs;

    [SerializeField]
    float spawnIntervalSeconds = 1f;

    [SerializeField]
    Transform spawnPoint;

    Coroutine spawnRoutine;
    CarController activeCar;

    /// <summary>生成コルーチンを開始する。</summary>
    public void start_spawning()
    {
        if (spawnRoutine != null)
        {
            return;
        }

        spawnRoutine = StartCoroutine(run_spawner());
    }

    /// <summary>生成コルーチンを停止する。</summary>
    public void stop_spawning()
    {
        if (spawnRoutine == null)
        {
            return;
        }

        StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    /// <summary>一定間隔で車を生成するループ。</summary>
    IEnumerator run_spawner()
    {
        WaitForSeconds delay = new WaitForSeconds(Mathf.Max(spawnIntervalSeconds, MINIMUM_INTERVAL_SECONDS));

        while (true)
        {
            spawn_random_car();
            yield return delay;
        }
    }

    /// <summary>現在表示中の車を取得する。</summary>
    /// <returns>コンベア上の車。存在しなければ null。</returns>
    public CarController get_active_car()
    {
        return activeCar ? activeCar : null;
    }

    /// <summary>生成ポイントにランダムな車プレハブを配置する。</summary>
    void spawn_random_car()
    {
        if (!activeCar)
        {
            activeCar = null;
        }

        if (activeCar)
        {
            return;
        }

        if (carPrefabs == null || carPrefabs.Length == 0)
        {
            return;
        }

        int index = Random.Range(0, carPrefabs.Length);
        Vector3 position = spawnPoint ? spawnPoint.position : transform.position;
        GameObject carObject = Instantiate(carPrefabs[index], position, Quaternion.identity);
        activeCar = carObject.GetComponent<CarController>();
    }
}
