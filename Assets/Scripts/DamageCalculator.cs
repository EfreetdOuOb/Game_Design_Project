using System;
using UnityEngine;

[Flags]
public enum DamageFlags
{
    None = 0,
    /// <summary>無視護盾，直接扣血（之後的穿透傷害用）</summary>
    IgnoreShield = 1
}

/// <summary>一次傷害的「申請單」：誰打誰、基礎傷害多少。數值怎麼被加減，全部交給 DamageCalculator。</summary>
public struct DamageRequest
{
    /// <summary>傷害來源；null 代表環境傷害（炸彈…），不吃出手端的虛弱等修正。</summary>
    public CombatActor source;
    public CombatActor target;
    /// <summary>基礎傷害（技能傷害、有效攻擊力…，由呼叫端決定）</summary>
    public int baseAmount;
    /// <summary>招式倍率（普攻 1、重擊 2…）</summary>
    public float multiplier;
    public DamageFlags flags;

    public DamageRequest(CombatActor source, CombatActor target, int baseAmount,
        float multiplier = 1f, DamageFlags flags = DamageFlags.None)
    {
        this.source = source;
        this.target = target;
        this.baseAmount = baseAmount;
        this.multiplier = multiplier;
        this.flags = flags;
    }
}

public readonly struct DamageResult
{
    /// <summary>吃完所有倍率、還沒扣護盾的傷害</summary>
    public readonly int Calculated;
    /// <summary>被護盾吸收的量</summary>
    public readonly int Absorbed;
    /// <summary>實際扣掉的血量</summary>
    public readonly int HpLoss;

    /// <summary>有傷害，但全被護盾擋下</summary>
    public bool FullyBlocked => Calculated > 0 && HpLoss == 0;

    public DamageResult(int calculated, int absorbed, int hpLoss)
    {
        Calculated = calculated;
        Absorbed = absorbed;
        HpLoss = hpLoss;
    }
}

/// <summary>
/// 全遊戲唯一的傷害計算入口（普攻、技能、敵人攻擊、炸彈都走這裡）。
/// 固定順序：基礎傷害 × 招式倍率 × 來源的虛弱 × 目標的易傷 → 護盾吸收 → 剩下的扣血。
/// 之後要加傷害規則（暴擊、遺物加成、穿透…）只需要改這一個檔案。
/// </summary>
public static class DamageCalculator
{
    /// <summary>
    /// 只算「吃完所有倍率、還沒扣護盾」的傷害，不會扣血也不會動護盾。
    /// 給預告類的 UI（敵人即將造成的傷害）用，保證預告和實際結算用同一套公式。
    /// </summary>
    public static int Calculate(DamageRequest request)
    {
        CombatActor target = request.target;
        if (target == null || target.IsDead)
            return 0;

        float amount = request.baseAmount * request.multiplier;
        if (request.source != null)
            amount *= request.source.GetOutgoingDamageMultiplier();
        amount *= target.GetDamageMultiplierFromDebuffs();

        return Mathf.Max(0, Mathf.RoundToInt(amount));
    }

    public static DamageResult Deal(DamageRequest request)
    {
        CombatActor target = request.target;
        if (target == null || target.IsDead)
            return default;

        int calculated = Calculate(request);

        bool ignoreShield = (request.flags & DamageFlags.IgnoreShield) != 0;
        int absorbed = ignoreShield ? 0 : target.AbsorbWithShield(calculated);
        int hpLoss = target.ApplyHpLoss(calculated - absorbed);

        string shieldText = absorbed > 0 ? $"，護盾吸收 {absorbed}" : string.Empty;
        Debug.Log($"[傷害] {(request.source != null ? request.source.actorId : "環境")} → {target.actorId}：基礎={request.baseAmount}，結算={calculated}{shieldText}，扣血={hpLoss}，HP={target.currentHp}/{target.maxHp}");
        CombatUI.Instance?.AppendBattleLog(
            $"{target.actorId} 受到 {hpLoss} 傷害{(absorbed > 0 ? $"（護盾吸收 {absorbed}）" : string.Empty)}，HP {target.currentHp}/{target.maxHp}");

        return new DamageResult(calculated, absorbed, hpLoss);
    }
}
