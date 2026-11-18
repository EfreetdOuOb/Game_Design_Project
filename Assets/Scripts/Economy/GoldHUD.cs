using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 常駐顯示玩家持有的金幣。金額變動時數字滾動，並依增減閃一下顏色。
/// 目前指定的是原本商店面板裡的 GoldTxt（已搬到玩家血條下方常駐）；商店不再自己更新金幣文字，統一由這裡負責。
/// 沒指定 Text 的話，會在 Place Below 那個 UI 的正下方自動建一個。
/// 資料來源只有 <see cref="PlayerCurrency"/> 的 OnGoldChanged 事件，這裡不改任何金幣數值。
/// </summary>
public class GoldHUD : MonoBehaviour
{
    [Header("顯示")]
    [Tooltip("選填：自己做好的 Text。留空就自動建立。")]
    [SerializeField] private Text _goldText;
    [Tooltip("自動建立時，放在這個 UI 的正下方（例如玩家血條）")]
    [SerializeField] private RectTransform _placeBelow;
    [SerializeField] private string _format = "金幣：{0}";
    [SerializeField] private int _autoFontSize = 28;
    [SerializeField] private Color _normalColor = new Color(1f, 0.87f, 0.3f, 1f);
    [SerializeField] private Color _gainColor = new Color(1f, 1f, 0.7f, 1f);
    [SerializeField] private Color _spendColor = new Color(1f, 0.45f, 0.35f, 1f);

    [Header("動畫")]
    [SerializeField, Min(0f)] private float _countDuration = 0.4f;

    private PlayerCurrency subscribed;
    private int displayedGold;
    private int lastKnownGold;
    private Tween countTween;
    private Sequence flashSequence;

    private void Awake()
    {
        if (_goldText == null)
            _goldText = BuildText();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Update()
    {
        // PlayerCurrency 可能比這個物件晚才出現，沒訂閱到就每幀補訂
        if (subscribed == null)
            TrySubscribe();
    }

    private void OnDisable()
    {
        if (subscribed != null)
            subscribed.OnGoldChanged -= HandleGoldChanged;

        subscribed = null;
        countTween?.Kill();
        flashSequence?.Kill();
    }

    private void TrySubscribe()
    {
        if (subscribed != null || PlayerCurrency.Instance == null)
            return;

        subscribed = PlayerCurrency.Instance;
        subscribed.OnGoldChanged += HandleGoldChanged;

        displayedGold = subscribed.CurrentGold;
        lastKnownGold = displayedGold;
        SetText(displayedGold);
    }

    private void HandleGoldChanged(int total)
    {
        int previous = displayedGold;

        countTween?.Kill();
        if (_countDuration > 0f && gameObject.activeInHierarchy)
        {
            countTween = DOTween.To(() => displayedGold, v => { displayedGold = v; SetText(v); }, total, _countDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
        }
        else
        {
            displayedGold = total;
            SetText(total);
        }

        if (total != previous)
            PlayFlash(total > previous ? _gainColor : _spendColor);

        // 增減量的跳字（用「上一次的實際金額」算，數字還在滾動時連續變動也不會算錯）
        int delta = total - lastKnownGold;
        lastKnownGold = total;
        if (delta != 0)
            SpawnDeltaText(delta);
    }

    // 金額旁飄出「+15」「-30」，往上飄並淡出
    private void SpawnDeltaText(int delta)
    {
        if (_goldText == null || !gameObject.activeInHierarchy)
            return;

        int alignment = (int)_goldText.alignment;
        int vertical = alignment / 3;      // 0 上、1 中、2 下
        float anchorY = vertical == 0 ? 1f : (vertical == 1 ? 0.5f : 0f);

        GameObject go = new GameObject("GoldDelta", typeof(RectTransform), typeof(Text), typeof(Outline));
        go.layer = _goldText.gameObject.layer;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(_goldText.transform, false);

        // 接在目前文字的右邊（文字是靠左對齊時用實際文字寬度，其餘情況貼在文字框右緣）
        bool leftAligned = alignment % 3 == 0;
        float x = leftAligned ? _goldText.preferredWidth + _goldText.fontSize * 0.4f : _goldText.rectTransform.rect.width + _goldText.fontSize * 0.4f;
        rect.anchorMin = new Vector2(0f, anchorY);
        rect.anchorMax = new Vector2(0f, anchorY);
        rect.pivot = new Vector2(0f, anchorY);
        rect.sizeDelta = new Vector2(_goldText.fontSize * 6f, _goldText.fontSize * 1.4f);
        rect.anchoredPosition = new Vector2(x, 0f);

        Text label = go.GetComponent<Text>();
        label.font = _goldText.font;
        label.fontSize = _goldText.fontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleLeft;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.color = delta > 0 ? _gainColor : _spendColor;
        label.text = (delta > 0 ? "+" : "") + delta;
        label.raycastTarget = false;

        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        Sequence floatUp = DOTween.Sequence().SetLink(go);
        floatUp.Append(rect.DOAnchorPosY(_goldText.fontSize * 1.2f, 0.9f).SetEase(Ease.OutQuad));
        floatUp.Insert(0.45f, label.DOFade(0f, 0.45f));
        floatUp.Insert(0.45f, outline.DOFade(0f, 0.45f));
        floatUp.OnComplete(() => Destroy(go));
    }

    private void PlayFlash(Color color)
    {
        if (_goldText == null)
            return;

        flashSequence?.Kill();
        _goldText.color = color;
        _goldText.rectTransform.localScale = Vector3.one;

        flashSequence = DOTween.Sequence().SetLink(gameObject);
        flashSequence.Append(_goldText.rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 6, 0.6f));
        flashSequence.Join(_goldText.DOColor(_normalColor, 0.5f));
    }

    private void SetText(int value)
    {
        if (_goldText != null)
            _goldText.text = string.Format(_format, value);
    }

    private Text BuildText()
    {
        Transform parent = _placeBelow != null ? _placeBelow.parent : transform;

        GameObject go = new GameObject("GoldText", typeof(RectTransform), typeof(Text), typeof(Outline));
        go.layer = gameObject.layer;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);

        if (_placeBelow != null)
        {
            // 和參考物件同一套錨點，位置在它正下方
            rect.anchorMin = _placeBelow.anchorMin;
            rect.anchorMax = _placeBelow.anchorMax;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(_placeBelow.sizeDelta.x, _autoFontSize * 1.6f);
            float bottomY = _placeBelow.anchoredPosition.y - _placeBelow.sizeDelta.y * _placeBelow.pivot.y;
            rect.anchoredPosition = new Vector2(_placeBelow.anchoredPosition.x, bottomY - 6f);
        }

        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = _autoFontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = _normalColor;
        text.raycastTarget = false;

        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        return text;
    }
}
