// Created: 2025-02-14
// Updated: 2026-02-26
// Author: Einn

using UnityEngine;
using UnityEngine.UI;

/// <summary>HowTo オーバーレイの表示・非表示を制御する。</summary>
public class HowToOverlayController : MonoBehaviour
{
    [SerializeField]
    GameObject overlayPanel;

    [SerializeField]
    Button closeButton;

    /// <summary>初回表示判定とボタンリスナー登録を行う。</summary>
    void Start()
    {
        if (closeButton)
        {
            closeButton.onClick.AddListener(close_overlay);
        }

        if (!SaveService.get_how_to_shown())
        {
            show_overlay();
        }
        else
        {
            set_overlay_active(false);
        }
    }

    /// <summary>オーバーレイを表示する。</summary>
    public void show_overlay()
    {
        set_overlay_active(true);
    }

    /// <summary>オーバーレイを閉じ、表示済みとして保存する。</summary>
    public void close_overlay()
    {
        set_overlay_active(false);
        SaveService.set_how_to_shown(true);
        SaveService.save();
    }

    /// <summary>オーバーレイの表示状態を変更する。</summary>
    /// <param name="isActive">表示する場合 true。</param>
    void set_overlay_active(bool isActive)
    {
        if (overlayPanel)
        {
            overlayPanel.SetActive(isActive);
        }
    }
}
