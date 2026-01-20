// Created: 2025-02-14
// Author: gpt-5.2-codex

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>タイトル画面のボタン操作を管理する。</summary>
public class TitleController : MonoBehaviour
{
    private const string STAGE_SELECT_SCENE = "StageSelect";

    [SerializeField]
    GameObject howToPanel;

    [SerializeField]
    GameObject settingsPanel;

    /// <summary>スタートボタン押下時にステージ選択へ遷移する。</summary>
    public void on_start_pressed()
    {
        SceneManager.LoadScene(STAGE_SELECT_SCENE);
    }

    /// <summary>HowTo パネルを開く。</summary>
    public void on_how_to_opened()
    {
        set_panel_active(howToPanel, true);
    }

    /// <summary>HowTo パネルを閉じる。</summary>
    public void on_how_to_closed()
    {
        set_panel_active(howToPanel, false);
    }

    /// <summary>設定パネルを開く。</summary>
    public void on_settings_opened()
    {
        set_panel_active(settingsPanel, true);
    }

    /// <summary>設定パネルを閉じる。</summary>
    public void on_settings_closed()
    {
        set_panel_active(settingsPanel, false);
    }

    /// <summary>指定したパネルの表示を切り替える。</summary>
    /// <param name="panel">対象パネル。</param>
    /// <param name="isActive">表示する場合 true。</param>
    void set_panel_active(GameObject panel, bool isActive)
    {
        if (panel)
        {
            panel.SetActive(isActive);
        }
    }
}
