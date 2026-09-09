using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopNodePanelUI : MonoBehaviour
{
    [SerializeField] private NodeContentManager _nodeContentManager;

    [Header("遺物貨架")]
    [Tooltip("這個商店可能販售的所有遺物，實際上架時會從裡面隨機抽、排除玩家已經擁有的")]
    [SerializeField] private List<RelicDefinition> _relicCatalog = new();
    [Tooltip("每次進商店隨機上架幾個遺物")]
    [SerializeField] private int _shopSlotCount = 2;
    [SerializeField] private RelicShopSlotUI _slotPrefab;
    [SerializeField] private Transform _slotContainer;

    [Header("金幣顯示")]
    [SerializeField] private Text _goldText;

    [Header("文字顯示")]
    [SerializeField] private Text _resultText;

    private readonly List<RelicShopSlotUI> _spawnedSlots = new();

    public void Show()
    {
        gameObject.SetActive(true);

        if (_resultText != null)
            _resultText.text = string.Empty;

        RefreshGoldText();
        RestockShelf();
    }

    private void RestockShelf()
    {
        ClearSlots();

        if (_slotPrefab == null || _slotContainer == null)
        {
            Debug.LogWarning("ShopNodePanelUI 缺少 Slot Prefab 或 Slot Container，無法上架遺物");
            return;
        }

        List<RelicDefinition> available = new List<RelicDefinition>();
        for (int i = 0; i < _relicCatalog.Count; i++)
        {
            RelicDefinition relic = _relicCatalog[i];
            if (relic == null || string.IsNullOrEmpty(relic.relicId))
                continue;

            if (RelicManager.Instance != null && RelicManager.Instance.HasRelic(relic.relicId))
                continue;

            available.Add(relic);
        }

        Shuffle(available);

        int slotCount = Mathf.Min(Mathf.Max(0, _shopSlotCount), available.Count);
        for (int i = 0; i < slotCount; i++)
        {
            RelicDefinition relic = available[i];
            int price = RelicManager.Instance != null
                ? RelicManager.Instance.ApplyShopPriceModifiers(relic.price)
                : relic.price;

            RelicShopSlotUI slot = Instantiate(_slotPrefab, _slotContainer);
            slot.Setup(relic, price, this);
            _spawnedSlots.Add(slot);
        }
    }

    private void ClearSlots()
    {
        for (int i = 0; i < _spawnedSlots.Count; i++)
        {
            if (_spawnedSlots[i] != null)
                Destroy(_spawnedSlots[i].gameObject);
        }

        _spawnedSlots.Clear();
    }

    public void TryPurchaseRelic(RelicDefinition relic, RelicShopSlotUI slot)
    {
        if (relic == null)
            return;

        int price = RelicManager.Instance != null
            ? RelicManager.Instance.ApplyShopPriceModifiers(relic.price)
            : relic.price;

        if (PlayerCurrency.Instance == null || !PlayerCurrency.Instance.TrySpendGold(price))
        {
            if (_resultText != null)
                _resultText.text = "金幣不足！";
            return;
        }

        if (!(RelicManager.Instance?.TryAcquireRelic(relic) ?? false))
        {
            // 理論上前面已經把已擁有的遺物過濾掉了，這裡是保底：拿取失敗要把錢退回去
            PlayerCurrency.Instance.AddGold(price);
            if (_resultText != null)
                _resultText.text = "購買失敗";
            return;
        }

        if (_resultText != null)
            _resultText.text = $"購買了「{relic.displayName}」";

        slot?.MarkSold();
        RefreshGoldText();
    }

    private void RefreshGoldText()
    {
        if (_goldText != null)
            _goldText.text = $"金幣：{(PlayerCurrency.Instance != null ? PlayerCurrency.Instance.CurrentGold : 0)}";
    }

    private void Shuffle(List<RelicDefinition> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
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
