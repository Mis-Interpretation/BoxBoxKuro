using System.Collections.Generic;

/// <summary>
/// 关卡元数据摘要，用于编排界面的轻量缓存。
/// 不含完整实体数据，仅保留预览所需信息。
/// </summary>
public class LevelMetadataSummary
{
    public string LevelName;
    public int Width;
    public int Height;
    public int EntityCount;
    public List<string> Tags = new List<string>();
    public float DifficultyRating;
    public bool IsSolvable;
    public string Comment = "";
    public string DisplayName = "";
}
