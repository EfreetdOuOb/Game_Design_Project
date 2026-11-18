using UnityEngine;

public abstract class SkillEffectDefinition : ScriptableObject
{
    public abstract void Apply(SkillExecutionContext context);

    /// <summary>這個效果的一行說明（給技能懸浮說明組合用）；沒有可說明的就回傳 null。</summary>
    public virtual string Describe(bool upgraded)
    {
        return null;
    }

    /// <summary>強化後這個效果會多出什麼；沒有就回傳 null。</summary>
    public virtual string DescribeUpgrade()
    {
        return null;
    }
}
