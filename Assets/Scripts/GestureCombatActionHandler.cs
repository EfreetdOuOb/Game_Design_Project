using System.Text;
using UnityEngine;

public class GestureCombatActionHandler : MonoBehaviour, IGestureActionHandler
{
    [Header("Dependencies")]
    public CombatActor self;
    public CombatActor target;
    public SkillLibrary skillLibrary;
    public MonoBehaviour skillSlotProviderBehaviour;
    public PlayerAnimator playerAnimator;

    [Header("Tuning")]
    public int baseAttackDamage = 15;
    public int defenseBonusPerPoint = 10;
    public bool enableSummaryLog = true;
    public bool scaleAttackWithActorPower = false;

    [Header("Poise")]
    [SerializeField] private int attackPointsPerPoiseDamage = 3;

    

    private ISkillSlotProvider skillSlotProvider;
    private int currentTurnAttackPointCount = 0;

    private void Awake()
    {
        skillSlotProvider = skillSlotProviderBehaviour as ISkillSlotProvider;
        if (self == null)
            self = GetComponent<CombatActor>();
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

    private void Start()
    {
        AutoSelectFirstAliveEnemy();
    }

    private void HandlePlayerTurnStart()
    {
        currentTurnAttackPointCount = 0;
        Debug.Log("[韌性判定] 玩家新回合開始，攻擊點累積已清空");

        if (!HasValidTarget())
            AutoSelectFirstAliveEnemy();
    }

    public void SetTarget(CombatActor newTarget)
    {
        if (newTarget == null)
            return;

        if (newTarget.currentHp <= 0)
            return;

        target = newTarget;
        Debug.Log($"[Target] 目前目標 => {target.actorId}");
    }

    public void ClearTarget()
    {
        target = null;
        Debug.Log("[Target] 目前目標已清空");
    }

    public bool HasValidTarget()
    {
        return target != null && target.currentHp > 0;
    }

    public void AutoSelectFirstAliveEnemy()
    {
        EnemyCombatAI[] enemies = Object.FindObjectsByType<EnemyCombatAI>(FindObjectsInactive.Exclude);

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null)
                continue;

            if (enemies[i].enemyActor == null)
                continue;

            if (enemies[i].enemyActor.currentHp <= 0)
                continue;

            SetTarget(enemies[i].enemyActor);
            return;
        }

        ClearTarget();
    }

    public void OnGestureResolved(GestureResult result)
    {
        if (result == null)
            return;

        if (BattleBombManager.Instance != null)
        {
            for (int i = 0; i < result.pointSnapshots.Count; i++)
            {
                BattleBombManager.Instance.NotifyPointTouched(result.pointSnapshots[i].pointId);
            }
        }

        if (!HasValidTarget())
            AutoSelectFirstAliveEnemy();

        int attackCount = CountPoints(result, PointFunction.Attack);
        int defenseCount = CountPoints(result, PointFunction.Defense);
        int skillCount = CountPoints(result, PointFunction.Skill);

        Debug.Log($"[攻擊判定] 本次結算 Attack={attackCount}, Defense={defenseCount}, Skill={skillCount}");

        int totalAttackDamage = ExecuteAttack(attackCount);
        int totalDefenseAdded = ExecuteDefense(defenseCount);
        AccumulateAttackPointsAndApplyPoise(attackCount);
        SkillExecutionReport skillReport = ExecuteSkill(result, skillCount);

        LogTurnSummary(result, attackCount, defenseCount, skillCount, totalAttackDamage, totalDefenseAdded, skillReport);

        if (attackCount > 0)
        {
            if (HasValidTarget())
                CombatUI.Instance?.AppendBattleLog($"玩家攻擊 {attackCount} 次，造成 {totalAttackDamage} 傷害");
            else
                CombatUI.Instance?.AppendBattleLog("玩家有攻擊點，但目前沒有可攻擊的敵人");
        }
    }

    private int ExecuteAttack(int attackCount)
    {
        if (target == null || target.currentHp <= 0 || attackCount <= 0)
            return 0;

        if (playerAnimator != null)
            playerAnimator.OnPlayerAttack();

        int totalDamage = 0;
        for (int i = 0; i < attackCount; i++)
        {
            if (target == null || target.currentHp <= 0)
                break;

            int rawDamage = baseAttackDamage;
            if (scaleAttackWithActorPower && self != null)
                rawDamage += self.attackPower;

            int actualDamage = target.ReceiveDamage(rawDamage);
            totalDamage += actualDamage;
        }

        return totalDamage;
    }

    private void AccumulateAttackPointsAndApplyPoise(int attackCount)
    {
        if (attackCount <= 0)
            return;

        if (target == null)
        {
            Debug.Log("[韌性判定] target 是 null，無法累積韌性扣除");
            return;
        }

        if (target.currentHp <= 0)
        {
            Debug.Log($"[韌性判定] {target.actorId} 已死亡，不進行韌性累積");
            return;
        }

        EnemyPoise poise = target.GetComponent<EnemyPoise>();
        if (poise == null)
            poise = target.GetComponentInParent<EnemyPoise>();

        if (poise == null)
        {
            Debug.LogWarning($"[韌性判定] 在 {target.actorId} 身上找不到 EnemyPoise");
            return;
        }

        currentTurnAttackPointCount += attackCount;
        Debug.Log($"[韌性判定] 本回合累積攻擊點 = {currentTurnAttackPointCount}");

        if (attackPointsPerPoiseDamage <= 0)
            attackPointsPerPoiseDamage = 3;

        int poiseDamage = currentTurnAttackPointCount / attackPointsPerPoiseDamage;

        if (poiseDamage <= 0)
        {
            Debug.Log($"[韌性判定] 尚未達到 {attackPointsPerPoiseDamage} 點攻擊，不扣韌性");
            return;
        }

        currentTurnAttackPointCount %= attackPointsPerPoiseDamage;

        Debug.Log($"[韌性判定] 達成扣韌性條件，扣除 {poiseDamage} 點韌性，剩餘累積攻擊點 = {currentTurnAttackPointCount}");

        poise.ReducePoise(poiseDamage);
        CombatUI.Instance?.AppendBattleLog($"{target.actorId} 因連續攻擊，韌性 -{poiseDamage}");
    }

    private int ExecuteDefense(int defenseCount)
    {
        if (self == null || defenseCount <= 0)
            return 0;

        int totalDefense = defenseCount * defenseBonusPerPoint;
        self.AddTemporaryDefense(totalDefense);
        return totalDefense;
    }

    private SkillExecutionReport ExecuteSkill(GestureResult result, int skillCount)
    {
        SkillExecutionReport report = new SkillExecutionReport();
        if (skillCount <= 0)
            return report;

        string skillId = ResolveSkillId(result);
        if (string.IsNullOrEmpty(skillId))
        {
            Debug.LogWarning("[戰鬥] 技能動作找不到可用的技能ID。");
            return report;
        }

        if (skillLibrary == null)
        {
            Debug.LogWarning("[戰鬥] 缺少 SkillLibrary 參考。");
            return report;
        }

        if (!skillLibrary.TryGet(skillId, out SkillDefinition skill))
        {
            Debug.LogWarning($"[戰鬥] 技能庫中找不到技能：{skillId}");
            return report;
        }

        SkillExecutionContext context = new SkillExecutionContext
        {
            self = self,
            target = target,
            gestureResult = result,
            report = report
        };

        skill.Execute(context);
        return report;
    }

    private string ResolveSkillId(GestureResult result)
    {
        for (int i = result.pointSnapshots.Count - 1; i >= 0; i--)
        {
            GesturePointSnapshot snap = result.pointSnapshots[i];
            if (snap.finalFunction != PointFunction.Skill)
                continue;

            if (!string.IsNullOrEmpty(snap.resolvedSkillId))
                return snap.resolvedSkillId;

            if (skillSlotProvider != null && skillSlotProvider.TryGetSkillIdForPoint(snap.pointId, out string fallbackSkillId))
                return fallbackSkillId;
        }

        return string.Empty;
    }

    private int CountPoints(GestureResult result, PointFunction function)
    {
        int count = 0;
        for (int i = 0; i < result.pointSnapshots.Count; i++)
        {
            if (result.pointSnapshots[i].finalFunction == function)
                count++;
        }

        return count;
    }

    private void LogTurnSummary(
        GestureResult result,
        int attackCount,
        int defenseCount,
        int skillCount,
        int totalAttackDamage,
        int totalDefenseAdded,
        SkillExecutionReport skillReport)
    {
        if (!enableSummaryLog)
            return;

        StringBuilder points = new StringBuilder();
        for (int i = 0; i < result.pointSnapshots.Count; i++)
        {
            GesturePointSnapshot snap = result.pointSnapshots[i];
            if (i > 0)
                points.Append(" -> ");

            string skill = string.IsNullOrEmpty(snap.resolvedSkillId) ? "-" : snap.resolvedSkillId;
            points.Append($"[{snap.pointId}:{snap.finalFunction}:技能{skill}]");
        }

        string debuffText = (skillReport != null && skillReport.appliedDebuffs.Count > 0)
            ? string.Join(", ", skillReport.appliedDebuffs)
            : "-";

        int skillDamage = skillReport != null ? skillReport.totalDamageDealt : 0;
        int skillHeal = skillReport != null ? skillReport.totalHealDone : 0;
        int skillDefense = skillReport != null ? skillReport.totalDefenseAdded : 0;

        Debug.Log(
            "[總結]\n" +
            $"目前目標: {(target != null ? target.actorId : "None")}\n" +
            $"路徑: {points}\n" +
            $"攻擊點數={attackCount}（攻擊次數={attackCount}，總傷害={totalAttackDamage}）\n" +
            $"防禦點數={defenseCount}（本回合防禦加成={totalDefenseAdded}）\n" +
            $"技能點數={skillCount}（技能傷害={skillDamage}，技能治療={skillHeal}，技能防禦={skillDefense}，Debuff={debuffText}）\n" +
            $"本回合剩餘攻擊點累積={currentTurnAttackPointCount}");
    }
}