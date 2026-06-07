using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager _turnManager;

    private readonly List<EnemyDead> _enemies = new();
    private bool _battleEnded = false;
    private bool _isRunningEnemyTurn = false;

    private void OnEnable()
    {
        if (_turnManager != null)
            _turnManager.OnEnemyTurnStart += HandleEnemyTurnStart;
    }

    private void OnDisable()
    {
        if (_turnManager != null)
            _turnManager.OnEnemyTurnStart -= HandleEnemyTurnStart;
    }

    public void StartBattle(MapNodeType nodeType)
    {
        _battleEnded = false;
        _isRunningEnemyTurn = false;
        _enemies.Clear();

        if (_turnManager != null)
            _turnManager.ResetBattle();

        RegisterAllEnemiesInScene();
    }

    private void RegisterAllEnemiesInScene()
    {
        EnemyDead[] enemies = Object.FindObjectsByType<EnemyDead>(FindObjectsInactive.Include);

        foreach (EnemyDead enemy in enemies)
        {
            RegisterEnemy(enemy);
        }
    }

    public void RegisterEnemy(EnemyDead enemy)
    {
        if (enemy == null) return;
        if (_enemies.Contains(enemy)) return;

        _enemies.Add(enemy);
        enemy.SetBattleController(this);
    }

    public void NotifyEnemyDeath(EnemyDead deadEnemy)
    {
        if (_battleEnded) return;
        CheckBattleResult();
    }

    private void HandleEnemyTurnStart()
    {
        if (_battleEnded) return;
        if (_isRunningEnemyTurn) return;

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

        if (_turnManager != null)
            _turnManager.IsBattleLocked = true;

        GameFlowController.Instance.EnterVictory();
    }
}