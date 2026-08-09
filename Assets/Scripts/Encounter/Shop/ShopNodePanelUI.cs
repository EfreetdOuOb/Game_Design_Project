using UnityEngine;
using UnityEngine.UI;

public class ShopNodePanelUI : MonoBehaviour
{
    [SerializeField] private NodeContentManager _nodeContentManager;

    [Header("互動按鈕")]
    [SerializeField] private Button _purchaseButton;

    [Header("文字顯示")]
    [SerializeField] private Text _resultText;

    public void Show()
    {
        gameObject.SetActive(true);

        if (_purchaseButton != null)
            _purchaseButton.interactable = true;

        if (_resultText != null)
            _resultText.text = string.Empty;
    }

    public void OnClickPurchase()
    {
        // TODO：貨幣／道具系統之後再補，目前先示意流程與 UI
        if (_resultText != null)
            _resultText.text = "商店功能開發中，尚未開放購買";
    }

    public void OnClickNextStep()
    {
        _nodeContentManager?.CloseShopPanel();
    }

    public void OnClickViewMap()
    {
        _nodeContentManager?.OnClickViewMap();
    }
}
