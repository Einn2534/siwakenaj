// Created: 2025-11-28
// Updated: 2026-02-26
// Author: Einn

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>ゲーム全体の進行と状態遷移を管理する。</summary>
public class GameController : MonoBehaviour
{
    private const int DEFAULT_STAGE_NUMBER = 1;
    private const float RESULT_PANEL_DELAY_SECONDS = 0.5f;
    private const string RESULT_SCENE = "Result";

    [SerializeField]
    StageManager stageManager;

    [SerializeField]
    ScoreManager scoreManager;

    [SerializeField]
    CarSpawner carSpawner;

    [SerializeField]
    PlayerAnimationController playerAnimationController;

    [SerializeField]
    GameObject resultPanel;

    [SerializeField]
    GameObject gameOverPanel;

    [SerializeField]
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

        int stageNumber = Mathf.Max(DEFAULT_STAGE_NUMBER, GameSession.stageIndex);
        load_stage(stageNumber - 1);
        if (SoundManager.instance)
        {
            SoundManager.instance.play_bgm();
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
        if (SoundManager.instance)
        {
            SoundManager.instance.play_game_over();
        }

        if (playerAnimationController)
        {
            playerAnimationController.play_cry();
        }

        set_result_data(false);
        StartCoroutine(load_result_after_delay());
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
        if (SoundManager.instance)
        {
            SoundManager.instance.play_clear();
        }

        if (playerAnimationController)
        {
            playerAnimationController.play_win();
        }

        set_result_data(true);
        StartCoroutine(load_result_after_delay());
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

    /// <summary>リザルトシーンへ遷移する。</summary>
    /// <returns>コルーチン。</returns>
    IEnumerator load_result_after_delay()
    {
        yield return new WaitForSeconds(RESULT_PANEL_DELAY_SECONDS);
        SceneManager.LoadScene(RESULT_SCENE);
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

    /// <summary>GameSession にリザルトデータを保存する。</summary>
    /// <param name="isClear">クリアなら true。</param>
    void set_result_data(bool isClear)
    {
        int stageNumber = stageManager ? stageManager.get_current_stage_index() + DEFAULT_STAGE_NUMBER : DEFAULT_STAGE_NUMBER;
        int score = scoreManager ? scoreManager.get_current_score() : 0;
        int missCount = scoreManager ? scoreManager.get_miss_count() : 0;
        int countA = scoreManager ? scoreManager.get_correct_count(CarType.LightTruck) : 0;
        int countB = scoreManager ? scoreManager.get_correct_count(CarType.CompactCar) : 0;
        int countC = scoreManager ? scoreManager.get_correct_count(CarType.SportsCar) : 0;

        GameSession.set_result(stageNumber, isClear, score, missCount, countA, countB, countC);
    }
}
