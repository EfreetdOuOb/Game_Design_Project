using UnityEngine;

public class BattleResultUI : MonoBehaviour
{
    [SerializeField] private GameObject _victoryPanel;

    public void ShowVictory()
    {
        if (_victoryPanel != null)
            _victoryPanel.SetActive(true);
    }

    public void HideVictory()
    {
        if (_victoryPanel != null)
            _victoryPanel.SetActive(false);
    }

    public void OnClickNext()
    {
        GameFlowController.Instance.ProceedAfterVictory();
    }
}