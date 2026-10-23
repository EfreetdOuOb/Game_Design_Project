/// <summary>
/// 能改變角色「有效攻擊／防禦」的來源（技能升級、遺物、Buff…）。
/// 掛到 <see cref="CombatActor.AddStatModifier"/> 之後，CombatActor 算出的有效數值會自動加上這些固定加成，
/// 戰鬥結算那邊完全不用知道加成是從哪來的。
/// </summary>
public interface IStatModifier
{
    int AttackBonus { get; }
    int DefenseBonus { get; }
}
