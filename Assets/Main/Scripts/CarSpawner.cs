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

    /// <summary>Instantiates a random car prefab at the spawn point.</summary>
    void spawn_random_car()
    {
        if (carPrefabs == null || carPrefabs.Length == 0)
        {
            return;
        }

        int index = Random.Range(0, carPrefabs.Length);
        Vector3 position = spawnPoint ? spawnPoint.position : transform.position;
        Instantiate(carPrefabs[index], position, Quaternion.identity);
    }
}
