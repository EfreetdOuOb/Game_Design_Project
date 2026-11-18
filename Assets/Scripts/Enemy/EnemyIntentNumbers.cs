using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 在敵人的意圖圖示旁（預設左邊，可改）常駐顯示這個招式的數字：攻擊「12」、連擊「8×3」、防禦「+10」、回血「+15」、強化「攻+10」。
/// 數字由 <see cref="EnemyCombatAI.DescribeIntent"/> 算出，和實際結算同一套公式，
/// 所以敵人被強化、玩家有易傷時，數字會即時跟著變。只讀取資料，不影響戰鬥。
/// 由 <see cref="EnemyHealthBarUI"/> 自動掛上；每個招式的 intentIcon 旁會自動建一個文字。
/// </summary>
public class EnemyIntentNumbers : MonoBehaviour
{
    // World Space 的小 Canvas 上文字會糊，所以字用 4 倍大再縮回 1/4
    private const float TextOversample = 4f;

    public enum LabelSide
    {
        Left,
        Right,
        Above,
        Below
    }

    [Header("位置")]
    [Tooltip("數字放在意圖圖示的哪一側。畫面右邊有法陣（螢幕 UI 會蓋在 3D 世界 UI 上面）時，放左邊最不會被遮到。")]
    [SerializeField] private LabelSide _side = LabelSide.Left;

    [Header("顏色")]
    [SerializeField] private Color _attackColor = new Color(1f, 0.85f, 0.4f, 1f);
    [SerializeField] private Color _defenseColor = new Color(0.55f, 0.8f, 1f, 1f);
    [SerializeField] private Color _healColor = new Color(0.5f, 1f, 0.55f, 1f);
    [SerializeField] private Color _empowerColor = new Color(1f, 0.6f, 0.3f, 1f);

    private EnemyCombatAI ai;
    private readonly Dictionary<EnemyActionStep, Text> labels = new Dictionary<EnemyActionStep, Text>();

    private void Awake()
    {
        ai = GetComponent<EnemyCombatAI>();
        if (ai == null)
            ai = GetComponentInParent<EnemyCombatAI>();
    }

    private void LateUpdate()
    {
        if (ai == null || ai.actionSequence == null)
            return;

        for (int i = 0; i < ai.actionSequence.Count; i++)
        {
            EnemyActionStep step = ai.actionSequence[i];
            if (step == null || step.intentIcon == null)
                continue;

            // 只更新目前顯示中的那個意圖圖示
            if (!step.intentIcon.activeInHierarchy)
                continue;

            if (!labels.TryGetValue(step, out Text label) || label == null)
            {
                label = BuildLabel(step.intentIcon);
                labels[step] = label;
            }

            IntentInfo info = ai.DescribeIntent(step);
            if (label.text != info.label)
                label.text = info.label;

            label.color = ColorFor(info.type);
        }
    }

    private Color ColorFor(EnemyActionType type)
    {
        switch (type)
        {
            case EnemyActionType.Defense: return _defenseColor;
            case EnemyActionType.Heal: return _healColor;
            case EnemyActionType.Empower: return _empowerColor;
            default: return _attackColor;
        }
    }

    private Text BuildLabel(GameObject icon)
    {
        RectTransform iconRect = icon.transform as RectTransform;
        Vector2 iconSize = iconRect != null ? iconRect.rect.size : new Vector2(30f, 30f);
        if (iconSize.x <= 0f || iconSize.y <= 0f)
            iconSize = new Vector2(30f, 30f);

        GameObject go = new GameObject("IntentNumber", typeof(RectTransform), typeof(Text), typeof(Outline));
        go.layer = icon.layer;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(icon.transform, false);

        // 依設定放在圖示的某一側；文字往「遠離圖示」的方向長，數字變長也不會蓋到圖示
        TextAnchor alignment;
        switch (_side)
        {
            case LabelSide.Right:
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(iconSize.x * 0.1f, 0f);
                alignment = TextAnchor.MiddleLeft;
                break;
            case LabelSide.Above:
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, iconSize.y * 0.02f);
                alignment = TextAnchor.LowerCenter;
                break;
            case LabelSide.Below:
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -iconSize.y * 0.02f);
                alignment = TextAnchor.UpperCenter;
                break;
            default:
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(1f, 0.5f);
                rect.anchoredPosition = new Vector2(-iconSize.x * 0.1f, 0f);
                alignment = TextAnchor.MiddleRight;
                break;
        }

        rect.sizeDelta = new Vector2(iconSize.x * 3f * TextOversample, iconSize.y * 0.9f * TextOversample);
        rect.localScale = Vector3.one / TextOversample;

        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = Mathf.RoundToInt(iconSize.y * 0.6f * TextOversample);
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);

        return text;
    }
}
