using UnityEngine;

/// <summary>
/// 「榮譽顧客的證明」：商店裡所有商品打折。
/// </summary>
[CreateAssetMenu(menuName = "Game/Relics/Effects/Shop Discount", fileName = "RelicEffect_ShopDiscount")]
public class ShopDiscountRelicEffect : RelicEffectDefinition
{
    [Range(0f, 1f)]
    [Tooltip("折扣比例，0.2 代表打 8 折")]
    public float discountPercent = 0.2f;

    public override int ModifyShopPrice(int basePrice)
    {
        return Mathf.Max(0, Mathf.RoundToInt(basePrice * (1f - discountPercent)));
    }
}
