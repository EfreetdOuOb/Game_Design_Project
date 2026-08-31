using UnityEngine;

[CreateAssetMenu(menuName = "GestureCombat/Skill Effects/Vulnerable", fileName = "EFF_Vulnerable")]
public class VulnerableEffectDefinition : SkillEffectDefinition
{
    public string debuffId = "Vulnerable";
    public int stacks = 1;
    public int durationTurns = 2;
    public float multiplierPerStack = 0.2f;

    public override void Apply(SkillExecutionContext context)
    {
        if (context == null || context.target == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(debuffId))
        {
            debuffId = "Vulnerable";
        }

        context.target.ApplyDebuff(debuffId, Mathf.Max(1, stacks), Mathf.Max(1, durationTurns));
        context.report?.appliedDebuffs.Add(debuffId);

        Debug.Log($"[技能效果] {debuffId} 效果觸發，增加 {stacks} 層，持續 {durationTurns} 回合，額外傷害倍率={1f + multiplierPerStack * stacks:F2}");
    }
}
