using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店貨架上單一遺物的顯示與購買按鈕，本身不決定價格/是否買得起，
/// 純粹顯示資料 + 把點擊丟回給 ShopNodePanelUI 判斷。
/// </summary>
public class RelicShopSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Text nameText;
    [SerializeField] private Text effectDescriptionText;
    [SerializeField] private Text priceText;
    [SerializeField] private Button buyButton;

    private RelicDefinition relic;
    private ShopNodePanelUI ownerPanel;

    public void Setup(RelicDefinition relicDefinition, int effectivePrice, ShopNodePanelUI panel)
    {
        relic = relicDefinition;
        ownerPanel = panel;

        if (icon != null)
        {
            icon.sprite = relic.icon;
            icon.enabled = relic.icon != null;
        }

        if (nameText != null)
            nameText.text = relic.displayName;

        if (effectDescriptionText != null)
            effectDescriptionText.text = relic.effectDescription;

        if (priceText != null)
            priceText.text = $"{effectivePrice} 金幣";

        if (buyButton != null)
        {
            buyButton.interactable = true;
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(HandleBuyClicked);
        }
    }

    private void HandleBuyClicked()
    {
        ownerPanel?.TryPurchaseRelic(relic, this);
    }

    public void MarkSold()
    {
        if (buyButton != null)
            buyButton.interactable = false;

        if (priceText != null)
            priceText.text = "已售出";
    }
}
