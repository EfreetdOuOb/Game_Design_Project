using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 地圖節點的互動與進度提示：
///   · 可以點的節點：外圍有往外擴散的光圈；滑鼠移上去會放大、光圈變亮；點下去彈一下
///   · 已完成的節點：蓋上半透明的完成印章（圓圈加勾勾），大小約等於節點，隱隱蓋在圖案上
///   · 滑鼠移上任何節點：顯示節點類型名稱（<see cref="MapNodeTooltip"/>）
/// 只讀取 <see cref="MapNode"/> 的狀態，不改任何地圖或流程資料。由 MapNode 自動掛上，圖形都是程式畫的，
/// 想換成自己的圖，指定 Ring / Badge 的 Sprite 即可。
/// </summary>
[RequireComponent(typeof(MapNode))]
public class MapNodeEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("可點提示")]
    [Tooltip("選填：自己的光圈圖。留空使用程式畫的圓環。")]
    [SerializeField] private Sprite _ringSprite;
    [SerializeField] private Color _ringColor = new Color(0.78f, 0.16f, 0.12f, 1f);
    [Tooltip("光圈擴散一輪需要的秒數")]
    [SerializeField, Min(0.1f)] private float _pulseSeconds = 1.3f;
    [SerializeField, Min(1f)] private float _ringMaxScale = 1.45f;
    [SerializeField, Range(0f, 1f)] private float _ringMaxAlpha = 0.9f;

    [Header("滑鼠與點擊")]
    [SerializeField, Min(1f)] private float _hoverScale = 1.15f;
    [SerializeField, Min(0f)] private float _clickPunch = 0.2f;

    [Header("已完成標記")]
    [Tooltip("選填：自己的完成圖（整張含底）。留空使用程式畫的圓圈加勾勾。")]
    [SerializeField] private Sprite _badgeSprite;
    [Tooltip("印章顏色；透明度決定蓋在節點圖案上有多淡")]
    [SerializeField] private Color _badgeColor = new Color(0.85f, 0.85f, 0.85f, 0.4f);
    [Tooltip("印章大小，單位是節點大小的比例（1 = 剛好蓋住整個節點圖案）")]
    [SerializeField, Range(0.3f, 1.6f)] private float _badgeSizeRatio = 1.05f;

    private MapNode node;
    private RectTransform ringRect;
    private Image ringImage;
    private RectTransform badgeRect;
    private Image badgeImage;
    private Vector3 baseScale;
    private bool hovered;
    private bool wasCompleted;

    private void Awake()
    {
        node = GetComponent<MapNode>();
        baseScale = transform.localScale;
        BuildRing();
        BuildBadge();
    }

    private void OnDisable()
    {
        transform.DOKill();
        hovered = false;
    }

    private bool IsSelectable =>
        node != null && node.isUnlocked && !node.isCompleted && node._button != null && node._button.interactable;

    private void Update()
    {
        UpdateRing();
        UpdateBadge();
    }

    // ---- 光圈 ----

    private void UpdateRing()
    {
        if (!IsSelectable)
        {
            if (ringImage.enabled)
                ringImage.enabled = false;
            return;
        }

        ringImage.enabled = true;

        // 每輪：光圈從節點大小往外擴散並淡出；滑鼠移上去時停在固定大小、保持亮
        float t = Mathf.Repeat(Time.unscaledTime / _pulseSeconds, 1f);
        float scale = hovered ? 1.12f : Mathf.Lerp(1f, _ringMaxScale, t);
        float alpha = hovered ? _ringMaxAlpha : (1f - t) * _ringMaxAlpha;

        ringRect.localScale = Vector3.one * scale;
        Color c = _ringColor;
        c.a = alpha;
        ringImage.color = c;
    }

    private void BuildRing()
    {
        GameObject go = new GameObject("SelectableRing", typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;
        ringRect = (RectTransform)go.transform;
        ringRect.SetParent(transform, false);
        ringRect.anchorMin = Vector2.zero;
        ringRect.anchorMax = Vector2.one;
        ringRect.offsetMin = new Vector2(-6f, -6f);
        ringRect.offsetMax = new Vector2(6f, 6f);

        ringImage = go.GetComponent<Image>();
        ringImage.sprite = _ringSprite != null ? _ringSprite : MapUiSprites.Ring;
        ringImage.raycastTarget = false;
        ringImage.enabled = false;
    }

    // ---- 已完成標記 ----

    private void UpdateBadge()
    {
        bool completed = node != null && node.isCompleted;
        if (completed == wasCompleted)
            return;

        wasCompleted = completed;
        if (completed)
            ShowBadge();
        else if (badgeRect != null)
            badgeRect.gameObject.SetActive(false);
    }

    private void ShowBadge()
    {
        if (badgeRect == null)
            return;

        badgeRect.gameObject.SetActive(true);

        // 蓋章：從大一點、斜一點、全透明落下，落地後彈一下；最後停在設定的半透明
        badgeRect.DOKill();
        badgeImage.DOKill();
        badgeRect.localScale = Vector3.one * 1.8f;
        badgeRect.localRotation = Quaternion.Euler(0f, 0f, 18f);
        Color target = _badgeColor;
        Color start = target;
        start.a = 0f;
        badgeImage.color = start;

        Sequence stamp = DOTween.Sequence().SetLink(badgeRect.gameObject);
        stamp.Append(badgeRect.DOScale(1f, 0.18f).SetEase(Ease.InQuad));
        stamp.Join(badgeRect.DOLocalRotate(Vector3.zero, 0.18f).SetEase(Ease.InQuad));
        stamp.Join(badgeImage.DOFade(target.a, 0.18f));
        stamp.Append(badgeRect.DOPunchScale(Vector3.one * 0.12f, 0.2f, 6, 0.6f));
    }

    private void BuildBadge()
    {
        Vector2 nodeSize = ((RectTransform)transform).rect.size;
        float size = Mathf.Min(nodeSize.x, nodeSize.y) * _badgeSizeRatio;

        GameObject go = new GameObject("CompletedBadge", typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;
        badgeRect = (RectTransform)go.transform;
        badgeRect.SetParent(transform, false);

        // 置中蓋在節點圖案上
        badgeRect.anchorMin = new Vector2(0.5f, 0.5f);
        badgeRect.anchorMax = new Vector2(0.5f, 0.5f);
        badgeRect.pivot = new Vector2(0.5f, 0.5f);
        badgeRect.sizeDelta = new Vector2(size, size);
        badgeRect.anchoredPosition = Vector2.zero;

        badgeImage = go.GetComponent<Image>();
        badgeImage.sprite = _badgeSprite != null ? _badgeSprite : MapUiSprites.Stamp;
        badgeImage.color = _badgeColor;
        badgeImage.raycastTarget = false;

        go.SetActive(false);
    }

    // ---- 滑鼠與點擊 ----

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        MapNodeTooltip.Instance?.Show(node);

        if (IsSelectable)
        {
            transform.DOKill();
            transform.DOScale(baseScale * _hoverScale, 0.12f).SetEase(Ease.OutQuad).SetLink(gameObject);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        MapNodeTooltip.Instance?.Hide();

        transform.DOKill();
        transform.DOScale(baseScale, 0.12f).SetEase(Ease.OutQuad).SetLink(gameObject);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsSelectable || _clickPunch <= 0f)
            return;

        transform.DOKill();
        transform.localScale = baseScale;
        transform.DOPunchScale(baseScale * _clickPunch, 0.25f, 6, 0.6f).SetLink(gameObject);
    }
}
