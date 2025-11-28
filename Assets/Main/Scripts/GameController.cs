// Created: 2025-11-28
// Author: gpt-5.1-codex-max

using UnityEngine;

/// <summary>Controls overall game flow and transitions between states.</summary>
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

    GameState currentState = GameState.Ready;

    /// <summary>Begins gameplay for the configured stage.</summary>
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

    /// <summary>Handles player failure and stops the game.</summary>
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

    /// <summary>Completes the current stage when the goal is met.</summary>
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

    /// <summary>Loads parameters for a given stage index.</summary>
    /// <param name="stageIndex">Index of the stage to load.</param>
    void load_stage(int stageIndex)
    {
        stageManager.apply_stage(stageIndex);
        scoreManager.reset_metrics(stageManager.get_target_score(), stageManager.get_allowed_misses());
    }
}

/// <summary>Enumerates the lifecycle phases of the game.</summary>
public enum GameState
{
    Ready,
    Playing,
    Result,
    GameOver
}
