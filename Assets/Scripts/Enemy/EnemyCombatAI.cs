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

    [Header("攻擊類招式（Attack / HeavyAttack / Combo）")]
    [Tooltip("這個招式每一下的傷害 = 敵人攻擊力 × 這個倍率。每個招式獨立調整，不互相影響。")]
    public float damageMultiplier = 1f;
    [Tooltip("這個招式連續打幾下（連擊設 3，其餘通常是 1）。")]
    [Min(1)] public int hitCount = 1;
}

public class EnemyCombatAI : MonoBehaviour
{
    public CombatActor enemyActor;
    public CombatActor playerActor;
    public float actionDelay = 0.4f;
    [Tooltip("敵人因暈眩跳過行動時，停下來給玩家看暈眩特效的時間（秒）")]
    public float stunBeatDuration = 1.0f;
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

        // 強化招式的「閃光 + 跳字」表現（預設版本，不用改 prefab）
        if (GetComponent<EnemyBuffFeedback>() == null)
            gameObject.AddComponent<EnemyBuffFeedback>();
    }

    private static readonly List<EnemyCombatAI> ActiveEnemies = new List<EnemyCombatAI>();

    /// <summary>目前啟用中的敵人（UI 預告傷害用，免得每幀去 Find）</summary>
    public static IReadOnlyList<EnemyCombatAI> Active => ActiveEnemies;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        ActiveEnemies.Clear();
    }

    private void OnEnable()
    {
        ActiveEnemies.Add(this);

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnPlayerTurnStart += HandlePlayerTurnStart;
    }

    private void OnDisable()
    {
        ActiveEnemies.Remove(this);

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnPlayerTurnStart -= HandlePlayerTurnStart;
    }

    /// <summary>
    /// 預告：這個敵人這回合出手會對玩家造成多少傷害（吃完虛弱、易傷等倍率，還沒扣護盾）。
    /// 暈眩中、無法行動、或這招不是攻擊類就回傳 0。
    /// </summary>
    public int PreviewDamageToPlayer()
    {
        if (!CanAct() || (enemyPoise != null && enemyPoise.IsStunned))
            return 0;

        EnemyActionStep step = GetCurrentAction();
        if (step.actionType != EnemyActionType.Attack
            && step.actionType != EnemyActionType.HeavyAttack
            && step.actionType != EnemyActionType.Combo)
            return 0;

        int perHit = DamageCalculator.Calculate(new DamageRequest(
            enemyActor, playerActor, enemyActor.EffectiveAttackPower, Mathf.Max(0f, step.damageMultiplier)));

        return perHit * Mathf.Max(1, step.hitCount);
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

        if (enemyPoise != null && enemyPoise.IsStunned)
        {
            // 先停一拍，讓玩家看到暈眩（星星、變色）再扣掉暈眩回合；
            // 不然暈眩剛成立就在同一刻被消耗，特效根本來不及出現
            yield return new WaitForSeconds(stunBeatDuration);
            enemyPoise.TryConsumeStunTurn();

            // 被跳過的這一回合，原本要出的招式也算用掉，下一回合輪到序列裡的下一招
            AdvanceActionSequence();
            yield break;
        }

        EnemyActionStep actionStep = GetCurrentAction();
        EnemyActionType action = actionStep.actionType;
        AdvanceActionSequence();

        switch (action)
        {
            case EnemyActionType.Defense:
                yield return ExecuteDefend(actionStep);
                break;
            case EnemyActionType.HeavyAttack:
            case EnemyActionType.Combo:
            case EnemyActionType.Attack:
                yield return ExecuteStrike(actionStep);
                break;
            case EnemyActionType.Heal:
                yield return ExecuteHeal(actionStep);
                break;
            case EnemyActionType.Empower:
                yield return ExecuteEmpower(actionStep);
                break;
            default:
                yield return ExecuteStrike(actionStep);
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

        int added = CombatActionResolver.Defend(enemyActor);
        CombatUI.Instance?.AppendBattleLog($"{enemyActor.actorId} 擺出防禦姿態，+{added} 護盾");
        defensePreparedThisTurn = true;
    }

    // 攻擊類招式共用：每一下傷害 = 攻擊力 × 該招式的 damageMultiplier，打 hitCount 下。
    // 各招式的傷害在 EnemyActionStep 上各自設定，不再寫死「重擊 2 倍、連擊 3 下」。
    private IEnumerator ExecuteStrike(EnemyActionStep actionStep)
    {
        yield return PlayActionAnimation(actionStep);

        int hits = Mathf.Max(1, actionStep.hitCount);
        float multiplier = Mathf.Max(0f, actionStep.damageMultiplier);
        string actionName = GetStrikeName(actionStep.actionType);

        for (int i = 0; i < hits; i++)
        {
            if (playerActor == null || playerActor.IsDead)
                break;

            DealDamage(multiplier, hits > 1 ? $"{actionName} ({i + 1}/{hits})" : actionName);
            if (i < hits - 1)
                yield return new WaitForSeconds(actionDelay);
        }

        yield return new WaitForSeconds(actionDelay);
    }

    private static string GetStrikeName(EnemyActionType type)
    {
        switch (type)
        {
            case EnemyActionType.HeavyAttack: return "重擊";
            case EnemyActionType.Combo: return "連擊";
            default: return "攻擊";
        }
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
        enemyActor.AddAttackBuff(attackPowerIncrease);
        CombatUI.Instance?.AppendBattleLog($"{enemyActor.actorId} 強化攻擊力 +{attackPowerIncrease}（目前 {enemyActor.EffectiveAttackPower}）");
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

    private void DealDamage(float powerMultiplier, string actionName)
    {
        if (playerActor == null || playerActor.IsDead)
            return;

        DamageResult result = CombatActionResolver.Strike(enemyActor, playerActor, powerMultiplier);

        if (result.FullyBlocked && enemyPoise != null)
        {
            enemyPoise.ReducePoise(1);
            CombatUI.Instance?.AppendBattleLog($"{enemyActor.actorId} 的攻擊被完全防禦，韌性 -1");
        }

        CombatUI.Instance?.AppendBattleLog($"{enemyActor.actorId} {actionName}玩家，造成 {result.HpLoss} 傷害");
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