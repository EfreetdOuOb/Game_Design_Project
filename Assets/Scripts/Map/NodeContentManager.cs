using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class NodeContentManager : MonoBehaviour
{
    [Header("Battle References")]
    [SerializeField] private NodeContentDatabase _database;
    [SerializeField] private Transform _enemySpawnRoot;
    [SerializeField] private List<Transform> _enemySpawnPoints = new();
    [SerializeField] private BattleController _battleController;
    [SerializeField] private CombatActor _playerActor;
    [SerializeField] private GestureCombatActionHandler _playerGestureHandler;

    [Header("Shared References")]
    [SerializeField] private MapController _mapController;

    [Header("Event Encounter")]
    [SerializeField] private GameObject _eventPanelRoot;

    [Header("Treasure Encounter")]
    [SerializeField] private CinemachineCamera _playerFollowCamera;
    [SerializeField] private CinemachineCamera _treasureCamera;
    [SerializeField] private int _activeCameraPriority = 20;
    [SerializeField] private int _inactiveCameraPriority = 10;
    [SerializeField] private TreasureChestSelectable _treasureChestSelectable;
    [SerializeField] private Animator _treasureChestAnimator;
    [SerializeField] private TreasureRewardPanelUI _treasureRewardPanelUI;
    [SerializeField] private string _defaultTreasureRewardText = "你獲得了 50 金幣";
    [SerializeField] private GameObject _magicTownPanel;

    [Header("Rest Encounter")]
    [SerializeField] private RestNodePanelUI _restPanelUI;
    [Tooltip("休息點回復的生命值比例（依玩家最大生命值計算）")]
    [Range(0f, 1f)] [SerializeField] private float _restHealPercent = 0.5f;

    [Header("Shop Encounter")]
    [SerializeField] private ShopNodePanelUI _shopPanelUI;

    private readonly List<GameObject> _spawnedObjects = new();

    private MapNodeData _currentNodeData;
    private bool _eventEncounterActive;
    private bool _treasureEncounterActive;
    private bool _treasureOpened;
    private bool _restEncounterActive;
    private bool _shopEncounterActive;

    private bool _isPeekingMap;
    private GameObject _panelHiddenForMapPeek;

    public void EnterNode(MapNodeData nodeData)
    {
        if (nodeData == null)
        {
            Debug.LogWarning("EnterNode 失敗：nodeData 是 null");
            return;
        }

        ClearCurrentContent();
        _currentNodeData = nodeData;

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
                EnterShopNode(nodeData.contentId);
                break;

            case MapNodeType.Rest:
                EnterRestNode(nodeData.contentId);
                break;

            case MapNodeType.Treasure:
                EnterTreasureNode(nodeData.contentId);
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

        _battleController.StartBattleWithEnemies(spawnedEnemies, _playerActor);
    }

    public void ResetPlayerForNewRun()
    {
        ClearCurrentContent();
        _currentNodeData = null;
        _playerActor?.ResetToDefaultStats();
    }

    private void EnterEventNode(string contentId)
    {
        if (_mapController != null)
        {
            _mapController.CloseMap();
        }

        _eventEncounterActive = true;

        if (_eventPanelRoot != null)
        {
            _eventPanelRoot.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Event Panel Root 未指定");
        }

        Debug.Log($"事件節點開啟：contentId = {contentId}");
    }

    public string ResolveCurrentEvent()
    {
        if (!_eventEncounterActive)
            return string.Empty;

        if (_currentNodeData == null)
            return string.Empty;

        if (_currentNodeData.mapNodeType != MapNodeType.Event)
            return string.Empty;

        return _currentNodeData.contentId;
    }

    public void CloseEventPanel()
    {
        if (!_eventEncounterActive)
            return;

        if (_eventPanelRoot != null)
        {
            _eventPanelRoot.SetActive(false);
        }

        _eventEncounterActive = false;

        _mapController?.CompleteCurrentNode();
        GameFlowController.Instance?.ReturnToMap();
    }

    private void EnterTreasureNode(string contentId)
    {
        if (_mapController != null)
        {
            _mapController.CloseMap();
        }

        _treasureEncounterActive = true;
        _treasureOpened = false;

        if (_treasureRewardPanelUI != null)
        {
            _treasureRewardPanelUI.gameObject.SetActive(false);
        }

        if (_magicTownPanel != null)
        {
            _magicTownPanel.SetActive(false);
        }

        if (_treasureChestSelectable != null)
        {
            _treasureChestSelectable.Setup(this);
            _treasureChestSelectable.SetInteractable(true);
        }
        else
        {
            Debug.LogWarning("TreasureChestSelectable 未指定");
        }

        SetTreasureCameraActive(true);

        Debug.Log($"寶箱節點開啟：contentId = {contentId}");
    }

    public string ResolveCurrentTreasure()
    {
        if (!_treasureEncounterActive)
            return string.Empty;

        if (_currentNodeData == null)
            return string.Empty;

        if (_currentNodeData.mapNodeType != MapNodeType.Treasure)
            return string.Empty;

        return _currentNodeData.contentId;
    }

    public void OpenTreasureChest()
    {
        if (!_treasureEncounterActive || _treasureOpened)
            return;

        _treasureOpened = true;

        if (_treasureChestSelectable != null)
        {
            _treasureChestSelectable.SetInteractable(false);
        }

        if (_treasureChestAnimator != null)
        {
            _treasureChestAnimator.SetTrigger("Open");
        }

        if (_treasureRewardPanelUI != null)
        {
            _treasureRewardPanelUI.ShowReward(_defaultTreasureRewardText);
        }
        else
        {
            Debug.LogWarning("TreasureRewardPanelUI 未指定");
        }
    }

    public void CloseTreasureReward()
    {
        if (!_treasureEncounterActive)
            return;

        if (_treasureRewardPanelUI != null)
        {
            _treasureRewardPanelUI.gameObject.SetActive(false);
        }

        if (_magicTownPanel != null)
        {
            _magicTownPanel.SetActive(true);
        }

        SetTreasureCameraActive(false);

        _treasureEncounterActive = false;
        _treasureOpened = false;

        _mapController?.CompleteCurrentNode();
        GameFlowController.Instance?.ReturnToMap();
    }

    private void SetTreasureCameraActive(bool active)
    {
        if (_playerFollowCamera != null)
        {
            _playerFollowCamera.Priority = active ? _inactiveCameraPriority : _activeCameraPriority;
        }

        if (_treasureCamera != null)
        {
            _treasureCamera.Priority = active ? _activeCameraPriority : _inactiveCameraPriority;
        }
    }

    private void EnterRestNode(string contentId)
    {
        if (_mapController != null)
        {
            _mapController.CloseMap();
        }

        _restEncounterActive = true;

        if (_restPanelUI != null)
        {
            _restPanelUI.Show();
        }
        else
        {
            Debug.LogWarning("Rest Panel UI 未指定");
        }

        Debug.Log($"休息節點開啟：contentId = {contentId}");
    }

    // 回傳實際回復的生命值，讓 UI 可以顯示結果文字
    public int RestHealPlayer()
    {
        if (!_restEncounterActive || _playerActor == null)
            return 0;

        int healAmount = Mathf.RoundToInt(_playerActor.maxHp * _restHealPercent);
        int actualHealed = _playerActor.Heal(healAmount);
        CombatUI.Instance?.AppendBattleLog($"{_playerActor.actorId} 在休息點回復了 {actualHealed} 點生命");
        return actualHealed;
    }

    public void CloseRestPanel()
    {
        if (!_restEncounterActive)
            return;

        if (_restPanelUI != null)
        {
            _restPanelUI.gameObject.SetActive(false);
        }

        _restEncounterActive = false;

        _mapController?.CompleteCurrentNode();
        GameFlowController.Instance?.ReturnToMap();
    }

    private void EnterShopNode(string contentId)
    {
        if (_mapController != null)
        {
            _mapController.CloseMap();
        }

        _shopEncounterActive = true;

        if (_shopPanelUI != null)
        {
            _shopPanelUI.Show();
        }
        else
        {
            Debug.LogWarning("Shop Panel UI 未指定");
        }

        Debug.Log($"商店節點開啟：contentId = {contentId}");
    }

    public void CloseShopPanel()
    {
        if (!_shopEncounterActive)
            return;

        if (_shopPanelUI != null)
        {
            _shopPanelUI.gameObject.SetActive(false);
        }

        _shopEncounterActive = false;

        _mapController?.CompleteCurrentNode();
        GameFlowController.Instance?.ReturnToMap();
    }

    // 給 Event / Treasure / Rest / Shop 面板的「觀看當前地圖」按鈕共用：
    // 暫時隱藏目前面板、顯示地圖，並鎖住節點按鈕互動，避免玩家在偷看地圖時誤觸目前節點。
    public void OnClickViewMap()
    {
        if (_isPeekingMap || _mapController == null)
            return;

        _panelHiddenForMapPeek = GetActiveNodePanelRoot();
        if (_panelHiddenForMapPeek != null)
            _panelHiddenForMapPeek.SetActive(false);

        _mapController.SetNodeInteractionEnabled(false);
        _mapController.OpenMap();
        _isPeekingMap = true;
    }

    public void OnClickCloseMapPeek()
    {
        if (!_isPeekingMap || _mapController == null)
            return;

        _mapController.CloseMap();
        _mapController.SetNodeInteractionEnabled(true);

        if (_panelHiddenForMapPeek != null)
            _panelHiddenForMapPeek.SetActive(true);

        _panelHiddenForMapPeek = null;
        _isPeekingMap = false;
    }

    private GameObject GetActiveNodePanelRoot()
    {
        if (_eventEncounterActive && _eventPanelRoot != null)
            return _eventPanelRoot;

        if (_treasureEncounterActive && _treasureOpened && _treasureRewardPanelUI != null)
            return _treasureRewardPanelUI.gameObject;

        if (_restEncounterActive && _restPanelUI != null)
            return _restPanelUI.gameObject;

        if (_shopEncounterActive && _shopPanelUI != null)
            return _shopPanelUI.gameObject;

        return null;
    }

    public void ClearCurrentContent()
    {
        if (_eventPanelRoot != null)
        {
            _eventPanelRoot.SetActive(false);
        }

        if (_treasureRewardPanelUI != null)
        {
            _treasureRewardPanelUI.gameObject.SetActive(false);
        }

        if (_restPanelUI != null)
        {
            _restPanelUI.gameObject.SetActive(false);
        }

        if (_shopPanelUI != null)
        {
            _shopPanelUI.gameObject.SetActive(false);
        }

        if (_magicTownPanel != null)
        {
            _magicTownPanel.SetActive(true);
        }

        SetTreasureCameraActive(false);

        if (_isPeekingMap && _mapController != null)
        {
            _mapController.SetNodeInteractionEnabled(true);
        }

        _eventEncounterActive = false;
        _treasureEncounterActive = false;
        _treasureOpened = false;
        _restEncounterActive = false;
        _shopEncounterActive = false;
        _isPeekingMap = false;
        _panelHiddenForMapPeek = null;

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