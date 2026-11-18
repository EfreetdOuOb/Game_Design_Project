using UnityEngine;

[CreateAssetMenu(menuName = "GestureCombat/Skills/Heal Skill", fileName = "SK_Heal")]
public class HealSkillDefinition : SkillDefinition
{
    public int heal = 20;

    [Header("升級")]
    [Tooltip("升級後額外增加的立即回復量（補血技能用）")]
    public int upgradedBonusHeal = 0;

    [Tooltip("升級後改為「持續回血」而非立即回血（大補技能用）")]
    public bool upgradedBecomesRegen = false;
    public int regenPerTurn = 10;
    public int regenTurns = 3;

    public override void Execute(SkillExecutionContext context)
    {
        CombatActor self = context != null ? context.self : null;
        if (self == null)
        {
            return;
        }

        if (context.skillUpgraded && upgradedBecomesRegen)
        {
            self.AddRegen(regenPerTurn, regenTurns);
            Debug.Log($"[技能] {displayName}（{skillId}）升級版：改為持續回血，每回合 {regenPerTurn}，持續 {regenTurns} 回合。");
            return;
        }

        int finalHeal = heal + (context.skillUpgraded ? upgradedBonusHeal : 0);
        int actualHeal = self.Heal(finalHeal);
        if (context.report != null)
        {
            context.report.totalHealDone += actualHeal;
        }
        Debug.Log($"[技能] {displayName}（{skillId}）回復 {finalHeal}（升級={context.skillUpgraded}）。");
    }

    public override System.Collections.Generic.List<string> DescribeLines(bool upgraded)
    {
        var lines = new System.Collections.Generic.List<string>();
        if (upgraded && upgradedBecomesRegen)
            lines.Add("每回合回復 " + TooltipText.Heal(regenPerTurn) + " 點生命，持續 " + regenTurns + " 回合");
        else
            lines.Add("回復 " + TooltipText.Heal(heal + (upgraded ? upgradedBonusHeal : 0)) + " 點生命");
        return lines;
    }

    public override System.Collections.Generic.List<string> DescribeUpgradeLines()
    {
        var lines = new System.Collections.Generic.List<string>();
        if (upgradedBecomesRegen)
            lines.Add("改為每回合回復 " + TooltipText.Heal(regenPerTurn) + "，持續 " + regenTurns + " 回合");
        else if (upgradedBonusHeal > 0)
            lines.Add("回復量 +" + TooltipText.Keyword(upgradedBonusHeal));
        return lines;
    }
}
