using UnityEngine;

/// <summary>
/// 遺物「效果」的抽象基底，比照 SkillEffectDefinition 的模式：
/// 每個具體效果是一個獨立的 ScriptableObject 子類別，數值都在 Inspector 上調整，
/// 之後要修改某個遺物的手感，不需要改程式碼，只要改資產欄位。
///
/// 這裡的掛鉤方法都給預設空實作（或原樣傳回），新遺物只要 override 自己用得到的那幾個就好，
/// 目前只開了 3 個遺物實際會用到的掛鉤；之後有新的觸發時機需求再照樣新增。
/// </summary>
public abstract class RelicEffectDefinition : ScriptableObject
{
    // 遺物被買下、加入玩家背包的當下觸發
    public virtual void OnAcquired(RelicContext context) { }

    // 每次戰鬥開始前觸發
    public virtual void OnBattleStart(RelicContext context) { }

    // 炸彈倒數引爆造成傷害前，讓遺物有機會調整最終傷害
    public virtual int ModifyBombDamage(int baseDamage) => baseDamage;

    // 商店計算某個遺物售價時，讓遺物有機會調整最終價格
    public virtual int ModifyShopPrice(int basePrice) => basePrice;
}
