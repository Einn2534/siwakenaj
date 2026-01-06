// Created: 2025-11-28
// Updated: 2025-12-01
// Author: gpt-5.1-codex-max

using System.Collections;
using UnityEngine;

/// <summary>ゲーム全体の進行と状態遷移を管理する。</summary>
public class GameController : MonoBehaviour
{
    private const int FIRST_STAGE_INDEX = 0;
    private const float RESULT_PANEL_DELAY_SECONDS = 0.5f;

    [SerializeField]
    // ステージ設定管理用。
    StageManager stageManager;

    [SerializeField]
    // スコア管理用。
    ScoreManager scoreManager;

    [SerializeField]
    // 車両スポーン管理用。
    CarSpawner carSpawner;

    [SerializeField]
    // プレイヤーアニメ制御用。
    PlayerAnimationController playerAnimationController;

    [SerializeField]
    // サウンド制御用。
    SoundManager soundManager;

    [SerializeField]
    // リザルト表示パネル。
    GameObject resultPanel;

    [SerializeField]
    // ゲームオーバー表示パネル。
    GameObject gameOverPanel;

    [SerializeField]
    // 現在のゲーム状態。
    GameState currentState = GameState.Ready;

    /// <summary>初回起動時にゲームを開始する。</summary>
    void Start()
    {
        start_game();
    }

    /// <summary>設定されたステージでゲームを開始する。</summary>
    public void start_game()
    {
        if (currentState != GameState.Ready)
        {
            return;
        }

        if (!stageManager || !scoreManager || !carSpawner)
        {
            return;
        }

        set_panel_active(resultPanel, false);
        set_panel_active(gameOverPanel, false);

        load_stage(FIRST_STAGE_INDEX);
        if (soundManager)
        {
            soundManager.play_bgm();
        }

        carSpawner.start_spawning();
        currentState = GameState.Playing;
    }

    /// <summary>失敗時の処理を行い、ゲームを停止する。</summary>
    public void handle_game_over()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        currentState = GameState.GameOver;
        carSpawner.stop_spawning();
        carSpawner.stop_all_cars();
        if (soundManager)
        {
            soundManager.play_game_over();
        }

        if (playerAnimationController)
        {
            playerAnimationController.play_cry();
        }

        StartCoroutine(show_panel_after_delay(gameOverPanel));
    }

    /// <summary>目標達成時に現在のステージをクリアする。</summary>
    public void finish_stage()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        currentState = GameState.Result;
        carSpawner.stop_spawning();
        carSpawner.stop_all_cars();
        if (soundManager)
        {
            soundManager.play_clear();
        }

        if (playerAnimationController)
        {
            playerAnimationController.play_win();
        }

        StartCoroutine(show_panel_after_delay(resultPanel));
    }

    /// <summary>現在のゲーム状態がプレイ中かどうか。</summary>
    /// <returns>プレイ中なら true。</returns>
    public bool is_playing()
    {
        return currentState == GameState.Playing;
    }

    /// <summary>指定したステージ番号のパラメーターを読み込む。</summary>
    /// <param name="stageIndex">読み込むステージのインデックス。</param>
    void load_stage(int stageIndex)
    {
        stageManager.apply_stage(stageIndex);
        StageConfig stageConfig = stageManager.get_stage_config();
        scoreManager.reset_metrics(stageConfig.targetScore, stageConfig.missLimit);
        carSpawner.apply_stage_config(stageConfig);
    }

    /// <summary>指定したパネルを遅延表示する。</summary>
    /// <param name="panel">表示対象のパネル。</param>
    /// <returns>コルーチン。</returns>
    IEnumerator show_panel_after_delay(GameObject panel)
    {
        if (panel == null)
        {
            yield break;
        }

        yield return new WaitForSeconds(RESULT_PANEL_DELAY_SECONDS);
        set_panel_active(panel, true);
    }

    /// <summary>パネルの表示状態を切り替える。</summary>
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

/// <summary>ゲームのライフサイクル段階を表す。</summary>
public enum GameState
{
    Ready,
    Playing,
    Result,
    GameOver
}
