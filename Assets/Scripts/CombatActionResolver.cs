using UnityEngine;

/// <summary>
/// 「出手一次／防守一次」的共用入口，玩家和敵人都走這裡，所以兩邊用同一套基準：
///   攻擊一次的基礎傷害 = 出手者的有效攻擊力 × 招式倍率
///   防守一次的護盾     = 防守者的有效防禦力
/// 數值全部來自 <see cref="CombatActor"/>，呼叫端（法陣手勢、敵人 AI）只決定「出手幾次、什麼招式」；
/// 傷害怎麼結算交給 <see cref="DamageCalculator"/>。
/// </summary>
public static class CombatActionResolver
{
    /// <summary>出手打一下。</summary>
    /// <param name="powerMultiplier">招式倍率（普攻 1、重擊 2…）</param>
    public static DamageResult Strike(CombatActor attacker, CombatActor target, float powerMultiplier = 1f)
    {
        if (attacker == null)
            return default;

        return DamageCalculator.Deal(new DamageRequest(attacker, target, attacker.EffectiveAttackPower, powerMultiplier));
    }

    /// <summary>防守 times 次，回傳這次新增的護盾。</summary>
    public static int Defend(CombatActor defender, int times = 1)
    {
        if (defender == null || times <= 0)
            return 0;

        int total = defender.EffectiveDefensePower * times;
        defender.AddShield(total);
        return total;
    }
}
