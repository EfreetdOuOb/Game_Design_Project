using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyActionType
{
    Attack,
    Defense,
    HeavyAttack,
    Combo,
    Heal,
    Empower
}

[System.Serializable]
public class EnemyActionStep
{
    public EnemyActionType actionType = EnemyActionType.Attack;
    public string animationStateName = "";
    public float animationDuration = 0.3f;
    public GameObject intentIcon;
    public int healAmount = 10;
    public int attackPowerIncrease = 2;
}

public class EnemyCombatAI : MonoBehaviour
{
    public CombatActor enemyActor;
    public CombatActor playerActor;
    public int defendBonus = 10;
    public float actionDelay = 0.4f;
    public string attackAnimationStateName = "Armature|攻擊";
    public string hitAnimationStateName = "Armature|受擊";
    public string idleAnimationStateName = "Armature|idle";
    public float attackAnimationDuration = 0.3f;

    [Header("行動順序")]
    [Tooltip("敵人會依照列表順序行動，跑到最後一個動作後會從第一個動作重複。")]
    public List<EnemyActionStep> actionSequence = new List<EnemyActionStep>
    {
        new EnemyActionStep { actionType = EnemyActionType.Attack, animationStateName = "Armature|攻擊" },
        new EnemyActionStep { actionType = EnemyActionType.Defense, animationStateName = "" },
        new EnemyActionStep { actionType = EnemyActionType.HeavyAttack, animationStateName = "Armature|攻擊" }
    };

    [Header("炸彈放置設定")]
    [Tooltip("是否啟用此敵人的炸彈放置行為")]
    [SerializeField] private bool canPlaceBomb = true;

    [Tooltip("每次行動放置炸彈的機率（0 = 永不放置，1 = 必定放置）")]
    [SerializeField, Range(0f, 1f)] private float bombPlacementChance = 0.5f;

    [Tooltip("炸彈放置時的初始倒數回合數")]
    [SerializeField] private int bombInitialTurns = 3;

    private Animator animator;
    private EnemyHitAnimator animationController;
    private EnemyPoise enemyPoise;
    private int actionSequenceIndex;
    private bool defensePreparedThisTurn;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        enemyPoise = GetComponent<EnemyPoise>();

        animationController = GetComponent<EnemyHitAnimator>();
        if (animationController == null)
        {
            animationController = gameObject.AddComponent<EnemyHitAnimator>();
        }

        animationController.hitAnimationStateName = hitAnimationStateName;
        animationController.idleAnimationStateName = idleAnimationStateName;
    }

    private void OnEnable()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnPlayerTurnStart += HandlePlayerTurnStart;
    }

    private void OnDisable()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnPlayerTurnStart -= HandlePlayerTurnStart;
    }

    // 防禦動作要在玩家出手之前生效，所以在玩家回合開始時先套用當前序列動作。
    private void HandlePlayerTurnStart()
    {
        if (!CanAct())
            return;

        if (enemyPoise != null && enemyPoise.IsStunned)
            return;

        defensePreparedThisTurn = false;
        if (GetCurrentAction().actionType == EnemyActionType.Defense)
            PrepareDefense();
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

        EnemyActionStep actionStep = GetCurrentAction();
        EnemyActionType action = actionStep.actionType;
        AdvanceActionSequence();

        switch (action)
        {
            case EnemyActionType.Defense:
                yield return ExecuteDefend(actionStep);
                break;
            case EnemyActionType.HeavyAttack:
                yield return ExecuteHeavyAttack(actionStep);
                break;
            case EnemyActionType.Combo:
                yield return ExecuteCombo(actionStep);
                break;
            case EnemyActionType.Heal:
                yield return ExecuteHeal(actionStep);
                break;
            case EnemyActionType.Empower:
                yield return ExecuteEmpower(actionStep);
                break;
            default:
                yield return ExecuteAttack(actionStep);
                break;
        }

        yield return TryPlaceRandomBomb();
    }

    private EnemyActionStep GetCurrentAction()
    {
        if (actionSequence == null || actionSequence.Count == 0)
        {
            return new EnemyActionStep
            {
                actionType = EnemyActionType.Attack,
                animationStateName = attackAnimationStateName,
                animationDuration = attackAnimationDuration
            };
        }

        actionSequenceIndex = Mathf.Clamp(actionSequenceIndex, 0, actionSequence.Count - 1);
        if (actionSequence[actionSequenceIndex] == null)
        {
            actionSequence[actionSequenceIndex] = new EnemyActionStep();
        }

        return actionSequence[actionSequenceIndex];
    }

    public EnemyActionStep CurrentAction => GetCurrentAction();

    private void AdvanceActionSequence()
    {
        if (actionSequence == null || actionSequence.Count == 0)
            return;

        actionSequenceIndex = (actionSequenceIndex + 1) % actionSequence.Count;
    }

    private void PrepareDefense()
    {
        if (defensePreparedThisTurn || enemyActor == null)
            return;

        enemyActor.AddTemporaryDefense(defendBonus);
        CombatUI.Instance?.AppendBattleLog($"{enemyActor.actorId} 擺出防禦姿態，+{defendBonus} 暫時防禦");
        defensePreparedThisTurn = true;
    }

    private IEnumerator ExecuteAttack(EnemyActionStep actionStep)
    {
        yield return PlayActionAnimation(actionStep);

        DealDamage(enemyActor.attackPower, "攻擊");
        yield return new WaitForSeconds(actionDelay);
    }

    private IEnumerator ExecuteHeavyAttack(EnemyActionStep actionStep)
    {
        yield return PlayActionAnimation(actionStep);

        int damage = enemyActor.attackPower * 2;
        DealDamage(damage, "重擊");
        yield return new WaitForSeconds(actionDelay);
    }

    private IEnumerator ExecuteCombo(EnemyActionStep actionStep)
    {
        yield return PlayActionAnimation(actionStep);

        for (int i = 0; i < 3; i++)
        {
            if (playerActor == null || playerActor.IsDead)
                break;

            DealDamage(enemyActor.attackPower, $"連擊 ({i + 1}/3)");
            if (i < 2)
                yield return new WaitForSeconds(actionDelay);
        }

        yield return new WaitForSeconds(actionDelay);
    }

    private IEnumerator ExecuteHeal(EnemyActionStep actionStep)
    {
        yield return PlayActionAnimation(actionStep);

        int healAmount = Mathf.Max(0, actionStep.healAmount);
        int actualHealed = enemyActor.Heal(healAmount);
        CombatUI.Instance?.AppendBattleLog($"{enemyActor.actorId} 回血 {actualHealed} 點");
        yield return new WaitForSeconds(actionDelay);
    }

    private IEnumerator ExecuteEmpower(EnemyActionStep actionStep)
    {
        yield return PlayActionAnimation(actionStep);

        int attackPowerIncrease = Mathf.Max(0, actionStep.attackPowerIncrease);
        enemyActor.attackPower += attackPowerIncrease;
        CombatUI.Instance?.AppendBattleLog($"{enemyActor.actorId} 強化攻擊力 +{attackPowerIncrease}（目前 {enemyActor.attackPower}）");
        yield return new WaitForSeconds(actionDelay);
    }

    private IEnumerator PlayActionAnimation(EnemyActionStep actionStep)
    {
        string stateName = actionStep != null ? actionStep.animationStateName : string.Empty;
        float duration = actionStep != null ? actionStep.animationDuration : 0f;

        if (string.IsNullOrEmpty(stateName))
            stateName = attackAnimationStateName;

        if (duration <= 0f)
            duration = attackAnimationDuration;

        if (animationController != null)
        {
            yield return animationController.PlayAttackAnimationAndWait(stateName, duration);
        }
        else
        {
            yield return new WaitForSeconds(duration);
        }
    }

    private void DealDamage(int rawDamage, string actionName)
    {
        if (playerActor == null || playerActor.IsDead)
            return;

        int damage = Mathf.RoundToInt(rawDamage * enemyActor.GetOutgoingDamageMultiplier());
        int actual = playerActor.ReceiveDamage(damage);

        if (actual == 0 && enemyPoise != null)
        {
            enemyPoise.ReducePoise(1);
            CombatUI.Instance?.AppendBattleLog($"{enemyActor.actorId} 的攻擊被完全防禦，韌性 -1");
        }

        CombatUI.Instance?.AppendBattleLog($"{enemyActor.actorId} {actionName}玩家，造成 {actual} 傷害");
    }

    private IEnumerator ExecuteDefend(EnemyActionStep actionStep)
    {
        PrepareDefense();
        defensePreparedThisTurn = false;
        yield return PlayActionAnimation(actionStep);
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