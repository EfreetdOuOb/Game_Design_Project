using UnityEngine;

/// <summary>
/// 麻痺：有機率讓目標敵人暈眩、跳過行動。升級後提高機率。
/// 透過 EnemyPoise.ApplyStun 實作，不需要先打破韌性。
/// </summary>
[CreateAssetMenu(menuName = "GestureCombat/Skill Effects/Paralysis", fileName = "EFF_Paralysis")]
public class ParalysisEffectDefinition : SkillEffectDefinition
{
    [Range(0f, 1f)]
    public float chance = 0.3f;

    [Tooltip("命中時讓敵人暈眩幾回合")]
    public int stunTurns = 1;

    [Header("升級")]
    [Range(0f, 1f)]
    [Tooltip("技能升級後，額外增加的麻痺機率")]
    public float upgradedBonusChance = 0.2f;

    public override void Apply(SkillExecutionContext context)
    {
        if (context == null || context.target == null)
            return;

        float finalChance = Mathf.Clamp01(chance + (context.skillUpgraded ? upgradedBonusChance : 0f));

        if (Random.value > finalChance)
        {
            Debug.Log($"[技能效果] 麻痺未命中（機率={finalChance:P0}）");
            CombatUI.Instance?.AppendBattleLog($"麻痺未命中（{finalChance:P0}）");
            return;
        }

        EnemyPoise poise = context.target.GetComponent<EnemyPoise>();
        if (poise == null)
            poise = context.target.GetComponentInParent<EnemyPoise>();

        if (poise == null)
        {
            Debug.LogWarning($"[技能效果] 麻痺命中，但 {context.target.actorId} 身上沒有 EnemyPoise");
            return;
        }

        poise.ApplyStun(Mathf.Max(1, stunTurns));
        context.report?.appliedDebuffs.Add($"麻痺({stunTurns}回合)");
        Debug.Log($"[技能效果] 麻痺命中（機率={finalChance:P0}），暈眩 {stunTurns} 回合");
    }
}
