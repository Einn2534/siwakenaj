// Created: 2025-11-28
// Updated: 2025-12-01
// Author: gpt-5.1-codex-max

using System.Collections.Generic;
using UnityEngine;

/// <summary>スコア・ミス回数・車種別正解数を集計する。</summary>
public class ScoreManager : MonoBehaviour
{
    private const int SCORE_PER_CORRECT = 10;
    private const int SCORE_PER_MISS = -5;
    private const int SCORE_MIN = 0;

    [SerializeField]
    // クリアに必要な目標スコア。
    int targetScore;

    [SerializeField]
    // 許容ミス回数。
    int allowedMisses = 3;

    [SerializeField]
    // レーンUI表示用。
    ScoreLaneUI scoreLaneUi;

    [SerializeField]
    // 現在スコア。
    int currentScore;

    // 累計ミス回数。
    int missCount;
    // 車種ごとの正解数。
    readonly Dictionary<CarType, int> laneCounts = new();
    // ゲーム状態参照。
    GameController controller;

    /// <summary>ゲームコントローラー参照を初期化する。</summary>
    void Awake()
    {
        controller = FindObjectOfType<GameController>();
    }

    /// <summary>ステージ開始時にスコアとミス回数を初期化する。</summary>
    /// <param name="stageTargetScore">クリアに必要なスコア。</param>
    /// <param name="maxMisses">許容ミス回数。</param>
    public void reset_metrics(int stageTargetScore, int maxMisses)
    {
        targetScore = stageTargetScore;
        allowedMisses = maxMisses;
        currentScore = 0;
        missCount = 0;
        laneCounts.Clear();
        if (scoreLaneUi)
        {
            scoreLaneUi.ResetAll();
        }
    }

    /// <summary>正しく仕分けられた際の処理。</summary>
    /// <param name="laneType">正解となった車種。</param>
    public void apply_success(CarType laneType)
    {
        if (!is_playing())
        {
            return;
        }

        currentScore += SCORE_PER_CORRECT;
        increment_lane(laneType);
        if (scoreLaneUi)
        {
            scoreLaneUi.UpdateLane(laneType, laneCounts[laneType]);
        }

        check_clear();
    }

    /// <summary>ミス時の処理とゲームオーバー判定。</summary>
    public void apply_miss()
    {
        if (!is_playing())
        {
            return;
        }

        currentScore += SCORE_PER_MISS;
        currentScore = Mathf.Max(currentScore, SCORE_MIN);
        missCount += 1;

        if (missCount >= allowedMisses)
        {
            GameController activeController = get_controller();
            if (activeController)
            {
                activeController.handle_game_over();
            }
        }
    }

    /// <summary>現在スコアが目標に達したか確認する。</summary>
    void check_clear()
    {
        if (currentScore >= targetScore)
        {
            GameController activeController = get_controller();
            if (activeController)
            {
                activeController.finish_stage();
            }
        }
    }

    /// <summary>車種別の正解数を加算する。</summary>
    /// <param name="laneType">加算対象の車種。</param>
    void increment_lane(CarType laneType)
    {
        if (!laneCounts.ContainsKey(laneType))
        {
            laneCounts[laneType] = 0;
        }

        laneCounts[laneType] += 1;
    }

    /// <summary>現在のミス回数を取得する。</summary>
    /// <returns>累計ミス回数。</returns>
    public int get_miss_count()
    {
        return missCount;
    }

    /// <summary>現在のスコアを取得する。</summary>
    /// <returns>現在スコア。</returns>
    public int get_current_score()
    {
        return currentScore;
    }

    /// <summary>指定した車種の正解数を取得する。</summary>
    /// <param name="laneType">取得対象の車種。</param>
    /// <returns>正解数。</returns>
    public int get_correct_count(CarType laneType)
    {
        if (laneCounts.TryGetValue(laneType, out int count))
        {
            return count;
        }

        return 0;
    }

    /// <summary>ゲームコントローラーの参照を取得し、存在しない場合は再検索する。</summary>
    /// <returns>現在のゲームコントローラー。</returns>
    GameController get_controller()
    {
        if (!controller)
        {
            controller = FindObjectOfType<GameController>();
        }

        return controller;
    }

    /// <summary>ゲームがプレイ中かどうかを確認する。</summary>
    /// <returns>プレイ中なら true。</returns>
    bool is_playing()
    {
        GameController activeController = get_controller();
        return activeController != null && activeController.is_playing();
    }
}
