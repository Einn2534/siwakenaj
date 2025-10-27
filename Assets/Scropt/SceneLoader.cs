// Created: 2024-05-25
// Author: gpt-5-codex

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Handles scene transitions triggered from the UI.</summary>
public class SceneLoader : MonoBehaviour
{
    [SerializeField]
    string sceneName;

    /// <summary>Loads the configured scene (legacy PascalCase entry point).</summary>
    public void LoadTarget()
    {
        load_target();
    }

    /// <summary>Loads the configured scene.</summary>
    public void load_target()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return;
        }

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    /// <summary>Reloads the currently active scene (legacy PascalCase entry point).</summary>
    public void ReloadCurrent()
    {
        reload_current();
    }

    /// <summary>Reloads the currently active scene.</summary>
    public void reload_current()
    {
        string current = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(current, LoadSceneMode.Single);
    }
}
