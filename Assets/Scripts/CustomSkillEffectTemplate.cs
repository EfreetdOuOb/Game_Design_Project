using UnityEngine;

[CreateAssetMenu(menuName = "GestureCombat/Skill Effects/Custom Template", fileName = "EFF_CustomTemplate")]
public class CustomSkillEffectTemplate : SkillEffectDefinition
{
    [Header("Custom Params")]
    public string note = "在這裡放你自訂效果的參數";
    public int value = 1;

    public override void Apply(SkillExecutionContext context)
    {
        // 你可以從 context 取得執行所需資訊：
        // context.self          -> 施放者
        // context.target        -> 目標
        // context.gestureResult -> 本次手勢完整結果（點位/轉換/技能槽）
        if (context == null)
        {
            return;
        }

        // TODO: 在這裡寫你的自訂效果邏輯
        // 例：給敵人特殊狀態、讀取手勢點位條件、依轉換狀態做不同效果...
        Debug.Log($"[技能效果][模板] note={note}, value={value}");
    }
}
