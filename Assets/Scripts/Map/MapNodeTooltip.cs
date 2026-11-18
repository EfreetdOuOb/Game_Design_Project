using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 滑鼠移上「可選的節點」、「過往的節點」、休息區或首領時，在節點上方顯示類型名稱（普通戰鬥、商店、休息區…）。
/// 整張地圖只會有一個，由 <see cref="MapController"/> 每次重建地圖時建立，
/// 放在節點同一個父物件下，所以座標可以直接沿用節點的 localPosition。
/// </summary>
public class MapNodeTooltip : MonoBehaviour
{
    public static MapNodeTooltip Instance { get; private set; }

    private static readonly Dictionary<MapNodeType, string> NodeNames = new Dictionary<MapNodeType, string>
    {
        { MapNodeType.Enemy, "戰鬥" },
        { MapNodeType.Boss, "首領" },
        { MapNodeType.Shop, "商店" },
        { MapNodeType.Treasure, "寶箱" },
        { MapNodeType.Event, "事件" },
        { MapNodeType.Rest, "休息區" }
    };

    private RectTransform rect;
    private CanvasGroup group;
    private Text label;
    private Tween fadeTween;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance()
    {
        Instance = null;
    }

    /// <summary>在 parent 底下建立提示框（放在最上層）。</summary>
    public static MapNodeTooltip Create(RectTransform parent)
    {
        GameObject go = new GameObject("MapNodeTooltip", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(MapNodeTooltip));
        go.layer = parent.gameObject.layer;

        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.SetAsLastSibling();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(170f, 48f);

        Image background = go.GetComponent<Image>();
        background.color = new Color(0.07f, 0.07f, 0.1f, 0.9f);
        background.raycastTarget = false;

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textObject.layer = go.layer;
        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 26;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.92f, 0.6f, 1f);
        text.raycastTarget = false;

        MapNodeTooltip tooltip = go.GetComponent<MapNodeTooltip>();
        tooltip.rect = rect;
        tooltip.group = go.GetComponent<CanvasGroup>();
        tooltip.label = text;
        tooltip.group.alpha = 0f;
        tooltip.group.blocksRaycasts = false;
        tooltip.group.interactable = false;

        Instance = tooltip;
        return tooltip;
    }

    private void OnDestroy()
    {
        fadeTween?.Kill();
        if (Instance == this)
            Instance = null;
    }

    public void Show(MapNode node)
    {
        if (node == null)
            return;

        label.text = NodeNames.TryGetValue(node._mapNodeType, out string name) ? name : string.Empty;
        if (string.IsNullOrEmpty(label.text))
            return;

        // 放在節點正上方（兩者同一個父物件，座標共用）
        float nodeHalfHeight = ((RectTransform)node.transform).rect.height * 0.5f * node.transform.localScale.y;
        rect.localPosition = node.transform.localPosition + Vector3.up * (nodeHalfHeight + 14f);
        rect.SetAsLastSibling();

        Fade(1f);
    }

    public void Hide()
    {
        Fade(0f);
    }

    private void Fade(float target)
    {
        fadeTween?.Kill();
        fadeTween = group.DOFade(target, 0.12f).SetLink(gameObject);
    }
}
