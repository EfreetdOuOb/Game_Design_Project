using System.Collections.Generic;
using UnityEngine;

public class NodeContentManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NodeContentDatabase _database;
    [SerializeField] private Transform _enemySpawnRoot;
    [SerializeField] private List<Transform> _enemySpawnPoints;
    [SerializeField] private BattleController _battleController;
    [SerializeField] private CombatActor _playerActor;
    [SerializeField] private GestureCombatActionHandler _playerGestureHandler;
    

    private readonly List<GameObject> _spawnedObjects = new();

    public void EnterNode(MapNodeData nodeData)
    {
        if (nodeData == null)
        {
            Debug.LogWarning("EnterNode 失敗：nodeData 是 null");
            return;
        }

        ClearCurrentContent();

        switch (nodeData.mapNodeType)
        {
            case MapNodeType.Enemy:
            case MapNodeType.Boss:
                EnterBattleNode(nodeData.contentId);
                break;

            case MapNodeType.Event:
                Debug.Log($"事件節點尚未實作，contentId = {nodeData.contentId}");
                break;

            case MapNodeType.Shop:
                Debug.Log($"商店節點尚未實作，contentId = {nodeData.contentId}");
                break;

            case MapNodeType.Rest:
                Debug.Log($"休息節點尚未實作，contentId = {nodeData.contentId}");
                break;

            case MapNodeType.Treasure:
                Debug.Log($"寶箱節點尚未實作，contentId = {nodeData.contentId}");
                break;

            default:
                Debug.LogWarning($"未處理的節點類型：{nodeData.mapNodeType}");
                break;
        }
    }

    private void EnterBattleNode(string contentId)
    {
        if (_database == null)
        {
            Debug.LogWarning("NodeContentManager 缺少 NodeContentDatabase");
            return;
        }

        BattleEncounterDefinition encounter = _database.GetBattleEncounter(contentId);
        if (encounter == null)
        {
            Debug.LogWarning($"找不到 BattleEncounterDefinition: {contentId}");
            return;
        }

        if (_battleController == null)
        {
            Debug.LogWarning("NodeContentManager 缺少 BattleController");
            return;
        }

        List<EnemyDead> spawnedEnemies = new();

        for (int i = 0; i < encounter.enemyPrefabs.Count; i++)
        {
            if (i >= _enemySpawnPoints.Count)
            {
                Debug.LogWarning("敵人 Spawn Point 數量不足，後續敵人不會生成");
                break;
            }

            if (encounter.enemyPrefabs[i] == null)
                continue;

            GameObject enemyObj = Instantiate(
                encounter.enemyPrefabs[i],
                _enemySpawnPoints[i].position,
                Quaternion.identity,
                _enemySpawnRoot
            );

            _spawnedObjects.Add(enemyObj);

            EnemyCombatAI ai = enemyObj.GetComponent<EnemyCombatAI>();
            if (ai != null)
            {
                ai.playerActor = _playerActor;
            }

            EnemyDead enemyDead = enemyObj.GetComponent<EnemyDead>();
            if (enemyDead != null)
            {
                spawnedEnemies.Add(enemyDead);
            }
            EnemyTargetSelectable selectable = enemyObj.GetComponentInChildren<EnemyTargetSelectable>();
            if (selectable != null)
            {
                selectable.Setup(_playerGestureHandler);
            }
        }

        _battleController.StartBattleWithEnemies(spawnedEnemies);
    }

    public void ClearCurrentContent()
    {
        for (int i = 0; i < _spawnedObjects.Count; i++)
        {
            if (_spawnedObjects[i] != null)
            {
                Destroy(_spawnedObjects[i]);
            }
        }

        _spawnedObjects.Clear();
    }
}