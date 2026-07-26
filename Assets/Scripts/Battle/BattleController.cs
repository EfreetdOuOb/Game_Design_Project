using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleController : MonoBehaviour
{
    [SerializeField] private TurnManager _turnManager;

    private readonly List<EnemyDead> _enemies = new();
    private CombatActor _playerActor;
    private bool _battleEnded = false;
    private bool _isRunningEnemyTurn = false;

    private void OnEnable()
    {
        if (_turnManager != null)
            _turnManager.OnEnemyTurnStart += HandleEnemyTurnStart;
    }

    private void OnDisable()
    {
        UnbindPlayer();
        if (_turnManager != null)
            _turnManager.OnEnemyTurnStart -= HandleEnemyTurnStart;
    }

    public void StartBattleWithEnemies(List<EnemyDead> enemies, CombatActor playerActor)
    {
        _battleEnded = false;
        _isRunningEnemyTurn = false;
        _enemies.Clear();
        BindPlayer(playerActor);

        foreach (EnemyDead enemy in enemies)
        {
            if (enemy == null) continue;
            _enemies.Add(enemy);
            enemy.SetBattleController(this);
        }

        BattleBombManager.Instance?.ClearAllBombs();

        _turnManager?.ResetBattle();
    }

    private void BindPlayer(CombatActor playerActor)
    {
        UnbindPlayer();
        _playerActor = playerActor;
        if (_playerActor != null)
        {
            _playerActor.OnDeath += HandlePlayerDeath;
        }
    }

    private void UnbindPlayer()
    {
        if (_playerActor != null)
        {
            _playerActor.OnDeath -= HandlePlayerDeath;
            _playerActor = null;
        }
    }

    public void NotifyEnemyDeath(EnemyDead deadEnemy)
    {
        if (_battleEnded) return;
        CheckBattleResult();
    }

    private void HandlePlayerDeath()
    {
        if (_battleEnded) return;

        _battleEnded = true;
        _isRunningEnemyTurn = false;
        StopAllCoroutines();

        BattleBombManager.Instance?.ClearAllBombs();

        if (_turnManager != null)
        {
            _turnManager.IsBattleLocked = true;
        }

        CombatUI.Instance?.AppendBattleLog("玩家陣亡");
        GameFlowController.Instance?.EnterDefeat();
    }

    private void HandleEnemyTurnStart()
    {
        if (_battleEnded || _isRunningEnemyTurn) return;
        if (_playerActor != null && _playerActor.IsDead) return;

        StartCoroutine(RunEnemyTurnSequence());
    }

    private IEnumerator RunEnemyTurnSequence()
    {
        _isRunningEnemyTurn = true;

        List<EnemyCombatAI> aliveEnemies = _enemies
            .Where(e => e != null && !e.IsDead)
            .Select(e => e.GetComponent<EnemyCombatAI>())
            .Where(ai => ai != null && ai.CanAct())
            .ToList();

        foreach (EnemyCombatAI enemyAI in aliveEnemies)
        {
            if (_battleEnded) yield break;
            yield return enemyAI.ExecuteTurn();
            CheckBattleResult();
            if (_battleEnded) yield break;
        }

        _isRunningEnemyTurn = false;

        if (_turnManager != null && _turnManager.CurrentPhase == TurnPhase.EnemyTurn)
            _turnManager.EndEnemyTurn();
    }

    public void CheckBattleResult()
    {
        if (_battleEnded) return;
        if (_enemies.Count == 0) return;

        bool allDead = _enemies.All(e => e == null || e.IsDead);
        if (!allDead) return;

        _battleEnded = true;
        _isRunningEnemyTurn = false;

        BattleBombManager.Instance?.ClearAllBombs();

        _turnManager.IsBattleLocked = true;

        GameFlowController.Instance.EnterVictory();
    }
}