using UnityEngine;

[CreateAssetMenu(menuName = "GestureCombat/Skill Effects/Heal", fileName = "EFF_Heal")]
public class HealEffectDefinition : SkillEffectDefinition
{
    public int healAmount = 10;

    [Header("升級")]
    [Tooltip("技能升級後，額外增加的立即回復量（補血技能用）")]
    public int upgradedBonusHeal = 0;

    [Tooltip("技能升級後，改為「持續回血」而非立即回血（大補技能用）")]
    public bool upgradedBecomesRegen = false;
    public int regenPerTurn = 10;
    public int regenTurns = 3;

    public override void Apply(SkillExecutionContext context)
    {
        if (context == null || context.self == null)
        {
            return;
        }

        if (context.skillUpgraded && upgradedBecomesRegen)
        {
            context.self.AddRegen(regenPerTurn, regenTurns);
            Debug.Log($"[技能效果] 治療效果（升級）：改為持續回血，每回合 {regenPerTurn}，持續 {regenTurns} 回合");
            return;
        }

        int finalHeal = healAmount + (context.skillUpgraded ? upgradedBonusHeal : 0);
        int actualHeal = context.self.Heal(finalHeal);
        if (context.report != null)
        {
            context.report.totalHealDone += actualHeal;
        }
        Debug.Log($"[技能效果] 治療效果觸發，回復 {finalHeal}（升級={context.skillUpgraded}）。");
    }
}
