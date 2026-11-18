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
    [Tooltip("已完成節點的圖案顏色（越暗越不搶眼）")]
    public Color completedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    // 這些類型的節點即使還不能選，外觀也和可選節點一樣（純白、不套用按鈕的半透明停用色）。
    // 只影響顯示，能不能點、解鎖、完成的流程都照原本的設定。目前只有休息區；商店不在內。
    // 刻意寫成程式碼常數而不是 Inspector 欄位：欄位會被 Unity 序列化進編輯器記憶體，
    // 改預設值時舊的值可能還殘留，造成「改了程式但畫面沒變」。要增減類型直接改這裡。
    private static readonly MapNodeType[] AlwaysClearTypes = { MapNodeType.Rest };

    /// <summary>這個節點的外觀是否要和可選節點一樣（休息區）；只影響顯示</summary>
    public bool IsAlwaysClear => System.Array.IndexOf(AlwaysClearTypes, _mapNodeType) >= 0;

    private ColorBlock _originalColors;
    private bool _colorsCached;

    private void Awake()
    {
        // 可點提示、完成標記、名稱提示都由這個元件負責（預設版本，不用改 prefab）
        if (GetComponent<MapNodeEffects>() == null)
            gameObject.AddComponent<MapNodeEffects>();
    }

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
            ApplyButtonTint();
        }

        RefreshVisual();
    }
    // 按鈕本身有「不可點時變灰、半透明（Disabled Color，約 50% 透明）」的設定，會疊在圖案顏色上，
    // 所以光改 Image 顏色，休息區還是會被它壓成半透明。休息區在還沒完成時把停用色換成純白，等於不套用；
    // 完成後恢復原本的停用色，和其他已完成節點一樣暗。
    private void ApplyButtonTint()
    {
        if (!_colorsCached)
        {
            _originalColors = _button.colors;
            _colorsCached = true;
        }

        ColorBlock colors = _originalColors;
        if (IsAlwaysClear && !isCompleted)
            colors.disabledColor = Color.white;

        _button.colors = colors;
    }

    private void RefreshVisual()
    {
        if (_image == null) return;

        if (isCompleted)
        {
            _image.color = completedColor;
        }
        else if (isUnlocked || IsAlwaysClear)
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