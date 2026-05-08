using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class StageSelectController : MonoBehaviour
{
    private const string MainSceneName = "Main";
    private const string TitleSceneName = "Title";
    private const string StageDatabaseResourcePath = "StageDatabase";

    [SerializeField, FormerlySerializedAs("swipeSnapController")]
    private SwipeSnapController _swipeSnapController;

    [SerializeField, FormerlySerializedAs("stageCardViews")]
    private StageCardView[] _stageCardViews;

    [SerializeField]
    private RectTransform _stageCardContainer;

    [SerializeField]
    private StageCardView _stageCardPrefab;

    [SerializeField, FormerlySerializedAs("playButton")]
    private Button _playButton;

    private StageDatabase _stageDatabase;
    private StageCardView _stageCardTemplate;
    private int _selectedStageNumber = 1;

    private void OnEnable()
    {
        if (_stageDatabase != null)
        {
            UpdateCards();
        }
    }

    private IEnumerator Start()
    {
        SoundManager.EnsureInstance().PlayStageSelectBgm();

        _stageDatabase = Resources.Load<StageDatabase>(StageDatabaseResourcePath);
        EnsureStageCardViews();

        if (_swipeSnapController != null)
        {
            _swipeSnapController.OnPageChanged += OnSelectionChanged;
        }

        UpdateCards();
        yield return null;

        int lastStageNumber = SaveService.GetLastStage();
        int startIndex = GetInitialStageIndex(lastStageNumber);
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
        SaveService.SetSelectedStage(_selectedStageNumber);
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
        _selectedStageNumber = GetDisplayStageNumber(stageDefinition, clampedIndex);
        UpdateCards();

        if (CanPlaySelectedStage())
        {
            SaveService.SetLastStage(_selectedStageNumber);
            SaveService.Save();
        }

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
            int stageNumber = GetDisplayStageNumber(stageDefinition, i);
            bool isUnlocked = _stageDatabase.IsStageUnlocked(i, SaveService.GetBestScore);
            StageCardStatus cardStatus = GetCardStatus(stageDefinition, isUnlocked);
            int bestScore = cardStatus == StageCardStatus.Unlocked ? SaveService.GetBestScore(stageNumber) : 0;
            int starRating = cardStatus == StageCardStatus.Unlocked ? SaveService.GetStarRating(stageNumber) : 0;
            _stageCardViews[i].SetData(
                stageNumber,
                stageDefinition != null ? stageDefinition.TargetScore : 0,
                bestScore,
                cardStatus,
                starRating,
                cardStatus == StageCardStatus.Locked ? GetRequiredStageNumber(i) : 0);
            _stageCardViews[i].SetSelected(stageNumber == _selectedStageNumber);
        }
    }

    private void EnsureStageCardViews()
    {
        if (_stageDatabase == null || !ResolveStageCardContainer())
        {
            return;
        }

        List<StageCardView> cardViews = GetOrderedStageCardViews();
        ResolveStageCardTemplate(cardViews);
        if (cardViews.Count == 0 && _stageCardTemplate == null && _stageCardPrefab == null)
        {
            return;
        }

        int requiredCount = _stageDatabase.Stages.Count;
        while (cardViews.Count < requiredCount)
        {
            StageCardView clone = CreateStageCard(cardViews.Count + 1);
            if (clone == null)
            {
                break;
            }

            cardViews.Add(clone);
        }

        List<StageCardView> activeCardViews = new List<StageCardView>(requiredCount);
        for (int i = 0; i < cardViews.Count; i += 1)
        {
            bool shouldBeActive = i < requiredCount;
            StageCardView cardView = cardViews[i];
            cardView.gameObject.SetActive(shouldBeActive);

            if (!shouldBeActive)
            {
                continue;
            }

            cardView.transform.SetSiblingIndex(activeCardViews.Count);
            activeCardViews.Add(cardView);
        }

        _stageCardViews = activeCardViews.ToArray();
        _swipeSnapController?.Refresh();
    }

    private bool ResolveStageCardContainer()
    {
        if (_stageCardContainer != null)
        {
            return true;
        }

        if (_stageCardViews != null)
        {
            foreach (StageCardView cardView in _stageCardViews)
            {
                if (cardView == null)
                {
                    continue;
                }

                _stageCardContainer ??= cardView.transform.parent as RectTransform;
                if (_stageCardContainer != null)
                {
                    return true;
                }
            }
        }

        _stageCardContainer ??= _swipeSnapController != null ? _swipeSnapController.GetContent() : null;
        return _stageCardContainer != null;
    }

    private List<StageCardView> GetOrderedStageCardViews()
    {
        List<StageCardView> cardViews = new List<StageCardView>();
        if (_stageCardContainer == null)
        {
            return cardViews;
        }

        for (int i = 0; i < _stageCardContainer.childCount; i += 1)
        {
            StageCardView cardView = _stageCardContainer.GetChild(i).GetComponent<StageCardView>();
            if (cardView != null)
            {
                cardViews.Add(cardView);
            }
        }

        return cardViews;
    }

    private void ResolveStageCardTemplate(List<StageCardView> cardViews)
    {
        if (_stageCardTemplate != null)
        {
            return;
        }

        if (cardViews.Count > 0)
        {
            _stageCardTemplate = cardViews[0];
            return;
        }

        if (_stageCardContainer != null)
        {
            _stageCardTemplate = _stageCardContainer.GetComponentInChildren<StageCardView>(true);
        }
    }

    private StageCardView CreateStageCard(int cardNumber)
    {
        StageCardView source = _stageCardPrefab != null ? _stageCardPrefab : _stageCardTemplate;
        if (source == null || _stageCardContainer == null)
        {
            return null;
        }

        StageCardView clone = Instantiate(source, _stageCardContainer, false);
        clone.name = $"{source.name.Trim()} ({cardNumber})";
        clone.gameObject.SetActive(true);
        clone.transform.SetAsLastSibling();
        return clone;
    }

    private bool CanPlaySelectedStage()
    {
        if (_stageDatabase == null || _stageDatabase.Stages.Count == 0)
        {
            return false;
        }

        if (!_stageDatabase.TryGetStageIndex(_selectedStageNumber, out int index))
        {
            return false;
        }

        return IsPlayableStage(index);
    }

    private int GetInitialStageIndex(int preferredStageNumber)
    {
        if (_stageDatabase == null || _stageDatabase.Stages.Count == 0)
        {
            return 0;
        }

        if (_stageDatabase.TryGetStageIndex(preferredStageNumber, out int preferredIndex) && IsPlayableStage(preferredIndex))
        {
            return preferredIndex;
        }

        for (int i = 0; i < _stageDatabase.Stages.Count; i += 1)
        {
            if (IsPlayableStage(i))
            {
                return i;
            }
        }

        return 0;
    }

    private bool IsPlayableStage(int stageIndex)
    {
        if (_stageDatabase == null || stageIndex < 0 || stageIndex >= _stageDatabase.Stages.Count)
        {
            return false;
        }

        StageDefinition stageDefinition = _stageDatabase.Stages[stageIndex];
        return stageDefinition != null
            && stageDefinition.IsImplemented
            && _stageDatabase.IsStageUnlocked(stageIndex, SaveService.GetBestScore);
    }

    private static StageCardStatus GetCardStatus(StageDefinition stageDefinition, bool isUnlocked)
    {
        if (stageDefinition == null || !stageDefinition.IsImplemented)
        {
            return StageCardStatus.ComingSoon;
        }

        return isUnlocked ? StageCardStatus.Unlocked : StageCardStatus.Locked;
    }

    private int GetRequiredStageNumber(int stageIndex)
    {
        return _stageDatabase != null ? _stageDatabase.GetRequiredClearStageNumber(stageIndex) : 0;
    }

    private static int GetDisplayStageNumber(StageDefinition stageDefinition, int stageIndex)
    {
        if (stageDefinition != null)
        {
            return Mathf.Max(1, stageDefinition.StageNumber);
        }

        return Mathf.Max(1, stageIndex + 1);
    }
}
