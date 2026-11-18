using UnityEngine;

/// <summary>
/// 法陣上每個點位的懸浮說明：
///   點 1～4 → 目前是攻擊點還是防禦點（和 <see cref="GesturePointValueLabels"/> 一樣看現在放的是哪張圖）
///   點 5～8 → 目前抽到的技能的詳細說明（自動由技能資產產生）
///   點 9    → 轉換點
/// 如果這個點上有炸彈，會在最後補上炸彈的倒數與傷害。
/// 掛在點位物件本身（不另外加圖形），手勢偵測才不會被影響；畫手勢（按住滑鼠）時說明會自動隱藏。
/// 由 <see cref="ModeSwitchImageController"/> 自動掛上。
/// </summary>
public class GesturePointTooltips : MonoBehaviour
{
    private ModeSwitchImageController controller;
    private CombatActor player;

    private void Awake()
    {
        controller = GetComponent<ModeSwitchImageController>();
    }

    private void Start()
    {
        GestureCombatActionHandler handler = Object.FindAnyObjectByType<GestureCombatActionHandler>();
        if (handler != null)
            player = handler.self;

        GesturePointRegistry registry = GesturePointRegistry.Instance;
        if (registry == null)
            return;

        var rules = registry.GetAllRules();
        for (int i = 0; i < rules.Count; i++)
        {
            GesturePointRegistry.PointRule rule = rules[i];
            if (rule == null || rule.point == null)
                continue;

            int pointId = rule.pointId;
            TooltipTrigger.Attach(rule.point.gameObject, () => Build(pointId));
        }
    }

    private TooltipContent Build(int pointId)
    {
        TooltipContent content = BuildBase(pointId);
        if (content == null)
            return null;

        System.Collections.Generic.List<string> bombLines = CombatTooltips.BombLines(pointId, player);
        if (bombLines.Count > 0)
            content.Gap().Lines(bombLines);

        return content;
    }

    private TooltipContent BuildBase(int pointId)
    {
        if (pointId == 9)
            return CombatTooltips.ForTransformPoint();

        if (pointId >= 1 && pointId <= 4)
        {
            UnityEngine.UI.Image image = controller != null && controller.attackDefenseImages != null
                ? controller.attackDefenseImages[pointId - 1]
                : null;

            bool isDefense = image != null && image.sprite != null && image.sprite == controller.defenseSprite;
            return isDefense ? CombatTooltips.ForDefensePoint(player) : CombatTooltips.ForAttackPoint(player);
        }

        if (pointId >= 5 && pointId <= 8)
        {
            SkillDefinition skill = FindShownSkill(pointId - 5);
            return skill != null ? CombatTooltips.ForSkill(skill) : null;
        }

        return null;
    }

    private SkillDefinition FindShownSkill(int slotIndex)
    {
        if (controller == null || controller.gestureRouter == null || controller.所有技能 == null)
            return null;

        var shown = controller.gestureRouter.ShownSkills;
        if (shown == null || slotIndex < 0 || slotIndex >= shown.Count)
            return null;

        string skillId = shown[slotIndex];
        if (string.IsNullOrEmpty(skillId))
            return null;

        for (int i = 0; i < controller.所有技能.Length; i++)
        {
            SkillDefinition skill = controller.所有技能[i];
            if (skill != null && skill.skillId == skillId)
                return skill;
        }

        return null;
    }
}
