using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 記錄本局哪些技能已經升過級（每個技能只能升一次）。
/// 這裡只是一個「旗標倉庫」——實際升級後效果怎麼變，是寫在各技能資產／效果資產／GestureCombatActionHandler 裡，
/// 由它們自己去問「我升級了嗎」，SkillUpgradeManager 不認識任何戰鬥細節。
/// 比照 RelicManager 的單例風格，RestartRun 時重置。
/// </summary>
public class SkillUpgradeManager : MonoBehaviour
{
    public static SkillUpgradeManager Instance { get; private set; }

    // 攻擊 / 防禦不是技能，用固定字串當旗標 id
    public const string AttackUpgradeId = "攻擊";
    public const string DefenseUpgradeId = "防禦";

    private readonly HashSet<string> upgradedIds = new HashSet<string>();

    public event Action<string> OnSkillUpgraded;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool IsUpgraded(string skillId)
    {
        return !string.IsNullOrEmpty(skillId) && upgradedIds.Contains(skillId);
    }

    public bool TryUpgrade(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
            return false;

        if (upgradedIds.Contains(skillId))
        {
            Debug.LogWarning($"[技能升級] {skillId} 已經升過級，略過");
            return false;
        }

        upgradedIds.Add(skillId);
        Debug.Log($"[技能升級] {skillId} 升級完成");
        CombatUI.Instance?.AppendBattleLog($"技能「{skillId}」已強化");
        OnSkillUpgraded?.Invoke(skillId);
        return true;
    }

    public void ResetForNewRun()
    {
        upgradedIds.Clear();
    }
}
