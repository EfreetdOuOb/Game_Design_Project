using UnityEngine;
using UnityEngine.UI;

public class EventEncounterPanelUI : MonoBehaviour
{
    [SerializeField] private NodeContentManager _nodeContentManager;

    [Header("事件內容顯示")]
    [SerializeField] private Text _eventText;
    [SerializeField] private Image _eventIcon;

    // NodeContentManager 隨機抽出事件後呼叫，把對應的文本/圖示換上去
    public void Display(EventDefinition eventDefinition)
    {
        if (eventDefinition == null)
            return;

        if (_eventText != null)
            _eventText.text = eventDefinition.eventText;

        if (_eventIcon != null)
        {
            _eventIcon.sprite = eventDefinition.icon;
            _eventIcon.enabled = eventDefinition.icon != null;
        }
    }

    public void OnClickNextStep()
    {
        _nodeContentManager?.CloseEventPanel();
    }

    public void OnClickViewMap()
    {
        _nodeContentManager?.OnClickViewMap();
    }
}
