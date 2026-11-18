using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 全遊戲唯一的懸浮說明框：跟著游標走，自動避開螢幕邊緣。第一次用到時自己建立（不需要放進場景）。
/// 誰要顯示就呼叫 <see cref="Show"/> 並提供「取得內容」的函式，說明框會定期重新取內容，
/// 所以說明裡的數字會跟著戰鬥狀態即時更新。按住滑鼠（畫手勢、拖曳）時會暫時隱藏，不會擋到操作。
/// </summary>
public class TooltipSystem : MonoBehaviour
{
    private const float MinWidth = 260f;
    private const float MaxWidth = 560f;
    private const float Padding = 16f;
    private const float CursorOffset = 26f;
    private const float RefreshInterval = 0.12f;

    /// <summary>
    /// 游標停在東西上多久（秒）才顯示說明框。想讓說明更快或更慢出現，改這個數字就好；0 = 一移上去就顯示。
    /// 剛從另一個有說明的東西移過來時（<see cref="QuickSwitchWindow"/> 秒內）不用再等，直接顯示。
    /// </summary>
    public static float ShowDelay = 0.35f;

    /// <summary>剛看過說明框後，這段時間內移到下一個東西，說明框立刻出現（秒）</summary>
    public const float QuickSwitchWindow = 0.25f;

    private static TooltipSystem instance;

    public static TooltipSystem Instance
    {
        get
        {
            if (instance == null)
                instance = Create();
            return instance;
        }
    }

    /// <summary>目前已存在的說明框（沒有就是 null，不會因為查詢而建立新的）。</summary>
    public static TooltipSystem Existing => instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance()
    {
        instance = null;
    }

    private object owner;
    private Func<TooltipContent> provider;
    private TooltipContent current;
    private float refreshTimer;
    private float showAt;
    private bool showWhilePressed;
    private float lastVisibleTime = -10f;

    private RectTransform canvasRect;
    private RectTransform panel;
    private CanvasGroup group;
    private Text titleText;
    private Text bodyText;

    /// <summary>owner 是呼叫端自己（用來之後只由同一個人關掉）。</summary>
    /// <param name="showWhilePressed">true：按住滑鼠（例如畫手勢到一半）時也照常顯示；預設會先藏起來，免得擋到操作</param>
    public void Show(object owner, Func<TooltipContent> provider, bool showWhilePressed = false)
    {
        this.owner = owner;
        this.provider = provider;
        this.showWhilePressed = showWhilePressed;
        current = null;
        refreshTimer = 0f;

        // 剛看過說明框就移到下一個東西：不用再等；否則要等游標停夠 ShowDelay 秒
        bool quickSwitch = Time.unscaledTime - lastVisibleTime <= QuickSwitchWindow;
        showAt = Time.unscaledTime + (quickSwitch ? 0f : Mathf.Max(0f, ShowDelay));

        Refresh(true);
    }

    public void Hide(object owner)
    {
        if (this.owner != owner)
            return;

        this.owner = null;
        provider = null;
        current = null;
        SetVisible(false);
    }

    private void Update()
    {
        if (owner == null || provider == null)
            return;

        // 按住滑鼠（畫手勢、拖曳）時先藏起來，放開還在原處就再顯示；法陣點位的說明例外（畫到一半想看技能）
        Mouse mouse = Mouse.current;
        if (!showWhilePressed && mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed))
        {
            SetVisible(false);
            return;
        }

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = RefreshInterval;
            Refresh(false);
        }

        bool ready = current != null && Time.unscaledTime >= showAt;
        SetVisible(ready);
        if (ready)
            Position();
    }

    private void SetVisible(bool visible)
    {
        group.alpha = visible ? 1f : 0f;
        if (visible)
            lastVisibleTime = Time.unscaledTime;
    }

    private void Refresh(bool force)
    {
        TooltipContent next = provider != null ? provider() : null;
        if (next == null)
        {
            current = null;
            return;
        }

        if (!force && next.SameAs(current))
            return;

        current = next;
        titleText.text = next.title;
        titleText.color = next.titleColor;
        titleText.gameObject.SetActive(!string.IsNullOrEmpty(next.title));
        bodyText.text = next.Body;
        bodyText.gameObject.SetActive(next.lines.Count > 0);

        // 寬度：依文字長度決定，但不超過上限；高度交給排版
        float width = Mathf.Max(titleText.preferredWidth, bodyText.preferredWidth) + Padding * 2f;
        width = Mathf.Clamp(width, MinWidth, MaxWidth);
        panel.sizeDelta = new Vector2(width, panel.sizeDelta.y);
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
    }

    private void Position()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || canvasRect == null || group.alpha <= 0f)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouse.position.ReadValue(), null, out Vector2 local);

        Vector2 size = panel.rect.size;
        Rect bounds = canvasRect.rect;

        // 預設放在游標右下；放不下就翻到左邊或上面
        float x = local.x + CursorOffset;
        float y = local.y - CursorOffset;
        if (x + size.x > bounds.xMax)
            x = local.x - CursorOffset - size.x;
        if (y - size.y < bounds.yMin)
            y = local.y + CursorOffset + size.y;

        x = Mathf.Clamp(x, bounds.xMin, Mathf.Max(bounds.xMin, bounds.xMax - size.x));
        y = Mathf.Clamp(y, Mathf.Min(bounds.yMax, bounds.yMin + size.y), bounds.yMax);

        panel.anchoredPosition = new Vector2(x, y);
    }

    private static TooltipSystem Create()
    {
        GameObject root = new GameObject("TooltipSystem", typeof(Canvas), typeof(CanvasScaler));
        DontDestroyOnLoad(root);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        TooltipSystem system = root.AddComponent<TooltipSystem>();
        system.canvasRect = (RectTransform)root.transform;
        system.BuildPanel(root.transform);
        return system;
    }

    private void BuildPanel(Transform parent)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image),
            typeof(Outline), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panel = (RectTransform)panelObject.transform;
        panel.SetParent(parent, false);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0f, 1f);
        panel.sizeDelta = new Vector2(MinWidth, 0f);

        group = panelObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;   // 說明框絕不能擋到游標下面的東西
        group.interactable = false;

        Image background = panelObject.GetComponent<Image>();
        background.color = new Color(0.06f, 0.06f, 0.09f, 0.95f);
        background.raycastTarget = false;

        Outline outline = panelObject.GetComponent<Outline>();
        outline.effectColor = new Color(1f, 0.85f, 0.45f, 0.55f);
        outline.effectDistance = new Vector2(2f, -2f);

        VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset((int)Padding, (int)Padding, (int)Padding - 4, (int)Padding - 2);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panelObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        titleText = CreateText("Title", panel, font, 32, FontStyle.Bold, new Color(1f, 0.88f, 0.5f, 1f));
        bodyText = CreateText("Body", panel, font, 26, FontStyle.Normal, new Color(0.92f, 0.92f, 0.95f, 1f));
        bodyText.lineSpacing = 1.15f;
        bodyText.supportRichText = true;
    }

    private static Text CreateText(string objectName, Transform parent, Font font, int size, FontStyle style, Color color)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        Text text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }
}
