using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    [Header("Scene Roots")]
    [SerializeField] private GameObject _mapRoot;
    [SerializeField] private GameObject _battleRoot;
    [SerializeField] private GameObject _victoryRoot;

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
        if (_mapRoot != null) _mapRoot.SetActive(false);
        if (_battleRoot != null) _battleRoot.SetActive(true);
        if (_victoryRoot != null) _victoryRoot.SetActive(false);
    }

    public void EnterVictory()
    {
        if (_mapRoot != null) _mapRoot.SetActive(false);
        if (_battleRoot != null) _battleRoot.SetActive(false);
        if (_victoryRoot != null) _victoryRoot.SetActive(true);
    }

    public void ReturnToMap()
    {
        if (_nodeContentManager != null)
        {
            _nodeContentManager.ClearCurrentContent();
        }

        if (_mapRoot != null) _mapRoot.SetActive(true);
        if (_battleRoot != null) _battleRoot.SetActive(false);
        if (_victoryRoot != null) _victoryRoot.SetActive(false);
    }

    public void ProceedAfterVictory()
    {
        _mapController?.CompleteCurrentNode();
        ReturnToMap();
    }
}
