using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class StageCardView : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("stageNumberText")]
    private TMP_Text _stageNumberText;

    [SerializeField, FormerlySerializedAs("targetScoreText")]
    private TMP_Text _targetScoreText;

    [SerializeField, FormerlySerializedAs("bestScoreText")]
    private TMP_Text _bestScoreText;

    [SerializeField, FormerlySerializedAs("statusText")]
    private TMP_Text _statusText;

    public void SetData(int stageNumber, int targetScore, int bestScore, string status)
    {
        if (_stageNumberText != null)
        {
            _stageNumberText.text = stageNumber.ToString();
        }

        if (_targetScoreText != null)
        {
            _targetScoreText.text = targetScore.ToString();
        }

        if (_bestScoreText != null)
        {
            _bestScoreText.text = bestScore.ToString();
        }

        if (_statusText != null)
        {
            _statusText.text = status ?? string.Empty;
        }
    }
}
