using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapNode : MonoBehaviour
{
    public int layerIndex;
    public int nodeIndex;
    public Image _image;
    public MapNodeType _mapNodeType;
    public List<int> nextLayerConnectedNodes = new();

    public void SetMapNodeType(MapNodeData mapNodeData)
    {
        layerIndex = mapNodeData.layerIndex;
        nodeIndex = mapNodeData.nodeIndex;
        _mapNodeType = mapNodeData.mapNodeType;
        nextLayerConnectedNodes = new List<int>(mapNodeData.nextLayerConnectedNodes);
    }

    public void SetIcon(Sprite iconSprite)
    {
        if (_image == null) return;

        _image.sprite = iconSprite;
        _image.enabled = iconSprite != null;
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