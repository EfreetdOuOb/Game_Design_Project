using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 在 3D 場景某個位置跳出一行會往上飄、淡出的文字（強化、麻痺…之類的提示）。
/// 用 World Space Canvas 實作，大小以「世界單位的字高」指定，不受場景縮放影響。
/// </summary>
public static class WorldPopupText
{
    private const float FontSize = 72f;
    private const float RiseTime = 1.0f;

    public static void Spawn(Vector3 worldPosition, float worldHeight, string text, Color color)
    {
        if (string.IsNullOrEmpty(text) || worldHeight <= 0f)
            return;

        GameObject root = new GameObject("PopupText", typeof(RectTransform), typeof(Canvas));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 500;

        RectTransform rect = (RectTransform)root.transform;
        rect.sizeDelta = new Vector2(900f, 140f);
        float scale = worldHeight / FontSize;
        rect.localScale = Vector3.one * scale;
        rect.position = worldPosition;

        Camera cam = Camera.main;
        if (cam != null)
            rect.rotation = cam.transform.rotation;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));
        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text label = textObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = text;
        label.fontSize = Mathf.RoundToInt(FontSize);
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.color = color;
        label.raycastTarget = false;

        Outline outline = textObject.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        outline.effectDistance = new Vector2(3f, -3f);

        // 彈出 → 往上飄 → 淡出，結束後自己銷毀
        rect.localScale = Vector3.zero;
        Sequence sequence = DOTween.Sequence().SetLink(root);
        sequence.Append(rect.DOScale(Vector3.one * scale * 1.25f, 0.14f).SetEase(Ease.OutBack));
        sequence.Append(rect.DOScale(Vector3.one * scale, 0.08f));
        sequence.Join(rect.DOMove(worldPosition + Vector3.up * worldHeight * 1.8f, RiseTime).SetEase(Ease.OutQuad));
        sequence.Insert(RiseTime * 0.6f, label.DOFade(0f, RiseTime * 0.4f));
        sequence.Insert(RiseTime * 0.6f, outline.DOFade(0f, RiseTime * 0.4f));
        sequence.OnComplete(() => Object.Destroy(root));
    }
}
