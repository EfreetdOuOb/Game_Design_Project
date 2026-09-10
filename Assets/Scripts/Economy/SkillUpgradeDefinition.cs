using UnityEngine;

/// <summary>
/// 冥想面板上一個「可升級項目」的顯示資料。
/// skillId 要對上：一般技能填技能資產的 skillId；攻擊／防禦填
/// SkillUpgradeManager.AttackUpgradeId / DefenseUpgradeId。
/// </summary>
[CreateAssetMenu(menuName = "Game/Skill Upgrade", fileName = "SkillUpgrade_")]
public class SkillUpgradeDefinition : ScriptableObject
{
    [Tooltip("對應 SkillUpgradeManager 的旗標 id，也對應技能資產的 skillId")]
    public string skillId;

    public string displayName;

    [Tooltip("升級效果說明，給玩家看")]
    [TextArea(2, 4)]
    public string upgradeDescription;

    public Sprite icon;
}
