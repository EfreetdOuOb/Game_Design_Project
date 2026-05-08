using UnityEngine;

[CreateAssetMenu(menuName = "GestureCombat/Skill Effects/Defense Buff", fileName = "EFF_DefenseBuff")]
public class DefenseBuffEffectDefinition : SkillEffectDefinition
{
    public int defenseBonus = 5;

    public override void Apply(SkillExecutionContext context)
    {
        if (context == null || context.self == null)
        {
            return;
        }

        context.self.AddTemporaryDefense(defenseBonus);
        if (context.report != null)
        {
            context.report.totalDefenseAdded += defenseBonus;
        }
        Debug.Log($"[技能效果] 防禦增益效果觸發，增加 {defenseBonus}。");
    }
}
