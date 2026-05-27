using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class GestureActionRouter : MonoBehaviour, IGestureActionHandler, IGestureRuntimeActionHandler, ISkillSlotProvider
{
    [Header("Actions")]
    public UnityEvent onAttack;
    public UnityEvent onDefense;
    public UnityEvent onSkill;
    public UnityEvent onTransform;

    [Header("Skill Pool")]
    public List<string> ownedSkillPool = new List<string>();
    [SerializeField] private List<string> shownSkills = new List<string>();
    public bool enableDebugLogs = false;

    public IReadOnlyList<string> ShownSkills => shownSkills;
    private void Start()
    {
        EnsureShownSkillsInitialized();
    }

    public void OnGestureResolved(GestureResult result)
    {
        EnsureShownSkillsInitialized();
        ApplyTransformedSkills(result);
        LogResolvedSkills(result);

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
                RerollShownSkills();
                onTransform?.Invoke();
                break;
        }
    }

    public void OnTransformActivated(GestureResult previewResult)
    {
        if (previewResult.transformPointIndex > 0)
        {
            RerollShownSkills();
            onTransform?.Invoke();
        }
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

    private void LogResolvedSkills(GestureResult result)
    {
        if (!enableDebugLogs || result == null)
        {
            return;
        }

        for (int i = 0; i < result.pointSnapshots.Count; i++)
        {
            GesturePointSnapshot point = result.pointSnapshots[i];
            if (point.finalFunction != PointFunction.Skill)
            {
                continue;
            }

            string skill = string.IsNullOrEmpty(point.resolvedSkillId) ? "(not assigned)" : point.resolvedSkillId;
            Debug.Log($"[手勢][技能] 點位={point.pointId}，最終功能={point.finalFunction}，技能={skill}");
        }
    }

    public bool TryGetSkillIdForPoint(int pointId, out string skillId)
    {
        int slotIndex = pointId - 5;
        if (slotIndex < 0 || slotIndex >= shownSkills.Count)
        {
            skillId = string.Empty;
            return false;
        }

        skillId = shownSkills[slotIndex];
        return !string.IsNullOrEmpty(skillId);
    }
}
