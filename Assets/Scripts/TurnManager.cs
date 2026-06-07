using System;
using UnityEngine;

public enum TurnPhase { PlayerTurn, EnemyTurn, Cleanup }

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public TurnPhase CurrentPhase { get; private set; } = TurnPhase.PlayerTurn;
    public int TurnNumber { get; private set; } = 1;
    public bool IsBattleLocked { get; set; } = false;

    public event Action OnPlayerTurnStart;
    public event Action OnPlayerTurnEnd;
    public event Action OnEnemyTurnStart;
    public event Action OnEnemyTurnEnd;
    public event Action OnTurnCleanup;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ResetBattle()
    {
        CurrentPhase = TurnPhase.PlayerTurn;
        TurnNumber = 1;
        IsBattleLocked = false;
        OnPlayerTurnStart?.Invoke();
    }

    public void EndPlayerTurn()
    {
        if (IsBattleLocked) return;
        if (CurrentPhase != TurnPhase.PlayerTurn) return;

        CurrentPhase = TurnPhase.EnemyTurn;
        OnPlayerTurnEnd?.Invoke();
        StartEnemyTurn();
    }

    private void StartEnemyTurn()
    {
        OnEnemyTurnStart?.Invoke();
    }

    public void EndEnemyTurn()
    {
        if (CurrentPhase != TurnPhase.EnemyTurn) return;

        CurrentPhase = TurnPhase.Cleanup;
        OnEnemyTurnEnd?.Invoke();
        OnTurnCleanup?.Invoke();

        TurnNumber++;
        CurrentPhase = TurnPhase.PlayerTurn;
        OnPlayerTurnStart?.Invoke();
    }
}