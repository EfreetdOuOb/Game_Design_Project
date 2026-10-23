using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家血條上的「來襲傷害預告」：這回合預計受到的傷害（敵人出手 + 到期引爆的炸彈，扣掉玩家目前護盾後）越高，
/// 蓋在整條血條上的淡紅虛影閃爍得越頻繁。只在玩家回合顯示；敵人被暈眩、玩家補了護盾，預告會立刻跟著變。
/// 傷害數字由 <see cref="EnemyCombatAI.PreviewDamageToPlayer"/> 與 <see cref="BattleBombManager.PreviewTimerDetonationDamage"/> 提供，和實際結算同一套公式。
/// </summary>
public class IncomingDamageIndicator : MonoBehaviour
{
    [Header("來源")]
    [SerializeField] private CombatActor _player;
    [Tooltip("要蓋上虛影的血條（Slider）；虛影會鋪滿它的整個範圍")]
    [SerializeField] private Slider _hpBar;

    [Header("閃爍")]
    [SerializeField] private Color _color = new Color(1f, 0.15f, 0.15f, 1f);
    [Tooltip("預計傷害佔玩家最大血量的這個比例（含）以上，就用最快的閃爍頻率與最深的顏色")]
    [SerializeField, Range(0.05f, 1f)] private float _fullDangerRatio = 0.5f;
    [Tooltip("傷害很小時的閃爍頻率（次/秒）")]
    [SerializeField, Min(0.1f)] private float _minFlashesPerSecond = 0.8f;
    [Tooltip("傷害達到 Full Danger Ratio 時的閃爍頻率（次/秒）")]
    [SerializeField, Min(0.1f)] private float _maxFlashesPerSecond = 5f;
    [Tooltip("虛影最淡／最深時的透明度")]
    [SerializeField, Range(0f, 1f)] private float _minAlpha = 0.12f;
    [SerializeField, Range(0f, 1f)] private float _maxAlpha = 0.4f;
    [SerializeField, Min(0f)] private float _fadeSpeed = 6f;

    private Image overlay;
    private float phase;
    private float visibility;   // 0 ~ 1，有預告時淡入、沒有時淡出

    /// <summary>目前的預告傷害（已扣護盾），給其他 UI 取用。</summary>
    public int PredictedDamage { get; private set; }

    private void Awake()
    {
        BuildOverlay();
    }

    private void Update()
    {
        PredictedDamage = ComputePredictedDamage();

        float ratio = _player != null && _player.maxHp > 0 ? (float)PredictedDamage / _player.maxHp : 0f;
        float danger = Mathf.Clamp01(ratio / _fullDangerRatio);

        visibility = Mathf.MoveTowards(visibility, PredictedDamage > 0 ? 1f : 0f, _fadeSpeed * Time.deltaTime);
        if (overlay == null)
            return;

        if (visibility <= 0.001f)
        {
            overlay.enabled = false;
            return;
        }

        // 用「累加相位」而不是 time * 頻率，頻率變動時閃爍才不會突然跳一下
        float flashesPerSecond = Mathf.Lerp(_minFlashesPerSecond, _maxFlashesPerSecond, danger);
        phase += flashesPerSecond * Time.deltaTime;

        float wave = 0.5f - 0.5f * Mathf.Cos(phase * Mathf.PI * 2f);   // 0 ~ 1 平滑來回
        float alpha = Mathf.Lerp(_minAlpha, Mathf.Lerp(_minAlpha, _maxAlpha, 0.4f + 0.6f * danger), wave) * visibility;

        overlay.enabled = true;
        Color c = _color;
        c.a = alpha;
        overlay.color = c;
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

    private void BuildOverlay()
    {
        if (_hpBar == null)
            return;

        GameObject go = new GameObject("IncomingDamageGhost", typeof(RectTransform), typeof(Image));
        go.layer = _hpBar.gameObject.layer;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(_hpBar.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsLastSibling();

        overlay = go.GetComponent<Image>();
        overlay.raycastTarget = false;
        overlay.enabled = false;
    }
}
