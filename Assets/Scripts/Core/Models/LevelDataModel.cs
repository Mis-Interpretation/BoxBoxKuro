using System.Collections.Generic;

/// <summary>
/// 可序列化的关卡数据，保存/加载时使用。纯 C# 类，不依赖 MonoBehaviour。
/// </summary>
[System.Serializable]
public class LevelDataModel
{
    public string LevelName = "Untitled";
    public int Width = 10;
    public int Height = 10;
    public List<EntityData> Entities = new List<EntityData>();
    public LevelMetadata Metadata = new LevelMetadata();

    /// <summary>
    /// 确保 Metadata 字段非空且合法。用于加载旧版 JSON 后的兼容性修复。
    /// </summary>
    public void EnsureMetadata()
    {
        if (Metadata == null)
            Metadata = new LevelMetadata();
        Metadata.EnsureValid();
        EnsureEntities();
    }

    /// <summary>
    /// 兼容旧版关卡数据，归一化实体扩展字段。
    /// </summary>
    public void EnsureEntities()
    {
        if (Entities == null)
            Entities = new List<EntityData>();

        foreach (var entity in Entities)
            entity?.EnsureValid();
    }
}

[System.Serializable]
public class EntityData
{
    public int Type;
    public int X;
    public int Y;
    public TextEntityPayload Text;

    public bool HasTextPayload => Text != null;

    public void EnsureValid()
    {
        Text?.EnsureValid();
    }
}

[System.Serializable]
public class TextEntityPayload
{
    public string Content = "Text";
    public int FontSize = 1;
    public int WidthInCells = 1;
    public int HeightInCells = 1;

    public void EnsureValid()
    {
        if (string.IsNullOrEmpty(Content))
            Content = "Text";

        if (FontSize < 1)
            FontSize = 1;
        // text 固定为单格占位，宽高字段仅为旧数据兼容保留。
        WidthInCells = 1;
        HeightInCells = 1;
    }
}
