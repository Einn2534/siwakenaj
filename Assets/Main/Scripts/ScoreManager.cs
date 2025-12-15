// Created: 2025-11-28
// Author: gpt-5.1-codex-max

using System.Collections.Generic;
using UnityEngine;

/// <summary>スコア・ミス回数・車種別正解数を集計する。</summary>
public class ScoreManager : MonoBehaviour
{
    private const int SCORE_PER_CORRECT = 10;
    private const int SCORE_PER_MISS = -5;

    [SerializeField]
    int targetScore;

    [SerializeField]
    int allowedMisses = 3;

    [SerializeField]
    ScoreLaneUI scoreLaneUi;
    
    [SerializeField]
    int currentScore;
    int missCount;
    readonly Dictionary<CarType, int> laneCounts = new();
    GameController controller;

    /// <summary>ゲームコントローラー参照を初期化する。</summary>
    void Awake()
    {
        controller = FindFirstObjectByType<GameController>();
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
        scoreLaneUi.reset_all();
    }

    /// <summary>正しく仕分けられた際の処理。</summary>
    /// <param name="laneType">正解となった車種。</param>
    public void apply_success(CarType laneType)
    {
        currentScore += SCORE_PER_CORRECT;
        increment_lane(laneType);
        scoreLaneUi.update_lane(laneType, laneCounts[laneType]);
        check_clear();
    }

    /// <summary>ミス時の処理とゲームオーバー判定。</summary>
    public void apply_miss()
    {
        currentScore += SCORE_PER_MISS;
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

    /// <summary>ゲームコントローラーの参照を取得し、存在しない場合は再検索する。</summary>
    /// <returns>現在のゲームコントローラー。</returns>
    GameController get_controller()
    {
        if (!controller)
        {
            controller = FindFirstObjectByType<GameController>();
        }

        return controller;
    }
}
