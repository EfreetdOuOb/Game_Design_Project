using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapGenerator
{
    private readonly MapSettings _settings;

    public MapGenerator(MapSettings settings)
    {
        _settings = settings;
    }

    public MapGraphData Generate()
    {
        MapGraphData graphData = new MapGraphData
        {
            layers = new List<MapLayerData>()
        };

        for (int layerIndex = 0; layerIndex < _settings.layerCount; layerIndex++)
        {
            int nodeCount = GenerateLayerCount(layerIndex);
            MapNodeType[] nodeTypes = GenerateLayerNodeTypes(layerIndex, nodeCount);

            MapLayerData layerData = new MapLayerData
            {
                layerIndex = layerIndex,
                nodes = new List<MapNodeData>()
            };

            for (int nodeIndex = 0; nodeIndex < nodeCount; nodeIndex++)
            {
                MapNodeType nodeType = nodeTypes[nodeIndex];
                string contentId = GetDefaultContentId(nodeType, layerIndex, nodeIndex);

                layerData.nodes.Add(new MapNodeData(nodeType, layerIndex, nodeIndex, contentId));
            }

            graphData.layers.Add(layerData);
        }

        GenerateConnections(graphData);
        RemoveCrossConnections(graphData);

        return graphData;
    }

    private int GenerateLayerCount(int layerIndex)
    {
        LayerNodeCountSetting setting = _settings.layerNodeCountSettings
            .FirstOrDefault(x => x.layerIndex == layerIndex);

        if (setting != null && setting.nodeCount > 0)
            return setting.nodeCount;

        if (layerIndex == 0) return Random.Range(3, 5);
        if (layerIndex == _settings.layerCount - 1) return 1;
        return Random.Range(3, 6);
    }

    private MapNodeType[] GenerateLayerNodeTypes(int layerIndex, int nodeCount)
    {
        MapNodeType[] result = new MapNodeType[nodeCount];
        for (int i = 0; i < result.Length; i++)
            result[i] = MapNodeType.None;

        if (layerIndex == 0)
        {
            for (int i = 0; i < nodeCount; i++)
                result[i] = MapNodeType.Enemy;

            return result;
        }

        if (layerIndex == _settings.layerCount - 1)
        {
            result[0] = MapNodeType.Boss;
            return result;
        }

        LayerFixedTypeSetting fixedSetting = _settings.layerFixedTypeSettings
            .FirstOrDefault(x => x.layerIndex == layerIndex);

        if (fixedSetting != null)
        {
            for (int i = 0; i < fixedSetting.nodeTypes.Count && i < result.Length; i++)
                result[i] = fixedSetting.nodeTypes[i];
        }

        LayerNodeRuleSetting ruleSetting = _settings.layerNodeRuleSettings
            .FirstOrDefault(x => x.layerIndex == layerIndex);

        List<int> emptyIndexes = new List<int>();
        for (int i = 0; i < result.Length; i++)
        {
            if (result[i] == MapNodeType.None)
                emptyIndexes.Add(i);
        }

        Shuffle(emptyIndexes);

        if (ruleSetting != null)
        {
            for (int i = 0; i < ruleSetting.requiredNodeTypes.Count && i < emptyIndexes.Count; i++)
            {
                result[emptyIndexes[i]] = ruleSetting.requiredNodeTypes[i];
            }
        }

        for (int i = 0; i < result.Length; i++)
        {
            if (result[i] != MapNodeType.None) continue;
            result[i] = GetRandomType(ruleSetting);
        }

        return result;
    }

    private MapNodeType GetRandomType(LayerNodeRuleSetting ruleSetting)
    {
        List<MapNodeType> pool = new List<MapNodeType>
        {
            MapNodeType.Enemy,
            MapNodeType.Shop,
            MapNodeType.Treasure,
            MapNodeType.Rest,
            MapNodeType.Event
        };

        if (ruleSetting != null && ruleSetting.bannedNodeTypes != null)
            pool = pool.Where(x => !ruleSetting.bannedNodeTypes.Contains(x)).ToList();

        if (pool.Count == 0)
            return MapNodeType.Enemy;

        if (ruleSetting != null && ruleSetting.useRuleOnly)
            return pool[Random.Range(0, pool.Count)];

        float total = _settings.enemyWeight +
                      _settings.shopWeight +
                      _settings.treasureWeight +
                      _settings.restWeight +
                      _settings.eventWeight;

        float roll = Random.value * total;
        float current = 0f;

        current += _settings.enemyWeight;
        if (roll <= current && pool.Contains(MapNodeType.Enemy))
            return MapNodeType.Enemy;

        current += _settings.shopWeight;
        if (roll <= current && pool.Contains(MapNodeType.Shop))
            return MapNodeType.Shop;

        current += _settings.treasureWeight;
        if (roll <= current && pool.Contains(MapNodeType.Treasure))
            return MapNodeType.Treasure;

        current += _settings.restWeight;
        if (roll <= current && pool.Contains(MapNodeType.Rest))
            return MapNodeType.Rest;

        if (pool.Contains(MapNodeType.Event))
            return MapNodeType.Event;

        return pool[Random.Range(0, pool.Count)];
    }

    private string GetDefaultContentId(MapNodeType nodeType, int layerIndex, int nodeIndex)
    {
        switch (nodeType)
        {
            case MapNodeType.Enemy:
                return "battle_slime_01";

            case MapNodeType.Boss:
                return "battle_boss_01";

            case MapNodeType.Event:
                return "event_default_01";

            case MapNodeType.Shop:
                return "shop_default_01";

            case MapNodeType.Rest:
                return "rest_default_01";

            case MapNodeType.Treasure:
                return "treasure_default_01";

            default:
                return string.Empty;
        }
    }

    private void GenerateConnections(MapGraphData graphData)
    {
        for (int i = 0; i < graphData.layers.Count - 1; i++)
        {
            List<MapNodeData> currentLayer = graphData.layers[i].nodes;
            List<MapNodeData> nextLayer = graphData.layers[i + 1].nodes;

            int currentCount = currentLayer.Count;
            int nextCount = nextLayer.Count;

            if (currentCount <= nextCount)
            {
                List<int> nextIndexes = nextLayer.Select(n => n.nodeIndex).ToList();
                List<List<int>> splitList = RandomSplit(nextIndexes, currentCount);

                for (int j = 0; j < currentLayer.Count; j++)
                {
                    currentLayer[j].nextLayerConnectedNodes.AddRange(splitList[j]);
                }
            }
            else
            {
                List<int> currentIndexes = currentLayer.Select(n => n.nodeIndex).ToList();
                List<List<int>> splitList = RandomSplit(currentIndexes, nextCount);

                Dictionary<int, List<int>> valueToIndexes = new Dictionary<int, List<int>>();
                for (int k = 0; k < splitList.Count; k++)
                {
                    foreach (int num in splitList[k])
                    {
                        if (!valueToIndexes.ContainsKey(num))
                            valueToIndexes[num] = new List<int>();

                        valueToIndexes[num].Add(k);
                    }
                }

                List<List<int>> result = valueToIndexes.Values.ToList();

                for (int j = 0; j < currentLayer.Count; j++)
                {
                    currentLayer[j].nextLayerConnectedNodes.AddRange(result[j]);
                }
            }
        }
    }

    private void RemoveCrossConnections(MapGraphData graphData)
    {
        for (int i = 0; i < graphData.layers.Count - 1; i++)
        {
            List<MapNodeData> currentLayer = graphData.layers[i].nodes;
            List<MapNodeData> nextLayer = graphData.layers[i + 1].nodes;

            for (int j = 0; j < currentLayer.Count; j++)
            {
                MapNodeData mapNode = currentLayer[j];
                mapNode.nextLayerConnectedNodes = mapNode.nextLayerConnectedNodes.Distinct().ToList();
                mapNode.nextLayerConnectedNodes.Sort();

                if (j == 0) continue;

                MapNodeData previousMapNode = currentLayer[j - 1];

                if (mapNode.nextLayerConnectedNodes.Count > 1 && currentLayer.Count <= nextLayer.Count)
                {
                    int currentMinIndex = mapNode.nextLayerConnectedNodes.Min();
                    int previousMaxIndex = previousMapNode.nextLayerConnectedNodes.Max();

                    if (currentMinIndex < previousMaxIndex)
                        mapNode.nextLayerConnectedNodes.Remove(currentMinIndex);
                }
            }
        }
    }

    private List<List<int>> RandomSplit(List<int> list, int groupCount)
    {
        List<List<int>> result = new List<List<int>>();

        if (list == null || list.Count == 0 || groupCount <= 0)
            return result;

        if (list.Count < groupCount)
            groupCount = list.Count;

        List<int> elementCounts = new List<int>();
        for (int i = 0; i < groupCount; i++)
            elementCounts.Add(1);

        int remainingElements = list.Count - groupCount;

        int specialGroupsCount = Mathf.Min(Random.Range(1, 4), groupCount);
        List<int> specialGroups = new List<int>();
        for (int i = 0; i < groupCount; i++)
            specialGroups.Add(i);

        Shuffle(specialGroups);
        specialGroups = specialGroups.Take(specialGroupsCount).ToList();

        while (remainingElements > 0)
        {
            if (Random.value < 0.75f && specialGroups.Count > 0)
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
        for (int i = 0; i < groupCount; i++)
        {
            result.Add(list.GetRange(index, elementCounts[i]));
            index += elementCounts[i];
        }

        return AddRandomElementToNestedList(result);
    }

    private List<List<int>> AddRandomElementToNestedList(List<List<int>> nestedList)
    {
        if (nestedList == null || nestedList.Count <= 1)
            return nestedList;

        for (int i = 0; i < nestedList.Count; i++)
        {
            if (Random.value < 0.7f)
                continue;

            if (nestedList[i] == null)
                nestedList[i] = new List<int>();

            if (nestedList[i].Count >= 3)
                continue;

            List<int> candidateSources = new List<int>();

            if (i - 1 >= 0 && nestedList[i - 1] != null && nestedList[i - 1].Count > 0)
                candidateSources.Add(i - 1);

            if (i + 1 < nestedList.Count && nestedList[i + 1] != null && nestedList[i + 1].Count > 0)
                candidateSources.Add(i + 1);

            if (candidateSources.Count == 0)
                continue;

            int sourceIndex = candidateSources[Random.Range(0, candidateSources.Count)];
            int valueToAdd;

            if (sourceIndex < i)
                valueToAdd = nestedList[sourceIndex].Max();
            else
                valueToAdd = nestedList[sourceIndex].Min();

            if (!nestedList[i].Contains(valueToAdd))
                nestedList[i].Add(valueToAdd);
        }

        return nestedList;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

public class MapGraphData
{
    public List<MapLayerData> layers = new();
}

public class MapLayerData
{
    public int layerIndex;
    public List<MapNodeData> nodes = new();
}

public class MapNodeData
{
    public MapNodeType mapNodeType;
    public int layerIndex;
    public int nodeIndex;
    public List<int> nextLayerConnectedNodes = new();
    public string contentId;

    public MapNodeData(MapNodeType mapNodeType, int layerIndex, int nodeIndex, string contentId = "")
    {
        this.mapNodeType = mapNodeType;
        this.layerIndex = layerIndex;
        this.nodeIndex = nodeIndex;
        this.contentId = contentId;
    }
}