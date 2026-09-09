using UnityEngine;

/// <summary>
/// 「看似珍貴的容器」：戰鬥開始前回復固定生命值。
/// </summary>
[CreateAssetMenu(menuName = "Game/Relics/Effects/Heal On Battle Start", fileName = "RelicEffect_HealOnBattleStart")]
public class HealOnBattleStartRelicEffect : RelicEffectDefinition
{
    [Tooltip("戰鬥開始前回復的生命值")]
    public int healAmount = 2;

    public override void OnBattleStart(RelicContext context)
    {
        if (context?.player == null)
            return;

        int actualHealed = context.player.Heal(healAmount);
        CombatUI.Instance?.AppendBattleLog($"{context.player.actorId} 因遺物效果回復了 {actualHealed} 點生命");
    }
}
