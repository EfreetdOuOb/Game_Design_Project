using UnityEngine;
using UnityEngine.UI;

public class RestNodePanelUI : MonoBehaviour
{
    [SerializeField] private NodeContentManager _nodeContentManager;

    [Header("互動按鈕")]
    [SerializeField] private Button _restButton;
    [SerializeField] private Button _meditateButton;

    [Header("文字顯示")]
    [SerializeField] private Text _resultText;

    public void Show()
    {
        gameObject.SetActive(true);

        if (_restButton != null)
            _restButton.interactable = true;

        if (_meditateButton != null)
            _meditateButton.interactable = true;

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
        // TODO：冥想升級技能的實際數值/選擇邏輯之後再補，目前先示意流程與 UI
        if (_resultText != null)
            _resultText.text = "你靜下心來冥想...（技能升級功能開發中）";

        if (_meditateButton != null)
            _meditateButton.interactable = false;
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
