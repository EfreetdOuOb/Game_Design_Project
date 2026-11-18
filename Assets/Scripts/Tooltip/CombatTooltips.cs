using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 戰鬥資訊的懸浮說明內容：把各系統的資料（技能、敵人意圖、護盾、狀態、韌性、炸彈…）整理成玩家看得懂的文字。
/// 純讀取資料、不改任何東西；數字用的都是和實際結算同一套公式，所以說明裡看到的就是真的會發生的。
/// </summary>
public static class CombatTooltips
{
    // ---- 技能 ----

    public static TooltipContent ForSkill(SkillDefinition skill)
    {
        if (skill == null)
            return null;

        bool upgraded = SkillUpgradeManager.Instance != null && SkillUpgradeManager.Instance.IsUpgraded(skill.skillId);
        TooltipContent content = new TooltipContent(skill.displayName + (upgraded ? "（已強化）" : string.Empty));
        content.Lines(skill.DescribeLines(upgraded));

        if (!upgraded)
        {
            List<string> upgradeLines = skill.DescribeUpgradeLines();
            if (upgradeLines.Count > 0)
            {
                content.Gap();
                content.Line(TooltipText.Dim("在休息區冥想強化後："));
                for (int i = 0; i < upgradeLines.Count; i++)
                    content.Line("・" + upgradeLines[i]);
            }
        }

        return content;
    }

    // ---- 法陣點位 ----

    public static TooltipContent ForAttackPoint(CombatActor player)
    {
        TooltipContent content = new TooltipContent("攻擊點");
        if (player != null)
            content.Line("對目標造成 " + TooltipText.Damage(player.EffectiveAttackPower) + " 點傷害");

        GestureCombatActionHandler handler = Object.FindAnyObjectByType<GestureCombatActionHandler>();
        int perPoise = handler != null ? handler.AttackPointsPerPoiseDamage : 3;
        content.Line("每回合累計 " + TooltipText.Keyword(perPoise) + " 個攻擊點，削減敵人 1 點韌性");
        content.Gap().Line(TooltipText.Dim("手勢經過中間的轉換點，這些點會變成防禦點"));
        return content;
    }

    public static TooltipContent ForDefensePoint(CombatActor player)
    {
        TooltipContent content = new TooltipContent("防禦點");
        if (player != null)
            content.Line("獲得 " + TooltipText.Shield(player.EffectiveDefensePower) + " 點護盾");

        content.Line("受到傷害時，護盾先被扣掉，剩下的才扣血");
        content.Line("護盾在回合結束時消失，不會留到下一回合");
        return content;
    }

    public static TooltipContent ForTransformPoint()
    {
        return new TooltipContent("轉換點")
            .Line("手勢經過這個點，攻擊點會變成防禦點")
            .Line(TooltipText.Dim("也可以在這裡開始畫，先切換再選點"));
    }

    /// <summary>如果這個點上有炸彈，回傳炸彈說明的行；沒有就回傳空清單。</summary>
    public static List<string> BombLines(int pointId, CombatActor player)
    {
        List<string> lines = new List<string>();
        BattleBombManager bombs = BattleBombManager.Instance;
        if (bombs == null || !bombs.HasBombAt(pointId) || !bombs.TryGetRemainingTurns(pointId, out int remaining))
            return lines;

        lines.Add("<color=#FF9F4A><b>這個點上有炸彈</b></color>");
        lines.Add(TooltipText.Keyword(remaining) + " 回合後引爆，對你造成 " + TooltipText.Damage(bombs.PreviewSingleBombDamage(player)) + " 點傷害");
        lines.Add("手勢經過這個點，倒數延緩 " + TooltipText.Keyword(bombs.DelayOnTouch) + " 回合");
        return lines;
    }

    // ---- 敵人 ----

    public static TooltipContent ForIntent(EnemyCombatAI ai, EnemyActionStep step)
    {
        if (ai == null || step == null)
            return null;

        IntentInfo info = ai.DescribeIntent(step);
        TooltipContent content = new TooltipContent("敵人意圖：" + EnemyCombatAI.ActionName(step.actionType));

        EnemyPoise poise = ai.GetComponent<EnemyPoise>();
        if (poise != null && poise.IsStunned)
            content.Line("<color=#FFE066><b>暈眩中：這回合不會行動</b></color>");

        CombatActor player = ai.playerActor;
        switch (step.actionType)
        {
            case EnemyActionType.Attack:
            case EnemyActionType.HeavyAttack:
            case EnemyActionType.Combo:
                content.Line(info.hits > 1
                    ? "連續 " + TooltipText.Keyword(info.hits) + " 次，每次對你造成 " + TooltipText.Damage(info.perHit) + " 點傷害（共 " + TooltipText.Damage(info.TotalDamage) + "）"
                    : "對你造成 " + TooltipText.Damage(info.perHit) + " 點傷害");

                if (player != null)
                {
                    int absorbed = Mathf.Min(player.shield, info.TotalDamage);
                    content.Line("你目前的護盾 " + TooltipText.Shield(player.shield) + "，這一招預計實際扣血 " + TooltipText.Damage(info.TotalDamage - absorbed));
                }

                content.Gap().Line(TooltipText.Dim("這一招的每一下若被護盾完全吃掉，會削減敵人 1 點韌性"));
                break;

            case EnemyActionType.Defense:
                content.Line("敵人獲得 " + TooltipText.Shield(info.amount) + " 點護盾");
                content.Line("你的攻擊會先打掉護盾，剩下的才扣血；護盾在回合結束時消失");
                break;

            case EnemyActionType.Heal:
                content.Line("敵人回復 " + TooltipText.Heal(info.amount) + " 點生命");
                break;

            case EnemyActionType.Empower:
                content.Line("敵人的攻擊力永久 +" + TooltipText.Keyword(info.amount));
                content.Line("目前累計強化 +" + TooltipText.Keyword(ai.enemyActor != null ? ai.enemyActor.AttackBuff : 0));
                break;
        }

        AppendUpcoming(content, ai, skipCurrent: true);
        return content;
    }

    public static TooltipContent ForEnemyOverview(EnemyCombatAI ai)
    {
        if (ai == null || ai.enemyActor == null)
            return null;

        CombatActor enemy = ai.enemyActor;
        TooltipContent content = new TooltipContent("敵人");
        content.Line("生命 " + TooltipText.Heal(enemy.currentHp + " / " + enemy.maxHp));

        string attackText = TooltipText.Damage(enemy.EffectiveAttackPower);
        if (enemy.AttackBuff > 0)
            attackText += TooltipText.Dim("（含強化 +" + enemy.AttackBuff + "）");
        content.Line("攻擊力 " + attackText + "　每次防禦獲得 " + TooltipText.Shield(enemy.EffectiveDefensePower) + " 護盾");

        if (enemy.shield > 0)
            content.Line("目前護盾 " + TooltipText.Shield(enemy.shield));

        AppendUpcoming(content, ai, skipCurrent: false);
        return content;
    }

    private static void AppendUpcoming(TooltipContent content, EnemyCombatAI ai, bool skipCurrent)
    {
        List<EnemyActionStep> upcoming = ai.GetUpcomingActions(4);
        if (upcoming.Count == 0)
            return;

        System.Text.StringBuilder text = new System.Text.StringBuilder();
        int start = skipCurrent ? 1 : 0;
        for (int i = start; i < upcoming.Count; i++)
        {
            if (text.Length > 0)
                text.Append(" → ");
            text.Append(EnemyCombatAI.ActionName(upcoming[i].actionType));
        }

        if (text.Length == 0)
            return;

        content.Gap().Line((skipCurrent ? "之後：" : "接下來：") + TooltipText.Keyword(text.ToString()));
    }

    public static TooltipContent ForPoise(EnemyPoise poise)
    {
        if (poise == null)
            return null;

        GestureCombatActionHandler handler = Object.FindAnyObjectByType<GestureCombatActionHandler>();
        int perPoise = handler != null ? handler.AttackPointsPerPoiseDamage : 3;

        TooltipContent content = new TooltipContent("韌性");
        content.Line("目前 " + TooltipText.Keyword(poise.CurrentPoise + " / " + poise.MaxPoise));
        content.Line("歸零時敵人暈眩 " + TooltipText.Keyword(poise.StunTurnsOnBreak) + " 回合：跳過行動（該回合原本的招式也算用掉）");
        content.Gap();
        content.Line("削減方式：");
        content.Line("・你每回合累計 " + TooltipText.Keyword(perPoise) + " 個攻擊點，削 1 點");
        content.Line("・敵人的攻擊（連擊的每一下）被護盾完全吃掉，削 1 點");
        return content;
    }

    // ---- 護盾、狀態、生命 ----

    public static TooltipContent ForShield(CombatActor actor)
    {
        if (actor == null)
            return null;

        return new TooltipContent("護盾 " + actor.shield)
            .Line("受到傷害時，護盾先被扣掉，剩下的才扣血")
            .Line("每一點被吸收的傷害，護盾就少一點")
            .Line(TooltipText.Dim("回合結束時護盾消失，不會留到下一回合"));
    }

    public static TooltipContent ForDebuff(CombatActor actor, string debuffId)
    {
        if (actor == null || string.IsNullOrEmpty(debuffId))
            return null;

        CombatActor.DebuffState state = null;
        IReadOnlyList<CombatActor.DebuffState> debuffs = actor.ActiveDebuffs;
        for (int i = 0; i < debuffs.Count; i++)
        {
            if (debuffs[i] != null && debuffs[i].debuffId == debuffId)
            {
                state = debuffs[i];
                break;
            }
        }

        if (state == null)
            return null;

        TooltipContent content;
        if (debuffId == CombatActor.VulnerableDebuffId)
        {
            int percent = Mathf.RoundToInt(CombatActor.VulnerableBonusPerStack * 100f * state.stacks);
            content = new TooltipContent("易傷 ×" + state.stacks)
                .Line("受到的傷害 +" + TooltipText.Damage(percent + "%"));
        }
        else if (debuffId == CombatActor.WeakDebuffId)
        {
            float multiplier = Mathf.Clamp(1f - actor.WeakReductionPerStack * state.stacks, actor.WeakMinMultiplier, 1f);
            int percent = Mathf.RoundToInt((1f - multiplier) * 100f);
            content = new TooltipContent("虛弱 ×" + state.stacks)
                .Line("造成的傷害 -" + TooltipText.Shield(percent + "%"));
        }
        else
        {
            content = new TooltipContent(debuffId + " ×" + state.stacks);
        }

        content.Line("剩餘 " + TooltipText.Keyword(state.remainingTurns) + " 回合");
        content.Line(TooltipText.Dim("同一種狀態再次施加，層數會疊加、持續回合取較長的"));
        return content;
    }

    public static TooltipContent ForPlayerHp(CombatActor player, IncomingDamageIndicator incoming)
    {
        if (player == null)
            return null;

        TooltipContent content = new TooltipContent("生命");
        content.Line(TooltipText.Heal(player.currentHp + " / " + player.maxHp));

        if (incoming != null && (incoming.IncomingFromEnemies > 0 || incoming.IncomingFromBombs > 0))
        {
            content.Gap().Line("<b>本回合預計受到</b>");
            if (incoming.IncomingFromEnemies > 0)
                content.Line("・敵人出手 " + TooltipText.Damage(incoming.IncomingFromEnemies));
            if (incoming.IncomingFromBombs > 0)
                content.Line("・炸彈引爆 " + TooltipText.Damage(incoming.IncomingFromBombs));
            if (incoming.ShieldAbsorbed > 0)
                content.Line("・護盾吸收 -" + TooltipText.Shield(incoming.ShieldAbsorbed) + TooltipText.Dim("（你現在的護盾 " + player.shield + "）"));
            content.Line("→ 實際扣血 " + TooltipText.Damage(incoming.PredictedDamage));
        }
        else
        {
            content.Gap().Line(TooltipText.Dim("本回合沒有預計受到的傷害"));
        }

        content.Gap();
        content.Line("每個攻擊點 " + TooltipText.Damage(player.EffectiveAttackPower) + " 傷害　每個防禦點 " + TooltipText.Shield(player.EffectiveDefensePower) + " 護盾");
        return content;
    }

    // ---- 遺物與冥想升級 ----

    public static TooltipContent ForRelic(RelicDefinition relic)
    {
        if (relic == null)
            return null;

        TooltipContent content = new TooltipContent(relic.displayName);
        content.Line(relic.effectDescription);
        if (!string.IsNullOrEmpty(relic.flavorText))
            content.Gap().Line(TooltipText.Dim(relic.flavorText));
        return content;
    }

    public static TooltipContent ForUpgrade(SkillUpgradeDefinition definition)
    {
        if (definition == null)
            return null;

        // 攻擊、防禦強化：不是技能資產，直接說明每個點的數值變化
        if (definition.skillId == SkillUpgradeManager.AttackUpgradeId || definition.skillId == SkillUpgradeManager.DefenseUpgradeId)
        {
            PlayerUpgradeStatModifier modifier = Object.FindAnyObjectByType<PlayerUpgradeStatModifier>();
            GestureCombatActionHandler handler = Object.FindAnyObjectByType<GestureCombatActionHandler>();
            CombatActor player = handler != null ? handler.self : null;

            TooltipContent stat = new TooltipContent(definition.displayName);
            stat.Line(definition.upgradeDescription);
            if (modifier != null && player != null)
            {
                bool isAttack = definition.skillId == SkillUpgradeManager.AttackUpgradeId;
                int now = isAttack ? player.EffectiveAttackPower : player.EffectiveDefensePower;
                int bonus = isAttack ? modifier.AttackUpgradeBonus : modifier.DefenseUpgradeBonus;
                bool upgraded = SkillUpgradeManager.Instance != null && SkillUpgradeManager.Instance.IsUpgraded(definition.skillId);
                string unit = isAttack ? "每個攻擊點傷害" : "每個防禦點護盾";
                stat.Gap().Line(upgraded
                    ? unit + "：" + TooltipText.Keyword(now) + TooltipText.Dim("（已包含強化）")
                    : unit + "：" + TooltipText.Keyword(now) + " → " + TooltipText.Keyword(now + bonus));
            }

            return stat;
        }

        // 一般技能：直接用技能的完整說明（含「強化後會多出什麼」）
        SkillLibrary library = Object.FindAnyObjectByType<SkillLibrary>();
        if (library != null && library.TryGet(definition.skillId, out SkillDefinition skill))
        {
            TooltipContent content = ForSkill(skill);
            if (content != null)
                return content;
        }

        return new TooltipContent(definition.displayName).Line(definition.upgradeDescription);
    }
}
