using UnityEngine;

/// <summary>
/// 戰鬥資訊用的統一配色：傷害、護盾、回復、關鍵數字各一種顏色。
/// 法陣點位上的數字、敵人意圖旁的數字、頭上跳字、懸浮說明裡的富文字都用這一組，
/// 同一種東西在畫面上永遠是同一個顏色；要換色只改這裡。
/// </summary>
public static class CombatPalette
{
    public static readonly Color Damage = new Color(1f, 0.478f, 0.42f, 1f);     // 傷害、攻擊
    public static readonly Color Shield = new Color(0.561f, 0.82f, 1f, 1f);     // 護盾、防禦
    public static readonly Color Heal = new Color(0.486f, 0.949f, 0.541f, 1f);  // 回復
    public static readonly Color Keyword = new Color(1f, 0.851f, 0.4f, 1f);     // 關鍵數字、強化
    public static readonly Color Dim = new Color(0.659f, 0.659f, 0.702f, 1f);   // 次要說明

    public static string Hex(Color color)
    {
        return ColorUtility.ToHtmlStringRGB(color);
    }
}
