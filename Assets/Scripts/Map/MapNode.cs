using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using System.Collections.Generic;

public class MapNode : MonoBehaviour
{
    public int layerIndex;
    public int nodeIndex;
    public List<int> nextLayerConnectedNodes; //下一個節點 
    public Image _image;
    public MapNodeType _mapNodeType; 

    public void SetMapNodeType(MapNodeData mapNodeData)
    {
        layerIndex = mapNodeData.layerIndex;
        nodeIndex = mapNodeData.nodeIndex;
        _mapNodeType = mapNodeData.mapNodeType;
    }
    
}

public enum MapNodeType
{
    None,
    Enemy,
    Shop,
    Treasure,
    Event,
    Rest,
    Boss
}
