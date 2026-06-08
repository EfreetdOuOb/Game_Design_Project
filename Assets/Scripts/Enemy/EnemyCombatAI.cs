using System.Collections;
using UnityEngine;

public class EnemyCombatAI : MonoBehaviour
{
    public CombatActor enemyActor;
    public CombatActor playerActor;
    public int defendBonus = 10;
    public float actionDelay = 0.4f;
    private int previousHp;
    private Animator ani;
    private bool isAnimating = false;

    private void Start()
    {
        ani = GetComponent<Animator>();
        if (enemyActor != null)
            previousHp = enemyActor.currentHp;
    }

    private void Update()
    {
        if (enemyActor == null || ani == null)
            return;

        if (enemyActor.currentHp < previousHp)
        {
            ani.SetTrigger("Hit");
            previousHp = enemyActor.currentHp;
            ani.SetTrigger("None");
        }
    }

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
        if (ani != null && !isAnimating)
        {
            isAnimating = true;
            ani.SetTrigger("Attack");
            yield return new WaitForSeconds(actionDelay);
            isAnimating = false;
            ani.SetTrigger("None");
        }
        else
        {
            yield return new WaitForSeconds(actionDelay);
        }
        
        int damage = enemyActor.attackPower;
        int actual = playerActor.ReceiveDamage(damage);
        CombatUI.Instance?.AppendBattleLog($"{enemyActor.actorId} 攻擊玩家，造成 {actual} 傷害");
    }

    private IEnumerator ExecuteDefend()
    {
        enemyActor.AddTemporaryDefense(defendBonus);
        CombatUI.Instance?.AppendBattleLog($"{enemyActor.actorId} 防守，獲得 +{defendBonus} 暫時防禦");
        yield return new WaitForSeconds(actionDelay);
    }
}