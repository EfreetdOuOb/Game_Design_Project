using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 冥想 → 技能升級面板：列出所有可升級項目，玩家挑一個升級（每次休息只能升一次）。
/// 已經升過級的項目會顯示「已強化」且不能再選。
/// </summary>
public class SkillUpgradePanelUI : MonoBehaviour
{
    [Header("升級清單")]
    [Tooltip("所有可以在休息點升級的項目（攻擊/防禦 + 各技能）")]
    [SerializeField] private List<SkillUpgradeDefinition> _upgradeCatalog = new();
    [SerializeField] private SkillUpgradeSlotUI _slotPrefab;
    [SerializeField] private Transform _slotContainer;

    [Header("文字顯示")]
    [SerializeField] private Text _resultText;

    private readonly List<SkillUpgradeSlotUI> _spawnedSlots = new();
    private RestNodePanelUI _owner;
    private bool _usedThisVisit;

    public void Open(RestNodePanelUI owner)
    {
        _owner = owner;
        _usedThisVisit = false;

        gameObject.SetActive(true);

        if (_resultText != null)
            _resultText.text = "選擇一個技能強化";

        RebuildSlots();
    }

    public void OnClickClose()
    {
        gameObject.SetActive(false);
    }

    private void RebuildSlots()
    {
        ClearSlots();

        if (_slotPrefab == null || _slotContainer == null)
        {
            Debug.LogWarning("SkillUpgradePanelUI 缺少 Slot Prefab 或 Slot Container");
            return;
        }

        for (int i = 0; i < _upgradeCatalog.Count; i++)
        {
            SkillUpgradeDefinition definition = _upgradeCatalog[i];
            if (definition == null || string.IsNullOrEmpty(definition.skillId))
                continue;

            bool alreadyUpgraded = SkillUpgradeManager.Instance != null
                && SkillUpgradeManager.Instance.IsUpgraded(definition.skillId);

            SkillUpgradeSlotUI slot = Instantiate(_slotPrefab, _slotContainer);
            slot.Setup(definition, alreadyUpgraded, this);
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

    public void TryUpgrade(SkillUpgradeDefinition definition, SkillUpgradeSlotUI slot)
    {
        if (definition == null || _usedThisVisit)
            return;

        if (SkillUpgradeManager.Instance == null)
        {
            Debug.LogWarning("場景裡沒有 SkillUpgradeManager");
            return;
        }

        if (!SkillUpgradeManager.Instance.TryUpgrade(definition.skillId))
        {
            if (_resultText != null)
                _resultText.text = "這個技能已經強化過了";
            return;
        }

        _usedThisVisit = true;

        slot?.SetUpgraded(true);
        for (int i = 0; i < _spawnedSlots.Count; i++)
            _spawnedSlots[i]?.LockForThisVisit();

        if (_resultText != null)
            _resultText.text = $"「{definition.displayName}」已強化";

        _owner?.NotifyMeditateUsed();
    }
}
