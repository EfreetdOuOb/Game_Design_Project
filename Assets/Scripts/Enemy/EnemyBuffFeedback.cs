using DG.Tweening;
using UnityEngine;

/// <summary>
/// 敵人施放「強化」時的當下表現：金色閃光 + 頭上跳出「攻擊力提升！」。
/// 常駐的強化數字在狀態欄（<see cref="EnemyHealthBarUI"/>），這裡只負責施放那一瞬間。
/// 由 <see cref="EnemyCombatAI"/> 自動掛上；想自訂可直接在 prefab 上掛這個元件。
/// </summary>
[RequireComponent(typeof(EnemyTintController))]
[DisallowMultipleComponent]
public class EnemyBuffFeedback : MonoBehaviour
{
    [SerializeField] private Color _flashColor = new Color(1f, 0.78f, 0.2f, 1f);
    [SerializeField, Range(0f, 1f)] private float _flashStrength = 0.7f;
    [SerializeField, Min(0.05f)] private float _flashDuration = 0.5f;
    [SerializeField] private Color _textColor = new Color(1f, 0.85f, 0.25f, 1f);
    [Tooltip("跳字的字高，單位是敵人高度的比例")]
    [SerializeField, Min(0.01f)] private float _textHeightRatio = 0.14f;
    [SerializeField] private string _popupFormat = "攻擊力 +{0}！";

    private CombatActor actor;
    private EnemyTintController tint;
    private float flashAmount;
    private Tween flashTween;

    private void Awake()
    {
        actor = GetComponent<CombatActor>();
        tint = GetComponent<EnemyTintController>();
    }

    private void OnEnable()
    {
        if (actor != null)
            actor.OnAttackBuffed += HandleAttackBuffed;
    }

    private void OnDisable()
    {
        if (actor != null)
            actor.OnAttackBuffed -= HandleAttackBuffed;

        flashTween?.Kill();
        flashAmount = 0f;
        if (tint != null)
            tint.SetChannel(TintChannel.Flash, _flashColor, 0f);
    }

    private void HandleAttackBuffed(int amount)
    {
        PlayFlash();

        tint.EnsureCache();
        float height = tint.BoundsReady ? tint.ModelBounds.size.y : 1f;
        WorldPopupText.Spawn(
            tint.HeadPosition + Vector3.up * (height * 0.1f),
            height * _textHeightRatio,
            string.Format(_popupFormat, amount),
            _textColor);
    }

    // 閃光：瞬間變金色，再淡回原色
    private void PlayFlash()
    {
        flashTween?.Kill();
        flashAmount = _flashStrength;
        tint.SetChannel(TintChannel.Flash, _flashColor, flashAmount);

        flashTween = DOTween.To(() => flashAmount, v =>
            {
                flashAmount = v;
                tint.SetChannel(TintChannel.Flash, _flashColor, flashAmount);
            }, 0f, _flashDuration)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject);
    }
}
