using UnityEngine;
using UnityEngine.UI;

public class TreasureRewardPanelUI : MonoBehaviour
{
    [SerializeField] private NodeContentManager _nodeContentManager;
    [SerializeField] private Text _rewardText;

    public void ShowReward(string rewardText)
    {
        if (_rewardText != null)
        {
            _rewardText.text = rewardText;
        }

        gameObject.SetActive(true);
    }

    public void OnClickClose()
    {
        if (_nodeContentManager != null)
        {
            _nodeContentManager.CloseTreasureReward();
        }
    }
}