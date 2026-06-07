using UnityEngine;

public enum GameFlowState
{
    Map,
    Battle,
    Victory
}

public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private MapController _mapController;
    [SerializeField] private BattleController _battleController;
    [SerializeField] private BattleResultUI _battleResultUI;
    [SerializeField] private GameObject _battleRoot;

    public GameFlowState CurrentState { get; private set; }

    private MapNodeType _currentNodeType;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        EnterMap();
    }

    public void StartNode(MapNodeType nodeType)
    {
        _currentNodeType = nodeType;

        switch (nodeType)
        {
            case MapNodeType.Enemy:
            case MapNodeType.Boss:
                EnterBattle(nodeType);
                break;

            case MapNodeType.Shop:
            case MapNodeType.Treasure:
            case MapNodeType.Event:
            case MapNodeType.Rest:
                // 這些你現在還沒做內容，先直接視為完成測試流程
                _mapController.CompleteCurrentNode();
                EnterMap();
                break;
        }
    }

    public void EnterMap()
    {
        CurrentState = GameFlowState.Map;

        if (_battleRoot != null)
            _battleRoot.SetActive(false);

        if (_battleResultUI != null)
            _battleResultUI.HideVictory();

        if (_mapController != null)
            _mapController.OpenMap();
    }

    public void EnterBattle(MapNodeType nodeType)
    {
        CurrentState = GameFlowState.Battle;

        if (_mapController != null)
            _mapController.CloseMap();

        if (_battleRoot != null)
            _battleRoot.SetActive(true);

        if (_battleResultUI != null)
            _battleResultUI.HideVictory();

        if (_battleController != null)
            _battleController.StartBattle(nodeType);
    }

    public void EnterVictory()
    {
        CurrentState = GameFlowState.Victory;

        if (_battleResultUI != null)
            _battleResultUI.ShowVictory();
    }

    public void ProceedAfterVictory()
    {
        if (_mapController != null)
            _mapController.CompleteCurrentNode();

        EnterMap();
    }
}