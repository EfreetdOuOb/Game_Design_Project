using System.Collections;
using UnityEngine;

public class EnemyCombatAI : MonoBehaviour
{
    public CombatActor enemyActor;
    public CombatActor playerActor;
    public int defendBonus = 10;
    public float actionDelay = 0.4f;

    private EnemyPoise enemyPoise;

    private void Awake()
    {
        enemyPoise = GetComponent<EnemyPoise>();
    }

    public bool CanAct()
    {
        return enemyActor != null && playerActor != null && enemyActor.currentHp > 0;
    }

    public IEnumerator ExecuteTurn()
    {
        if (!CanAct())
            yield break;

        if (enemyPoise != null && enemyPoise.TryConsumeStunTurn())
            yield break;

        int turn = TurnManager.Instance != null ? TurnManager.Instance.TurnNumber : 1;

        if (turn % 2 == 1)
            yield return ExecuteAttack();
        else
            yield return ExecuteDefend();
    }

    private IEnumerator ExecuteAttack()
    {
        int damage = enemyActor.attackPower;
        int actual = playerActor.ReceiveDamage(damage);

        if (actual == 0 && enemyPoise != null)
        {
            enemyPoise.ReducePoise(1);
            CombatUI.Instance?.AppendBattleLog($"{enemyActor.actorId} 的攻擊被完全防禦，韌性 -1");
        }

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