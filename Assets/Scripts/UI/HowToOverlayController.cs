using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class HowToOverlayController : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("overlayPanel")]
    private GameObject _overlayPanel;

    [SerializeField, FormerlySerializedAs("closeButton")]
    private Button _closeButton;

    [SerializeField]
    private Button[] _extraCloseButtons;

    private void Start()
    {
        AddCloseListener(_closeButton);

        if (_extraCloseButtons != null)
        {
            foreach (Button closeButton in _extraCloseButtons)
            {
                AddCloseListener(closeButton);
            }
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

    private void OnDestroy()
    {
        RemoveCloseListener(_closeButton);

        if (_extraCloseButtons == null)
        {
            return;
        }

        foreach (Button closeButton in _extraCloseButtons)
        {
            RemoveCloseListener(closeButton);
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

    private void AddCloseListener(Button closeButton)
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseOverlay);
        }
    }

    private void RemoveCloseListener(Button closeButton)
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseOverlay);
        }
    }
}
