using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 敵人狀態欄裡的一個格子：圖示 + 右下角剩餘回合 + 左上角層數（≥2 才顯示）。
/// 新的狀態出現時播「印章蓋下去」的進場動畫；沒有圖示時改顯示文字標籤。
/// 文字與動畫都在執行時建立，格子 prefab 只要有一個 Image 就能用。
/// </summary>
[RequireComponent(typeof(Image))]
public class DebuffSlotView : MonoBehaviour
{
    // World Space 的小 Canvas 上文字會糊，所以字用 4 倍大再縮回 1/4
    private const float TextOversample = 4f;

    private static readonly Color PlaceholderColor = new Color(0.12f, 0.14f, 0.2f, 0.9f);

    private Image icon;
    private Text turnsText;
    private Text stacksText;
    private Text labelText;
    private Vector3 baseScale;
    private bool baseScaleCaptured;
    private string boundId;
    private int boundStacks;
    private int boundTurns;
    private string boundBadge;
    private Sequence stampSequence;

    public string BoundId => boundId;

    private void Awake()
    {
        EnsureBuilt();
    }

    private void OnDestroy()
    {
        stampSequence?.Kill();
    }

    /// <summary>更新內容；回傳 true 代表這次是「新的狀態」（外層可以據此決定要不要播音效）。</summary>
    public bool Bind(string debuffId, Sprite sprite, string label, int stacks, int turns, string badge = null)
    {
        EnsureBuilt();

        bool isNew = !gameObject.activeSelf || boundId != debuffId;
        bool valueIncreased = !isNew
            && (stacks > boundStacks || turns > boundTurns || (badge != null && badge != boundBadge));

        boundId = debuffId;
        boundStacks = stacks;
        boundTurns = turns;
        boundBadge = badge;

        bool hasSprite = sprite != null;
        icon.enabled = true;
        icon.sprite = sprite;
        icon.color = hasSprite ? Color.white : PlaceholderColor;
        labelText.text = hasSprite ? string.Empty : label;

        // badge 有值時取代回合數（例如強化的「+10」，這種狀態沒有回合限制）
        turnsText.text = !string.IsNullOrEmpty(badge) ? badge : (turns > 0 ? turns.ToString() : string.Empty);
        stacksText.text = stacks >= 2 ? stacks.ToString() : string.Empty;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (isNew)
            PlayStamp();
        else if (valueIncreased)
            PlayPulse();

        return isNew;
    }

    public void Hide()
    {
        stampSequence?.Kill();
        boundId = null;
        boundStacks = 0;
        boundTurns = 0;
        boundBadge = null;
        if (baseScaleCaptured)
            transform.localScale = baseScale;
        gameObject.SetActive(false);
    }

    // 印章：從大一點、斜一點、半透明「砸」下來，落地後彈一下
    private void PlayStamp()
    {
        stampSequence?.Kill();

        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group == null)
            group = gameObject.AddComponent<CanvasGroup>();

        transform.localScale = baseScale * 2.4f;
        transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
        group.alpha = 0f;

        stampSequence = DOTween.Sequence().SetLink(gameObject);
        stampSequence.Append(transform.DOScale(baseScale, 0.16f).SetEase(Ease.InQuad));
        stampSequence.Join(transform.DOLocalRotate(Vector3.zero, 0.16f).SetEase(Ease.InQuad));
        stampSequence.Join(group.DOFade(1f, 0.1f));
        stampSequence.Append(transform.DOPunchScale(baseScale * 0.25f, 0.22f, 8, 0.6f));
        stampSequence.OnKill(() =>
        {
            transform.localScale = baseScale;
            transform.localRotation = Quaternion.identity;
            group.alpha = 1f;
        });
    }

    // 層數或回合增加（例如又被上同一個 debuff）：小彈一下
    private void PlayPulse()
    {
        stampSequence?.Kill();
        stampSequence = DOTween.Sequence().SetLink(gameObject);
        stampSequence.Append(transform.DOPunchScale(baseScale * 0.3f, 0.25f, 8, 0.6f));
        stampSequence.OnKill(() => transform.localScale = baseScale);
    }

    private void EnsureBuilt()
    {
        if (icon != null)
            return;

        icon = GetComponent<Image>();
        icon.raycastTarget = false;

        if (!baseScaleCaptured)
        {
            baseScale = transform.localScale;
            baseScaleCaptured = true;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Vector2 size = ((RectTransform)transform).rect.size;
        if (size.x <= 0f || size.y <= 0f)
            size = new Vector2(16f, 16f);

        float fontSize = Mathf.Max(6f, size.y * 0.5f) * TextOversample;

        turnsText = CreateCornerText("Turns", font, fontSize, TextAnchor.LowerRight, new Vector2(1f, 0f), size, new Color(1f, 1f, 1f, 1f));
        stacksText = CreateCornerText("Stacks", font, fontSize, TextAnchor.UpperLeft, new Vector2(0f, 1f), size, new Color(1f, 0.9f, 0.3f, 1f));
        labelText = CreateCornerText("Label", font, fontSize * 0.8f, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), size, Color.white);
    }

    private Text CreateCornerText(string objectName, Font font, float fontSize, TextAnchor alignment,
        Vector2 anchor, Vector2 slotSize, Color color)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Text), typeof(Outline));
        go.layer = gameObject.layer;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = slotSize * TextOversample * 0.7f;
        rect.localScale = Vector3.one / TextOversample;

        Text text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = Mathf.RoundToInt(fontSize);
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);

        return text;
    }
}
