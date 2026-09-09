using UnityEngine;

/// <summary>
/// 單一事件的內容資料：面板要顯示的文本、圖示，以及查表用的 contentId
/// （跟 NodeRewardDefinition 用同一個 contentId 對應，讓每個事件可以各自決定給不給金幣）。
/// </summary>
[CreateAssetMenu(menuName = "Game/Node Content/Event", fileName = "Event_")]
public class EventDefinition : ScriptableObject
{
    [Tooltip("唯一識別碼，NodeRewardDefinition 用這個查表決定這個事件給不給金幣")]
    public string contentId;

    [Tooltip("面板上顯示的事件文本")]
    [TextArea(3, 6)]
    public string eventText;

    [Tooltip("面板上顯示的事件圖示")]
    public Sprite icon;
}
