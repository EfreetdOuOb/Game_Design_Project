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

    [Header("條件加成")]
    [Tooltip("目標處於暈眩或有任何 debuff（易傷、虛弱…）時，傷害乘上這個倍率（背刺用）。1 = 沒有加成。")]
    public float afflictedTargetMultiplier = 1f;

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

    // 暈眩（麻痺、韌性歸零）或身上有任何 debuff
    private static bool IsAfflicted(CombatActor target)
    {
        if (target == null)
            return false;

        if (target.ActiveDebuffs != null && target.ActiveDebuffs.Count > 0)
            return true;

        EnemyPoise poise = target.GetComponent<EnemyPoise>();
        if (poise == null)
            poise = target.GetComponentInParent<EnemyPoise>();

        return poise != null && poise.IsStunned;
    }

    private void ExecuteOnCurrentTarget(SkillExecutionContext context)
    {
        CombatActor self = context.self;
        CombatActor target = context.target;
        if (target == null)
            return;

        int perHit = damage + (context.skillUpgraded ? upgradedBonusDamage : 0);
        if (scaleWithCasterAttackPower && self != null)
            perHit += self.EffectiveAttackPower;

        int hits = Mathf.Max(1, hitCount + (context.skillUpgraded ? upgradedBonusHits : 0));

        float multiplier = IsAfflicted(target) ? afflictedTargetMultiplier : 1f;

        int totalActual = 0;
        for (int i = 0; i < hits; i++)
        {
            if (target.currentHp <= 0)
                break;

            totalActual += DamageCalculator.Deal(new DamageRequest(self, target, perHit, multiplier)).HpLoss;
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

    public override System.Collections.Generic.List<string> DescribeLines(bool upgraded)
    {
        var lines = new System.Collections.Generic.List<string>();
        int perHit = damage + (upgraded ? upgradedBonusDamage : 0);
        int hits = Mathf.Max(1, hitCount + (upgraded ? upgradedBonusHits : 0));
        string targetText = hitsAllEnemies ? "所有敵人" : "目標";

        if (perHit > 0 || scaleWithCasterAttackPower)
        {
            string amount = TooltipText.Damage(perHit) + (scaleWithCasterAttackPower ? " + 攻擊力" : string.Empty);
            lines.Add(hits > 1
                ? "對" + targetText + "造成 " + amount + " 點傷害，連續 " + TooltipText.Keyword(hits) + " 次"
                : "對" + targetText + "造成 " + amount + " 點傷害");
        }

        if (afflictedTargetMultiplier > 1.001f)
        {
            lines.Add("目標暈眩或有負面狀態時，傷害 ×" + TooltipText.Keyword(afflictedTargetMultiplier.ToString("0.##"))
                + "（每次 " + TooltipText.Damage(Mathf.RoundToInt(perHit * afflictedTargetMultiplier)) + "）");
        }

        for (int i = 0; i < extraEffects.Count; i++)
        {
            string text = extraEffects[i] != null ? extraEffects[i].Describe(upgraded) : null;
            if (!string.IsNullOrEmpty(text))
                lines.Add(text);
        }

        return lines;
    }

    public override System.Collections.Generic.List<string> DescribeUpgradeLines()
    {
        var lines = new System.Collections.Generic.List<string>();
        if (upgradedBonusDamage > 0)
            lines.Add("每次傷害 +" + TooltipText.Keyword(upgradedBonusDamage));
        if (upgradedBonusHits > 0)
            lines.Add("攻擊次數 +" + TooltipText.Keyword(upgradedBonusHits));

        for (int i = 0; i < extraEffects.Count; i++)
        {
            string text = extraEffects[i] != null ? extraEffects[i].DescribeUpgrade() : null;
            if (!string.IsNullOrEmpty(text))
                lines.Add(text);
        }

        return lines;
    }
}
