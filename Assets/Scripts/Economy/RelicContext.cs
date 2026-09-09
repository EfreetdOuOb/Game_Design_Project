/// <summary>
/// 遺物效果觸發時可以取用的上下文，比照 SkillExecutionContext 的做法。
/// 之後遺物效果需要更多資訊（例如目前敵人、目前金幣）可以直接加欄位，
/// 不用改動每一個 RelicEffectDefinition 的方法簽章。
/// </summary>
public class RelicContext
{
    public CombatActor player;
}
