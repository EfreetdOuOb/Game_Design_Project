using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一個懸浮說明的內容：標題 + 若干行說明（可用 &lt;color&gt;、&lt;b&gt; 等富文字標籤）。
/// 只是純資料，跟畫面怎麼畫無關；由 <see cref="TooltipSystem"/> 負責顯示。
/// </summary>
public class TooltipContent
{
    public string title;
    public Color titleColor = new Color(1f, 0.88f, 0.5f, 1f);
    public readonly List<string> lines = new List<string>();

    public TooltipContent(string title)
    {
        this.title = title;
    }

    public TooltipContent Line(string text)
    {
        if (!string.IsNullOrEmpty(text))
            lines.Add(text);
        return this;
    }

    /// <summary>空一行，把說明分成不同段落</summary>
    public TooltipContent Gap()
    {
        if (lines.Count > 0 && lines[lines.Count - 1] != string.Empty)
            lines.Add(string.Empty);
        return this;
    }

    public TooltipContent Lines(IEnumerable<string> texts)
    {
        if (texts == null)
            return this;

        foreach (string text in texts)
            Line(text);
        return this;
    }

    public string Body => string.Join("\n", lines);

    /// <summary>內容是否有變（給 TooltipSystem 判斷要不要重排版）</summary>
    public bool SameAs(TooltipContent other)
    {
        return other != null && title == other.title && Body == other.Body;
    }
}

/// <summary>懸浮說明的富文字配色：數字、關鍵字用固定顏色，全遊戲看起來一致。</summary>
public static class TooltipText
{
    public static string Damage(object value) => Colored(value, "FF7A6B");
    public static string Shield(object value) => Colored(value, "8FD1FF");
    public static string Heal(object value) => Colored(value, "7CF28A");
    public static string Keyword(object value) => Colored(value, "FFD966");
    public static string Dim(object value) => Colored(value, "A8A8B3");

    private static string Colored(object value, string hex)
    {
        return "<color=#" + hex + "><b>" + value + "</b></color>";
    }
}
