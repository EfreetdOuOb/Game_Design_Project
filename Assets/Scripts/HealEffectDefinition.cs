using UnityEngine;

[CreateAssetMenu(menuName = "GestureCombat/Skill Effects/Heal", fileName = "EFF_Heal")]
public class HealEffectDefinition : SkillEffectDefinition
{
    public int healAmount = 10;

    public override void Apply(SkillExecutionContext context)
    {
        if (context == null || context.self == null)
        {
            return;
        }

        int actualHeal = context.self.Heal(healAmount);
        if (context.report != null)
        {
            context.report.totalHealDone += actualHeal;
        }
        Debug.Log($"[技能效果] 治療效果觸發，回復 {healAmount}。");
    }
}
