using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// campaign.json 的读写以及关卡目录扫描。
/// 参照 EditorFileController 的模式，使用 StreamingAssets 路径。
/// </summary>
public class ArrangementFileController
{
    private string CampaignPath => Path.Combine(Application.streamingAssetsPath, "campaign.json");
    private string LevelsDirectory => Path.Combine(Application.streamingAssetsPath, "Levels");

    /// <summary>
    /// 从 campaign.json 加载战役数据。文件不存在时返回空模型。
    /// </summary>
    public CampaignDataModel LoadCampaign()
    {
        if (!File.Exists(CampaignPath))
            return new CampaignDataModel();

        string json = File.ReadAllText(CampaignPath);
        var campaign = JsonUtility.FromJson<CampaignDataModel>(json);
        return campaign ?? new CampaignDataModel();
    }

    /// <summary>
    /// 将战役数据保存到 campaign.json。
    /// </summary>
    public void SaveCampaign(CampaignDataModel campaign)
    {
        string json = JsonUtility.ToJson(campaign, true);
        File.WriteAllText(CampaignPath, json);
        Debug.Log($"战役数据已保存: {CampaignPath}");
    }

    /// <summary>
    /// 扫描 StreamingAssets/Levels/ 目录，返回所有关卡文件名（不含 .json 后缀）。
    /// </summary>
    public List<string> ScanAllLevels()
    {
        var result = new List<string>();
        if (!Directory.Exists(LevelsDirectory))
            return result;

        foreach (var filePath in Directory.GetFiles(LevelsDirectory, "*.json"))
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            result.Add(fileName);
        }
        result.Sort();
        return result;
    }

    /// <summary>
    /// 读取单个关卡文件的元数据摘要（不保留完整实体数据）。
    /// </summary>
    public LevelMetadataSummary LoadLevelMetadata(string levelName)
    {
        string filePath = Path.Combine(LevelsDirectory, levelName + ".json");
        if (!File.Exists(filePath))
        {
            return new LevelMetadataSummary
            {
                LevelName = levelName,
                Comment = "[文件不存在]"
            };
        }

        string json = File.ReadAllText(filePath);
        var levelData = JsonUtility.FromJson<LevelDataModel>(json);
        if (levelData == null)
        {
            return new LevelMetadataSummary
            {
                LevelName = levelName,
                Comment = "[JSON 解析失败]"
            };
        }

        levelData.EnsureMetadata();

        return new LevelMetadataSummary
        {
            LevelName = levelData.LevelName,
            Width = levelData.Width,
            Height = levelData.Height,
            EntityCount = levelData.Entities?.Count ?? 0,
            Tags = levelData.Metadata.Tags ?? new List<string>(),
            DifficultyRating = levelData.Metadata.DifficultyRating,
            IsSolvable = levelData.Metadata.IsSolvable,
            Comment = levelData.Metadata.Comment ?? "",
            DisplayName = levelData.Metadata.DisplayName ?? ""
        };
    }

    /// <summary>
    /// 批量加载所有关卡的元数据摘要。
    /// </summary>
    public Dictionary<string, LevelMetadataSummary> LoadAllMetadata(List<string> levelNames)
    {
        var cache = new Dictionary<string, LevelMetadataSummary>();
        foreach (var name in levelNames)
            cache[name] = LoadLevelMetadata(name);
        return cache;
    }

    /// <summary>
    /// 在 Levels 目录创建空白关卡 JSON，不写入 campaign（默认出现在「未分配」列表）。
    /// levelName 为玩家输入（可含空格，会 Trim）；成功返回 true 与规范化后的文件名（不含 .json）。
    /// </summary>
    public bool TryCreateNewEmptyLevel(string levelName, out string createdName, out string errorMessage)
    {
        createdName = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(levelName))
        {
            errorMessage = "名称不能为空";
            return false;
        }

        string name = levelName.Trim();
        if (name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            name = name.Substring(0, name.Length - 5).TrimEnd();

        if (name.Length == 0)
        {
            errorMessage = "名称不能为空";
            return false;
        }

        if (name.Length > 120)
        {
            errorMessage = "名称过长（最多 120 字符）";
            return false;
        }

        foreach (char c in Path.GetInvalidFileNameChars())
        {
            if (name.IndexOf(c) >= 0)
            {
                errorMessage = "名称包含非法字符";
                return false;
            }
        }

        try
        {
            if (!Directory.Exists(LevelsDirectory))
                Directory.CreateDirectory(LevelsDirectory);

            var existing = new HashSet<string>(ScanAllLevels(), StringComparer.OrdinalIgnoreCase);
            if (existing.Contains(name))
            {
                errorMessage = "已存在同名关卡";
                return false;
            }

            var level = new LevelDataModel { LevelName = name };
            level.EnsureMetadata();
            string json = JsonUtility.ToJson(level, true);
            string filePath = Path.Combine(LevelsDirectory, name + ".json");
            File.WriteAllText(filePath, json);
            Debug.Log($"已新建关卡: {filePath}");
            createdName = name;
            return true;
        }
        catch (Exception e)
        {
            errorMessage = $"写入失败: {e.Message}";
            Debug.LogWarning($"新建关卡失败: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 校验 campaign 中的关卡引用，返回缺失文件的关卡名列表。
    /// </summary>
    public List<string> ValidateReferences(CampaignDataModel campaign, HashSet<string> existingFiles)
    {
        var missing = new List<string>();
        foreach (var chapter in campaign.Chapters)
            foreach (var level in chapter.Levels)
                if (!existingFiles.Contains(level))
                    missing.Add(level);
        return missing;
    }
}
