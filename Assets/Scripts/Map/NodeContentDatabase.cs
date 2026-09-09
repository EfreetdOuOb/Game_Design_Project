using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NodeContentDatabase", menuName = "Game/Node Content/Database")]
public class NodeContentDatabase : ScriptableObject
{
    public List<BattleEncounterDefinition> battleEncounters = new();
    public List<NodeRewardDefinition> nodeRewards = new();

    public BattleEncounterDefinition GetBattleEncounter(string contentId)
    {
        return battleEncounters.Find(x => x != null && x.contentId == contentId);
    }

    public NodeRewardDefinition GetNodeReward(string contentId)
    {
        return nodeRewards.Find(x => x != null && x.contentId == contentId);
    }
}