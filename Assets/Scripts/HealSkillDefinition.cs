using UnityEngine;

[CreateAssetMenu(menuName = "GestureCombat/Skills/Heal Skill", fileName = "SK_Heal")]
public class HealSkillDefinition : SkillDefinition
{
    public int heal = 20;

    public override void Execute(SkillExecutionContext context)
    {
        CombatActor self = context != null ? context.self : null;
        if (self == null)
        {
            return;
        }

        int actualHeal = self.Heal(heal);
        if (context != null && context.report != null)
        {
            context.report.totalHealDone += actualHeal;
        }
        Debug.Log($"[技能] {displayName}（{skillId}）回復 {heal}。");
    }
}
