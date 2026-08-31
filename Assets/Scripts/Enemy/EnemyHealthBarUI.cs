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
    private int lastIntentTurn = int.MinValue;
    private readonly List<Image> debuffIcons = new List<Image>();
    private readonly Dictionary<string, Sprite> debuffSpriteLookup = new Dictionary<string, Sprite>(StringComparer.Ordinal);
    private string lastDebuffSignature = string.Empty;

    private void Start()
    {
        enemyPoise = GetComponent<EnemyPoise>();
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

        var activeDebuffs = enemyActor.ActiveDebuffs;
        if (activeDebuffs == null || activeDebuffs.Count == 0)
        {
            if (debuffIcons.Count > 0)
            {
                for (int i = 0; i < debuffIcons.Count; i++)
                {
                    if (debuffIcons[i] != null)
                        debuffIcons[i].enabled = false;
                }
            }

            lastDebuffSignature = string.Empty;
            return;
        }

        string signature = BuildDebuffSignature(activeDebuffs);
        if (string.Equals(signature, lastDebuffSignature, StringComparison.Ordinal))
        {
            return;
        }

        lastDebuffSignature = signature;
        RefreshDebuffSlots(activeDebuffs);
    }

    private void RefreshDebuffSlots(IReadOnlyList<CombatActor.DebuffState> activeDebuffs)
    {
        int slotCount = Mathf.Min(activeDebuffs.Count, maxDebuffSlots);
        EnsureSlotCount(slotCount);

        for (int i = 0; i < debuffIcons.Count; i++)
        {
            bool shouldShow = i < slotCount;
            if (debuffIcons[i] != null)
            {
                debuffIcons[i].enabled = shouldShow;
                if (shouldShow)
                {
                    var state = activeDebuffs[i];
                    debuffIcons[i].sprite = GetDebuffSprite(state.debuffId);
                    debuffIcons[i].color = debuffIcons[i].sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                }
            }
        }
    }

    private void EnsureSlotCount(int requiredCount)
    {
        while (debuffIcons.Count < requiredCount)
        {
            GameObject slot = debuffSlotPrefab != null ? Instantiate(debuffSlotPrefab, debuffContainer) : CreateDefaultDebuffSlot();
            var icon = slot.GetComponent<Image>();
            if (icon == null)
            {
                icon = slot.AddComponent<Image>();
            }

            icon.raycastTarget = false;
            icon.enabled = false;
            debuffIcons.Add(icon);
        }

        while (debuffIcons.Count > requiredCount)
        {
            int lastIndex = debuffIcons.Count - 1;
            if (debuffIcons[lastIndex] != null && debuffIcons[lastIndex].gameObject != null)
            {
                Destroy(debuffIcons[lastIndex].gameObject);
            }

            debuffIcons.RemoveAt(lastIndex);
        }
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

    private string BuildDebuffSignature(IReadOnlyList<CombatActor.DebuffState> activeDebuffs)
    {
        if (activeDebuffs == null || activeDebuffs.Count == 0)
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder();
        for (int i = 0; i < activeDebuffs.Count; i++)
        {
            var state = activeDebuffs[i];
            if (state == null)
            {
                continue;
            }

            builder.Append(state.debuffId);
            builder.Append('|');
            builder.Append(state.stacks);
            builder.Append('|');
            builder.Append(state.remainingTurns);
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