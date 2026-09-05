using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCombatAI : MonoBehaviour
{
    public CombatActor enemyActor;
    public CombatActor playerActor;
    public int defendBonus = 10;
    public float actionDelay = 0.4f;

    public float animationDelay = 0.3f;

    [Header("炸彈放置設定")]
    [Tooltip("是否啟用此敵人的炸彈放置行為")]
    [SerializeField] private bool canPlaceBomb = true;

    [Tooltip("每次行動放置炸彈的機率（0 = 永不放置，1 = 必定放置）")]
    [SerializeField, Range(0f, 1f)] private float bombPlacementChance = 0.5f;

    [Tooltip("炸彈放置時的初始倒數回合數")]
    [SerializeField] private int bombInitialTurns = 3;

    private Animator animator;

    private EnemyPoise enemyPoise;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        enemyPoise = GetComponent<EnemyPoise>();
    }

    public bool CanAct()
    {
        return enemyActor != null
            && playerActor != null
            && !playerActor.IsDead
            && enemyActor.currentHp > 0;
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

        yield return TryPlaceRandomBomb();
    }

    private IEnumerator ExecuteAttack()
    {
        animator.SetBool("Attack", true);
        yield return new WaitForSeconds(0.3f);
        animator.SetBool("Attack", false);

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

    private IEnumerator TryPlaceRandomBomb()
{
    if (!canPlaceBomb)
        yield break;

    if (Random.value > bombPlacementChance)
    {
        Debug.Log("[炸彈] 本回合骰到不放置炸彈");
        yield break;
    }

    if (BattleBombManager.Instance == null)
    {
        Debug.LogWarning("[炸彈] 場景中找不到 BattleBombManager，無法放置炸彈");
        yield break;
    }

    if (!BattleBombManager.Instance.CanRegisterMoreBombs())
    {
        Debug.Log($"[炸彈] 場上炸彈已達上限（{BattleBombManager.Instance.ActiveBombCount}/{BattleBombManager.Instance.MaxActiveBombs}），本回合放棄放置");
        yield break;
    }

    if (GesturePointRegistry.Instance == null)
    {
        Debug.LogWarning("[炸彈] 場景中找不到 GesturePointRegistry，無法放置炸彈");
        yield break;
    }

    List<int> availablePointIds = new List<int>();
    var rules = GesturePointRegistry.Instance.GetAllRules();

    for (int i = 0; i < rules.Count; i++)
    {
        var rule = rules[i];
        if (rule == null || rule.point == null)
            continue;

        if (BattleBombManager.Instance.HasBombAt(rule.pointId))
            continue;

        availablePointIds.Add(rule.pointId);
    }

    if (availablePointIds.Count == 0)
    {
        Debug.Log("[炸彈] 所有點位都已經有炸彈，無法再放置");
        yield break;
    }

    int chosenPointId = availablePointIds[Random.Range(0, availablePointIds.Count)];
    bool success = BattleBombManager.Instance.RegisterBomb(chosenPointId, bombInitialTurns);

    if (success)
    {
        CombatUI.Instance?.AppendBattleLog($"{enemyActor.actorId} 在法陣點位 {chosenPointId} 放置了炸彈");
        yield return new WaitForSeconds(actionDelay);
    }
}

}