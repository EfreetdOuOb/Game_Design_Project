using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MapController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapSettings _mapSettings;
    [SerializeField] private GameObject _mapNode;
    [SerializeField] private GameObject _nodeLine;
    [SerializeField] private RectTransform _mapNodeParentRect;

    private MapGraphData _mapGraphData;
    private MapNode[][] _mapNodeArray;

    void Awake()
    {
        SubscribeEvents();
    }

    void Start()
    {
        EventManager.Publish(GameEvent.GameStarted);
    }

    void OnDestroy()
    {
        UnsubscribeEvents();
    }

    void SubscribeEvents()
    {
        EventManager.Subscribe(GameEvent.GameStarted, OnGameStarted);
    }

    void UnsubscribeEvents()
    {
        EventManager.Unsubscribe(GameEvent.GameStarted, OnGameStarted);
    }

    void OnGameStarted()
    {
        ClearMapView();
        GenerateMapData();
        SetView();
        CreateMapView();
        SetNodeLines();
        ResetScrollPosition();
    }

    private void ClearMapView()
    {
        for (int i = _mapNodeParentRect.childCount - 1; i >= 0; i--)
        {
            Destroy(_mapNodeParentRect.GetChild(i).gameObject);
        }
    }

    private void GenerateMapData()
    {
        MapGenerator generator = new MapGenerator(_mapSettings);
        _mapGraphData = generator.Generate();

        _mapNodeArray = new MapNode[_mapGraphData.layers.Count][];
        for (int i = 0; i < _mapGraphData.layers.Count; i++)
        {
            _mapNodeArray[i] = new MapNode[_mapGraphData.layers[i].nodes.Count];
        }
    }

    private void SetView()
    {
        var size = _mapNodeParentRect.sizeDelta;
        size.y = _mapSettings.mapNodeHeight * (_mapSettings.layerCount - 1) + _mapSettings.padding * 2;
        _mapNodeParentRect.sizeDelta = size;
    }

    private void CreateMapView()
    {
        int offsetY = _mapSettings.padding;

        for (int i = 0; i < _mapGraphData.layers.Count; i++)
        {
            var layer = _mapGraphData.layers[i];
            int offsetX = -(layer.nodes.Count - 1) * _mapSettings.mapNodeWidth / 2;

            for (int j = 0; j < layer.nodes.Count; j++)
            {
                GameObject nodeObject = Instantiate(_mapNode, _mapNodeParentRect);
                MapNode mapNode = nodeObject.GetComponent<MapNode>();
                _mapNodeArray[i][j] = mapNode;

                MapNodeData mapNodeData = layer.nodes[j];
                mapNode.SetMapNodeType(mapNodeData);
                mapNode.SetIcon(GetNodeSprite(mapNodeData.mapNodeType));

                int roomOffsetX = offsetX + Random.Range(-_mapSettings.mapNodeWidth / 4, _mapSettings.mapNodeWidth / 4);
                int roomOffsetY = offsetY;

                if (i != 0 && i != _mapSettings.layerCount - 1)
                {
                    roomOffsetY = offsetY + Random.Range(-_mapSettings.mapNodeHeight / 6, _mapSettings.mapNodeHeight / 6);
                }

                nodeObject.transform.localPosition = new Vector3(roomOffsetX, roomOffsetY);
                nodeObject.SetActive(true);

                offsetX += _mapSettings.mapNodeWidth;
            }

            offsetY += _mapSettings.mapNodeHeight;
        }
    }

    private void SetNodeLines()
    {
        for (int i = 0; i < _mapGraphData.layers.Count - 1; i++)
        {
            for (int j = 0; j < _mapGraphData.layers[i].nodes.Count; j++)
            {
                MapNode currentNodeView = _mapNodeArray[i][j];
                MapNodeData currentNodeData = _mapGraphData.layers[i].nodes[j];
                Vector2 startPosition = currentNodeView.transform.localPosition;

                foreach (int nextIndex in currentNodeData.nextLayerConnectedNodes)
                {
                    if (nextIndex < 0 || nextIndex >= _mapNodeArray[i + 1].Length)
                        continue;

                    MapNode nextNodeView = _mapNodeArray[i + 1][nextIndex];
                    Vector2 endPosition = nextNodeView.transform.localPosition;

                    GameObject nodeLine = Instantiate(_nodeLine, _mapNodeParentRect);
                    nodeLine.SetActive(true);

                    SetNodeLinePosition(nodeLine, startPosition, endPosition);
                }
            }
        }
    }

    private void SetNodeLinePosition(GameObject nodeLine, Vector2 startPosition, Vector2 endPosition)
    {
        RectTransform rectTransform = nodeLine.GetComponent<RectTransform>();
        rectTransform.localPosition = startPosition;

        var size = rectTransform.sizeDelta;
        size.x = Vector2.Distance(startPosition, endPosition);
        rectTransform.sizeDelta = size;

        Vector2 direction = endPosition - startPosition;
        rectTransform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    private Sprite GetNodeSprite(MapNodeType nodeType)
    {
        MapNodeSpriteSetting setting = _mapSettings.mapNodeSpriteSettings
            .FirstOrDefault(x => x.nodeType == nodeType);

        if (setting != null && setting.icon != null)
            return setting.icon;

        return _mapSettings.defaultNodeSprite;
    }

    private void ResetScrollPosition()
    {
        ScrollRect scrollRect = _mapNodeParentRect.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }
}