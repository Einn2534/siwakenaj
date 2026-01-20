// Created: 2025-02-14
// Author: gpt-5.2-codex

using UnityEngine;

/// <summary>HowTo のオーバーレイ表示を制御する。</summary>
public class HowToOverlayController : MonoBehaviour
{
    [SerializeField]
    GameObject overlayRoot;

    [SerializeField]
    bool showOnStart;

    [SerializeField]
    bool markAsShownOnClose = true;

    /// <summary>開始時に必要なら HowTo を表示する。</summary>
    void Start()
    {
        if (showOnStart && !SaveService.get_how_to_shown())
        {
            show_overlay();
        }
        else
        {
            set_overlay_active(false);
        }
    }

    /// <summary>HowTo を表示する。</summary>
    public void show_overlay()
    {
        set_overlay_active(true);
    }

    /// <summary>HowTo を閉じ、必要なら保存フラグを更新する。</summary>
    public void hide_overlay()
    {
        set_overlay_active(false);
        if (markAsShownOnClose)
        {
            SaveService.set_how_to_shown(true);
            SaveService.save();
        }
    }

    /// <summary>タップで閉じるボタン用の処理。</summary>
    public void on_overlay_tapped()
    {
        hide_overlay();
    }

    /// <summary>HowTo の表示状態を切り替える。</summary>
    /// <param name="isActive">表示する場合 true。</param>
    void set_overlay_active(bool isActive)
    {
        if (overlayRoot)
        {
            overlayRoot.SetActive(isActive);
        }
    }
}
