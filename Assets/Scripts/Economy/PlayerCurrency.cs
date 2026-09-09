using System;
using UnityEngine;

/// <summary>
/// 玩家金幣的單一來源，比照專案裡其他管理器（TurnManager、CombatUI…）的單例風格。
/// 只管錢的加減與查詢，不認識商店、遺物、事件等任何上層概念。
/// </summary>
public class PlayerCurrency : MonoBehaviour
{
    public static PlayerCurrency Instance { get; private set; }

    [SerializeField] private int startingGold = 0;

    public int CurrentGold { get; private set; }

    public event Action<int> OnGoldChanged; // 目前總金額

    private void Awake()
    {
        Instance = this;
        CurrentGold = Mathf.Max(0, startingGold);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        CurrentGold += amount;
        Debug.Log($"[金幣] 獲得 {amount} 金幣，目前共 {CurrentGold}");
        CombatUI.Instance?.AppendBattleLog($"獲得 {amount} 金幣");
        OnGoldChanged?.Invoke(CurrentGold);
    }

    public bool TrySpendGold(int amount)
    {
        if (amount < 0)
            return false;

        if (CurrentGold < amount)
            return false;

        CurrentGold -= amount;
        Debug.Log($"[金幣] 花費 {amount} 金幣，剩餘 {CurrentGold}");
        OnGoldChanged?.Invoke(CurrentGold);
        return true;
    }

    public void ResetForNewRun()
    {
        CurrentGold = Mathf.Max(0, startingGold);
        OnGoldChanged?.Invoke(CurrentGold);
    }
}
