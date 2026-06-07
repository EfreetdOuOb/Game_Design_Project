using System.Collections;
using UnityEngine;

public class EnemyCombatAI : MonoBehaviour
{
    public CombatActor enemyActor;
    public CombatActor playerActor;
    public int defendBonus = 10;
    public float actionDelay = 0.4f;

    public bool CanAct()
    {
        return enemyActor != null && playerActor != null && enemyActor.currentHp > 0;
    }

    public IEnumerator ExecuteTurn()
    {
        if (!CanAct())
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