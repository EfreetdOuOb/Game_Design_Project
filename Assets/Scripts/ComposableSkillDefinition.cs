using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GestureCombat/Skills/Composable Skill", fileName = "SK_Composable")]
public class ComposableSkillDefinition : SkillDefinition
{
    public List<SkillEffectDefinition> effects = new List<SkillEffectDefinition>();

    public override void Execute(SkillExecutionContext context)
    {
        for (int i = 0; i < effects.Count; i++)
        {
            SkillEffectDefinition effect = effects[i];
            if (effect == null)
            {
                continue;
            }

            effect.Apply(context);
        }
    }

    public override System.Collections.Generic.List<string> DescribeLines(bool upgraded)
    {
        var lines = new System.Collections.Generic.List<string>();
        for (int i = 0; i < effects.Count; i++)
        {
            string text = effects[i] != null ? effects[i].Describe(upgraded) : null;
            if (!string.IsNullOrEmpty(text))
                lines.Add(text);
        }

        return lines;
    }

    public override System.Collections.Generic.List<string> DescribeUpgradeLines()
    {
        var lines = new System.Collections.Generic.List<string>();
        for (int i = 0; i < effects.Count; i++)
        {
            string text = effects[i] != null ? effects[i].DescribeUpgrade() : null;
            if (!string.IsNullOrEmpty(text))
                lines.Add(text);
        }

        return lines;
    }
}
