using UnityEngine;

[CreateAssetMenu(menuName = "GestureCombat/Skill Effects/Damage", fileName = "EFF_Damage")]
public class DamageEffectDefinition : SkillEffectDefinition
{
    public int baseDamage = 10;
    public bool scaleWithAttackPower = false;

    [Tooltip("攻擊次數（雙擊之類的技能設 2）")]
    public int hitCount = 1;

    [Header("升級")]
    [Tooltip("技能升級後，每次攻擊額外增加的傷害（背刺之類的技能用）")]
    public int upgradedBonusDamage = 0;
    [Tooltip("技能升級後，額外增加的攻擊次數（雙擊之類的技能用）")]
    public int upgradedBonusHits = 0;

    public override void Apply(SkillExecutionContext context)
    {
        if (context == null || context.target == null)
        {
            return;
        }

        int perHit = baseDamage + (context.skillUpgraded ? upgradedBonusDamage : 0);
        if (scaleWithAttackPower && context.self != null)
        {
            perHit += context.self.attackPower;
        }

        if (context.self != null)
        {
            perHit = Mathf.RoundToInt(perHit * context.self.GetOutgoingDamageMultiplier());
        }

        int hits = Mathf.Max(1, hitCount + (context.skillUpgraded ? upgradedBonusHits : 0));

        int totalActual = 0;
        for (int i = 0; i < hits; i++)
        {
            if (context.target == null || context.target.currentHp <= 0)
                break;

            totalActual += context.target.ReceiveDamage(perHit);
        }

        if (context.report != null)
        {
            context.report.totalDamageDealt += totalActual;
        }

        Debug.Log($"[技能效果] 傷害效果觸發，{hits} 次 x {perHit}（升級={context.skillUpgraded}），實際造成 {totalActual}。");
        if (context.self != null && context.target != null)
            CombatUI.Instance?.AppendBattleLog($"{context.self.actorId} 對 {context.target.actorId} 造成 {totalActual} 傷害");
    }
}
