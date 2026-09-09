using UnityEngine;

/// <summary>
/// 對話序列裡的一句話：可選的說話者名稱 + 內文。
/// 獨立成資料類別，讓 <see cref="DialogueSequencePlayer"/> 保持通用，
/// 之後任何過場文本（開場劇情、事件對話…）都能重複使用同一套播放器。
/// </summary>
[System.Serializable]
public class DialogueLine
{
    [Tooltip("說話者名稱，留空則不顯示")]
    public string speaker;

    [TextArea(2, 4)]
    public string text;
}
