using UnityEngine;
using UnityEngine.UI;

public class RestNodePanelUI : MonoBehaviour
{
    [SerializeField] private NodeContentManager _nodeContentManager;

    [Header("互動按鈕")]
    [SerializeField] private Button _restButton;
    [SerializeField] private Button _meditateButton;

    [Header("冥想（技能升級）")]
    [SerializeField] private SkillUpgradePanelUI _skillUpgradePanel;

    [Header("文字顯示")]
    [SerializeField] private Text _resultText;

    public void Show()
    {
        gameObject.SetActive(true);

        if (_restButton != null)
            _restButton.interactable = true;

        if (_meditateButton != null)
            _meditateButton.interactable = true;

        if (_skillUpgradePanel != null)
            _skillUpgradePanel.Close();

        if (_resultText != null)
            _resultText.text = string.Empty;
    }

    public void OnClickRest()
    {
        if (_nodeContentManager == null)
            return;

        int healed = _nodeContentManager.RestHealPlayer();

        if (_resultText != null)
            _resultText.text = $"你休息了一下，回復了 {healed} 點生命";

        if (_restButton != null)
            _restButton.interactable = false;
    }

    public void OnClickMeditate()
    {
        if (_skillUpgradePanel != null)
        {
            _skillUpgradePanel.Open(this);
        }
        else if (_resultText != null)
        {
            _resultText.text = "冥想面板未設定";
        }
    }

    // 由 SkillUpgradePanelUI 在玩家實際升級一個技能後回呼：這次休息的冥想機會用掉了
    public void NotifyMeditateUsed()
    {
        if (_meditateButton != null)
            _meditateButton.interactable = false;

        if (_resultText != null)
            _resultText.text = "你完成了冥想，領悟了新的技巧";
    }

    public void OnClickNextStep()
    {
        _nodeContentManager?.CloseRestPanel();
    }

    public void OnClickViewMap()
    {
        _nodeContentManager?.OnClickViewMap();
    }
}
