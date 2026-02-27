// Created: 2025-02-14
// Updated: 2026-02-26
// Author: Einn

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>リザルト画面のスコアやボタン動作を管理する。</summary>
public class ResultController : MonoBehaviour
{
    private const string MAIN_SCENE = "Main";
    private const string STAGE_SELECT_SCENE = "StageSelect";
    private const string SCORE_FORMAT = "{0}";
    private const string STAGE_FORMAT = "Stage {0}";
    private const string GAME_CLEAR_LABEL = "ゲームクリア！";
    private const string GAME_OVER_LABEL = "ゲームオーバー…";

    [SerializeField]
    TMPro.TMP_Text scoreText;

    [SerializeField]
    TMPro.TMP_Text bestScoreText;

    [SerializeField]
    TMPro.TMP_Text stageText;

    [SerializeField]
    TMPro.TMP_Text resultText;

    [SerializeField]
    TMPro.TMP_Text countAText;

    [SerializeField]
    TMPro.TMP_Text countBText;

    [SerializeField]
    TMPro.TMP_Text countCText;

    [SerializeField]
    TMPro.TMP_Text missCountText;

    [SerializeField]
    GameObject clearPanel;

    [SerializeField]
    GameObject gameOverPanel;

    [SerializeField]
    PlayerAnimationController playerAnimationController;

    /// <summary>開始時にリザルトデータを反映する。</summary>
    void Start()
    {
        apply_result();
    }

    /// <summary>リトライボタンが押されたらメインへ遷移する。</summary>
    public void on_retry_pressed()
    {
        SceneManager.LoadScene(MAIN_SCENE);
    }

    /// <summary>ステージ選択ボタンが押されたらステージ選択へ遷移する。</summary>
    public void on_stage_select_pressed()
    {
        SceneManager.LoadScene(STAGE_SELECT_SCENE);
    }

    /// <summary>GameSession のリザルトデータを画面に反映する。</summary>
    void apply_result()
    {
        int stageNumber = GameSession.stageIndex;
        bool isClear = GameSession.isClear;
        int score = GameSession.score;
        int missCount = GameSession.missCount;
        int countA = GameSession.correctCountA;
        int countB = GameSession.correctCountB;
        int countC = GameSession.correctCountC;

        set_text(stageText, string.Format(STAGE_FORMAT, stageNumber));
        set_text(scoreText, string.Format(SCORE_FORMAT, score));
        set_text(countAText, string.Format(SCORE_FORMAT, countA));
        set_text(countBText, string.Format(SCORE_FORMAT, countB));
        set_text(countCText, string.Format(SCORE_FORMAT, countC));
        set_text(missCountText, string.Format(SCORE_FORMAT, missCount));

        if (isClear)
        {
            set_text(resultText, GAME_CLEAR_LABEL);
            update_best_score(stageNumber, score);
        }
        else
        {
            set_text(resultText, GAME_OVER_LABEL);
        }

        int bestScore = SaveService.get_best_score(stageNumber);
        set_text(bestScoreText, string.Format(SCORE_FORMAT, bestScore));

        set_panel_active(clearPanel, isClear);
        set_panel_active(gameOverPanel, !isClear);

        if (playerAnimationController)
        {
            if (isClear)
            {
                playerAnimationController.play_win();
            }
            else
            {
                playerAnimationController.play_cry();
            }
        }
    }

    /// <summary>今回のスコアがベストなら更新する。</summary>
    /// <param name="stageNumber">1 から始まるステージ番号。</param>
    /// <param name="score">今回スコア。</param>
    void update_best_score(int stageNumber, int score)
    {
        int currentBest = SaveService.get_best_score(stageNumber);
        if (score > currentBest)
        {
            SaveService.set_best_score(stageNumber, score);
            SaveService.save();
        }
    }

    /// <summary>テキスト要素に文字列を設定する。</summary>
    /// <param name="textElement">対象のテキスト要素。</param>
    /// <param name="value">設定する文字列。</param>
    void set_text(TMPro.TMP_Text textElement, string value)
    {
        if (textElement)
        {
            textElement.text = value;
        }
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
