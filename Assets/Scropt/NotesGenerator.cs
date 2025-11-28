// Created: 2024-05-25
// Author: gpt-5-codex

using System.Collections;
using UnityEngine;

/// <summary>Spawns note prefabs at a constant interval.</summary>
public class NotesGenerator : MonoBehaviour
{
    private const float MINIMUM_INTERVAL_SECONDS = 0.01f;

    [SerializeField]
    GameObject notesPrefab;

    [SerializeField]
    float intervalSeconds = 1f;

    WaitForSeconds spawnDelay;

    /// <summary>Prepares cached delay instructions.</summary>
    void Awake()
    {
        refresh_spawn_delay();
    }

    /// <summary>Starts the repeated spawning coroutine.</summary>
    void Start()
    {
        StartCoroutine(generate_notes());
    }

    /// <summary>Keeps the configured interval within valid limits.</summary>
    void OnValidate()
    {
        refresh_spawn_delay();
    }

    /// <summary>Creates note instances while the generator is active.</summary>
    IEnumerator generate_notes()
    {
        while (true)
        {
            if (notesPrefab)
            {
                Instantiate(notesPrefab, transform.position, Quaternion.identity);
            }

            yield return spawnDelay;
        }
    }

    /// <summary>Updates the cached delay value.</summary>
    void refresh_spawn_delay()
    {
        float clampedInterval = Mathf.Max(intervalSeconds, MINIMUM_INTERVAL_SECONDS);
        intervalSeconds = clampedInterval;
        spawnDelay = new WaitForSeconds(clampedInterval);
    }
}
