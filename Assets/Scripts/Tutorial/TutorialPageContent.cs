using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 單一彈出教學頁的內容：標題、內文、可選圖片。
/// 版面（暗色遮罩 + 卡片 + 標題 + 內文 + 底部提示）在第一次顯示時用程式建立，
/// 所有文字都在 Inspector 編輯，不用手動排 UI；之後想換成自己做的圖片，
/// 只要在 Image 欄位放圖，圖片會顯示在卡片左側、文字移到右側。
/// 「第幾頁／共幾頁」由 TutorialUIControl 顯示時透過 <see cref="SetProgress"/> 帶入。
/// </summary>
[DisallowMultipleComponent]
public class TutorialPageContent : MonoBehaviour
{
    public enum PageLayout
    {
        Auto,        // 這頁的 Image 有放自己的圖就只顯示圖，沒放圖就顯示文字卡片
        TextCard,    // 一律顯示文字卡片（標題 + 內文，可選左側小圖）
        PictureOnly  // 只顯示這頁 Image 上的圖，不加遮罩、不畫卡片、不改顏色
    }

    [Header("版面")]
    [Tooltip("Auto：這頁的 Image 有放自己的圖就只顯示圖，沒放圖就顯示文字卡片。TextCard / PictureOnly 可強制指定。")]
    [SerializeField] private PageLayout _layout = PageLayout.Auto;
    [Tooltip("只顯示圖片時，是否在畫面底部疊上「點擊畫面任意處繼續 1 / 2」。圖片本身已經有提示的話可以關掉。")]
    [SerializeField] private bool _showHintOnPicture = true;

    [Header("內容")]
    [SerializeField] private string _title;
    [SerializeField, TextArea(4, 12)] private string _body;
    [Tooltip("選填：有圖片時顯示在卡片左側，文字移到右側。")]
    [SerializeField] private Sprite _image;

    [Header("外觀")]
    [Tooltip("卡片與外框用的圓角 Sprite；留空則為直角矩形。")]
    [SerializeField] private Sprite _cardSprite;
    [SerializeField] private Vector2 _cardSize = new Vector2(1500f, 820f);
    [SerializeField] private float _imageWidth = 560f;
    [SerializeField] private Color _dimColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private Color _borderColor = new Color(0.447f, 0.29f, 0.157f, 1f);
    [SerializeField] private Color _paperColor = new Color(0.99f, 0.973f, 0.91f, 1f);
    [SerializeField] private Color _textColor = new Color(0.15f, 0.1f, 0.07f, 1f);
    [SerializeField] private Color _hintColor = new Color(0.45f, 0.38f, 0.3f, 1f);
    [SerializeField] private int _titleFontSize = 64;
    [SerializeField] private int _bodyFontSize = 42;
    [SerializeField] private int _hintFontSize = 30;
    [SerializeField] private Font _font;

    private const float BorderThickness = 12f;

    private Text _hintText;
    private bool _built;
    private int _pageIndex = 1;
    private int _pageTotal = 1;

    private void OnEnable()
    {
        EnsureBuilt();
    }

    public void SetProgress(int pageIndex, int pageTotal)
    {
        _pageIndex = Mathf.Max(1, pageIndex);
        _pageTotal = Mathf.Max(1, pageTotal);

        EnsureBuilt();
        RefreshHint();
    }

    private void RefreshHint()
    {
        if (_hintText == null)
            return;

        bool isLastPage = _pageIndex >= _pageTotal;
        string action = isLastPage ? "關閉" : "繼續";
        _hintText.text = _pageTotal > 1
            ? $"點擊畫面任意處{action}　{_pageIndex} / {_pageTotal}"
            : $"點擊畫面任意處{action}";
    }

    private void EnsureBuilt()
    {
        if (_built)
            return;

        _built = true;

        Image pageImage = GetComponent<Image>();
        Font font = _font != null ? _font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (IsPictureMode(pageImage))
        {
            BuildPictureHint(font);
            return;
        }

        // 文字卡片模式：這個物件本身的 Image 當作全螢幕暗色遮罩，同時負責擋住底下的點擊
        if (pageImage != null)
            pageImage.color = _dimColor;

        RectTransform border = CreateImage("Border", transform, _borderColor);
        SetCentered(border, _cardSize + new Vector2(BorderThickness * 2f, BorderThickness * 2f));

        RectTransform card = CreateImage("Card", transform, _paperColor);
        SetCentered(card, _cardSize);

        BuildTitle(card, font);
        BuildBody(card, font);
        BuildHint(card, font);
        RefreshHint();
    }

    private bool IsPictureMode(Image pageImage)
    {
        switch (_layout)
        {
            case PageLayout.TextCard:
                return false;
            case PageLayout.PictureOnly:
                return true;
            default:
                return pageImage != null && !IsPlaceholderSprite(pageImage.sprite);
        }
    }

    // 頁面原本的 Image 預設是 Unity 內建的 Background 底圖（綠色佔位用），不算「自己的圖」
    private bool IsPlaceholderSprite(Sprite sprite)
    {
        return sprite == null || sprite == _cardSprite || sprite.name == "Background";
    }

    // 圖片模式：圖片完全交給這頁的 Image 顯示，這裡只在畫面底部疊一行描邊的提示文字
    private void BuildPictureHint(Font font)
    {
        if (!_showHintOnPicture)
            return;

        _hintText = CreateText("Hint", transform, string.Empty, _hintFontSize, FontStyle.Bold,
            TextAnchor.LowerCenter, Color.white, font);

        Outline outline = _hintText.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        RectTransform hintRect = _hintText.rectTransform;
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.anchoredPosition = new Vector2(0f, 24f);
        hintRect.sizeDelta = new Vector2(900f, 50f);

        RefreshHint();
    }

    private void BuildTitle(RectTransform card, Font font)
    {
        Text title = CreateText("Title", card, _title, _titleFontSize, FontStyle.Bold, TextAnchor.MiddleCenter, _textColor, font);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -36f);
        titleRect.sizeDelta = new Vector2(-120f, 110f);

        RectTransform divider = CreateImage("Divider", card, _borderColor);
        divider.anchorMin = new Vector2(0f, 1f);
        divider.anchorMax = new Vector2(1f, 1f);
        divider.pivot = new Vector2(0.5f, 1f);
        divider.anchoredPosition = new Vector2(0f, -156f);
        divider.sizeDelta = new Vector2(-120f, 4f);
    }

    private void BuildBody(RectTransform card, Font font)
    {
        const float sideMargin = 70f;
        const float topMargin = 190f;
        const float bottomMargin = 90f;

        float textLeft = sideMargin;

        if (_image != null)
        {
            RectTransform imageRect = CreateRect("Image", card);
            Image image = imageRect.gameObject.AddComponent<Image>();
            image.sprite = _image;
            image.preserveAspect = true;
            image.raycastTarget = false;

            imageRect.anchorMin = new Vector2(0f, 0f);
            imageRect.anchorMax = new Vector2(0f, 1f);
            imageRect.pivot = new Vector2(0f, 0.5f);
            imageRect.offsetMin = new Vector2(sideMargin, bottomMargin);
            imageRect.offsetMax = new Vector2(sideMargin + _imageWidth, -topMargin);

            textLeft = sideMargin + _imageWidth + 40f;
        }

        Text body = CreateText("Body", card, _body, _bodyFontSize, FontStyle.Normal, TextAnchor.UpperLeft, _textColor, font);
        body.lineSpacing = 1.3f;
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform bodyRect = body.rectTransform;
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(textLeft, bottomMargin);
        bodyRect.offsetMax = new Vector2(-sideMargin, -topMargin);
    }

    private void BuildHint(RectTransform card, Font font)
    {
        _hintText = CreateText("Hint", card, string.Empty, _hintFontSize, FontStyle.Normal, TextAnchor.LowerRight, _hintColor, font);
        RectTransform hintRect = _hintText.rectTransform;
        hintRect.anchorMin = new Vector2(1f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(1f, 0f);
        hintRect.anchoredPosition = new Vector2(-60f, 30f);
        hintRect.sizeDelta = new Vector2(900f, 50f);
    }

    private RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.layer = gameObject.layer;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private RectTransform CreateImage(string objectName, Transform parent, Color color)
    {
        RectTransform rect = CreateRect(objectName, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        if (_cardSprite != null)
        {
            image.sprite = _cardSprite;
            image.type = Image.Type.Sliced;
        }

        return rect;
    }

    private Text CreateText(string objectName, Transform parent, string content, int fontSize,
        FontStyle style, TextAnchor alignment, Color color, Font font)
    {
        RectTransform rect = CreateRect(objectName, parent);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static void SetCentered(RectTransform rect, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
    }
}
