using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NodeContentDatabase", menuName = "Game/Node Content/Database")]
public class NodeContentDatabase : ScriptableObject
{
    public List<BattleEncounterDefinition> battleEncounters = new();
    public List<NodeRewardDefinition> nodeRewards = new();
    public List<EventDefinition> eventDefinitions = new();

    public BattleEncounterDefinition GetBattleEncounter(string contentId)
    {
        return battleEncounters.Find(x => x != null && x.contentId == contentId);
    }

    public NodeRewardDefinition GetNodeReward(string contentId)
    {
        return nodeRewards.Find(x => x != null && x.contentId == contentId);
    }

    // 每次進事件節點時呼叫，從所有已設定的事件裡隨機抽一個
    public EventDefinition GetRandomEventDefinition()
    {
        if (eventDefinitions == null || eventDefinitions.Count == 0)
            return null;

        List<EventDefinition> validEvents = eventDefinitions.FindAll(x => x != null);
        if (validEvents.Count == 0)
            return null;

        return validEvents[Random.Range(0, validEvents.Count)];
    }
}