using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家血條上的「來襲傷害預告」：這回合預計受到的傷害（敵人出手 + 到期引爆的炸彈，扣掉玩家目前護盾後）
/// 會在血條上標出「即將失去的那一段血量」，並淡淡地閃爍；預計傷害越高閃得越快。
/// 只在玩家回合顯示；敵人被暈眩、玩家補了護盾，預告會立刻跟著變。
/// 傷害數字由 <see cref="EnemyCombatAI.PreviewDamageToPlayer"/> 與 <see cref="BattleBombManager.PreviewTimerDetonationDamage"/> 提供，和實際結算同一套公式。
/// </summary>
public class IncomingDamageIndicator : MonoBehaviour
{
    [Header("來源")]
    [SerializeField] private CombatActor _player;
    [Tooltip("要蓋上虛影的血條（Slider）。虛影會掛在它的 Fill 底下，跟著 Fill 實際畫出的形狀與位置走")]
    [SerializeField] private Slider _hpBar;
    [Tooltip("選填：血條不是標準 Slider 結構時，手動指定虛影要掛的區域（此時虛影以整個區域當 0 ~ 最大血量來定位）")]
    [SerializeField] private RectTransform _targetOverride;

    [Header("顯示範圍")]
    [Tooltip("開：虛影只標出「這一波會被扣掉的那一段血量」（從目前血量往回算）。關：虛影鋪滿整條血條。")]
    [SerializeField] private bool _showLossSegmentOnly = true;

    [Header("閃爍")]
    [Tooltip("虛影的顏色。血條圖片本身偏暗，所以要用亮一點的顏色才看得出來。")]
    [SerializeField] private Color _color = new Color(1f, 0.22f, 0.18f, 1f);
    [Tooltip("預計傷害佔玩家最大血量的這個比例（含）以上，就用最快的閃爍頻率與最深的顏色")]
    [SerializeField, Range(0.05f, 1f)] private float _fullDangerRatio = 0.8f;
    [Tooltip("傷害很小時的閃爍頻率（次/秒）")]
    [SerializeField, Min(0.1f)] private float _minFlashesPerSecond = 0.5f;
    [Tooltip("傷害達到 Full Danger Ratio 時的閃爍頻率（次/秒）")]
    [SerializeField, Min(0.1f)] private float _maxFlashesPerSecond = 3f;
    [Tooltip("虛影最淡／最深時的透明度")]
    [SerializeField, Range(0f, 1f)] private float _minAlpha = 0.2f;
    [SerializeField, Range(0f, 1f)] private float _maxAlpha = 0.65f;
    [SerializeField, Min(0f)] private float _fadeSpeed = 6f;

    private RectTransform ghostRoot;   // 套了 Mask 的外框，鋪滿父物件，形狀跟血條一樣（斜角、圓角都一致）
    private RectTransform glowRect;    // 亮色填色的位置（只佔「即將失去的那一段」）
    private Image glow;                // 真正閃爍的亮色填色，被外框裁成血條形狀
    private bool parentIsFill;         // 虛影是掛在 Fill 底下（用 Fill 自己的範圍定位），還是掛在整條軌道底下
    private float phase;
    private float visibility;          // 0 ~ 1，有預告時淡入、沒有時淡出

    /// <summary>目前的預告傷害（已扣護盾），給其他 UI 取用。</summary>
    public int PredictedDamage { get; private set; }

    private void Awake()
    {
        BuildGhost();
    }

    private void Update()
    {
        PredictedDamage = ComputePredictedDamage();

        float ratio = _player != null && _player.maxHp > 0 ? (float)PredictedDamage / _player.maxHp : 0f;
        float danger = Mathf.Clamp01(ratio / _fullDangerRatio);

        visibility = Mathf.MoveTowards(visibility, PredictedDamage > 0 ? 1f : 0f, _fadeSpeed * Time.deltaTime);
        if (ghostRoot == null)
            return;

        if (visibility <= 0.001f)
        {
            if (ghostRoot.gameObject.activeSelf)
                ghostRoot.gameObject.SetActive(false);
            return;
        }

        if (!ghostRoot.gameObject.activeSelf)
            ghostRoot.gameObject.SetActive(true);

        // 用「累加相位」而不是 time * 頻率，頻率變動時閃爍才不會突然跳一下
        float flashesPerSecond = Mathf.Lerp(_minFlashesPerSecond, _maxFlashesPerSecond, danger);
        phase += flashesPerSecond * Time.deltaTime;

        float wave = 0.5f - 0.5f * Mathf.Cos(phase * Mathf.PI * 2f);   // 0 ~ 1 平滑來回
        float alpha = Mathf.Lerp(_minAlpha, Mathf.Lerp(_minAlpha, _maxAlpha, 0.4f + 0.6f * danger), wave) * visibility;

        UpdateGhostRange();

        Color c = _color;
        c.a = alpha;
        glow.color = c;
    }

    // 虛影只標出血條上「即將失去的那一段」。
    // 掛在 Fill 底下時，Fill 自己就是「目前血量」的範圍，所以虛影只要佔它靠近血量末端的 傷害 ÷ 目前血量 那一段，
    // 完全不用算 Fill 的錨點或位移，也不會因為血條位置、大小調整而偏掉。
    private void UpdateGhostRange()
    {
        float low = 0f;   // 以血條起點（Slider 最小值那一側）為 0 的位置
        float high = 1f;

        if (_showLossSegmentOnly && _player != null && _player.maxHp > 0)
        {
            if (parentIsFill)
            {
                float fillValue = Mathf.Max(0.0001f, _hpBar.value - _hpBar.minValue);
                float portion = Mathf.Clamp01(PredictedDamage / fillValue);
                high = 1f;
                low = 1f - portion;
            }
            else
            {
                high = Mathf.Clamp01((float)_player.currentHp / _player.maxHp);
                low = Mathf.Clamp01((float)(_player.currentHp - PredictedDamage) / _player.maxHp);
            }
        }

        Slider.Direction direction = _hpBar != null ? _hpBar.direction : Slider.Direction.LeftToRight;
        bool horizontal = direction == Slider.Direction.LeftToRight || direction == Slider.Direction.RightToLeft;
        bool reversed = direction == Slider.Direction.RightToLeft || direction == Slider.Direction.TopToBottom;

        float from = reversed ? 1f - high : low;
        float to = reversed ? 1f - low : high;

        glowRect.anchorMin = horizontal ? new Vector2(from, 0f) : new Vector2(0f, from);
        glowRect.anchorMax = horizontal ? new Vector2(to, 1f) : new Vector2(1f, to);
        glowRect.offsetMin = Vector2.zero;
        glowRect.offsetMax = Vector2.zero;
    }

    private int ComputePredictedDamage()
    {
        if (_player == null || _player.IsDead)
            return 0;

        // 只有玩家回合才預告；敵人出手時傷害已經在結算了
        if (TurnManager.Instance != null && TurnManager.Instance.CurrentPhase != TurnPhase.PlayerTurn)
            return 0;

        int total = 0;
        var enemies = EnemyCombatAI.Active;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null)
                total += enemies[i].PreviewDamageToPlayer();
        }

        // 炸彈在回合結算時引爆，比敵人出手晚、比護盾清零早，所以和敵人傷害一起被同一池護盾吸收
        if (BattleBombManager.Instance != null)
            total += BattleBombManager.Instance.PreviewTimerDetonationDamage(_player);

        // 護盾是整體吸收量，所以先加總再扣
        return Mathf.Max(0, total - _player.shield);
    }

    // 結構：外框（血條形狀的圖 + Mask，自己不顯示）→ 裡面一塊亮色填色。
    // 血條圖片是暗色，Image 的顏色是和圖片相乘，直接拿它上色一定偏暗，
    // 所以只借它的「形狀」當遮罩，顏色由底下純色的 Image 負責。
    private void BuildGhost()
    {
        RectTransform target = ResolveTarget(out Image fillImage);
        if (target == null)
            return;

        GameObject rootObject = new GameObject("IncomingDamageGhost", typeof(RectTransform), typeof(Image), typeof(Mask));
        rootObject.layer = target.gameObject.layer;
        ghostRoot = (RectTransform)rootObject.transform;
        ghostRoot.SetParent(target, false);
        ghostRoot.anchorMin = Vector2.zero;
        ghostRoot.anchorMax = Vector2.one;
        ghostRoot.offsetMin = Vector2.zero;
        ghostRoot.offsetMax = Vector2.zero;
        ghostRoot.SetAsLastSibling();

        Image shape = rootObject.GetComponent<Image>();
        shape.raycastTarget = false;
        if (fillImage != null && fillImage.sprite != null)
        {
            shape.sprite = fillImage.sprite;
            shape.type = fillImage.type == Image.Type.Filled ? Image.Type.Simple : fillImage.type;
            shape.pixelsPerUnitMultiplier = fillImage.pixelsPerUnitMultiplier;
        }

        rootObject.GetComponent<Mask>().showMaskGraphic = false;

        GameObject glowObject = new GameObject("Glow", typeof(RectTransform), typeof(Image));
        glowObject.layer = rootObject.layer;
        glowRect = (RectTransform)glowObject.transform;
        glowRect.SetParent(ghostRoot, false);
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = Vector2.zero;
        glowRect.offsetMax = Vector2.zero;

        glow = glowObject.GetComponent<Image>();
        glow.raycastTarget = false;

        rootObject.SetActive(false);
    }

    // 虛影的父物件：優先用 Fill（血條實際畫出來的範圍），其次是手動指定的區域，最後才是 Slider 本身。
    // 不用 Slider 物件的框，因為調整 UI 位置時它和實際看到的血條常常不一樣。
    private RectTransform ResolveTarget(out Image fillImage)
    {
        fillImage = null;
        parentIsFill = false;

        if (_hpBar != null && _hpBar.fillRect != null)
            fillImage = _hpBar.fillRect.GetComponent<Image>();

        if (_targetOverride != null)
            return _targetOverride;

        if (_hpBar == null)
            return null;

        // 優先掛在 Fill 底下：Fill 實際畫出來的範圍就是「目前血量」，虛影跟著它走才不會偏
        if (_hpBar.fillRect != null)
        {
            parentIsFill = true;
            return _hpBar.fillRect;
        }

        return (RectTransform)_hpBar.transform;
    }
}
