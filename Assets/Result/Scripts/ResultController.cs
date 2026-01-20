// Created: 2025-02-14
// Author: gpt-5.2-codex

using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>リザルト画面の表示とボタン処理を管理する。</summary>
public class ResultController : MonoBehaviour
{
    private const string MAIN_SCENE = "Main";
    private const string TITLE_SCENE = "Title";
    private const string CLEAR_TITLE = "Clear";
    private const string GAME_OVER_TITLE = "GameOver";
    private const string NEXT_COMING_SOON_TEXT = "準備中";
    private const int DEFAULT_STAGE_NUMBER = 1;

    [SerializeField]
    TMP_Text headerText;

    [SerializeField]
    TMP_Text scoreText;

    [SerializeField]
    TMP_Text bestScoreText;

    [SerializeField]
    TMP_Text correctCountAText;

    [SerializeField]
    TMP_Text correctCountBText;

    [SerializeField]
    TMP_Text correctCountCText;

    [SerializeField]
    TMP_Text missCountText;

    [SerializeField]
    TMP_Text nextButtonText;

    [SerializeField]
    Button nextButton;

    [SerializeField]
    PlayerAnimationController playerAnimationController;

    [SerializeField]
    int totalStageCount = DEFAULT_STAGE_NUMBER;

    int currentStageNumber = DEFAULT_STAGE_NUMBER;

    /// <summary>開始時にリザルト表示を更新する。</summary>
    void Start()
    {
        apply_result();
    }

    /// <summary>リトライボタン押下時に同じステージを開始する。</summary>
    public void on_retry_pressed()
    {
        GameSession.set_stage(currentStageNumber);
        SceneManager.LoadScene(MAIN_SCENE);
    }

    /// <summary>次のステージへ進む。</summary>
    public void on_next_pressed()
    {
        if (!can_go_next())
        {
            return;
        }

        GameSession.set_stage(currentStageNumber + 1);
        SceneManager.LoadScene(MAIN_SCENE);
    }

    /// <summary>タイトルへ戻る。</summary>
    public void on_home_pressed()
    {
        SceneManager.LoadScene(TITLE_SCENE);
    }

    /// <summary>リザルト内容を反映する。</summary>
    void apply_result()
    {
        currentStageNumber = Mathf.Max(DEFAULT_STAGE_NUMBER, GameSession.stageIndex);
        set_header(GameSession.isClear);
        update_score_texts();
        update_best_score();
        update_counts();
        update_next_button();
        play_result_animation();
    }

    /// <summary>見出しテキストを更新する。</summary>
    /// <param name="isClear">クリアなら true。</param>
    void set_header(bool isClear)
    {
        if (headerText)
        {
            headerText.text = isClear ? CLEAR_TITLE : GAME_OVER_TITLE;
        }
    }

    /// <summary>スコア表示を更新する。</summary>
    void update_score_texts()
    {
        if (scoreText)
        {
            scoreText.text = GameSession.score.ToString();
        }
    }

    /// <summary>ベストスコアを更新し表示に反映する。</summary>
    void update_best_score()
    {
        int bestScore = SaveService.get_best_score(currentStageNumber);
        if (GameSession.score > bestScore)
        {
            bestScore = GameSession.score;
            SaveService.set_best_score(currentStageNumber, bestScore);
            SaveService.save();
        }

        if (bestScoreText)
        {
            bestScoreText.text = bestScore.ToString();
        }
    }

    /// <summary>車種別正解数とミス回数を反映する。</summary>
    void update_counts()
    {
        if (correctCountAText)
        {
            correctCountAText.text = GameSession.correctCountA.ToString();
        }

        if (correctCountBText)
        {
            correctCountBText.text = GameSession.correctCountB.ToString();
        }

        if (correctCountCText)
        {
            correctCountCText.text = GameSession.correctCountC.ToString();
        }

        if (missCountText)
        {
            missCountText.text = GameSession.missCount.ToString();
        }
    }

    /// <summary>Next ボタンの状態を更新する。</summary>
    void update_next_button()
    {
        bool canMoveNext = can_go_next();
        if (nextButton)
        {
            nextButton.interactable = canMoveNext;
        }

        if (nextButtonText && !canMoveNext)
        {
            nextButtonText.text = NEXT_COMING_SOON_TEXT;
        }
    }

    /// <summary>次ステージへ進めるか判定する。</summary>
    /// <returns>進めるなら true。</returns>
    bool can_go_next()
    {
        return currentStageNumber + 1 <= Mathf.Max(DEFAULT_STAGE_NUMBER, totalStageCount);
    }

    /// <summary>結果に応じたアニメーションを再生する。</summary>
    void play_result_animation()
    {
        if (!playerAnimationController)
        {
            return;
        }

        if (GameSession.isClear)
        {
            playerAnimationController.play_win();
        }
        else
        {
            playerAnimationController.play_cry();
        }
    }
}
