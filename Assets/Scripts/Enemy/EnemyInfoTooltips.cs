using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 血條區域的懸浮說明接線：
///   · 敵人：游標移到血條 → 敵人概況（生命、攻擊力、護盾、接下來的行動順序）；移到韌性條 → 韌性規則；
///     移到意圖圖示 → 這一招實際會發生什麼
///   · 玩家：游標移到血條 → 本回合預計受到的傷害明細（敵人出手、炸彈、護盾吸收）
/// 只負責把 <see cref="TooltipTrigger"/> 掛到對的物件上，說明內容由 <see cref="CombatTooltips"/> 產生。
/// 由 <see cref="EnemyHealthBarUI"/> 自動掛上（玩家血條也用同一個腳本）。
/// </summary>
public class EnemyInfoTooltips : MonoBehaviour
{
    private EnemyHealthBarUI bar;
    private CombatActor actor;
    private EnemyCombatAI ai;
    private EnemyPoise poise;
    private IncomingDamageIndicator incoming;

    private void Start()
    {
        bar = GetComponent<EnemyHealthBarUI>();
        if (bar == null)
            return;

        actor = bar.enemyActor;
        ai = GetComponent<EnemyCombatAI>();
        if (ai == null)
            ai = GetComponentInParent<EnemyCombatAI>();

        poise = GetComponent<EnemyPoise>();
        incoming = GetComponent<IncomingDamageIndicator>();

        AttachBar();
        AttachPoise();
        AttachIntents();
    }

    private void AttachBar()
    {
        GameObject area = TrackArea(bar.hpSlider);
        if (area == null)
            return;

        if (ai != null)
            TooltipTrigger.Attach(area, () => CombatTooltips.ForEnemyOverview(ai));
        else
            TooltipTrigger.Attach(area, () => CombatTooltips.ForPlayerHp(actor, incoming));
    }

    private void AttachPoise()
    {
        GameObject area = TrackArea(bar.poiseSlider);
        if (area != null && poise != null)
            TooltipTrigger.Attach(area, () => CombatTooltips.ForPoise(poise));
    }

    private void AttachIntents()
    {
        if (ai == null || ai.actionSequence == null)
            return;

        for (int i = 0; i < ai.actionSequence.Count; i++)
        {
            EnemyActionStep step = ai.actionSequence[i];
            if (step == null || step.intentIcon == null)
                continue;

            TooltipTrigger.Attach(step.intentIcon, () => CombatTooltips.ForIntent(ai, step));
        }
    }

    // 血條「實際看得到的範圍」是 Fill 的上一層（Fill Area），不是 Slider 物件本身的框
    private static GameObject TrackArea(Slider slider)
    {
        if (slider == null)
            return null;

        if (slider.fillRect != null && slider.fillRect.parent is RectTransform fillArea)
            return fillArea.gameObject;

        return slider.gameObject;
    }
}
