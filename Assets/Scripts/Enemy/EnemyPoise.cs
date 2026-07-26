using UnityEngine;

public class EnemyPoise : MonoBehaviour
{
    [Header("Poise Settings")]
    [SerializeField] private int maxPoise = 3;
    [SerializeField] private int currentPoise = 3;

    [Header("Stun Settings")]
    [SerializeField] private int stunTurnsOnBreak = 1;

    private int pendingStunTurns = 0;
    private CombatActor actor;

    public int MaxPoise => maxPoise;
    public int CurrentPoise => currentPoise;
    public bool IsBroken => currentPoise <= 0;
    public bool IsStunned => pendingStunTurns > 0;

    public event System.Action OnPoiseBroken; // 韌性剛好歸零那一刻觸發一次

    private void Awake()
    {
        actor = GetComponent<CombatActor>();

        maxPoise = Mathf.Max(1, maxPoise);
        currentPoise = Mathf.Clamp(currentPoise <= 0 ? maxPoise : currentPoise, 0, maxPoise);
        stunTurnsOnBreak = Mathf.Max(1, stunTurnsOnBreak);
    }

    private string ActorName => actor != null ? actor.actorId : gameObject.name;

    public void ResetPoiseToFull()
    {
        currentPoise = maxPoise;
        Debug.Log($"[韌性] {ActorName} 韌性恢復為 {currentPoise}/{maxPoise}");
    }

    public void ReducePoise(int amount)
    {
        if (amount <= 0)
            return;

        if (IsStunned)
        {
            Debug.Log($"[韌性] {ActorName} 目前處於暈眩中，不再扣減韌性");
            return;
        }

        if (currentPoise <= 0)
        {
            Debug.Log($"[韌性] {ActorName} 韌性已經是 0，無法再扣");
            return;
        }

        int before = currentPoise;
        currentPoise = Mathf.Max(0, currentPoise - amount);

        Debug.Log($"[韌性] {ActorName} 韌性 -{amount}（{before} -> {currentPoise}/{maxPoise}）");

        if (currentPoise == 0)
        {
            pendingStunTurns = stunTurnsOnBreak;

            Debug.Log($"[韌性] {ActorName} 韌性歸零，進入暈眩 {pendingStunTurns} 回合");
            CombatUI.Instance?.AppendBattleLog($"{ActorName} 韌性歸零，陷入暈眩");

            OnPoiseBroken?.Invoke();
        }
    }

    public bool TryConsumeStunTurn()
    {
        if (pendingStunTurns <= 0)
            return false;

        pendingStunTurns--;

        Debug.Log($"[韌性] {ActorName} 因暈眩跳過行動，剩餘暈眩回合={pendingStunTurns}");
        CombatUI.Instance?.AppendBattleLog($"{ActorName} 暈眩，無法行動");

        if (pendingStunTurns <= 0)
        {
            ResetPoiseToFull();
            Debug.Log($"[韌性] {ActorName} 暈眩結束");
            CombatUI.Instance?.AppendBattleLog($"{ActorName} 從暈眩中恢復，韌性恢復");
        }

        return true;
    }

    public void SetCurrentPoise(int value)
    {
        int before = currentPoise;
        currentPoise = Mathf.Clamp(value, 0, maxPoise);
        Debug.Log($"[韌性] {ActorName} 韌性設定為 {currentPoise}/{maxPoise}（原本 {before}）");
    }
}