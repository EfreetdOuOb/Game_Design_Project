using UnityEngine;

/// <summary>
/// 虛弱：對目標施加「Weak」debuff，降低目標「造成」的傷害。
/// 升級後延長持續回合。
/// </summary>
[CreateAssetMenu(menuName = "GestureCombat/Skill Effects/Weak", fileName = "EFF_Weak")]
public class WeakEffectDefinition : SkillEffectDefinition
{
    public int stacks = 1;
    public int durationTurns = 2;

    [Header("升級")]
    [Tooltip("技能升級後，額外增加的持續回合")]
    public int upgradedBonusDuration = 1;

    public override void Apply(SkillExecutionContext context)
    {
        if (context == null || context.target == null)
            return;

        int finalDuration = durationTurns + (context.skillUpgraded ? upgradedBonusDuration : 0);
        context.target.ApplyDebuff(CombatActor.WeakDebuffId, Mathf.Max(1, stacks), Mathf.Max(1, finalDuration));
        context.report?.appliedDebuffs.Add($"虛弱(層數:{stacks},回合:{finalDuration})");

        Debug.Log($"[技能效果] 虛弱觸發，{stacks} 層，持續 {finalDuration} 回合（升級={context.skillUpgraded}）");
    }

    public override string Describe(bool upgraded)
    {
        int finalStacks = Mathf.Max(1, stacks);
        int finalDuration = Mathf.Max(1, durationTurns + (upgraded ? upgradedBonusDuration : 0));
        int percent = Mathf.RoundToInt(0.2f * 100f * finalStacks);
        return "施加 " + TooltipText.Keyword("虛弱") + " " + finalStacks + " 層（持續 " + TooltipText.Keyword(finalDuration)
            + " 回合）：目標造成的傷害 -" + TooltipText.Shield(percent + "%");
    }

    public override string DescribeUpgrade()
    {
        return upgradedBonusDuration > 0 ? "虛弱持續 +" + TooltipText.Keyword(upgradedBonusDuration) + " 回合" : null;
    }
}
