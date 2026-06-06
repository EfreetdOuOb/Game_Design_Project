using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CombatUI : MonoBehaviour
{
    public static CombatUI Instance { get; private set; }

    [Header("角色來源")]
    public CombatActor playerActor;
    public CombatActor enemyActor;

    [Header("技能來源")]
    public GestureActionRouter actionRouter;

    [Header("玩家 UI")]
    public Text playerHpText;
    // 可以留著攻擊、防禦欄位，也可以不顯示
    public Text playerAttackText;
    public Text playerDefenseText;
    public Text playerTempDefenseText;

    [Header("敵人 UI")]
    public Text enemyHpText;
    public Text enemyAttackText;
    public Text enemyDefenseText;
    public Text enemyTempDefenseText;

    [Header("技能 UI")]
    public Text skillSlot_Left;
    public Text skillSlot_Right;
    public Text skillSlot_Down;
    public Text skillSlot_Up;

    [Header("戰鬥記錄")]
    public Text battleLogText;
    public int maxBattleLogLines = 6;
    private readonly List<string> battleLog = new List<string>();

    [Header("回合 UI")]
    public Text turnText;
    public Text phaseText;

    public float refreshInterval = 0.1f;
    private float timer;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        RefreshUI();
        timer = refreshInterval;
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = refreshInterval;
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        RefreshActorUI(playerActor,
            playerHpText, playerAttackText, playerDefenseText, playerTempDefenseText);

        RefreshActorUI(enemyActor,
            enemyHpText, enemyAttackText, enemyDefenseText, enemyTempDefenseText);

        RefreshSkillUI();
        RefreshTurnUI();
        RefreshBattleLog();
    }

    private void RefreshActorUI(
        CombatActor actor,
        Text hpText,
        Text attackText,
        Text defenseText,
        Text tempDefenseText)
    {
        if (actor == null) return;

        if (hpText != null)
            hpText.text = $"血量：{actor.currentHp}/{actor.maxHp}";
        if (attackText != null)
            attackText.text = $"攻擊：{actor.attackPower}";
        if (defenseText != null)
            defenseText.text = $"防禦：{actor.defensePower}";
        if (tempDefenseText != null)
            tempDefenseText.text = $"暫時防禦：{actor.temporaryDefenseBonus}";
    }

    private void RefreshSkillUI()
    {
        if (actionRouter == null)
        {
            ClearSkillSlots();
            return;
        }

        var skills = actionRouter.ShownSkills;
        if (skills == null || skills.Count == 0)
        {
            ClearSkillSlots();
            return;
        }

        // 分配技能到四个位置
        if (skillSlot_Left != null)
            skillSlot_Left.text = skills.Count > 0 ? skills[0] : "";
        if (skillSlot_Right != null)
            skillSlot_Right.text = skills.Count > 1 ? skills[1] : "";
        if (skillSlot_Down != null)
            skillSlot_Down.text = skills.Count > 2 ? skills[2] : "";
        if (skillSlot_Up != null)
            skillSlot_Up.text = skills.Count > 3 ? skills[3] : "";
    }

    private void ClearSkillSlots()
    {
        if (skillSlot_Left != null) skillSlot_Left.text = "";
        if (skillSlot_Right != null) skillSlot_Right.text = "";
        if (skillSlot_Down != null) skillSlot_Down.text = "";
        if (skillSlot_Up != null) skillSlot_Up.text = "";
    }

    private void RefreshTurnUI()
    {
        if (turnText != null)
            turnText.text = TurnManager.Instance != null
                ? $"回合: {TurnManager.Instance.TurnNumber}"
                : "回合: -";

        if (phaseText != null)
            phaseText.text = TurnManager.Instance != null
                ? TurnManager.Instance.CurrentPhase.ToString()
                : "階段: -";
    }

    private void RefreshBattleLog()
    {
        if (battleLogText == null) return;
        battleLogText.text = string.Join("\n", battleLog);
    }

    public void AppendBattleLog(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        battleLog.Add(line);
        if (battleLog.Count > maxBattleLogLines)
            battleLog.RemoveAt(0);

        RefreshBattleLog();
    }
}