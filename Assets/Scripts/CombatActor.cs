using System;
using UnityEngine;
using System.Collections.Generic;

public class CombatActor : MonoBehaviour
{
    public const string VulnerableDebuffId = "Vulnerable";
    public const string WeakDebuffId = "Weak";

    public class DebuffState
    {
        public string debuffId;
        public int stacks;
        public int remainingTurns;
    }

    private class RegenState
    {
        public int healPerTurn;
        public int remainingTurns;
    }

    [Header("Stats")]
    public string actorId = "Actor";
    public int maxHp = 100;
    public int attackPower = 10;
    public int defensePower = 5;

    [Header("Runtime")]
    public int currentHp = 100;
    [Tooltip("護盾：先吸收傷害並被扣掉，回合清算時歸零")]
    [UnityEngine.Serialization.FormerlySerializedAs("temporaryDefenseBonus")]
    public int shield = 0;
    [SerializeField] private List<DebuffState> debuffs = new List<DebuffState>();
    private readonly List<RegenState> regens = new List<RegenState>();

    [Header("虛弱設定")]
    [Tooltip("虛弱每層降低的輸出傷害比例")]
    [SerializeField] private float weakReductionPerStack = 0.2f;
    [Tooltip("虛弱能把輸出傷害壓到的最低比例")]
    [SerializeField] private float weakMinMultiplier = 0.2f;

    public IReadOnlyList<DebuffState> ActiveDebuffs => debuffs;

    private readonly List<IStatModifier> statModifiers = new List<IStatModifier>();

    // 有效攻擊／防禦 = 基礎數值 + 所有加成來源（技能升級、遺物…）。戰鬥結算一律用這兩個
    public int EffectiveAttackPower
    {
        get
        {
            int total = attackPower;
            for (int i = 0; i < statModifiers.Count; i++)
                total += statModifiers[i].AttackBonus;
            return Mathf.Max(0, total);
        }
    }

    public int EffectiveDefensePower
    {
        get
        {
            int total = defensePower;
            for (int i = 0; i < statModifiers.Count; i++)
                total += statModifiers[i].DefenseBonus;
            return Mathf.Max(0, total);
        }
    }

    public void AddStatModifier(IStatModifier modifier)
    {
        if (modifier != null && !statModifiers.Contains(modifier))
            statModifiers.Add(modifier);
    }

    public void RemoveStatModifier(IStatModifier modifier)
    {
        statModifiers.Remove(modifier);
    }

    public event Action OnDeath;
    public event Action<int> OnDamaged;

    public bool IsDead { get; private set; }

    private void Awake()
    {
        currentHp = Mathf.Clamp(currentHp <= 0 ? maxHp : currentHp, 0, maxHp);
        IsDead = currentHp <= 0;
    }
    private void Start()  
{
    if (TurnManager.Instance != null)
    {
        TurnManager.Instance.OnTurnCleanup += ClearRoundDefense;
        TurnManager.Instance.OnTurnCleanup += TickDebuffs;
        TurnManager.Instance.OnTurnCleanup += TickRegen;
    }
    else
    {
        Debug.LogError($"[{actorId}] TurnManager.Instance 是 null，訂閱失敗！");
    }
}

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnCleanup -= ClearRoundDefense;
            TurnManager.Instance.OnTurnCleanup -= TickDebuffs;
            TurnManager.Instance.OnTurnCleanup -= TickRegen;
        }
    }

    /// <summary>
    /// 直接扣血並處理受傷／死亡事件，不做任何傷害計算。
    /// 只給 <see cref="DamageCalculator"/> 呼叫；要傷害一個角色請走 DamageCalculator.Deal。
    /// </summary>
    public int ApplyHpLoss(int amount)
    {
        if (IsDead)
            return 0;

        int loss = Mathf.Max(0, amount);
        currentHp = Mathf.Max(0, currentHp - loss);
        if (loss > 0 && currentHp > 0)
            OnDamaged?.Invoke(loss);

        if (currentHp <= 0)
            MarkDead();

        return loss;
    }

    /// <summary>
    /// 護盾先吸收傷害，被吸收的部分會從護盾扣掉；回傳實際吸收量。
    /// 只給 <see cref="DamageCalculator"/> 呼叫。
    /// </summary>
    public int AbsorbWithShield(int damage)
    {
        int absorbed = Mathf.Clamp(damage, 0, shield);
        shield -= absorbed;
        return absorbed;
    }

    public void ResetToDefaultStats()
    {
        gameObject.SetActive(true);
        IsDead = false;
        currentHp = maxHp;
        shield = 0;
        debuffs.Clear();
        regens.Clear();
    }

    // 虛弱：降低這個角色「造成」的傷害。由 DamageCalculator 統一乘上。
    public float GetOutgoingDamageMultiplier()
    {
        int weakStacks = GetDebuffStacks(WeakDebuffId);
        if (weakStacks <= 0)
            return 1f;

        float multiplier = 1f - weakReductionPerStack * weakStacks;
        return Mathf.Clamp(multiplier, weakMinMultiplier, 1f);
    }

    // 持續回血（HoT）：每次回合清算時回復固定量，持續數回合
    public void AddRegen(int healPerTurn, int turns)
    {
        if (healPerTurn <= 0 || turns <= 0)
            return;

        regens.Add(new RegenState { healPerTurn = healPerTurn, remainingTurns = turns });
        Debug.Log($"[戰鬥] {actorId} 獲得持續回血，每回合 +{healPerTurn}，持續 {turns} 回合");
        CombatUI.Instance?.AppendBattleLog($"{actorId} 獲得持續回血（每回合 +{healPerTurn}，{turns} 回合）");
    }

    private void TickRegen()
    {
        for (int i = regens.Count - 1; i >= 0; i--)
        {
            RegenState regen = regens[i];
            int healed = Heal(regen.healPerTurn);
            if (healed > 0)
                CombatUI.Instance?.AppendBattleLog($"{actorId} 持續回血 +{healed}");

            regen.remainingTurns--;
            if (regen.remainingTurns <= 0)
                regens.RemoveAt(i);
        }
    }

    private void MarkDead()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;
        currentHp = 0;
        OnDeath?.Invoke();
    }

    public int Heal(int amount)
    {
        int before = currentHp;
        currentHp = Mathf.Min(maxHp, currentHp + Mathf.Max(0, amount));
        int actual = currentHp - before;
        Debug.Log($"[戰鬥] {actorId} 回復 {actual}，HP={currentHp}/{maxHp}");
        return actual;
    }

    public void AddShield(int amount)
    {
        shield += Mathf.Max(0, amount);
        Debug.Log($"[戰鬥] {actorId} 護盾 +{amount}，目前護盾={shield}");
    }

    private void ClearRoundDefense()
    {
        shield = 0;
        Debug.Log($"[回合清算] {actorId} 護盾已清除");
    }

    public void ApplyDebuff(string debuffId, int stacks, int durationTurns)
    {
        if (string.IsNullOrEmpty(debuffId))
        {
            return;
        }

        DebuffState state = null;
        for (int i = 0; i < debuffs.Count; i++)
        {
            if (debuffs[i].debuffId == debuffId)
            {
                state = debuffs[i];
                break;
            }
        }

        if (state == null)
        {
            state = new DebuffState
            {
                debuffId = debuffId,
                stacks = Mathf.Max(1, stacks),
                remainingTurns = Mathf.Max(1, durationTurns)
            };
            debuffs.Add(state);
        }
        else
        {
            state.stacks += Mathf.Max(1, stacks);
            state.remainingTurns = Mathf.Max(state.remainingTurns, durationTurns);
        }

        Debug.Log($"[戰鬥] {actorId} 被施加 Debuff：{debuffId}，層數={state.stacks}，剩餘回合={state.remainingTurns}");
    }

    public bool HasDebuff(string debuffId)
    {
        if (string.IsNullOrEmpty(debuffId) || debuffs == null)
        {
            return false;
        }

        for (int i = 0; i < debuffs.Count; i++)
        {
            if (debuffs[i] != null && debuffs[i].debuffId == debuffId)
            {
                return true;
            }
        }

        return false;
    }

    public int GetDebuffStacks(string debuffId)
    {
        if (string.IsNullOrEmpty(debuffId) || debuffs == null)
        {
            return 0;
        }

        for (int i = 0; i < debuffs.Count; i++)
        {
            if (debuffs[i] != null && debuffs[i].debuffId == debuffId)
            {
                return debuffs[i].stacks;
            }
        }

        return 0;
    }

    public float GetDamageMultiplierFromDebuffs()
    {
        if (debuffs == null || debuffs.Count == 0)
        {
            return 1f;
        }

        float multiplier = 1f;
        for (int i = 0; i < debuffs.Count; i++)
        {
            if (debuffs[i] == null)
            {
                continue;
            }

            if (debuffs[i].debuffId == VulnerableDebuffId)
            {
                multiplier *= 1f + (0.2f * debuffs[i].stacks);
            }
        }

        return multiplier;
    }

    private void TickDebuffs()
    {
        for (int i = debuffs.Count - 1; i >= 0; i--)
        {
            debuffs[i].remainingTurns--;
            if (debuffs[i].remainingTurns <= 0)
            {
                Debug.Log($"[回合清算] {actorId} Debuff 到期：{debuffs[i].debuffId}");
                debuffs.RemoveAt(i);
            }
        }
    }

    
}
