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
}
