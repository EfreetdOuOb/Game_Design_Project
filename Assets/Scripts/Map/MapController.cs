using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.Rendering;
using System.Linq; 

public class MapController : MonoBehaviour
{
    //房間節點
    [SerializeField] private GameObject _mapNode;
    [SerializeField] private GameObject _nodeLine;
    //房間層數
    private int _layerCount = 16;
    [SerializeField] private int _mapNodeWidth = 100;
    [SerializeField] private int _mapNodeHeight = 100;
    [SerializeField] private int _padding = 50;
    [SerializeField] private RectTransform _mapNodeParentRect;
    [SerializeField] private MapNode[][] _mapNodeArray;
    void Awake()
    {
        InitMapNodeArray();
        SubscribeEvents();
    }
    void Start()
    {
        EventManager.Publish(GameEvent.GameStarted);
    }

    void SubscribeEvents()
    {
        EventManager.Subscribe(GameEvent.GameStarted, OnGameStarted);
    }
    void OnDestroy()
    {
        UnsubscribeEvents();
    }
    void UnsubscribeEvents()
    {
        EventManager.Unsubscribe(GameEvent.GameStarted, OnGameStarted);
    }
    void OnGameStarted()
    {
        SetView();
        CreateMap();
        SetConnectedNodes();
        CheckCrossConnection();
        SetNodeLine();

        ScrollRect scrollRect = _mapNodeParentRect.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        scrollRect.verticalNormalizedPosition = 0f;
    }
    private void InitMapNodeArray()
    {
        _mapNodeArray = new MapNode[_layerCount][];
        for (int i = 0; i < _layerCount; i++)
        {
            int roomCount = GenerateLayerCount(i);
            _mapNodeArray[i] = new MapNode[roomCount];
        }
    }

    private int GenerateLayerCount(int layerIndex)
    {
        if(layerIndex == 0)
        {
            return Random.Range(3,5);
        }
        else if (layerIndex == _layerCount - 1)
        {
            return 1;
        }
        else
        {
            return Random.Range(3,6);
        }
    }
    private void SetView()
    {
        var size = _mapNodeParentRect.sizeDelta;
        size.y = _mapNodeHeight * (_layerCount - 1) + _padding * 2 ;
        _mapNodeParentRect.sizeDelta = size;
    }
    private void CreateMap()
    {
        int offsetY = _padding;
        for(int i = 0; i< _mapNodeArray.Length; i++)
        {
            int offsetX = -(_mapNodeArray[i].Length-1)* _mapNodeWidth/ 2;
            for(int j= 0; j< _mapNodeArray[i].Length; j++)
            {
                GameObject node = Instantiate(_mapNode, _mapNodeParentRect);
                MapNode mapNode = node.GetComponent<MapNode>();
                _mapNodeArray[i][j] = mapNode;

                MapNodeData mapNodeData = MakeMapNodeData(i, j);
                SetMapNode(mapNode, mapNodeData);

                int roomOffsetX = offsetX + Random.Range(-_mapNodeWidth / 4, _mapNodeWidth / 4);
                int roomOffsetY = offsetY;
                if(i != 0 && i != _layerCount -1)
                {
                    roomOffsetY = offsetY + Random.Range(-_mapNodeHeight / 6, _mapNodeHeight /6);
                }

                node.transform.localPosition = new Vector3(roomOffsetX, roomOffsetY);
                node.SetActive(true);

                offsetX += _mapNodeWidth;

            }

            offsetY += _mapNodeHeight;
        }
    }

    

    private void SetMapNode(MapNode mapNode, MapNodeData mapNodeData)
    {
        if(mapNode == null) return;
        mapNode.SetMapNodeType(mapNodeData);
    }

    private MapNodeData MakeMapNodeData(int layerIndex, int nodeIndex)
    {
        MapNodeType type;

    if (layerIndex == _layerCount - 1)
    {
        type = MapNodeType.Boss;
    }
    else if (layerIndex == 0)
    {
        type = MapNodeType.Enemy;
    }
    else
    {
        // 依機率隨機分配房間類型
        float roll = Random.value;
        if (roll < 0.45f)       type = MapNodeType.Enemy;
        else if (roll < 0.65f)  type = MapNodeType.Shop;
        else if (roll < 0.80f)  type = MapNodeType.Treasure;
        else if (roll < 0.90f)  type = MapNodeType.Rest;
        else                    type = MapNodeType.Event;
    }

    return new MapNodeData(type, layerIndex, nodeIndex);
    }

    private void SetConnectedNodes()
    {
        for(int i = 0; i< _mapNodeArray.Length - 1; i++)
        {
            int nextLayerCount = _mapNodeArray[i+1].Length;
            int currentLayerCount = _mapNodeArray[i].Length;
            if(currentLayerCount <= nextLayerCount)
            {
                List<int> nextLayerConnectedNodes = _mapNodeArray[i+1].Select(node => node.nodeIndex).ToList();
                List<List<int>> splitList = RandomSplit(nextLayerConnectedNodes, currentLayerCount);

                for(int j = 0;j< _mapNodeArray[i].Length; j++)
                {
                    MapNode mapNode = _mapNodeArray[i][j];
                    for(int k = 0; k< splitList[j].Count; k++)
                    {
                        mapNode.nextLayerConnectedNodes.Add(splitList[j][k]);
                    }
                }
            }
            else if (currentLayerCount > nextLayerCount)
            {
                List<int> currentLayerConnectNodes = _mapNodeArray[i].Select(node => node.nodeIndex).ToList();
                List<List<int>> splitList = RandomSplit(currentLayerConnectNodes, nextLayerCount);

                Dictionary<int, List<int>> valueToIndexes = new Dictionary<int, List<int>>();
                for(int k = 0; k < splitList.Count; k++)
                {
                    foreach(int num in splitList[k])
                    {
                        if(!valueToIndexes.ContainsKey(num))
                        {
                            valueToIndexes[num] = new List<int>();
                        }
                        valueToIndexes[num].Add(k);
                    }
                }

                List<List<int>> result = valueToIndexes.Values.ToList();
                string debugStr = "嵌套列表內容:\n"+ string.Join("\n",
                    result.Select((sublist, index) =>
                        $"列表 {index}: [{string.Join(", ", sublist)}]"));
                Debug.Log(debugStr);

                for(int j = 0; j < _mapNodeArray[i].Length; j++)
                {
                    MapNode mapNode = _mapNodeArray[i][j];

                    foreach(int index in result[j])
                    {
                        mapNode.nextLayerConnectedNodes.Add(index);
                    }
                }
            }
        }
    }

    private void CheckCrossConnection()
    {
        for(int i = 0; i< _mapNodeArray.Length -1; i++)
        {
            int currentLayerCount = _mapNodeArray[i].Length;
            int nextLayerCount = _mapNodeArray[i+1].Length;

            for(int j = 0; j< _mapNodeArray[i].Length; j++)
            {
                MapNode mapNode = _mapNodeArray[i][j];
                mapNode.nextLayerConnectedNodes = mapNode.nextLayerConnectedNodes.Distinct().ToList();
                mapNode.nextLayerConnectedNodes.Sort();

                if(j == 0)
                {
                    continue;
                }

                MapNode previousMapNode = _mapNodeArray[i][j-1];

                if(mapNode.nextLayerConnectedNodes.Count > 1)
                {
                    if(currentLayerCount <= nextLayerCount)
                    {
                        int currentMinIndex = mapNode.nextLayerConnectedNodes.Min();
                        int previousMaxIndex = previousMapNode.nextLayerConnectedNodes.Max();
                        if(currentMinIndex < previousMaxIndex)
                        {
                            Debug.Log("去除交叉節點");
                            mapNode.nextLayerConnectedNodes.Remove(currentMinIndex);
                        }
                    }
                }
            }
        }
    }
    private void SetNodeLine()
    {
        for(int i =0; i< _mapNodeArray.Length; i++)
        {
            for(int j =0; j< _mapNodeArray[i].Length; j++)
            {
                MapNode mapNode = _mapNodeArray[i][j];
                Vector2 startPosition = mapNode.transform.localPosition;
                if(mapNode.nextLayerConnectedNodes.Count > 0)
                {
                    List<int> connectedNodes = mapNode.nextLayerConnectedNodes;

                    for(int k =0; k< connectedNodes.Count; k++)
                    {
                        MapNode connectedNode = _mapNodeArray[i + 1][connectedNodes[k]];
                        Vector2 endPosition = connectedNode.transform.localPosition;

                        GameObject nodeLine = Instantiate(_nodeLine, _mapNodeParentRect);
                        nodeLine.SetActive(true);

                        SetNodeLinePosition(nodeLine, startPosition, endPosition);
                    }
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
        rectTransform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x)*Mathf.Rad2Deg);
    }
    List<List<int>> RandomSplit(List<int> list, int groupCount)
    {
        List<List<int>> result = new List<List<int>>();

        if(list.Count < groupCount)
        {
            Debug.LogError("列表元素數量小於組數");
            return result;
        }

        List<int> elementCounts = new List<int>();
        for(int i = 0; i< groupCount; i++)
        {
            elementCounts.Add(1);
        }

        //分配剩餘元素，使用不同的隨機策略
        int remainingElements = list.Count - groupCount;

        //先隨機選擇1~3個組進行加權
        int specialGroupsCount = Mathf.Min(Random.Range(1,4), groupCount);
        List<int> specialGroups = new List<int>();
        for(int i = 0; i < groupCount; i++)
        {
            specialGroups.Add(i);
        }
        Shuffle(specialGroups);
        specialGroups = specialGroups.Take(specialGroupsCount).ToList();
        //對特殊組進行權重分配
        while(remainingElements > 0)
        {
            //75%機率分配到特殊組，25%機率隨機分配到任意組
            if(Random.value < 0.75f && specialGroups.Count > 0)
            {
                int randomSpecialGroup = specialGroups[Random.Range(0, specialGroups.Count)];
                elementCounts[randomSpecialGroup]++;
            }
            else
            {
                int randomGroup = Random.Range(0, groupCount);
                elementCounts[randomGroup]++;
            }
            remainingElements--;
        }

        int index = 0;
        for(int i =0; i< groupCount; i++)
        {
            result.Add(list.GetRange(index, elementCounts[i]));
            index += elementCounts[i];
        }
        
        result = AddRandomElementToNestedList(result);

        return result;
    }

    void Shuffle<T>(List<T> list)
    {
        // Fisher-Yates Shuffle
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private List<List<int>> AddRandomElementToNestedList(List<List<int>> nestedList)
    {
        for(int i = 0; i < nestedList.Count; i++)
        {
            if(Random.value < 0.7f)
            {
                continue;
            }

            if(nestedList[i].Count < 3)
            {
                if(i == 0)
                {
                    nestedList[i].Add(nestedList[i + 1].Min());
                }
                else if(i == nestedList.Count - 1)
                {
                    nestedList[i].Add(nestedList[i - 1].Max());
                }
                else
                {
                    bool isForward = Random.value < 0.5f;
                    if(isForward)
                    {
                        nestedList[i].Add(nestedList[i + 1].Min());
                    }
                    else
                    {
                        nestedList[i].Add(nestedList[i - 1].Max());
                    }
                }
            }
        }

        return nestedList;
    }

}



public class MapNodeData
{
    public MapNodeType mapNodeType;
    public int layerIndex;
    public int nodeIndex;

    public MapNodeData(MapNodeType mapNodeType, int layerIndex, int nodeIndex)
    {
        this.mapNodeType = mapNodeType;
        this.layerIndex = layerIndex;
        this.nodeIndex = nodeIndex;
    }
}
