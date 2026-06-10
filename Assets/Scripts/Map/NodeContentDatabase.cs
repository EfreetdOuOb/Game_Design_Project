using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NodeContentDatabase", menuName = "Game/Node Content/Database")]
public class NodeContentDatabase : ScriptableObject
{
    public List<BattleEncounterDefinition> battleEncounters = new();

    public BattleEncounterDefinition GetBattleEncounter(string contentId)
    {
        return battleEncounters.Find(x => x != null && x.contentId == contentId);
    }
}