using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GestureCombat/Skills/Damage Skill", fileName = "SK_Damage")]
public class DamageSkillDefinition : SkillDefinition
{
    public int damage = 20;
    public bool scaleWithCasterAttackPower = false;

    [Header("進階")]
    [Tooltip("攻擊次數（雙擊設 2）")]
    public int hitCount = 1;
    [Tooltip("是否對場上所有敵人生效（群體技能）")]
    public bool hitsAllEnemies = false;
    [Tooltip("造成傷害後額外執行的效果（虛弱、麻痺、群體的易傷…）")]
    public List<SkillEffectDefinition> extraEffects = new List<SkillEffectDefinition>();

    [Header("升級")]
    [Tooltip("升級後每次攻擊額外增加的傷害")]
    public int upgradedBonusDamage = 0;
    [Tooltip("升級後額外增加的攻擊次數")]
    public int upgradedBonusHits = 0;

    public override void Execute(SkillExecutionContext context)
    {
        if (context == null)
            return;

        CombatActor originalTarget = context.target;

        if (hitsAllEnemies && context.allEnemies != null && context.allEnemies.Count > 0)
        {
            // 複製一份避免迭代時清單被改動
            List<CombatActor> enemies = new List<CombatActor>(context.allEnemies);
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] == null || enemies[i].currentHp <= 0)
                    continue;

                context.target = enemies[i];
                ExecuteOnCurrentTarget(context);
            }
        }
        else
        {
            ExecuteOnCurrentTarget(context);
        }

        context.target = originalTarget;
    }

    private void ExecuteOnCurrentTarget(SkillExecutionContext context)
    {
        CombatActor self = context.self;
        CombatActor target = context.target;
        if (target == null)
            return;

        int perHit = damage + (context.skillUpgraded ? upgradedBonusDamage : 0);
        if (scaleWithCasterAttackPower && self != null)
            perHit += self.attackPower;

        if (self != null)
            perHit = Mathf.RoundToInt(perHit * self.GetOutgoingDamageMultiplier());

        int hits = Mathf.Max(1, hitCount + (context.skillUpgraded ? upgradedBonusHits : 0));

        int totalActual = 0;
        for (int i = 0; i < hits; i++)
        {
            if (target.currentHp <= 0)
                break;

            totalActual += target.ReceiveDamage(perHit);
        }

        if (context.report != null)
            context.report.totalDamageDealt += totalActual;

        if (totalActual > 0 || damage > 0)
        {
            Debug.Log($"[技能] {displayName}（{skillId}）對 {target.actorId} 造成 {totalActual}（{hits} 次 x {perHit}）。");
            if (self != null)
                CombatUI.Instance?.AppendBattleLog($"{self.actorId} 使用 {displayName}，對 {target.actorId} 造成 {totalActual} 傷害");
        }

        for (int i = 0; i < extraEffects.Count; i++)
        {
            extraEffects[i]?.Apply(context);
        }
    }
}
