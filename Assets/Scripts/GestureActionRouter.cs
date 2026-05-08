using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class GestureActionRouter : MonoBehaviour, IGestureActionHandler, IGestureRuntimeActionHandler
{
    [Header("Actions")]
    public UnityEvent onAttack;
    public UnityEvent onDefense;
    public UnityEvent onSkill;
    public UnityEvent onTransform;

    [Header("Skill Pool")]
    public List<string> ownedSkillPool = new List<string>();
    [SerializeField] private List<string> shownSkills = new List<string>();

    public IReadOnlyList<string> ShownSkills => shownSkills;

    public void OnGestureResolved(GestureResult result)
    {
        EnsureShownSkillsInitialized();
        ApplyTransformedSkills(result);

        switch (result.resolvedFunction)
        {
            case PointFunction.Attack:
                onAttack?.Invoke();
                break;
            case PointFunction.Defense:
                onDefense?.Invoke();
                break;
            case PointFunction.Skill:
                onSkill?.Invoke();
                break;
            case PointFunction.Transform:
                onTransform?.Invoke();
                break;
        }
    }

    public void OnTransformActivated(GestureResult previewResult)
    {
        RerollShownSkills();
        onTransform?.Invoke();
    }

    private void EnsureShownSkillsInitialized()
    {
        if (shownSkills.Count == 0)
        {
            RerollShownSkills();
        }
    }

    private void RerollShownSkills()
    {
        shownSkills.Clear();
        if (ownedSkillPool == null || ownedSkillPool.Count == 0)
        {
            return;
        }

        List<string> candidates = new List<string>(ownedSkillPool);
        int pickCount = Mathf.Min(4, candidates.Count);
        for (int i = 0; i < pickCount; i++)
        {
            int index = Random.Range(0, candidates.Count);
            shownSkills.Add(candidates[index]);
            candidates.RemoveAt(index);
        }
    }

    private void ApplyTransformedSkills(GestureResult result)
    {
        for (int i = 0; i < result.pointSnapshots.Count; i++)
        {
            GesturePointSnapshot point = result.pointSnapshots[i];
            if (point.finalFunction != PointFunction.Skill)
            {
                continue;
            }

            int slotIndex = point.pointId - 5;
            if (slotIndex < 0 || slotIndex >= shownSkills.Count)
            {
                continue;
            }

            point.resolvedSkillId = shownSkills[slotIndex];
        }
    }
}
