using UnityEngine;

/// <summary>
/// 地圖特效用的簡單圖形（光圈、完成印章），用程式畫好後快取，不需要美術資源。
/// 之後想換成自己的圖，直接在 <see cref="MapNodeEffects"/> 的 Sprite 欄位指定就會取代。
/// </summary>
public static class MapUiSprites
{
    private const int Size = 128;

    private static Sprite ring;
    private static Sprite stamp;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        ring = null;
        stamp = null;
    }

    /// <summary>空心圓環（可點節點的光圈）</summary>
    public static Sprite Ring => ring != null ? ring : ring = Build("MapRingRuntime", (x, y) =>
    {
        float d = Distance(x, y);
        return Mathf.Clamp01(Smooth(d, 0.5f)) * Mathf.Clamp01(1f - Smooth(d, 0.4f));
    });

    /// <summary>完成印章：外圈 + 中間的勾勾（白色，顏色與透明度由 Image 決定）</summary>
    public static Sprite Stamp => stamp != null ? stamp : stamp = Build("MapStampRuntime", StampAlpha);

    private static float StampAlpha(float x, float y)
    {
        // 3 x 3 取樣，讓勾勾的斜線邊緣平滑
        float total = 0f;
        const float step = 1f / (3f * Size);
        for (int sy = -1; sy <= 1; sy++)
        {
            for (int sx = -1; sx <= 1; sx++)
            {
                Vector2 p = new Vector2(x + sx * step, y + sy * step);
                total += InsideStamp(p) ? 1f : 0f;
            }
        }

        return total / 9f;
    }

    private static bool InsideStamp(Vector2 p)
    {
        float d = Vector2.Distance(p, new Vector2(0.5f, 0.5f));
        if (d <= 0.48f && d >= 0.42f)
            return true;   // 外圈

        // 勾勾：兩段粗線，左短右長
        Vector2 a = new Vector2(0.27f, 0.50f);
        Vector2 b = new Vector2(0.44f, 0.33f);
        Vector2 c = new Vector2(0.74f, 0.67f);
        return Mathf.Min(DistanceToSegment(p, a, b), DistanceToSegment(p, b, c)) <= 0.05f;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    // 回傳 0 ~ 1 的不透明度；x、y 是 0 ~ 1 的座標
    private static Sprite Build(string name, System.Func<float, float, float> alphaAt)
    {
        Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float alpha = Mathf.Clamp01(alphaAt((x + 0.5f) / Size, (y + 0.5f) / Size));
                pixels[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
    }

    private static float Distance(float x, float y)
    {
        return Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.5f));
    }

    // 邊緣柔化：距離小於 edge 時為 1，超過後在約 1 像素內淡出（抗鋸齒）
    private static float Smooth(float distance, float edge)
    {
        float feather = 1f / Size;
        return Mathf.Clamp01((edge - distance) / feather + 0.5f);
    }

}
