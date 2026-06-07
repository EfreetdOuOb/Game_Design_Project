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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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

        switch (nodeData.mapNodeType)
        {
            case MapNodeType.Enemy:
            case MapNodeType.Boss:
                EnterBattle();
                break;

            case MapNodeType.Event:
                Debug.Log($"事件流程尚未完成，contentId = {nodeData.contentId}");
                break;

            case MapNodeType.Shop:
                Debug.Log($"商店流程尚未完成，contentId = {nodeData.contentId}");
                break;

            case MapNodeType.Rest:
                Debug.Log($"休息流程尚未完成，contentId = {nodeData.contentId}");
                break;

            case MapNodeType.Treasure:
                Debug.Log($"寶箱流程尚未完成，contentId = {nodeData.contentId}");
                break;

            default:
                Debug.LogWarning($"未支援的節點類型：{nodeData.mapNodeType}");
                break;
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
        ReturnToMap();
    }
}