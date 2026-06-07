using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapSettings", menuName = "Game/Map Settings")]
public class MapSettings : ScriptableObject
{
    [Header("Map Layout")]
    public int layerCount = 16;
    public int mapNodeWidth = 100;
    public int mapNodeHeight = 100;
    public int padding = 50;

    [Header("Default Spawn Weight")]
    [Range(0f, 1f)] public float enemyWeight = 0.45f;
    [Range(0f, 1f)] public float shopWeight = 0.20f;
    [Range(0f, 1f)] public float treasureWeight = 0.15f;
    [Range(0f, 1f)] public float restWeight = 0.10f;
    [Range(0f, 1f)] public float eventWeight = 0.10f;

    [Header("Layer Count Settings")]
    public List<LayerNodeCountSetting> layerNodeCountSettings = new();

    [Header("Fixed Node Type Settings")]
    public List<LayerFixedTypeSetting> layerFixedTypeSettings = new();

    [Header("Layer Rule Settings")]
    public List<LayerNodeRuleSetting> layerNodeRuleSettings = new();

    [Header("Node Icon Settings")]
    public List<MapNodeSpriteSetting> mapNodeSpriteSettings = new();
    public Sprite defaultNodeSprite;

    private void OnValidate()
    {
        layerCount = Mathf.Max(2, layerCount);
        mapNodeWidth = Mathf.Max(1, mapNodeWidth);
        mapNodeHeight = Mathf.Max(1, mapNodeHeight);
        padding = Mathf.Max(0, padding);

        if (layerNodeCountSettings != null)
        {
            foreach (var setting in layerNodeCountSettings)
            {
                setting.layerIndex = Mathf.Clamp(setting.layerIndex, 0, layerCount - 1);
                setting.nodeCount = Mathf.Max(1, setting.nodeCount);
            }
        }

        if (layerFixedTypeSettings != null)
        {
            foreach (var setting in layerFixedTypeSettings)
            {
                setting.layerIndex = Mathf.Clamp(setting.layerIndex, 0, layerCount - 1);
            }
        }

        if (layerNodeRuleSettings != null)
        {
            foreach (var setting in layerNodeRuleSettings)
            {
                setting.layerIndex = Mathf.Clamp(setting.layerIndex, 0, layerCount - 1);
            }
        }
    }
}

[System.Serializable]
public class LayerNodeCountSetting
{
    public int layerIndex;
    public int nodeCount;
}

[System.Serializable]
public class LayerFixedTypeSetting
{
    public int layerIndex;
    public List<MapNodeType> nodeTypes = new();
}

[System.Serializable]
public class LayerNodeRuleSetting
{
    public int layerIndex;
    public bool useRuleOnly = false;
    public List<MapNodeType> requiredNodeTypes = new();
    public List<MapNodeType> bannedNodeTypes = new();
}

[System.Serializable]
public class MapNodeSpriteSetting
{
    public MapNodeType nodeType;
    public Sprite icon;
}