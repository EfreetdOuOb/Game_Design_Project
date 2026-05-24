using UnityEngine;

[CreateAssetMenu(menuName = "GestureCombat/Skill Effects/Damage", fileName = "EFF_Damage")]
public class DamageEffectDefinition : SkillEffectDefinition
{
    public int baseDamage = 10;
    public bool scaleWithAttackPower = false;

    public override void Apply(SkillExecutionContext context)
    {
        if (context == null || context.target == null)
        {
            return;
        }

        int value = baseDamage;
        if (scaleWithAttackPower && context.self != null)
        {
            value += context.self.attackPower;
        }

        int actualDamage = context.target.ReceiveDamage(value);
        if (context.report != null)
        {
            context.report.totalDamageDealt += actualDamage;
        }
        Debug.Log($"[技能效果] 傷害效果觸發，造成 {value} 傷害。");
        CombatUI.Instance?.AppendBattleLog($"{context.self.actorId} 對 {context.target.actorId} 造成 {actualDamage} 傷害");
    }
}
