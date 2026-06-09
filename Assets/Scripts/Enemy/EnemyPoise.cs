using UnityEngine;

public class EnemyPoise : MonoBehaviour
{
    [Header("Poise Settings")]
    [SerializeField] private int maxPoise = 3;
    [SerializeField] private int currentPoise = 3;

    [Header("Stun Settings")]
    [SerializeField] private int stunTurnsOnBreak = 1;

    private int pendingStunTurns = 0;

    public int MaxPoise => maxPoise;
    public int CurrentPoise => currentPoise;
    public bool IsBroken => currentPoise <= 0;
    public bool IsStunned => pendingStunTurns > 0;

    private void Awake()
    {
        maxPoise = Mathf.Max(1, maxPoise);
        currentPoise = Mathf.Clamp(currentPoise <= 0 ? maxPoise : currentPoise, 0, maxPoise);
        stunTurnsOnBreak = Mathf.Max(1, stunTurnsOnBreak);
    }

    public void ResetPoiseToFull()
    {
        currentPoise = maxPoise;
    }

    public void ReducePoise(int amount)
    {
        if (amount <= 0)
            return;

        if (IsStunned)
            return;

        if (currentPoise <= 0)
            return;

        currentPoise = Mathf.Max(0, currentPoise - amount);
        Debug.Log($"[Poise] {name} 韌性 -{amount}，目前 {currentPoise}/{maxPoise}");

        if (currentPoise == 0)
        {
            pendingStunTurns = stunTurnsOnBreak;

            CombatActor actor = GetComponent<CombatActor>();
            string actorName = actor != null ? actor.actorId : name;

            Debug.Log($"[Poise] {actorName} 韌性歸零，陷入暈眩 {pendingStunTurns} 回合");
            CombatUI.Instance?.AppendBattleLog($"{actorName} 韌性歸零，陷入暈眩");
        }
    }

    public bool TryConsumeStunTurn()
    {
        if (pendingStunTurns <= 0)
            return false;

        pendingStunTurns--;

        CombatActor actor = GetComponent<CombatActor>();
        string actorName = actor != null ? actor.actorId : name;

        Debug.Log($"[Poise] {actorName} 因暈眩跳過本回合行動，剩餘暈眩回合={pendingStunTurns}");
        CombatUI.Instance?.AppendBattleLog($"{actorName} 暈眩，無法行動");

        if (pendingStunTurns <= 0)
        {
            ResetPoiseToFull();
            Debug.Log($"[Poise] {actorName} 暈眩結束，韌性恢復為 {currentPoise}/{maxPoise}");
            CombatUI.Instance?.AppendBattleLog($"{actorName} 從暈眩中恢復，韌性恢復");
        }

        return true;
    }
}