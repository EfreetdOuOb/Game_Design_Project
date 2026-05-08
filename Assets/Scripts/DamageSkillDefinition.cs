using UnityEngine;

[CreateAssetMenu(menuName = "GestureCombat/Skills/Damage Skill", fileName = "SK_Damage")]
public class DamageSkillDefinition : SkillDefinition
{
    public int damage = 20;
    public bool scaleWithCasterAttackPower = false;

    public override void Execute(SkillExecutionContext context)
    {
        CombatActor self = context != null ? context.self : null;
        CombatActor target = context != null ? context.target : null;
        if (target == null)
        {
            return;
        }

        int finalDamage = damage;
        if (scaleWithCasterAttackPower && self != null)
        {
            finalDamage += self.attackPower;
        }
        int actualDamage = target.ReceiveDamage(finalDamage);
        if (context != null && context.report != null)
        {
            context.report.totalDamageDealt += actualDamage;
        }
        Debug.Log($"[技能] {displayName}（{skillId}）造成 {finalDamage} 傷害。");
    }
}
