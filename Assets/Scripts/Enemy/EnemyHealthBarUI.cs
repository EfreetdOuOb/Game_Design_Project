using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBarUI : MonoBehaviour
{
    [Serializable]
    public class DebuffIconEntry
    {
        public string debuffId;
        public Sprite sprite;
    }

    public CombatActor enemyActor;
    public Slider hpSlider;
    public Text hpText;

    public Slider poiseSlider;
    public Text poiseText;

    public GameObject attackIntent;
    public GameObject protectIntent;

    [Header("Debuff display")]
    public Transform debuffContainer;
    public GameObject debuffSlotPrefab;
    public List<DebuffIconEntry> debuffSprites = new List<DebuffIconEntry>();
    public int maxDebuffSlots = 6;

    public float smoothSpeed = 8f;

    private EnemyPoise enemyPoise;
    private EnemyCombatAI enemyCombatAI;
    private int lastIntentTurn = int.MinValue;
    private struct DebuffDisplay
    {
        public string id;
        public int stacks;
        public int turns;
        public string badge;
    }

    /// <summary>暈眩（麻痺／韌性歸零）在狀態欄上用的 id；想換圖就在 debuffSprites 加一筆這個 id。</summary>
    public const string StunDebuffId = "Stun";

    /// <summary>敵人攻擊力強化（強化招式）在狀態欄上用的 id；沒有回合限制，右下角顯示累積的攻擊增加量。</summary>
    public const string AttackUpDebuffId = "AttackUp";

    private readonly Dictionary<string, DebuffSlotView> debuffSlots = new Dictionary<string, DebuffSlotView>(StringComparer.Ordinal);
    private readonly List<DebuffDisplay> debuffDisplayBuffer = new List<DebuffDisplay>();
    private readonly HashSet<string> usedDebuffIds = new HashSet<string>(StringComparer.Ordinal);
    private readonly Dictionary<string, Sprite> debuffSpriteLookup = new Dictionary<string, Sprite>(StringComparer.Ordinal);
    private string lastDebuffSignature = string.Empty;

    private void Start()
    {
        enemyPoise = GetComponent<EnemyPoise>();
        enemyCombatAI = GetComponent<EnemyCombatAI>();
        if (enemyCombatAI == null)
            enemyCombatAI = GetComponentInParent<EnemyCombatAI>();

        RebuildDebuffSpriteLookup();
        RefreshImmediate();
    }

    private void Update()
    {
        if (enemyActor == null)
        {
            UpdateIntentDisplay();
            return;
        }

        UpdateHealthBar();
        UpdatePoiseBar();
        UpdateIntentDisplay();
        UpdateDebuffDisplay();
    }

    public void RefreshImmediate()
    {
        if (enemyActor != null && hpSlider != null)
        {
            hpSlider.maxValue = enemyActor.maxHp;
            hpSlider.value = enemyActor.currentHp;
        }

        if (enemyActor != null && hpText != null)
        {
            hpText.text = enemyActor.currentHp + "/" + enemyActor.maxHp;
        }

        if (enemyPoise != null)
        {
            if (poiseSlider != null)
            {
                poiseSlider.maxValue = enemyPoise.MaxPoise;
                poiseSlider.value = enemyPoise.CurrentPoise;
            }

            if (poiseText != null)
            {
                poiseText.text = enemyPoise.CurrentPoise + "/" + enemyPoise.MaxPoise;
            }
        }

        UpdateIntentDisplay();
        UpdateDebuffDisplay();
    }

    private void UpdateHealthBar()
    {
        if (hpSlider == null)
        {
            return;
        }

        hpSlider.maxValue = enemyActor.maxHp;
        float targetValue = enemyActor.currentHp;
        float nextValue = Mathf.MoveTowards(hpSlider.value, targetValue, smoothSpeed * Time.deltaTime);

        if (!Mathf.Approximately(hpSlider.value, nextValue))
        {
            hpSlider.value = nextValue;
        }

        if (hpText != null)
        {
            string nextText = enemyActor.currentHp + "/" + enemyActor.maxHp;
            if (hpText.text != nextText)
            {
                hpText.text = nextText;
            }
        }
    }

    private void UpdatePoiseBar()
    {
        if (enemyPoise == null)
        {
            return;
        }

        if (poiseSlider != null)
        {
            poiseSlider.maxValue = enemyPoise.MaxPoise;
            float nextValue = Mathf.MoveTowards(poiseSlider.value, enemyPoise.CurrentPoise, smoothSpeed * Time.deltaTime);

            if (!Mathf.Approximately(poiseSlider.value, nextValue))
            {
                poiseSlider.value = nextValue;
            }
        }

        if (poiseText != null)
        {
            string nextText = enemyPoise.CurrentPoise + "/" + enemyPoise.MaxPoise;
            if (poiseText.text != nextText)
            {
                poiseText.text = nextText;
            }
        }
    }

    private void UpdateIntentDisplay()
    {
        if (TurnManager.Instance == null)
        {
            return;
        }

        int turn = TurnManager.Instance.TurnNumber;
        if (turn == lastIntentTurn)
        {
            return;
        }

        lastIntentTurn = turn;
        if (enemyCombatAI != null)
        {
            EnemyActionStep currentAction = enemyCombatAI.CurrentAction;

            if (attackIntent != null)
                attackIntent.SetActive(false);

            if (protectIntent != null)
                protectIntent.SetActive(false);

            if (currentAction != null && currentAction.intentIcon != null)
            {
                currentAction.intentIcon.SetActive(true);
            }
            else if (currentAction != null)
            {
                bool isDefenseAction = currentAction.actionType == EnemyActionType.Defense;
                if (attackIntent != null)
                    attackIntent.SetActive(!isDefenseAction);

                if (protectIntent != null)
                    protectIntent.SetActive(isDefenseAction);
            }

            if (enemyCombatAI.actionSequence != null)
            {
                for (int i = 0; i < enemyCombatAI.actionSequence.Count; i++)
                {
                    EnemyActionStep action = enemyCombatAI.actionSequence[i];
                    if (action != null && action.intentIcon != null && action != currentAction)
                        action.intentIcon.SetActive(false);
                }
            }
            return;
        }

        bool isAttackTurn = turn % 2 == 1;
        if (attackIntent != null)
            attackIntent.SetActive(isAttackTurn);

        if (protectIntent != null)
            protectIntent.SetActive(!isAttackTurn);
    }

    private void UpdateDebuffDisplay()
    {
        if (enemyActor == null)
        {
            return;
        }

        BuildDebuffDisplayList();
        if (debuffDisplayBuffer.Count == 0)
        {
            HideAllDebuffSlots();
            lastDebuffSignature = string.Empty;
            return;
        }

        string signature = BuildDebuffSignature();
        if (string.Equals(signature, lastDebuffSignature, StringComparison.Ordinal))
        {
            return;
        }

        lastDebuffSignature = signature;
        RefreshDebuffSlots();
    }

    // 狀態欄要顯示的東西 = CombatActor 的 debuff + 暈眩（麻痺／韌性歸零，暈眩狀態存在 EnemyPoise）
    private void BuildDebuffDisplayList()
    {
        debuffDisplayBuffer.Clear();

        var activeDebuffs = enemyActor.ActiveDebuffs;
        if (activeDebuffs != null)
        {
            for (int i = 0; i < activeDebuffs.Count; i++)
            {
                CombatActor.DebuffState state = activeDebuffs[i];
                if (state == null)
                    continue;

                debuffDisplayBuffer.Add(new DebuffDisplay { id = state.debuffId, stacks = state.stacks, turns = state.remainingTurns });
            }
        }

        if (enemyPoise != null && enemyPoise.StunTurns > 0)
            debuffDisplayBuffer.Add(new DebuffDisplay { id = StunDebuffId, stacks = 1, turns = enemyPoise.StunTurns });

        if (enemyActor.AttackBuff > 0)
            debuffDisplayBuffer.Add(new DebuffDisplay { id = AttackUpDebuffId, stacks = 1, turns = 0, badge = "+" + enemyActor.AttackBuff });
    }

    // 每種狀態固定用同一個格子（用 id 對應），這樣其他狀態到期、格子順序變動時，不會讓留著的圖示重播進場動畫
    private void RefreshDebuffSlots()
    {
        usedDebuffIds.Clear();
        int shown = Mathf.Min(debuffDisplayBuffer.Count, maxDebuffSlots);

        for (int i = 0; i < shown; i++)
        {
            DebuffDisplay display = debuffDisplayBuffer[i];
            // 沒有設定圖示的狀態（例如暈眩、強化）不佔狀態列，只靠特效表現；之後在 debuffSprites 加圖就會自動出現
            Sprite sprite = GetDebuffSprite(display.id);
            if (sprite == null)
                continue;

            DebuffSlotView slot = GetOrCreateSlot(display.id);
            if (slot == null)
                continue;

            slot.transform.SetSiblingIndex(i);
            slot.Bind(display.id, sprite, display.stacks, display.turns, display.badge);
            usedDebuffIds.Add(display.id);
        }

        foreach (KeyValuePair<string, DebuffSlotView> pair in debuffSlots)
        {
            if (pair.Value != null && !usedDebuffIds.Contains(pair.Key))
                pair.Value.Hide();
        }
    }

    private void HideAllDebuffSlots()
    {
        foreach (DebuffSlotView slot in debuffSlots.Values)
        {
            if (slot != null)
                slot.Hide();
        }
    }

    private DebuffSlotView GetOrCreateSlot(string debuffId)
    {
        if (string.IsNullOrEmpty(debuffId))
            return null;

        if (debuffSlots.TryGetValue(debuffId, out DebuffSlotView existing) && existing != null)
            return existing;

        GameObject slotObject = debuffSlotPrefab != null ? Instantiate(debuffSlotPrefab, debuffContainer) : CreateDefaultDebuffSlot();
        slotObject.name = "Debuff_" + debuffId;

        DebuffSlotView slot = slotObject.GetComponent<DebuffSlotView>();
        if (slot == null)
            slot = slotObject.AddComponent<DebuffSlotView>();

        slotObject.SetActive(false);
        debuffSlots[debuffId] = slot;
        return slot;
    }

    private GameObject CreateDefaultDebuffSlot()
    {
        GameObject slot = new GameObject("DebuffSlot", typeof(RectTransform), typeof(Image));
        if (debuffContainer != null)
        {
            slot.transform.SetParent(debuffContainer, false);
        }

        var rect = slot.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(28f, 28f);
        return slot;
    }

    private string BuildDebuffSignature()
    {
        var builder = new System.Text.StringBuilder();
        for (int i = 0; i < debuffDisplayBuffer.Count; i++)
        {
            DebuffDisplay display = debuffDisplayBuffer[i];
            builder.Append(display.id);
            builder.Append('|');
            builder.Append(display.stacks);
            builder.Append('|');
            builder.Append(display.turns);
            builder.Append('|');
            builder.Append(display.badge);
            builder.Append(';');
        }

        return builder.ToString();
    }

    private Sprite GetDebuffSprite(string debuffId)
    {
        if (string.IsNullOrEmpty(debuffId))
        {
            return null;
        }

        debuffSpriteLookup.TryGetValue(debuffId, out Sprite sprite);
        return sprite;
    }

    private void RebuildDebuffSpriteLookup()
    {
        debuffSpriteLookup.Clear();
        for (int i = 0; i < debuffSprites.Count; i++)
        {
            var entry = debuffSprites[i];
            if (entry == null || string.IsNullOrEmpty(entry.debuffId))
            {
                continue;
            }

            debuffSpriteLookup[entry.debuffId] = entry.sprite;
        }
    }
}