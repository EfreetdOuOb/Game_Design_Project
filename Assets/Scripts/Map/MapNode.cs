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
    public Button _button;
    public bool isUnlocked;
    public bool isCompleted;

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
    public void SetState(bool unlocked, bool completed)
    {
        isUnlocked = unlocked;
        isCompleted = completed;

        if (_button != null)
        {
            _button.interactable = unlocked && !completed;
        }

        RefreshVisual();
    }
    private void RefreshVisual()
    {
        if (_image == null) return;

        if (isCompleted)
        {
            _image.color = Color.gray;
        }
        else if (isUnlocked)
        {
            _image.color = Color.white;
        }
        else
        {
            _image.color = new Color(1f, 1f, 1f, 0.35f);
        }
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