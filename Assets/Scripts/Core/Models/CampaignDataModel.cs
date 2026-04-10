using System.Collections.Generic;

/// <summary>
/// 战役数据：包含多个大关卡（Chapter），每个 Chapter 内含有序关卡列表。
/// 存储在 StreamingAssets/campaign.json。
/// </summary>
[System.Serializable]
public class CampaignDataModel
{
    public List<ChapterData> Chapters = new List<ChapterData>();
}

/// <summary>
/// 大关卡（章节）：名称、备注、解锁条件、关卡文件名有序列表。
/// 关卡序号由数组下标自动生成，如 Chapter[0].Levels[2] → "1-3"。
/// </summary>
[System.Serializable]
public class ChapterData
{
    public string ChapterName = "New Chapter";
    public string Comment = "";
    public UnlockCondition Unlock = new UnlockCondition();
    /// <summary>
    /// 章节是否对玩家可见。false = 草稿/未上线，仅编辑器可见。
    /// 默认值 true 保证旧存档反序列化后自动视为已上线。
    /// </summary>
    public bool IsOnline = true;
    /// <summary>
    /// 有序关卡文件名列表（不含 .json 后缀），与 LevelLoaderController.LevelToLoad 一致。
    /// </summary>
    public List<string> Levels = new List<string>();
}

[System.Serializable]
public class UnlockCondition
{
    public UnlockType Type = UnlockType.ClearPreviousChapter;
    /// <summary>需要累计通关关数才能解锁（字段名沿用保持兼容）。</summary>
    public int RequiredStars = 0;
}

[System.Serializable]
public enum UnlockType
{
    AlwaysOpen,           // 始终开放
    ClearPreviousChapter, // 通关前一章
    StarCount             // 累计关数（枚举名沿用保持兼容）
}
