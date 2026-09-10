using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 冥想面板上單一「可升級技能」的顯示與升級按鈕。
/// 純顯示 + 把點擊丟回給 SkillUpgradePanelUI 處理。
/// </summary>
public class SkillUpgradeSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Text nameText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Text statusText;
    [SerializeField] private Button upgradeButton;

    private SkillUpgradeDefinition definition;
    private SkillUpgradePanelUI ownerPanel;

    public void Setup(SkillUpgradeDefinition upgradeDefinition, bool alreadyUpgraded, SkillUpgradePanelUI panel)
    {
        definition = upgradeDefinition;
        ownerPanel = panel;

        if (icon != null)
        {
            icon.sprite = definition.icon;
            icon.enabled = definition.icon != null;
        }

        if (nameText != null)
            nameText.text = definition.displayName;

        if (descriptionText != null)
            descriptionText.text = definition.upgradeDescription;

        SetUpgraded(alreadyUpgraded);

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(HandleUpgradeClicked);
        }
    }

    private void HandleUpgradeClicked()
    {
        ownerPanel?.TryUpgrade(definition, this);
    }

    // 這個項目本身已經升過級：鎖住並顯示「已強化」
    public void SetUpgraded(bool upgraded)
    {
        if (upgradeButton != null)
            upgradeButton.interactable = !upgraded;

        if (statusText != null)
            statusText.text = upgraded ? "已強化" : string.Empty;
    }

    // 這次休息已經用掉升級機會：只鎖按鈕，不改文字
    public void LockForThisVisit()
    {
        if (upgradeButton != null)
            upgradeButton.interactable = false;
    }
}
