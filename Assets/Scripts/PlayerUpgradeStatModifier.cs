using UnityEngine;

/// <summary>
/// 把休息點冥想買到的「攻擊強化／防禦強化」變成玩家的固定數值加成。
/// 掛在玩家的 CombatActor 同一個物件上；升級旗標由 <see cref="SkillUpgradeManager"/> 記錄。
/// </summary>
[RequireComponent(typeof(CombatActor))]
public class PlayerUpgradeStatModifier : MonoBehaviour, IStatModifier
{
    [Tooltip("攻擊強化後，每次攻擊增加的傷害")]
    [SerializeField] private int _attackUpgradeBonus = 5;
    [Tooltip("防禦強化後，每個防禦點增加的防禦值")]
    [SerializeField] private int _defenseUpgradeBonus = 2;

    private CombatActor _actor;

    public int AttackBonus => IsUpgraded(SkillUpgradeManager.AttackUpgradeId) ? _attackUpgradeBonus : 0;
    public int DefenseBonus => IsUpgraded(SkillUpgradeManager.DefenseUpgradeId) ? _defenseUpgradeBonus : 0;

    private void OnEnable()
    {
        if (_actor == null)
            _actor = GetComponent<CombatActor>();

        _actor.AddStatModifier(this);
    }

    private void OnDisable()
    {
        if (_actor != null)
            _actor.RemoveStatModifier(this);
    }

    private static bool IsUpgraded(string upgradeId)
    {
        return SkillUpgradeManager.Instance != null && SkillUpgradeManager.Instance.IsUpgraded(upgradeId);
    }
}
