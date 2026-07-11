using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class EventSystemInputModuleUtility
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallBootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureCompatibleEventSystem();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureCompatibleEventSystem();
    }

    public static EventSystem EnsureCompatibleEventSystem()
    {
        EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (eventSystems.Length == 0)
        {
            GameObject eventSystemObject = new("EventSystem", typeof(EventSystem));
            eventSystemObject.layer = LayerMask.NameToLayer("UI");
            EventSystem eventSystem = eventSystemObject.GetComponent<EventSystem>();
            ConfigureInputModule(eventSystem);
            return eventSystem;
        }

        foreach (EventSystem eventSystem in eventSystems)
        {
            ConfigureInputModule(eventSystem);
        }

        return eventSystems[0];
    }

    private static void ConfigureInputModule(EventSystem eventSystem)
    {
        if (eventSystem == null)
        {
            return;
        }

#if ENABLE_INPUT_SYSTEM
        StandaloneInputModule[] standaloneModules = eventSystem.GetComponents<StandaloneInputModule>();
        foreach (StandaloneInputModule standaloneModule in standaloneModules)
        {
            if (standaloneModule != null)
            {
                standaloneModule.enabled = false;
                Object.Destroy(standaloneModule);
            }
        }

        if (eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (eventSystem.GetComponent<StandaloneInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }
#endif
    }
}
