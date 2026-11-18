using UnityEngine;

/// <summary>
/// 數值變化的跳字提示：角色身上發生傷害、回復、護盾增減、debuff、韌性變化時，在頭上跳出對應的數字或文字。
/// 玩家與敵人共用，只監聽 <see cref="CombatActor"/>（與 EnemyPoise）的事件，不影響任何戰鬥數值。
/// 由 CombatActor 在 Awake 自動掛上預設版本；想自訂顏色、大小就在 prefab 上自己掛這個元件。
/// </summary>
[RequireComponent(typeof(CombatActor))]
[DisallowMultipleComponent]
public class CombatFeedbackPresenter : MonoBehaviour
{
    [Header("顏色")]
    [SerializeField] private Color _damageColor = new Color(1f, 0.35f, 0.3f, 1f);
    [SerializeField] private Color _shieldColor = new Color(0.55f, 0.82f, 1f, 1f);
    [SerializeField] private Color _healColor = new Color(0.4f, 1f, 0.5f, 1f);
    [SerializeField] private Color _expireColor = new Color(0.78f, 0.78f, 0.85f, 1f);
    [SerializeField] private Color _debuffColor = new Color(0.82f, 0.62f, 1f, 1f);
    [SerializeField] private Color _poiseColor = new Color(1f, 0.9f, 0.4f, 1f);

    [Header("大小（單位是角色高度的比例）")]
    [SerializeField, Min(0.02f)] private float _bigSize = 0.17f;
    [SerializeField, Min(0.02f)] private float _smallSize = 0.12f;

    [Tooltip("連續跳字時，每個字往左右錯開的幅度（角色高度的比例），避免疊在一起")]
    [SerializeField, Min(0f)] private float _laneSpread = 0.22f;
    [Tooltip("多久內的連續跳字算同一串、需要錯開（秒）")]
    [SerializeField, Min(0.05f)] private float _burstWindow = 0.45f;

    private CombatActor actor;
    private EnemyPoise poise;
    private Vector3 headLocalOffset;
    private float height = 1f;
    private bool boundsReady;
    private float lastPopupTime = -10f;
    private int lane;

    private void Awake()
    {
        actor = GetComponent<CombatActor>();
        poise = GetComponent<EnemyPoise>();
    }

    private void OnEnable()
    {
        if (actor == null)
            return;

        actor.OnDamageResolved += HandleDamage;
        actor.OnHealed += HandleHealed;
        actor.OnShieldGained += HandleShieldGained;
        actor.OnShieldExpired += HandleShieldExpired;
        actor.OnDebuffApplied += HandleDebuff;

        if (poise != null)
            poise.OnPoiseReduced += HandlePoiseReduced;
    }

    private void OnDisable()
    {
        if (actor != null)
        {
            actor.OnDamageResolved -= HandleDamage;
            actor.OnHealed -= HandleHealed;
            actor.OnShieldGained -= HandleShieldGained;
            actor.OnShieldExpired -= HandleShieldExpired;
            actor.OnDebuffApplied -= HandleDebuff;
        }

        if (poise != null)
            poise.OnPoiseReduced -= HandlePoiseReduced;
    }

    // ---- 事件 ----

    private void HandleDamage(DamageResult result)
    {
        if (result.Calculated <= 0)
            return;

        if (result.HpLoss > 0)
            Popup("-" + result.HpLoss, _damageColor, _bigSize);

        if (result.Absorbed > 0)
            Popup((result.FullyBlocked ? "格擋 " : "盾 ") + "-" + result.Absorbed, _shieldColor, _smallSize);
    }

    private void HandleHealed(int amount)
    {
        Popup("+" + amount, _healColor, _bigSize);
    }

    private void HandleShieldGained(int amount)
    {
        Popup("+" + amount + " 盾", _shieldColor, _smallSize);
    }

    private void HandleShieldExpired(int amount)
    {
        Popup("護盾消失 -" + amount, _expireColor, _smallSize);
    }

    private void HandleDebuff(string debuffId, int addedStacks, int remainingTurns)
    {
        Popup(DebuffName(debuffId) + " +" + addedStacks, _debuffColor, _smallSize);
    }

    private void HandlePoiseReduced(int amount)
    {
        Popup("韌性 -" + amount, _poiseColor, _smallSize);
    }

    private static string DebuffName(string debuffId)
    {
        switch (debuffId)
        {
            case CombatActor.VulnerableDebuffId: return "易傷";
            case CombatActor.WeakDebuffId: return "虛弱";
            default: return debuffId;
        }
    }

    // ---- 跳字 ----

    private void Popup(string text, Color color, float sizeRatio)
    {
        EnsureBounds();

        // 同一串連續跳字依序往左右錯開，不會疊成一團
        lane = Time.time - lastPopupTime <= _burstWindow ? lane + 1 : 0;
        lastPopupTime = Time.time;
        float side = (lane % 3 - 1) * _laneSpread * height;

        Camera cam = Camera.main;
        Vector3 right = cam != null ? cam.transform.right : Vector3.right;
        Vector3 position = transform.position + headLocalOffset + Vector3.up * (height * 0.1f) + right * side;

        WorldPopupText.Spawn(position, height * sizeRatio, text, color);
    }

    // 以模型外框決定跳字的位置與大小（和 EnemyTintController 同樣的算法，但玩家身上沒有那個元件）
    private void EnsureBounds()
    {
        if (boundsReady)
            return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Bounds total = new Bounds(transform.position, Vector3.zero);
        bool has = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (!(r is SkinnedMeshRenderer) && !(r is MeshRenderer))
                continue;

            if (!has) { total = r.bounds; has = true; }
            else total.Encapsulate(r.bounds);
        }

        if (has)
        {
            height = Mathf.Max(0.1f, total.size.y);
            headLocalOffset = new Vector3(total.center.x, total.max.y, total.center.z) - transform.position;
        }
        else
        {
            headLocalOffset = Vector3.up;
        }

        boundsReady = true;
    }
}
