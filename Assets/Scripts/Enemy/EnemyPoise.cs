using UnityEngine;

public class EnemyPoise : MonoBehaviour
{
    [Header("Poise Settings")]
    [SerializeField] private int maxPoise = 3;
    [SerializeField] private int currentPoise = 3;

    [Header("Stun Settings")]
    [SerializeField] private int stunTurnsOnBreak = 1;

    private int pendingStunTurns = 0;
    private bool restorePoiseAfterStun = false;
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
            restorePoiseAfterStun = true;

            Debug.Log($"[韌性] {ActorName} 韌性歸零，進入暈眩 {pendingStunTurns} 回合");
            CombatUI.Instance?.AppendBattleLog($"{ActorName} 韌性歸零，陷入暈眩");

            OnPoiseBroken?.Invoke();
        }
    }

    // 麻痺技能用：直接讓敵人暈眩指定回合數，不需要先打破韌性，
    // 而且結束後不會像韌性歸零那樣把韌性補滿。
    public void ApplyStun(int turns)
    {
        if (turns <= 0)
            return;

        pendingStunTurns += turns;
        Debug.Log($"[韌性] {ActorName} 被麻痺，暈眩 +{turns} 回合（目前 {pendingStunTurns}）");
        CombatUI.Instance?.AppendBattleLog($"{ActorName} 被麻痺，暈眩 {pendingStunTurns} 回合");
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
            Debug.Log($"[韌性] {ActorName} 暈眩結束");

            if (restorePoiseAfterStun)
            {
                restorePoiseAfterStun = false;
                ResetPoiseToFull();
                CombatUI.Instance?.AppendBattleLog($"{ActorName} 從暈眩中恢復，韌性恢復");
            }
            else
            {
                CombatUI.Instance?.AppendBattleLog($"{ActorName} 從麻痺中恢復");
            }
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