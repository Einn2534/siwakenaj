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

    [SerializeField]
    private HowToOverlayController _howToOverlayController;

    private void Start()
    {
        SoundManager.EnsureInstance().PlayTitleBgm();
    }

    public void OnStartPressed()
    {
        SceneManager.LoadScene(StageSelectScene);
    }

    public void OnHowToOpen()
    {
        if (_howToOverlayController != null)
        {
            _howToOverlayController.ShowOverlay();
            return;
        }

        SetPanelActive(_howToPanel, true);
    }

    public void OnHowToClose()
    {
        if (_howToOverlayController != null)
        {
            _howToOverlayController.CloseOverlay();
            return;
        }

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
