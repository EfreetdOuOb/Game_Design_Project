using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleEncounterDefinition", menuName = "Game/Node Content/Battle Encounter")]
public class BattleEncounterDefinition : ScriptableObject
{
    public string contentId;
    public MapNodeType nodeType = MapNodeType.Enemy;
    public List<GameObject> enemyPrefabs = new();

    [Header("Boss 專用：擊敗後的過場文本")]
    [Tooltip("僅 Boss 類型節點會用到。擊敗後依序顯示這些句子，留空則不會播放過場文本、直接視為一般勝利")]
    public List<DialogueLine> victoryDialogueLines = new();
}