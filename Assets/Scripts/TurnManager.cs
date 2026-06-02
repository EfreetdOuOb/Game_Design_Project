using System;
using UnityEngine;

public enum TurnPhase { PlayerTurn, EnemyTurn, Cleanup }

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public TurnPhase CurrentPhase { get; private set; } = TurnPhase.PlayerTurn;
    public int TurnNumber { get; private set; } = 1;

    // 其他系統訂閱這些事件
    public event Action OnPlayerTurnStart;
    public event Action OnPlayerTurnEnd;
    public event Action OnEnemyTurnStart;
    public event Action OnEnemyTurnEnd;
    public event Action OnTurnCleanup;   // ← 防禦在這裡才清除

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // 玩家手勢完成後呼叫
    public void EndPlayerTurn()
    {
        if (CurrentPhase != TurnPhase.PlayerTurn) return;
        CurrentPhase = TurnPhase.EnemyTurn;
        OnPlayerTurnEnd?.Invoke();
        StartEnemyTurn();
    }

    private void StartEnemyTurn()
    {
        OnEnemyTurnStart?.Invoke();

    // TODO: 之後換成真實敵人 AI，現在暫時直接結束敵人回合
    //EndEnemyTurn();
    }

    public void EndEnemyTurn()
    {
        if (CurrentPhase != TurnPhase.EnemyTurn) return;
        CurrentPhase = TurnPhase.Cleanup;
        OnEnemyTurnEnd?.Invoke();

        // 清算回合
        OnTurnCleanup?.Invoke();   // ← 防禦在這裡清除
        TurnNumber++;
        CurrentPhase = TurnPhase.PlayerTurn;
        OnPlayerTurnStart?.Invoke();
    }
}