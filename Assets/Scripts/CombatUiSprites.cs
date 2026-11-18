using UnityEngine;

/// <summary>
/// 戰鬥資訊 UI 用的簡單圖形（護盾），用程式畫好後快取，不需要美術資源。
/// 圖形本身是灰階，實際顏色由 Image.color 決定（邊框會比內部暗一階，所以上色後有層次）。
/// </summary>
public static class CombatUiSprites
{
    private const int Size = 128;

    private static Sprite shield;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        shield = null;
    }

    /// <summary>盾牌（上寬下尖），邊框較暗、內部較亮</summary>
    public static Sprite Shield => shield != null ? shield : shield = BuildShield();

    private static Sprite BuildShield()
    {
        Vector2[] outer =
        {
            new Vector2(0.14f, 0.90f), new Vector2(0.50f, 0.97f), new Vector2(0.86f, 0.90f),
            new Vector2(0.86f, 0.52f), new Vector2(0.50f, 0.05f), new Vector2(0.14f, 0.52f)
        };

        // 內部 = 外框往中心縮小，兩者之間就是邊框
        Vector2[] inner = new Vector2[outer.Length];
        Vector2 center = new Vector2(0.5f, 0.55f);
        for (int i = 0; i < outer.Length; i++)
            inner[i] = center + (outer[i] - center) * 0.78f;

        Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = "ShieldRuntime",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[Size * Size];
        const float step = 1f / (3f * Size);
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float cx = (x + 0.5f) / Size;
                float cy = (y + 0.5f) / Size;

                // 3 x 3 取樣讓斜邊平滑
                float outerCover = 0f;
                float innerCover = 0f;
                for (int sy = -1; sy <= 1; sy++)
                {
                    for (int sx = -1; sx <= 1; sx++)
                    {
                        Vector2 p = new Vector2(cx + sx * step, cy + sy * step);
                        if (Inside(p, outer))
                            outerCover += 1f / 9f;
                        if (Inside(p, inner))
                            innerCover += 1f / 9f;
                    }
                }

                // 內部亮（255）、邊框暗（150）
                byte gray = (byte)Mathf.RoundToInt(Mathf.Lerp(150f, 255f, innerCover / Mathf.Max(0.0001f, outerCover)));
                pixels[y * Size + x] = new Color32(gray, gray, gray, (byte)Mathf.RoundToInt(outerCover * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
    }

    private static bool Inside(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            if (((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x))
                inside = !inside;
        }

        return inside;
    }
}
