using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class HowToOverlayController : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("overlayPanel")]
    private GameObject _overlayPanel;

    [SerializeField, FormerlySerializedAs("closeButton")]
    private Button _closeButton;

    private void Start()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(CloseOverlay);
        }

        if (!SaveService.GetHowToShown())
        {
            ShowOverlay();
        }
        else
        {
            SetOverlayActive(false);
        }
    }

    public void ShowOverlay()
    {
        SetOverlayActive(true);
    }

    public void CloseOverlay()
    {
        SetOverlayActive(false);
        SaveService.SetHowToShown(true);
        SaveService.Save();
    }

    private void SetOverlayActive(bool isActive)
    {
        if (_overlayPanel != null)
        {
            _overlayPanel.SetActive(isActive);
        }
    }
}
