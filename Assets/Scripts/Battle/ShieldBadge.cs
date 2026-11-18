using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 血條左端的護盾徽章：盾牌圖形 + 目前護盾數字，護盾為 0 時整個隱藏。
/// 玩家與敵人共用，只讀取 <see cref="CombatActor.shield"/>。護盾增加時彈一下，被打掉時閃紅。
/// 由 <see cref="EnemyHealthBarUI"/> 自動掛上（玩家血條也用同一個腳本），也可以手動掛並指定欄位。
/// </summary>
public class ShieldBadge : MonoBehaviour
{
    // 字用 4 倍大再縮回 1/4，World Space 的小 Canvas 上文字才不會糊
    private const float TextOversample = 4f;

    [SerializeField] private CombatActor _actor;
    [Tooltip("要掛徽章的血條（Slider）；徽章會貼在它的 Fill Area 左端")]
    [SerializeField] private Slider _hpBar;

    [Header("外觀")]
    [Tooltip("徽章大小，單位是血條高度的倍數")]
    [SerializeField, Min(0.5f)] private float _sizeRatio = 1.9f;
    [SerializeField] private Color _shieldColor = new Color(0.35f, 0.65f, 1f, 1f);
    [SerializeField] private Color _gainColor = new Color(0.75f, 0.9f, 1f, 1f);
    [SerializeField] private Color _lossColor = new Color(1f, 0.45f, 0.4f, 1f);

    private RectTransform root;
    private Image image;
    private Text label;
    private int lastShield;
    private bool built;
    private Sequence flash;

    /// <summary>執行時由程式指定角色與血條（自動掛上時用）。</summary>
    public void Bind(CombatActor actor, Slider hpBar)
    {
        _actor = actor;
        _hpBar = hpBar;
        Build();
    }

    private void Awake()
    {
        Build();
    }

    private void OnDestroy()
    {
        flash?.Kill();
    }

    private void Update()
    {
        if (!built || _actor == null)
            return;

        int shield = _actor.shield;
        if (shield == lastShield)
            return;

        bool gained = shield > lastShield;
        lastShield = shield;

        if (shield <= 0)
        {
            // 被打光：先閃一下紅再消失
            PlayFlash(_lossColor, true);
            return;
        }

        label.text = shield.ToString();
        root.gameObject.SetActive(true);
        PlayFlash(gained ? _gainColor : _lossColor, false);
    }

    private void PlayFlash(Color color, bool hideAfter)
    {
        flash?.Kill();
        root.localScale = Vector3.one;
        image.color = color;

        flash = DOTween.Sequence().SetLink(gameObject);
        flash.Append(root.DOPunchScale(Vector3.one * 0.3f, 0.25f, 6, 0.6f));
        flash.Join(image.DOColor(_shieldColor, 0.4f));
        flash.OnComplete(() =>
        {
            if (hideAfter && _actor != null && _actor.shield <= 0)
                root.gameObject.SetActive(false);
        });
    }

    private void Build()
    {
        if (built || _actor == null || _hpBar == null)
            return;

        // 掛在 Fill Area 底下：它是血條實際可見的範圍；沒有就退而求其次用 Slider 本身
        RectTransform target = _hpBar.fillRect != null && _hpBar.fillRect.parent is RectTransform fillArea
            ? fillArea
            : (RectTransform)_hpBar.transform;

        float barHeight = target.rect.height > 1f ? target.rect.height : 30f;
        float size = barHeight * _sizeRatio;

        GameObject go = new GameObject("ShieldBadge", typeof(RectTransform), typeof(Image));
        go.layer = target.gameObject.layer;
        root = (RectTransform)go.transform;
        root.SetParent(target, false);
        root.anchorMin = new Vector2(0f, 0.5f);
        root.anchorMax = new Vector2(0f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(size, size);
        root.anchoredPosition = Vector2.zero;
        root.SetAsLastSibling();

        image = go.GetComponent<Image>();
        image.sprite = CombatUiSprites.Shield;
        image.color = _shieldColor;
        image.raycastTarget = false;

        GameObject textObject = new GameObject("Value", typeof(RectTransform), typeof(Text), typeof(Outline));
        textObject.layer = go.layer;
        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.SetParent(root, false);
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(size * TextOversample, size * TextOversample);
        textRect.localScale = Vector3.one / TextOversample;
        textRect.anchoredPosition = new Vector2(0f, size * 0.04f);

        label = textObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = Mathf.RoundToInt(size * 0.42f * TextOversample);
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.color = Color.white;
        label.raycastTarget = false;

        Outline outline = textObject.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0.1f, 0.3f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        TooltipTrigger.Attach(go, () => CombatTooltips.ForShield(_actor));

        built = true;
        lastShield = _actor.shield;
        label.text = lastShield.ToString();
        go.SetActive(lastShield > 0);
    }
}
