using System.Collections.Generic;

/// <summary>
/// 编排界面的运行时状态。纯 C# 类，由 ArrangementController 持有。
/// </summary>
public class ArrangementStateModel
{
    public CampaignDataModel Campaign = new CampaignDataModel();
    public int SelectedChapterIndex = -1;
    public string SelectedLevelName = "";
    public Dictionary<string, LevelMetadataSummary> MetadataCache = new Dictionary<string, LevelMetadataSummary>();
    public List<string> AllLevelFiles = new List<string>();

    /// <summary>
    /// 章节内「未验证」关卡数量：无元数据缓存、或 <see cref="LevelMetadataSummary.IsSolvable"/> 为 false。
    /// </summary>
    public int CountUnverifiedLevelsInChapter(ChapterData chapter)
    {
        if (chapter?.Levels == null || chapter.Levels.Count == 0)
            return 0;

        int n = 0;
        foreach (string levelName in chapter.Levels)
        {
            if (!MetadataCache.TryGetValue(levelName, out var meta))
            {
                n++;
                continue;
            }

            if (!meta.IsSolvable)
                n++;
        }

        return n;
    }

    /// <summary>
    /// 获取不在任何 Chapter 中的关卡文件名列表。
    /// </summary>
    public List<string> GetUnassignedLevels()
    {
        var assigned = new HashSet<string>();
        foreach (var chapter in Campaign.Chapters)
            foreach (var level in chapter.Levels)
                assigned.Add(level);

        var unassigned = new List<string>();
        foreach (var file in AllLevelFiles)
            if (!assigned.Contains(file))
                unassigned.Add(file);
        return unassigned;
    }
}
