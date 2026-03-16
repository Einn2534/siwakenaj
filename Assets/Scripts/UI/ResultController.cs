using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class ResultController : MonoBehaviour
{
    private const string MainSceneName = "Main";
    private const string StageSelectSceneName = "StageSelect";
    private const string ScoreFormat = "{0}";
    private const string StageFormat = "Stage {0}";
    private const string GameClearLabel = "ゲームクリア";
    private const string GameOverLabel = "ゲームオーバー";

    [SerializeField, FormerlySerializedAs("scoreText")]
    private TMP_Text _scoreText;

    [SerializeField, FormerlySerializedAs("bestScoreText")]
    private TMP_Text _bestScoreText;

    [SerializeField, FormerlySerializedAs("stageText")]
    private TMP_Text _stageText;

    [SerializeField, FormerlySerializedAs("resultText")]
    private TMP_Text _resultText;

    [SerializeField, FormerlySerializedAs("countAText")]
    private TMP_Text _countAText;

    [SerializeField, FormerlySerializedAs("countBText")]
    private TMP_Text _countBText;

    [SerializeField, FormerlySerializedAs("countCText")]
    private TMP_Text _countCText;

    [SerializeField, FormerlySerializedAs("missCountText")]
    private TMP_Text _missCountText;

    [SerializeField, FormerlySerializedAs("clearPanel")]
    private GameObject _clearPanel;

    [SerializeField, FormerlySerializedAs("gameOverPanel")]
    private GameObject _gameOverPanel;

    [SerializeField, FormerlySerializedAs("playerAnimationController")]
    private PlayerAnimationController _playerAnimationController;

    private void Start()
    {
        ApplyResult();
    }

    public void OnRetryPressed()
    {
        SceneManager.LoadScene(MainSceneName);
    }

    public void OnStageSelectPressed()
    {
        SceneManager.LoadScene(StageSelectSceneName);
    }

    private void ApplyResult()
    {
        GameResultData result = SessionState.LastResult ?? GameResultData.Empty(SessionState.SelectedStageNumber);
        SetText(_stageText, string.Format(StageFormat, result.StageNumber));
        SetText(_scoreText, string.Format(ScoreFormat, result.Score));
        SetText(_countAText, string.Format(ScoreFormat, result.LightTruckCount));
        SetText(_countBText, string.Format(ScoreFormat, result.CompactCarCount));
        SetText(_countCText, string.Format(ScoreFormat, result.SportsCarCount));
        SetText(_missCountText, string.Format(ScoreFormat, result.MissCount));
        int bestScore = UpdateBestScore(result.StageNumber, result.Score);

        if (result.IsClear)
        {
            SetText(_resultText, GameClearLabel);
        }
        else
        {
            SetText(_resultText, GameOverLabel);
        }

        SetText(_bestScoreText, string.Format(ScoreFormat, bestScore));
        SetPanelActive(_clearPanel, result.IsClear);
        SetPanelActive(_gameOverPanel, !result.IsClear);

        if (_playerAnimationController != null)
        {
            if (result.IsClear)
            {
                _playerAnimationController.PlayWin();
            }
            else
            {
                _playerAnimationController.PlayCry();
            }
        }
    }

    private static int UpdateBestScore(int stageNumber, int score)
    {
        int currentBest = SaveService.GetBestScore(stageNumber);
        if (score > currentBest)
        {
            SaveService.SetBestScore(stageNumber, score);
            SaveService.Save();
            return score;
        }

        return currentBest;
    }

    private static void SetText(TMP_Text textElement, string value)
    {
        if (textElement != null)
        {
            textElement.text = value;
        }
    }

    private static void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }
}
