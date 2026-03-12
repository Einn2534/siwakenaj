using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class StageSelectController : MonoBehaviour
{
    private const string MainSceneName = "Main";
    private const string TitleSceneName = "Title";
    private const string StatusUnlocked = "Unlocked";
    private const string StatusLocked = "Locked";
    private const string StatusComingSoon = "ComingSoon";
    private const string StageDatabaseResourcePath = "StageDatabase";

    [SerializeField, FormerlySerializedAs("swipeSnapController")]
    private SwipeSnapController _swipeSnapController;

    [SerializeField, FormerlySerializedAs("stageCardViews")]
    private StageCardView[] _stageCardViews;

    [SerializeField, FormerlySerializedAs("playButton")]
    private Button _playButton;

    private StageDatabase _stageDatabase;
    private int _selectedStageNumber = 1;

    private IEnumerator Start()
    {
        _stageDatabase = Resources.Load<StageDatabase>(StageDatabaseResourcePath);

        if (_swipeSnapController != null)
        {
            _swipeSnapController.OnPageChanged += OnSelectionChanged;
        }

        UpdateCards();
        yield return null;

        int lastStageNumber = SaveService.GetLastStage();
        int startIndex = _stageDatabase != null ? _stageDatabase.GetStageIndex(lastStageNumber) : 0;
        _swipeSnapController?.JumpToIndex(startIndex);
        ApplySelectionIndex(startIndex);
    }

    private void OnDestroy()
    {
        if (_swipeSnapController != null)
        {
            _swipeSnapController.OnPageChanged -= OnSelectionChanged;
        }
    }

    public void OnSelectionChanged(int index)
    {
        ApplySelectionIndex(index);
    }

    public void OnPlayPressed()
    {
        if (!CanPlaySelectedStage())
        {
            return;
        }

        SessionState.SelectStage(_selectedStageNumber);
        SaveService.SetLastStage(_selectedStageNumber);
        SaveService.Save();
        SceneManager.LoadScene(MainSceneName);
    }

    public void OnBackPressed()
    {
        SceneManager.LoadScene(TitleSceneName);
    }

    public int GetSelectedStageNumber()
    {
        return _selectedStageNumber;
    }

    private void ApplySelectionIndex(int index)
    {
        if (_stageDatabase == null || _stageDatabase.Stages.Count == 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(index, 0, _stageDatabase.Stages.Count - 1);
        StageDefinition stageDefinition = _stageDatabase.Stages[clampedIndex];
        _selectedStageNumber = stageDefinition.StageNumber;
        SaveService.SetLastStage(_selectedStageNumber);
        SaveService.Save();
        UpdateCards();

        if (_playButton != null)
        {
            _playButton.interactable = CanPlaySelectedStage();
        }
    }

    private void UpdateCards()
    {
        if (_stageDatabase == null || _stageCardViews == null)
        {
            return;
        }

        int count = Mathf.Min(_stageDatabase.Stages.Count, _stageCardViews.Length);
        for (int i = 0; i < count; i += 1)
        {
            StageDefinition stageDefinition = _stageDatabase.Stages[i];
            int bestScore = SaveService.GetBestScore(stageDefinition.StageNumber);
            bool isUnlocked = _stageDatabase.IsStageUnlocked(i, SaveService.GetBestScore);
            _stageCardViews[i].SetData(
                stageDefinition.StageNumber,
                stageDefinition.TargetScore,
                bestScore,
                GetStatusLabel(stageDefinition, isUnlocked));
        }
    }

    private bool CanPlaySelectedStage()
    {
        if (_stageDatabase == null || _stageDatabase.Stages.Count == 0)
        {
            return false;
        }

        int index = _stageDatabase.GetStageIndex(_selectedStageNumber);
        if (index < 0 || index >= _stageDatabase.Stages.Count)
        {
            return false;
        }

        StageDefinition stageDefinition = _stageDatabase.Stages[index];
        return stageDefinition != null
            && stageDefinition.IsImplemented
            && _stageDatabase.IsStageUnlocked(index, SaveService.GetBestScore);
    }

    private static string GetStatusLabel(StageDefinition stageDefinition, bool isUnlocked)
    {
        if (stageDefinition == null || !stageDefinition.IsImplemented)
        {
            return StatusComingSoon;
        }

        return isUnlocked ? StatusUnlocked : StatusLocked;
    }
}
