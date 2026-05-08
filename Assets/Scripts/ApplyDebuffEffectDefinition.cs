using UnityEngine;

[CreateAssetMenu(menuName = "GestureCombat/Skill Effects/Apply Debuff", fileName = "EFF_ApplyDebuff")]
public class ApplyDebuffEffectDefinition : SkillEffectDefinition
{
    public string debuffId = "Burn";
    public int stacks = 1;
    public int durationTurns = 2;

    public override void Apply(SkillExecutionContext context)
    {
        if (context == null || context.target == null || string.IsNullOrEmpty(debuffId))
        {
            return;
        }

        context.target.ApplyDebuff(debuffId, stacks, durationTurns);
        if (context.report != null)
        {
            context.report.appliedDebuffs.Add($"{debuffId}(層數:{stacks},回合:{durationTurns})");
        }
        Debug.Log($"[技能效果] Debuff 效果觸發，套用 {debuffId}，層數={stacks}，持續回合={durationTurns}。");
    }
}
