// Created: 2025-11-28
// Author: gpt-5.1-codex-max

using UnityEngine;

/// <summary>ゲーム全体の進行と状態遷移を管理する。</summary>
public class GameController : MonoBehaviour
{
    private const int FIRST_STAGE_INDEX = 0;

    [SerializeField]
    StageManager stageManager;

    [SerializeField]
    ScoreManager scoreManager;

    [SerializeField]
    CarSpawner carSpawner;

    [SerializeField]
    PlayerAnimationController playerAnimationController;

    [SerializeField]
    SoundManager soundManager;
    [SerializeField]
    GameState currentState = GameState.Ready;

    void Start()
    {
        start_game();
        Debug.Log("kaisi");
    }

    /// <summary>設定されたステージでゲームを開始する。</summary>
    public void start_game()
    {
        if (currentState != GameState.Ready)
        {
            return;
        }

        load_stage(FIRST_STAGE_INDEX);
        soundManager.play_bgm();
        carSpawner.start_spawning();
        currentState = GameState.Playing;
    }

    /// <summary>失敗時の処理を行い、ゲームを停止する。</summary>
    public void handle_game_over()
    {
        if (currentState == GameState.GameOver)
        {
            return;
        }

        currentState = GameState.GameOver;
        carSpawner.stop_spawning();
        soundManager.play_game_over();
        playerAnimationController.play_cry();
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
        soundManager.play_clear();
        playerAnimationController.play_win();
    }

    /// <summary>指定したステージ番号のパラメーターを読み込む。</summary>
    /// <param name="stageIndex">読み込むステージのインデックス。</param>
    void load_stage(int stageIndex)
    {
        stageManager.apply_stage(stageIndex);
        scoreManager.reset_metrics(stageManager.get_target_score(), stageManager.get_allowed_misses());
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
