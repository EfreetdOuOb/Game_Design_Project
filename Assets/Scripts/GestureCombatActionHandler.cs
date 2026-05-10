using System.Text;
using UnityEngine;

public class GestureCombatActionHandler : MonoBehaviour, IGestureActionHandler
{
    [Header("Dependencies")]
    public CombatActor self;
    public CombatActor target;
    public SkillLibrary skillLibrary;
    public MonoBehaviour skillSlotProviderBehaviour;

    [Header("Tuning")]
    public int baseAttackDamage = 15;
    public int defenseBonusPerPoint = 10;
    public bool enableSummaryLog = true;
    public bool scaleAttackWithActorPower = false;

    private ISkillSlotProvider skillSlotProvider;

    private void Awake()
    {
        skillSlotProvider = skillSlotProviderBehaviour as ISkillSlotProvider;
    }

    public void OnGestureResolved(GestureResult result)
    {
        if (result == null)
        {
            return;
        }

        

        int attackCount = CountPoints(result, PointFunction.Attack);
        int defenseCount = CountPoints(result, PointFunction.Defense);
        int skillCount = CountPoints(result, PointFunction.Skill);

        int totalAttackDamage = ExecuteAttack(attackCount);
        int totalDefenseAdded = ExecuteDefense(defenseCount);
        SkillExecutionReport skillReport = ExecuteSkill(result, skillCount);

        LogTurnSummary(result, attackCount, defenseCount, skillCount, totalAttackDamage, totalDefenseAdded, skillReport);
    }

    private int ExecuteAttack(int attackCount)
    {
        if (target == null || attackCount <= 0)
        {
            return 0;
        }

        int totalDamage = 0;
        for (int i = 0; i < attackCount; i++)
        {
            int rawDamage = baseAttackDamage;
            if (scaleAttackWithActorPower && self != null)
            {
                rawDamage += self.attackPower;
            }
            int actualDamage = target.ReceiveDamage(rawDamage);
            totalDamage += actualDamage;
        }

        return totalDamage;
    }

    private int ExecuteDefense(int defenseCount)
    {
        if (self == null || defenseCount <= 0)
        {
            return 0;
        }

        int totalDefense = defenseCount * defenseBonusPerPoint;
        self.AddTemporaryDefense(totalDefense);
        return totalDefense;
    }

    private SkillExecutionReport ExecuteSkill(GestureResult result, int skillCount)
    {
        SkillExecutionReport report = new SkillExecutionReport();
        if (skillCount <= 0)
        {
            return report;
        }

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
            {
                continue;
            }

            if (!string.IsNullOrEmpty(snap.resolvedSkillId))
            {
                return snap.resolvedSkillId;
            }

            if (skillSlotProvider != null && skillSlotProvider.TryGetSkillIdForPoint(snap.pointId, out string fallbackSkillId))
            {
                return fallbackSkillId;
            }
        }

        return string.Empty;
    }

    private int CountPoints(GestureResult result, PointFunction function)
    {
        int count = 0;
        for (int i = 0; i < result.pointSnapshots.Count; i++)
        {
            if (result.pointSnapshots[i].finalFunction == function)
            {
                count++;
            }
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
        {
            return;
        }

        StringBuilder points = new StringBuilder();
        for (int i = 0; i < result.pointSnapshots.Count; i++)
        {
            GesturePointSnapshot snap = result.pointSnapshots[i];
            if (i > 0)
            {
                points.Append(" -> ");
            }

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
            $"路徑: {points}\n" +
            $"攻擊點數={attackCount}（攻擊次數={attackCount}，總傷害={totalAttackDamage}）\n" +
            $"防禦點數={defenseCount}（本回合防禦加成={totalDefenseAdded}）\n" +
            $"技能點數={skillCount}（技能傷害={skillDamage}，技能治療={skillHeal}，技能防禦={skillDefense}，Debuff={debuffText}）");
    }
}
