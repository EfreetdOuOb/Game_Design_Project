using UnityEngine;

public class BattleResultUI : MonoBehaviour
{
    [SerializeField] private GameObject _victoryPanel;
    [SerializeField] private GameObject _defeatPanel;

    public void ShowVictory()
    {
        if (_defeatPanel != null)
        {
            _defeatPanel.SetActive(false);
        }

        if (_victoryPanel != null)
        {
            _victoryPanel.SetActive(true);
        }
    }

    public void HideVictory()
    {
        if (_victoryPanel != null)
        {
            _victoryPanel.SetActive(false);
        }
    }

    public void ShowDefeat()
    {
        if (_victoryPanel != null)
        {
            _victoryPanel.SetActive(false);
        }

        if (_defeatPanel != null)
        {
            _defeatPanel.SetActive(true);
        }
    }

    public void HideDefeat()
    {
        if (_defeatPanel != null)
        {
            _defeatPanel.SetActive(false);
        }
    }

    public void OnClickNext()
    {
        GameFlowController.Instance?.ProceedAfterVictory();
    }

    public void OnClickRestart()
    {
        GameFlowController.Instance?.RestartRun();
    }
}
