using UnityEngine;

public class SkillExecutionContext
{
    public CombatActor self;
    public CombatActor target;
    public GestureResult gestureResult;
    public SkillExecutionReport report;

    // 這次施放的技能是否已在休息點升過級；技能／效果自己讀取決定要不要套用強化
    public bool skillUpgraded;

    // 場上所有存活的敵人，供群體（AoE）技能使用；單體技能忽略即可
    public System.Collections.Generic.IReadOnlyList<CombatActor> allEnemies;
}

public class SkillExecutionReport
{
    public int totalDamageDealt;
    public int totalHealDone;
    public int totalDefenseAdded;
    public readonly System.Collections.Generic.List<string> appliedDebuffs = new System.Collections.Generic.List<string>();
}

public abstract class SkillDefinition : ScriptableObject
{
    public string skillId;
    public string displayName;

    public Sprite icon;

    public abstract void Execute(SkillExecutionContext context);
}
