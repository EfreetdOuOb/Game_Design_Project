using UnityEngine;

/// <summary>
/// 「守護手鐲」：降低炸彈倒數引爆對玩家造成的傷害。
/// 先扣固定值，再套用比例減免；兩者預設都可以只用其中一種。
/// </summary>
[CreateAssetMenu(menuName = "Game/Relics/Effects/Reduce Bomb Damage", fileName = "RelicEffect_ReduceBombDamage")]
public class ReduceBombDamageRelicEffect : RelicEffectDefinition
{
    [Tooltip("固定減少的傷害量")]
    public int flatReduction = 5;

    [Range(0f, 1f)]
    [Tooltip("額外的傷害減免比例，0 表示不額外減免")]
    public float percentReduction = 0f;

    public override int ModifyBombDamage(int baseDamage)
    {
        int afterFlat = Mathf.Max(0, baseDamage - flatReduction);
        int afterPercent = Mathf.RoundToInt(afterFlat * (1f - percentReduction));
        return Mathf.Max(0, afterPercent);
    }
}
