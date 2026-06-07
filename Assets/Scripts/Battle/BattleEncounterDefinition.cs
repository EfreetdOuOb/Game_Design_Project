using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleEncounterDefinition", menuName = "Game/Node Content/Battle Encounter")]
public class BattleEncounterDefinition : ScriptableObject
{
    public string contentId;
    public MapNodeType nodeType = MapNodeType.Enemy;
    public List<GameObject> enemyPrefabs = new();
}