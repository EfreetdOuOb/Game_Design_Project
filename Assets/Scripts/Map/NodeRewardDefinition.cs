using UnityEngine;

/// <summary>
/// 事件／寶箱節點的固定金幣獎勵設定，跟 BattleEncounterDefinition 一樣用 contentId 查表。
/// goldReward 設 0 就代表這個事件／寶箱不會給金幣，設計師可以自由決定每個節點給不給、給多少。
/// </summary>
[CreateAssetMenu(fileName = "NodeRewardDefinition", menuName = "Game/Node Content/Node Reward")]
public class NodeRewardDefinition : ScriptableObject
{
    public string contentId;

    [Tooltip("固定金幣獎勵，0 表示這個事件／寶箱不給金幣")]
    public int goldReward = 0;
}
