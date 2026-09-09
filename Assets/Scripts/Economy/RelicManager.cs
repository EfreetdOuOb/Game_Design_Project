using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家目前擁有的遺物清單，並負責在對的時機呼叫每個遺物效果的對應方法。
/// 只有這裡認識「戰鬥開始」「炸彈引爆」「商店」等其他系統，
/// RelicEffectDefinition 本身完全不需要知道是誰呼叫它。
/// </summary>
public class RelicManager : MonoBehaviour
{
    public static RelicManager Instance { get; private set; }

    [SerializeField] private CombatActor playerActor;

    private readonly List<RelicDefinition> ownedRelics = new List<RelicDefinition>();
    private readonly HashSet<string> ownedRelicIds = new HashSet<string>();

    public event Action<RelicDefinition> OnRelicAcquired;

    public IReadOnlyList<RelicDefinition> OwnedRelics => ownedRelics;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool HasRelic(string relicId)
    {
        return !string.IsNullOrEmpty(relicId) && ownedRelicIds.Contains(relicId);
    }

    // 每種遺物全場只能擁有一個；已經擁有的話會直接失敗
    public bool TryAcquireRelic(RelicDefinition relic)
    {
        if (relic == null || string.IsNullOrEmpty(relic.relicId))
            return false;

        if (ownedRelicIds.Contains(relic.relicId))
        {
            Debug.LogWarning($"[遺物] 已經擁有 {relic.displayName}，略過重複取得");
            return false;
        }

        ownedRelics.Add(relic);
        ownedRelicIds.Add(relic.relicId);

        relic.effect?.OnAcquired(BuildContext());

        Debug.Log($"[遺物] 取得遺物：{relic.displayName}");
        CombatUI.Instance?.AppendBattleLog($"獲得遺物：{relic.displayName}");
        OnRelicAcquired?.Invoke(relic);
        return true;
    }

    public void NotifyBattleStart()
    {
        RelicContext context = BuildContext();
        for (int i = 0; i < ownedRelics.Count; i++)
        {
            ownedRelics[i].effect?.OnBattleStart(context);
        }
    }

    public int ApplyBombDamageModifiers(int baseDamage)
    {
        int result = baseDamage;
        for (int i = 0; i < ownedRelics.Count; i++)
        {
            if (ownedRelics[i].effect != null)
                result = ownedRelics[i].effect.ModifyBombDamage(result);
        }

        return result;
    }

    public int ApplyShopPriceModifiers(int basePrice)
    {
        int result = basePrice;
        for (int i = 0; i < ownedRelics.Count; i++)
        {
            if (ownedRelics[i].effect != null)
                result = ownedRelics[i].effect.ModifyShopPrice(result);
        }

        return result;
    }

    public void ResetForNewRun()
    {
        ownedRelics.Clear();
        ownedRelicIds.Clear();
    }

    private RelicContext BuildContext()
    {
        return new RelicContext { player = playerActor };
    }
}
