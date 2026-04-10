using System.Collections.Generic;

/// <summary>
/// 关卡元数据：标签、难度评分、是否可通关、评论。
/// 纯 C# 可序列化类，随 LevelDataModel 一同存储在 JSON 中。
/// </summary>
[System.Serializable]
public class LevelMetadata
{
    public const string DefaultBgmPath = "Sound/BGM/Baba Is You";
    /// <summary>
    /// 关卡标签（自由文本，如 "tutorial"、"hard" 等）。
    /// </summary>
    public List<string> Tags = new List<string>();

    /// <summary>
    /// 难度评分。0 = 未评分，0.5-5 = 已评分（0.5 步进）。
    /// </summary>
    public float DifficultyRating = 0f;

    /// <summary>
    /// 此关卡是否已被验证为可通关（通过试玩通关或求解器求解成功）。
    /// </summary>
    public bool IsSolvable = false;

    /// <summary>
    /// 关卡评论/备注。
    /// </summary>
    public string Comment = "";

    /// <summary>
    /// 关卡显示名（呈现给玩家）。空字符串表示使用文件名作为显示名。
    /// </summary>
    public string DisplayName = "";

    /// <summary>
    /// 关卡 BGM 资源路径（Resources 相对路径，不含扩展名）。空字符串表示使用默认 BGM。
    /// </summary>
    public string BgmPath = DefaultBgmPath;

    public void EnsureValid()
    {
        if (Tags == null) Tags = new List<string>();
        if (Comment == null) Comment = "";
        if (DisplayName == null) DisplayName = "";
        if (string.IsNullOrWhiteSpace(BgmPath)) BgmPath = DefaultBgmPath;

        // 对齐到 0.5 步进
        DifficultyRating = UnityEngine.Mathf.Round(DifficultyRating * 2f) / 2f;
        DifficultyRating = UnityEngine.Mathf.Clamp(DifficultyRating, 0f, 5f);
    }

    public LevelMetadata DeepCopy()
    {
        var copy = new LevelMetadata();
        copy.Tags = new List<string>(Tags ?? new List<string>());
        copy.DifficultyRating = DifficultyRating;
        copy.IsSolvable = IsSolvable;
        copy.Comment = Comment ?? "";
        copy.DisplayName = DisplayName ?? "";
        copy.BgmPath = BgmPath ?? "";
        return copy;
    }
}
