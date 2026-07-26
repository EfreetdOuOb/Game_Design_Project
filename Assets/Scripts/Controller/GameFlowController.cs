using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    [Header("Scene Roots")]
    [SerializeField] private GameObject _mapRoot;
    [SerializeField] private GameObject _battleRoot;
    [SerializeField] private GameObject _victoryRoot;
    [SerializeField] private GameObject _defeatRoot;

    [Header("Managers")]
    [SerializeField] private NodeContentManager _nodeContentManager;
    [SerializeField] private MapController _mapController;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_mapController == null)
        {
            _mapController = FindAnyObjectByType<MapController>();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void StartNode(MapNodeData nodeData)
    {
        if (nodeData == null)
        {
            Debug.LogWarning("StartNode 失敗：nodeData 是 null");
            return;
        }

        if (_nodeContentManager == null)
        {
            Debug.LogWarning("GameFlowController 缺少 NodeContentManager");
            return;
        }

        _nodeContentManager.EnterNode(nodeData);

        if (nodeData.mapNodeType is MapNodeType.Enemy or MapNodeType.Boss)
        {
            EnterBattle();
        }
    }

    public void EnterBattle()
    {
        SetActiveRoots(map: false, battle: true, victory: false, defeat: false);
    }

    public void EnterVictory()
    {
        SetActiveRoots(map: false, battle: false, victory: true, defeat: false);
    }

    public void EnterDefeat()
    {
        _nodeContentManager?.ClearCurrentContent();
        SetActiveRoots(map: false, battle: false, victory: false, defeat: true);
    }

    public void ReturnToMap()
    {
        if (_nodeContentManager != null)
        {
            _nodeContentManager.ClearCurrentContent();
        }

        SetActiveRoots(map: true, battle: false, victory: false, defeat: false);
    }

    public void ProceedAfterVictory()
    {
        _mapController?.CompleteCurrentNode();
        ReturnToMap();
    }

    public void CompleteCurrentNodeAndReturnToMap()
    {
        _mapController?.CompleteCurrentNode();
        ReturnToMap();
    }

    public void RestartRun()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.IsBattleLocked = false;
        }

        _nodeContentManager?.ResetPlayerForNewRun();
        _mapController?.ResetRun();
        CombatUI.Instance?.ClearBattleLog();
        ReturnToMap();
    }

    private void SetActiveRoots(bool map, bool battle, bool victory, bool defeat)
    {
        if (_mapRoot != null) _mapRoot.SetActive(map);
        if (_battleRoot != null) _battleRoot.SetActive(battle);
        if (_victoryRoot != null) _victoryRoot.SetActive(victory);
        if (_defeatRoot != null) _defeatRoot.SetActive(defeat);
    }
}
