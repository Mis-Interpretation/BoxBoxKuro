using System.IO;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战役推进控制器：管理当前关卡进度，支持通关后自动跳转下一关。
/// 使用 PlayerPrefs 持久化当前章节/关卡索引。
/// </summary>
public static class CampaignProgressController
{
    private const string PrefKeyChapter = "Campaign_CurrentChapter";
    private const string PrefKeyLevel = "Campaign_CurrentLevel";
    private const string CompletionFileName = "campaign_completion.json";

    private static string CompletionSavePath => Path.Combine(Application.persistentDataPath, CompletionFileName);

    public static int CurrentChapter
    {
        get => PlayerPrefs.GetInt(PrefKeyChapter, 0);
        set { PlayerPrefs.SetInt(PrefKeyChapter, value); PlayerPrefs.Save(); }
    }

    public static int CurrentLevel
    {
        get => PlayerPrefs.GetInt(PrefKeyLevel, 0);
        set { PlayerPrefs.SetInt(PrefKeyLevel, value); PlayerPrefs.Save(); }
    }

    /// <summary>
    /// 加载 campaign.json。
    /// </summary>
    public static CampaignDataModel LoadCampaign()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "campaign.json");
        if (!File.Exists(path))
            return null;

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<CampaignDataModel>(json);
    }

    /// <summary>
    /// ��取当前应加载的关卡文件名。无有效关卡时返回 null。
    /// </summary>
    public static string GetCurrentLevelName()
    {
        var campaign = LoadCampaign();
        if (campaign == null) return null;

        int ch = CurrentChapter;
        int lv = CurrentLevel;

        if (ch < 0 || ch >= campaign.Chapters.Count) return null;
        var chapter = campaign.Chapters[ch];
        if (lv < 0 || lv >= chapter.Levels.Count) return null;

        return chapter.Levels[lv];
    }

    /// <summary>
    /// 获取当前关卡的展示标签，如 "1-3"。
    /// </summary>
    public static string GetDisplayLabel()
    {
        return $"{CurrentChapter + 1}-{CurrentLevel + 1}";
    }

    /// <summary>
    /// 在 campaign 中按关卡文件名（不含 .json）查找「章-关」展示编号，如 "1-3"。
    /// 未出现在战役配置中时返回 null（例如编辑器指定了非战役关卡）。
    /// </summary>
    public static string TryGetDisplayLabelForLevelFile(string levelFileBaseName)
    {
        var campaign = LoadCampaign();
        if (campaign?.Chapters == null || string.IsNullOrEmpty(levelFileBaseName))
            return null;

        for (int ch = 0; ch < campaign.Chapters.Count; ch++)
        {
            var chapter = campaign.Chapters[ch];
            if (chapter?.Levels == null) continue;

            for (int lv = 0; lv < chapter.Levels.Count; lv++)
            {
                if (chapter.Levels[lv] == levelFileBaseName)
                    return $"{ch + 1}-{lv + 1}";
            }
        }

        return null;
    }

    public static bool MarkCurrentLevelCompleted()
    {
        return MarkLevelCompleted(CurrentChapter, CurrentLevel);
    }

    public static bool MarkLevelCompleted(int chapterIndex, int levelIndex)
    {
        if (chapterIndex < 0 || levelIndex < 0) return false;

        if (!TryGetLevelFileNameAt(chapterIndex, levelIndex, out string levelFileName))
            return false;

        var save = LoadCompletionSave();
        save.CompletedLevelKeys ??= new List<string>();

        string key = BuildLevelIdKey(chapterIndex, levelFileName);
        if (save.CompletedLevelKeys.Contains(key))
            return false;

        save.CompletedLevelKeys.Add(key);
        SaveCompletionSave(save);
        return true;
    }

    public static bool IsLevelCompleted(int chapterIndex, int levelIndex)
    {
        if (chapterIndex < 0 || levelIndex < 0) return false;

        if (!TryGetLevelFileNameAt(chapterIndex, levelIndex, out string levelFileName))
            return false;

        var save = LoadCompletionSave();
        if (save.CompletedLevelKeys == null || save.CompletedLevelKeys.Count == 0)
            return false;

        return save.CompletedLevelKeys.Contains(BuildLevelIdKey(chapterIndex, levelFileName));
    }

    public static bool IsChapterCleared(int chapterIndex)
    {
        var campaign = LoadCampaign();
        if (campaign == null) return false;
        if (chapterIndex < 0 || chapterIndex >= campaign.Chapters.Count) return false;

        var chapter = campaign.Chapters[chapterIndex];
        if (chapter.Levels == null || chapter.Levels.Count == 0) return true;

        for (int levelIndex = 0; levelIndex < chapter.Levels.Count; levelIndex++)
        {
            if (!IsLevelCompleted(chapterIndex, levelIndex))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 是否有下一关（包括跨章节）。
    /// </summary>
    public static bool HasNextLevel()
    {
        var campaign = LoadCampaign();
        if (campaign == null) return false;

        int ch = CurrentChapter;
        int lv = CurrentLevel;

        if (ch < 0 || ch >= campaign.Chapters.Count) return false;
        var chapter = campaign.Chapters[ch];

        // 仅在当前章节内判断“下一关”
        return lv + 1 < chapter.Levels.Count;
    }

    /// <summary>
    /// 推进到下一关。返回 true 表示成功推进，false 表示已无下一关。
    /// </summary>
    public static bool AdvanceToNext()
    {
        var campaign = LoadCampaign();
        if (campaign == null) return false;

        int ch = CurrentChapter;
        int lv = CurrentLevel;

        if (ch < 0 || ch >= campaign.Chapters.Count) return false;
        var chapter = campaign.Chapters[ch];

        // 尝试当前章的下一关
        if (lv + 1 < chapter.Levels.Count)
        {
            CurrentLevel = lv + 1;
            return true;
        }

        // 当前章节结束后不自动跨章节推进
        return false;
    }

    /// <summary>
    /// 检查指定章节是否已解锁。
    /// </summary>
    public static bool IsChapterUnlocked(int chapterIndex)
    {
        var campaign = LoadCampaign();
        if (campaign == null) return false;
        if (chapterIndex < 0 || chapterIndex >= campaign.Chapters.Count) return false;

        var unlock = campaign.Chapters[chapterIndex].Unlock;
        if (unlock == null) return true;

        switch (unlock.Type)
        {
            case UnlockType.AlwaysOpen:
                return true;

            case UnlockType.ClearPreviousChapter:
                if (chapterIndex == 0) return true;
                return IsChapterCleared(chapterIndex - 1);

            case UnlockType.StarCount:
                return GetTotalCompletedLevelCountInOnlineChapters() >= unlock.RequiredStars;

            default:
                return true;
        }
    }

    /// <summary>
    /// 重置进度到第一关。
    /// </summary>
    public static void ResetProgress()
    {
        CurrentChapter = 0;
        CurrentLevel = 0;
    }

    public static void ClearCompletionRecords()
    {
        if (File.Exists(CompletionSavePath))
            File.Delete(CompletionSavePath);
    }

    public static void ResetProgressAndCompletion()
    {
        ClearCompletionRecords();
        ResetProgress();
    }

    /// <summary>
    /// 获取累计通关关数（仅统计当前 campaign.json 中已上线章节内的有效关卡）。
    /// </summary>
    public static int GetTotalCompletedLevelCountInOnlineChapters()
    {
        var campaign = LoadCampaign();
        if (campaign == null || campaign.Chapters == null || campaign.Chapters.Count == 0)
            return 0;

        var save = LoadCompletionSave();
        if (save.CompletedLevelKeys == null || save.CompletedLevelKeys.Count == 0)
            return 0;

        var counted = new HashSet<string>();
        foreach (string key in save.CompletedLevelKeys)
        {
            if (!TryParseLevelIdKey(key, out int chapterIndex, out string levelFileName))
                continue;

            if (chapterIndex < 0 || chapterIndex >= campaign.Chapters.Count)
                continue;

            var chapter = campaign.Chapters[chapterIndex];
            if (chapter == null || !chapter.IsOnline || chapter.Levels == null)
                continue;

            if (string.IsNullOrEmpty(levelFileName) || !chapter.Levels.Contains(levelFileName))
                continue;

            counted.Add(key);
        }

        return counted.Count;
    }

    private static CampaignCompletionSaveModel LoadCompletionSave()
    {
        if (!File.Exists(CompletionSavePath))
            return new CampaignCompletionSaveModel();

        try
        {
            string json = File.ReadAllText(CompletionSavePath);
            var save = JsonUtility.FromJson<CampaignCompletionSaveModel>(json);
            if (save == null)
                return new CampaignCompletionSaveModel();

            save.CompletedLevelKeys ??= new List<string>();
            MigrateLegacyCompletionKeys(save);
            return save;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"读取通关记录失败，使用默认空记录: {e.Message}");
            return new CampaignCompletionSaveModel();
        }
    }

    private static void SaveCompletionSave(CampaignCompletionSaveModel save)
    {
        if (save == null)
            save = new CampaignCompletionSaveModel();

        save.CompletedLevelKeys ??= new List<string>();
        string json = JsonUtility.ToJson(save, true);
        File.WriteAllText(CompletionSavePath, json);
    }

    /// <summary>
    /// 通关记录主键：章节索引 + 关卡文件名（与 campaign.json / Levels 下 json 基名一致），
    /// 避免调整关卡排序后通关状态错位。
    /// </summary>
    private static string BuildLevelIdKey(int chapterIndex, string levelFileBaseName)
    {
        return $"L|{chapterIndex}|{levelFileBaseName}";
    }

    private static bool TryParseLevelIdKey(string key, out int chapterIndex, out string levelFileName)
    {
        chapterIndex = -1;
        levelFileName = null;

        if (string.IsNullOrEmpty(key) || key.Length < 4 || key[0] != 'L' || key[1] != '|')
            return false;

        string[] parts = key.Split(new[] { '|' }, 3);
        if (parts.Length != 3 || string.IsNullOrEmpty(parts[2]))
            return false;

        if (!int.TryParse(parts[1], out chapterIndex))
            return false;

        levelFileName = parts[2];
        return true;
    }

    private static bool TryParseLegacyIndexKey(string key, out int chapterIndex, out int levelIndex)
    {
        chapterIndex = -1;
        levelIndex = -1;

        if (string.IsNullOrWhiteSpace(key) || key.StartsWith("L|", System.StringComparison.Ordinal))
            return false;

        string[] parts = key.Split(':');
        if (parts.Length != 2)
            return false;

        return int.TryParse(parts[0], out chapterIndex) &&
               int.TryParse(parts[1], out levelIndex);
    }

    private static bool TryGetLevelFileNameAt(int chapterIndex, int levelIndex, out string levelFileName)
    {
        levelFileName = null;
        var campaign = LoadCampaign();
        if (campaign == null || chapterIndex < 0 || chapterIndex >= campaign.Chapters.Count)
            return false;

        var chapter = campaign.Chapters[chapterIndex];
        if (chapter?.Levels == null || levelIndex < 0 || levelIndex >= chapter.Levels.Count)
            return false;

        levelFileName = chapter.Levels[levelIndex];
        return !string.IsNullOrEmpty(levelFileName);
    }

    /// <summary>
    /// 将旧版「章索引:关卡序号」记录按当前 campaign 映射为「章索引|关卡文件名」，便于旧存档在升级后仍能对应到具体关卡。
    /// </summary>
    private static void MigrateLegacyCompletionKeys(CampaignCompletionSaveModel save)
    {
        if (save.CompletedLevelKeys == null || save.CompletedLevelKeys.Count == 0)
            return;

        var campaign = LoadCampaign();
        if (campaign == null)
            return;

        var keys = save.CompletedLevelKeys;
        bool changed = false;

        for (int i = keys.Count - 1; i >= 0; i--)
        {
            string key = keys[i];
            if (!TryParseLegacyIndexKey(key, out int ch, out int lv))
                continue;

            keys.RemoveAt(i);
            changed = true;

            if (ch < 0 || ch >= campaign.Chapters.Count)
                continue;

            var chapter = campaign.Chapters[ch];
            if (chapter?.Levels == null || lv < 0 || lv >= chapter.Levels.Count)
                continue;

            string levelName = chapter.Levels[lv];
            if (string.IsNullOrEmpty(levelName))
                continue;

            string newKey = BuildLevelIdKey(ch, levelName);
            if (!keys.Contains(newKey))
                keys.Add(newKey);
        }

        if (changed)
            SaveCompletionSave(save);
    }
}
