using System;
using UnityEngine;
using System.Collections.Generic;

public class CombatActor : MonoBehaviour
{
    public class DebuffState
    {
        public string debuffId;
        public int stacks;
        public int remainingTurns;
    }

    [Header("Stats")]
    public string actorId = "Actor";
    public int maxHp = 100;
    public int attackPower = 10;
    public int defensePower = 5;

    [Header("Runtime")]
    public int currentHp = 100;
    public int temporaryDefenseBonus = 0;
    [SerializeField] private List<DebuffState> debuffs = new List<DebuffState>();

    public IReadOnlyList<DebuffState> ActiveDebuffs => debuffs;

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
    }
    else
    {
        Debug.LogError($"[{actorId}] TurnManager.Instance 是 null，訂閱失敗！");
    }
}

    public int ReceiveDamage(int rawDamage)
    {
        if (IsDead)
        {
            return 0;
        }

        float damageMultiplier = GetDamageMultiplierFromDebuffs();
        int adjustedDamage = Mathf.RoundToInt(rawDamage * damageMultiplier);
        int reduced = Mathf.Max(0, adjustedDamage - temporaryDefenseBonus);
        currentHp = Mathf.Max(0, currentHp - reduced);
        Debug.Log($"[戰鬥] {actorId} 受到 {reduced} 傷害（原始={rawDamage}，倍率={damageMultiplier:F2}），HP={currentHp}/{maxHp}");
        CombatUI.Instance?.AppendBattleLog($"{actorId} 受到 {reduced} 傷害，HP {currentHp}/{maxHp}");
        if (reduced > 0)
            OnDamaged?.Invoke(reduced);

        if (currentHp <= 0)
        {
            MarkDead();
        }

        return reduced;
    }

    public void ResetToDefaultStats()
    {
        gameObject.SetActive(true);
        IsDead = false;
        currentHp = maxHp;
        temporaryDefenseBonus = 0;
        debuffs.Clear();
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

    public void AddTemporaryDefense(int amount)
    {
        temporaryDefenseBonus += Mathf.Max(0, amount);
        Debug.Log($"[戰鬥] {actorId} 防禦加成 +{amount}，目前總加成={temporaryDefenseBonus}");
    }

    private void ClearRoundDefense()
    {
        temporaryDefenseBonus = 0;
        Debug.Log($"[回合清算] {actorId} 防禦加成已清除");
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

            if (debuffs[i].debuffId == "Vulnerable")
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
