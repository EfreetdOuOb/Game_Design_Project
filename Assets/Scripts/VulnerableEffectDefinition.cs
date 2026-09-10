using UnityEngine;

[CreateAssetMenu(menuName = "GestureCombat/Skill Effects/Vulnerable", fileName = "EFF_Vulnerable")]
public class VulnerableEffectDefinition : SkillEffectDefinition
{
    public string debuffId = CombatActor.VulnerableDebuffId;
    public int stacks = 1;
    public int durationTurns = 2;
    public float multiplierPerStack = 0.2f;

    [Header("升級")]
    [Tooltip("技能升級後，額外增加的層數（例如群體技能升級 +1 層易傷）")]
    public int upgradedBonusStacks = 0;
    [Tooltip("技能升級後，額外增加的持續回合（例如易傷技能升級延長回合）")]
    public int upgradedBonusDuration = 0;

    public override void Apply(SkillExecutionContext context)
    {
        if (context == null || context.target == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(debuffId))
        {
            debuffId = CombatActor.VulnerableDebuffId;
        }

        int finalStacks = stacks + (context.skillUpgraded ? upgradedBonusStacks : 0);
        int finalDuration = durationTurns + (context.skillUpgraded ? upgradedBonusDuration : 0);

        context.target.ApplyDebuff(debuffId, Mathf.Max(1, finalStacks), Mathf.Max(1, finalDuration));
        context.report?.appliedDebuffs.Add(debuffId);

        Debug.Log($"[技能效果] {debuffId} 效果觸發，增加 {finalStacks} 層，持續 {finalDuration} 回合（升級={context.skillUpgraded}）");
    }
}
