using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 在法陣的攻擊／防禦點（點 1～4）旁標出這個點的實際數值：
/// 攻擊點顯示「每次攻擊的傷害」，防禦點顯示「每次獲得的護盾」。
/// 點位圖示是攻擊還是防禦，直接看 <see cref="ModeSwitchImageController"/> 現在放的是哪個圖，所以點位被鎖住的樣子也會對得上。
/// 數值取自玩家的有效攻擊力／防禦力（含升級加成），只讀取，不影響戰鬥。
/// 由 ModeSwitchImageController 自動掛上。
/// </summary>
public class GesturePointValueLabels : MonoBehaviour
{
    [Tooltip("數字大小，單位是點位圖示高度的比例")]
    [SerializeField, Range(0.15f, 0.8f)] private float _fontRatio = 0.34f;

    private ModeSwitchImageController controller;
    private CombatActor player;
    private Text[] labels;

    private void Awake()
    {
        controller = GetComponent<ModeSwitchImageController>();
    }

    private void Start()
    {
        GestureCombatActionHandler handler = Object.FindAnyObjectByType<GestureCombatActionHandler>();
        if (handler != null)
            player = handler.self;

        BuildLabels();
    }

    private void Update()
    {
        if (labels == null || player == null || controller == null)
            return;

        for (int i = 0; i < labels.Length; i++)
        {
            Image image = controller.attackDefenseImages[i];
            if (labels[i] == null || image == null)
                continue;

            if (image.sprite != null && image.sprite == controller.attackSprite)
            {
                labels[i].text = player.EffectiveAttackPower.ToString();
                labels[i].color = CombatPalette.Damage;
            }
            else if (image.sprite != null && image.sprite == controller.defenseSprite)
            {
                labels[i].text = player.EffectiveDefensePower.ToString();
                labels[i].color = CombatPalette.Shield;
            }
            else
            {
                labels[i].text = string.Empty;
            }
        }
    }

    private void BuildLabels()
    {
        if (controller == null || controller.attackDefenseImages == null)
            return;

        Image[] images = controller.attackDefenseImages;
        labels = new Text[images.Length];
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null)
                continue;

            float height = ((RectTransform)images[i].transform).rect.height;
            if (height <= 1f)
                height = 60f;

            GameObject go = new GameObject("PointValue", typeof(RectTransform), typeof(Text), typeof(Outline));
            go.layer = images[i].gameObject.layer;
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(images[i].transform, false);

            // 點位圖示的右下角
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(height * 1.2f, height * 0.5f);
            rect.anchoredPosition = new Vector2(height * 0.05f, -height * 0.05f);

            Text text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = Mathf.RoundToInt(height * _fontRatio);
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.LowerRight;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            Outline outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);

            labels[i] = text;
        }
    }
}
