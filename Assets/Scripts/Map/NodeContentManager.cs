using System.Collections.Generic;
using UnityEngine;

public class NodeContentManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NodeContentDatabase _database;
    [SerializeField] private Transform _enemySpawnRoot;
    [SerializeField] private List<Transform> _enemySpawnPoints = new();
    [SerializeField] private BattleController _battleController;
    [SerializeField] private CombatActor _playerActor;
    [SerializeField] private GestureCombatActionHandler _playerGestureHandler;
    [SerializeField] private MapController _mapController;

    [Header("Event Encounter")]
    [SerializeField] private GameObject _eventPanel;

    private readonly List<GameObject> _spawnedObjects = new();
    private MapNodeData _currentNodeData;
    private bool _eventEncounterActive;

    public void EnterNode(MapNodeData nodeData)
    {
        if (nodeData == null)
        {
            Debug.LogWarning("EnterNode 失敗：nodeData 是 null");
            return;
        }

        _currentNodeData = nodeData;
        _eventEncounterActive = false;

        ClearCurrentContent();

        switch (nodeData.mapNodeType)
        {
            case MapNodeType.Enemy:
            case MapNodeType.Boss:
                EnterBattleNode(nodeData.contentId);
                break;

            case MapNodeType.Event:
                EnterEventNode(nodeData.contentId);
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

    private void EnterEventNode(string contentId)
    {
        if (_eventPanel == null)
        {
            Debug.LogWarning($"事件節點無法開啟：_eventPanel 未指定，contentId = {contentId}");
            return;
        }

        _eventEncounterActive = true;

        if (_mapController != null)
        {
            _mapController.CloseMap();
        }

        _eventPanel.SetActive(true);
        Debug.Log($"事件節點開啟：contentId = {contentId}");
    }

    public void CloseEventEncounter()
    {
        if (!_eventEncounterActive)
        {
            Debug.LogWarning("CloseEventEncounter 被呼叫，但目前沒有進行中的事件遭遇");
            return;
        }

        _eventEncounterActive = false;

        if (_eventPanel != null)
        {
            _eventPanel.SetActive(false);
        }

        if (_mapController != null)
        {
            _mapController.CompleteCurrentNode();
        }
        else
        {
            Debug.LogWarning("NodeContentManager 缺少 MapController，無法完成事件節點");
        }

        if (GameFlowController.Instance != null)
        {
            GameFlowController.Instance.ReturnToMap();
        }
        else
        {
            Debug.LogWarning("找不到 GameFlowController.Instance，無法返回地圖");
        }

        _currentNodeData = null;
        Debug.Log("事件節點已完成，返回地圖");
    }

    public void ClearCurrentContent()
    {
        if (_eventPanel != null)
        {
            _eventPanel.SetActive(false);
        }

        _eventEncounterActive = false;

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