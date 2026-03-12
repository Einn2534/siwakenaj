using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class TitleController : MonoBehaviour
{
    private const string StageSelectScene = "StageSelect";

    [SerializeField, FormerlySerializedAs("howToPanel")]
    private GameObject _howToPanel;

    [SerializeField, FormerlySerializedAs("settingsPanel")]
    private GameObject _settingsPanel;

    public void OnStartPressed()
    {
        SceneManager.LoadScene(StageSelectScene);
    }

    public void OnHowToOpen()
    {
        SetPanelActive(_howToPanel, true);
    }

    public void OnHowToClose()
    {
        SetPanelActive(_howToPanel, false);
    }

    public void OnSettingsOpen()
    {
        SetPanelActive(_settingsPanel, true);
    }

    public void OnSettingsClose()
    {
        SetPanelActive(_settingsPanel, false);
    }

    private static void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }
}
