/// <summary>
/// 元数据展示的纯文本工具方法，供 UGUI (EditorMetadataView) 和 UI Toolkit (LevelPreviewView) 共用。
/// 输出的富文本标签兼容 TextMeshPro 和 UI Toolkit 的 Label。
/// </summary>
public static class MetadataDisplayHelper
{
    /// <summary>
    /// 构建星级评分文本。rating=0 时显示未评分；0.5-5 时显示对应星星。
    /// 返回纯 Unicode 星号字符串（不含 rich text 标签），便于跨 UI 使用。
    /// </summary>
    public static string BuildStarText(float rating)
    {
        if (rating <= 0f)
            return "\u2606\u2606\u2606\u2606\u2606 未评分";

        var sb = new System.Text.StringBuilder();
        for (int i = 1; i <= 5; i++)
        {
            if (rating >= i)
                sb.Append('\u2605'); // ★
            else if (rating >= i - 0.5f)
                sb.Append('\u2605'); // ★（与 BuildStarRichText 一致；U+2BEA 半星在多数 UI 字体中缺字形）
            else
                sb.Append('\u2606'); // ☆
        }
        sb.Append($" {rating:0.#}/5");
        return sb.ToString();
    }

    /// <summary>
    /// 构建星级评分富文本（带颜色标签，适用于 TMP 和 UI Toolkit）。
    /// </summary>
    public static string BuildStarRichText(float rating)
    {
        if (rating <= 0f)
            return "<color=#555555>\u2606\u2606\u2606\u2606\u2606</color>  未评分";

        var sb = new System.Text.StringBuilder();
        for (int i = 1; i <= 5; i++)
        {
            if (rating >= i)
                sb.Append("<color=#FFD700>\u2605</color>");
            else if (rating >= i - 0.5f)
                sb.Append("<color=#FFA500>\u2605</color>");
            else
                sb.Append("<color=#555555>\u2606</color>");
        }
        sb.Append($"  {rating:0.#}/5");
        return sb.ToString();
    }

    /// <summary>
    /// 构建可通关状态文本（纯文本版）。
    /// </summary>
    public static string BuildSolvableText(bool isSolvable)
    {
        // U+2714/U+2718 在 UITK+Noto 动态字体里常缺字形；用 √ / ×（221A / 00D7）替代
        return isSolvable ? "\u221A 可通关" : "\u00D7 未验证";
    }

    /// <summary>
    /// 构建可通关状态富文本（带颜色标签）。
    /// </summary>
    public static string BuildSolvableRichText(bool isSolvable)
    {
        return isSolvable
            ? "<color=#44DD55>\u221A 可通关</color>"
            : "<color=#AA4444>\u00D7 未验证</color>";
    }
}
