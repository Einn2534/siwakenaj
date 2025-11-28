// Created: 2025-11-28
// Author: gpt-5.1-codex-max

using System.Collections;
using UnityEngine;

/// <summary>Spawns cars at fixed intervals with randomized types.</summary>
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

    /// <summary>Starts the spawning coroutine if available.</summary>
    public void start_spawning()
    {
        if (spawnRoutine != null)
        {
            return;
        }

        spawnRoutine = StartCoroutine(run_spawner());
    }

    /// <summary>Stops the spawning coroutine.</summary>
    public void stop_spawning()
    {
        if (spawnRoutine == null)
        {
            return;
        }

        StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    /// <summary>Spawns cars at repeated intervals.</summary>
    IEnumerator run_spawner()
    {
        WaitForSeconds delay = new WaitForSeconds(Mathf.Max(spawnIntervalSeconds, MINIMUM_INTERVAL_SECONDS));

        while (true)
        {
            spawn_random_car();
            yield return delay;
        }
    }

    /// <summary>Provides the currently active car if available.</summary>
    /// <returns>Existing car on the conveyor or null.</returns>
    public CarController get_active_car()
    {
        return activeCar ? activeCar : null;
    }

    /// <summary>Instantiates a random car prefab at the spawn point.</summary>
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
