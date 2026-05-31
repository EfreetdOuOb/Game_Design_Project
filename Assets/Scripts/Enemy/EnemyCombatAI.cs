using System.Collections;
using UnityEngine;

public class EnemyCombatAI : MonoBehaviour
{
    public CombatActor enemyActor;
    public CombatActor playerActor;
    public int defendBonus = 10;
    public float actionDelay = 0.4f;

    private bool isSubscribed;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (isSubscribed && TurnManager.Instance != null)
        {
            TurnManager.Instance.OnEnemyTurnStart -= OnEnemyTurnStart;
            isSubscribed = false;
        }
    }

    private void TrySubscribe()
    {
        if (isSubscribed) return;
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnEnemyTurnStart += OnEnemyTurnStart;
            isSubscribed = true;
        }
    }

    private void OnEnemyTurnStart()
    {
        StartCoroutine(RunTurnAndEnd());
    }

    private IEnumerator RunTurnAndEnd()
    {
        if (enemyActor == null || playerActor == null)
        {
            TurnManager.Instance?.EndEnemyTurn();
            yield break;
        }

        if (enemyActor.currentHp <= 0)
        {
            TurnManager.Instance?.EndEnemyTurn();
            yield break;
        }

        int turn = TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 1;

        if (turn % 2 == 1)
            yield return StartCoroutine(ExecuteAttack());
        else
            yield return StartCoroutine(ExecuteDefend());

        if (TurnManager.Instance != null && TurnManager.Instance.CurrentPhase == TurnPhase.EnemyTurn)
            TurnManager.Instance.EndEnemyTurn();
    }

    private IEnumerator ExecuteAttack()
    {
        int damage = enemyActor.attackPower;
        int actual = playerActor.ReceiveDamage(damage);
        CombatUI.Instance?.AppendBattleLog($"{enemyActor.actorId} 攻擊玩家，造成 {actual} 傷害");
        yield return new WaitForSeconds(actionDelay);
    }

    private IEnumerator ExecuteDefend()
    {
        enemyActor.AddTemporaryDefense(defendBonus);
        CombatUI.Instance?.AppendBattleLog($"{enemyActor.actorId} 防守，獲得 +{defendBonus} 暫時防禦");
        yield return new WaitForSeconds(actionDelay);
    }
}