using UnityEngine;

public class SkillExecutionContext
{
    public CombatActor self;
    public CombatActor target;
    public GestureResult gestureResult;
    public SkillExecutionReport report;
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

    public abstract void Execute(SkillExecutionContext context);
}
