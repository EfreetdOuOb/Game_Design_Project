using System.Collections.Generic;
using UnityEngine;

public class SkillLibrary : MonoBehaviour
{
    public List<SkillDefinition> skills = new List<SkillDefinition>();

    private readonly Dictionary<string, SkillDefinition> map = new Dictionary<string, SkillDefinition>();

    private void Awake()
    {
        Rebuild();
    }

    public void Rebuild()
    {
        map.Clear();
        for (int i = 0; i < skills.Count; i++)
        {
            SkillDefinition skill = skills[i];
            if (skill == null || string.IsNullOrEmpty(skill.skillId))
            {
                continue;
            }

            if (!map.ContainsKey(skill.skillId))
            {
                map.Add(skill.skillId, skill);
            }
        }
    }

    public bool TryGet(string skillId, out SkillDefinition skill)
    {
        return map.TryGetValue(skillId, out skill);
    }
}
