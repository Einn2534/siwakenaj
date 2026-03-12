using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameFlowController : MonoBehaviour
{
    private const float ResultSceneDelaySeconds = 0.5f;
    private const string ResultSceneName = "Result";
    private const string StageDatabaseResourcePath = "StageDatabase";

    [SerializeField, FormerlySerializedAs("scoreManager")]
    private ScoreManager _scoreManager;

    [SerializeField, FormerlySerializedAs("carSpawner")]
    private CarSpawner _carSpawner;

    [SerializeField, FormerlySerializedAs("playerAnimationController")]
    private PlayerAnimationController _playerAnimationController;

    [SerializeField, FormerlySerializedAs("currentState")]
    private GameState _currentState = GameState.Ready;

    private JudgeController _judgeController;
    private StageDatabase _stageDatabase;
    private StageDefinition _currentStageDefinition;

    private void Awake()
    {
        _judgeController = FindAnyObjectByType<JudgeController>();
        _stageDatabase = Resources.Load<StageDatabase>(StageDatabaseResourcePath);
    }

    private void OnEnable()
    {
        if (_carSpawner != null)
        {
            _carSpawner.CarMissed += OnCarMissed;
        }
    }

    private void OnDisable()
    {
        if (_carSpawner != null)
        {
            _carSpawner.CarMissed -= OnCarMissed;
        }
    }

    private void Start()
    {
        StartGame();
    }

    public bool IsPlaying()
    {
        return _currentState == GameState.Playing;
    }

    public void StartGame()
    {
        if (_currentState != GameState.Ready)
        {
            return;
        }

        if (_scoreManager == null || _carSpawner == null || _stageDatabase == null)
        {
            return;
        }

        int selectedStageNumber = Mathf.Max(1, SessionState.SelectedStageNumber);
        _currentStageDefinition = _stageDatabase.GetStageDefinition(selectedStageNumber);
        SessionState.SelectStage(_currentStageDefinition.StageNumber);

        _scoreManager.Initialize(_currentStageDefinition);
        _carSpawner.Initialize(_currentStageDefinition);
        _carSpawner.StartSpawning();
        SoundManager.Instance?.PlayBgm();
        _currentState = GameState.Playing;
    }

    public void HandleLaneInput(CarType laneType)
    {
        if (!IsPlaying())
        {
            return;
        }

        CarController activeCar = _carSpawner != null ? _carSpawner.GetActiveCar() : null;
        JudgeResult result = _judgeController != null
            ? _judgeController.Evaluate(activeCar, laneType)
            : JudgeEvaluator.Evaluate(activeCar != null ? activeCar.CarType : null, laneType);

        switch (result)
        {
            case JudgeResult.Correct:
                _scoreManager.ApplySuccess(laneType);
                _playerAnimationController?.PlayHappy();
                SoundManager.Instance?.PlayCorrect();
                _carSpawner?.DespawnCar(activeCar);
                break;
            case JudgeResult.WrongLane:
            case JudgeResult.NoCar:
                _scoreManager.ApplyMiss();
                _playerAnimationController?.PlayCry();
                SoundManager.Instance?.PlayMiss();
                break;
        }

        EvaluateCompletion();
    }

    private void OnCarMissed(CarController car)
    {
        if (!IsPlaying())
        {
            return;
        }

        _scoreManager.ApplyMiss();
        EvaluateCompletion();
    }

    private void EvaluateCompletion()
    {
        if (_scoreManager == null)
        {
            return;
        }

        if (_scoreManager.HasReachedMissLimit)
        {
            HandleGameOver();
            return;
        }

        if (_scoreManager.HasReachedTargetScore)
        {
            FinishStage();
        }
    }

    private void HandleGameOver()
    {
        if (_currentState != GameState.Playing)
        {
            return;
        }

        _currentState = GameState.GameOver;
        StopGameplay();
        SoundManager.Instance?.PlayGameOver();
        _playerAnimationController?.PlayCry();
        StoreResult(false);
        StartCoroutine(LoadResultAfterDelay());
    }

    private void FinishStage()
    {
        if (_currentState != GameState.Playing)
        {
            return;
        }

        _currentState = GameState.Result;
        StopGameplay();
        SoundManager.Instance?.PlayClear();
        _playerAnimationController?.PlayWin();
        StoreResult(true);
        StartCoroutine(LoadResultAfterDelay());
    }

    private void StopGameplay()
    {
        _carSpawner?.StopSpawning();
        _carSpawner?.StopAllCars();
    }

    private void StoreResult(bool isClear)
    {
        int stageNumber = _currentStageDefinition != null ? _currentStageDefinition.StageNumber : SessionState.SelectedStageNumber;
        SessionState.StoreResult(GameResultData.FromScoreState(stageNumber, isClear, _scoreManager?.State));
    }

    private IEnumerator LoadResultAfterDelay()
    {
        yield return new WaitForSeconds(ResultSceneDelaySeconds);
        SceneManager.LoadScene(ResultSceneName);
    }
}
