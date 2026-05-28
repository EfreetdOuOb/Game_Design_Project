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

    // Start 時 TurnManager.Instance 一定已經在 Awake 設好了
    if (TurnManager.Instance != null)
        TurnManager.Instance.OnPlayerTurnStart += OnPlayerTurnStart;
    else
        Debug.LogWarning("[GestureActionRouter] 找不到 TurnManager，無法訂閱 OnPlayerTurnStart");
}

private void OnDestroy()  // OnDisable 改成 OnDestroy，對應 Start 的訂閱
{
    if (TurnManager.Instance != null)
        TurnManager.Instance.OnPlayerTurnStart -= OnPlayerTurnStart;
}

    private void OnPlayerTurnStart()
    {
        UnlockAllSlots();
        RerollShownSkills();
    }

// 鎖定哪些槽位（index 對應 shownSkills 的位置）
private readonly HashSet<int> lockedSlots = new HashSet<int>();

// 外部呼叫：鎖定 / 解鎖某個槽位
public void LockSlot(int slotIndex)
{
    if (slotIndex >= 0 && slotIndex < shownSkills.Count)
        lockedSlots.Add(slotIndex);
}

public void UnlockSlot(int slotIndex) => lockedSlots.Remove(slotIndex);

public void UnlockAllSlots() => lockedSlots.Clear();

public bool IsSlotLocked(int slotIndex) => lockedSlots.Contains(slotIndex);

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
            foreach (var snap in previewResult.pointSnapshots)
            {
                if (snap.lockedBeforeTransform && snap.baseFunction == PointFunction.Skill)
                {
                    int slotIndex = snap.pointId - 5;
                    LockSlot(slotIndex);
                }
            }
            RerollShownSkills();
            onTransform?.Invoke();
            UnlockAllSlots();
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
    // 先把鎖定的技能記下來
    Dictionary<int, string> preserved = new Dictionary<int, string>();
    foreach (int lockedIdx in lockedSlots)
    {
        if (lockedIdx < shownSkills.Count)
            preserved[lockedIdx] = shownSkills[lockedIdx];
    }

    shownSkills.Clear();

    // 先填 4 個空位（null 代表待抽）
    for (int i = 0; i < 4; i++)
        shownSkills.Add(string.Empty);

    // 把鎖定的技能放回原位
    foreach (var kv in preserved)
        shownSkills[kv.Key] = kv.Value;

    // 候選池排除已鎖定的技能（避免重複抽到同一張）
    List<string> candidates = new List<string>(ownedSkillPool);
    foreach (var kv in preserved)
        candidates.Remove(kv.Value);

    // 只抽沒被鎖定的空槽
    for (int i = 0; i < shownSkills.Count; i++)
    {
        if (lockedSlots.Contains(i)) continue;    // 鎖定的跳過
        if (candidates.Count == 0) break;

        int idx = Random.Range(0, candidates.Count);
        shownSkills[i] = candidates[idx];
        candidates.RemoveAt(idx);
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
