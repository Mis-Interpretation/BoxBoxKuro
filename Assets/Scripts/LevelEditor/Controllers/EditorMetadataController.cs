using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 编辑器元数据控制器：管理当前关卡的标签、难度评分、可通关标记、评论。
/// 同时维护全局标签注册表（TagRegistry）。
/// </summary>
public class EditorMetadataController : MonoBehaviour
{
    private EditorStateModel _state;
    private TagRegistry _tagRegistry;
    private AudioSettingsData _audioSettings;

    public TagRegistry Registry => _tagRegistry;

    private LevelMetadata CurrentMetadata
    {
        get
        {
            _state.CurrentLevel.EnsureMetadata();
            return _state.CurrentLevel.Metadata;
        }
    }

    private void Awake()
    {
        _state = FindAnyObjectByType<EditorStateModel>();
        _tagRegistry = TagRegistry.Load();
        _audioSettings = AudioSettingsLoader.Load();
    }

    // ───────── Tag 操作 ─────────

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;

        tag = tag.Trim().ToLowerInvariant();
        if (!CurrentMetadata.Tags.Contains(tag))
        {
            CurrentMetadata.Tags.Add(tag);
            _tagRegistry.RegisterTag(tag);
            Debug.Log($"[Metadata] 已添加标签: {tag}");
        }
    }

    public void RemoveTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;

        tag = tag.Trim().ToLowerInvariant();
        if (CurrentMetadata.Tags.Remove(tag))
        {
            Debug.Log($"[Metadata] 已移除标签: {tag}");
        }
    }

    public bool HasTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return false;
        return CurrentMetadata.Tags.Contains(tag.Trim().ToLowerInvariant());
    }

    public IReadOnlyList<string> GetTags()
    {
        return CurrentMetadata.Tags.AsReadOnly();
    }

    public void ClearTags()
    {
        CurrentMetadata.Tags.Clear();
        Debug.Log("[Metadata] 已清空所有标签");
    }

    /// <summary>
    /// 搜索已知标签（排除当前关卡已有的标签）。
    /// </summary>
    public List<string> SearchKnownTags(string keyword)
    {
        var results = _tagRegistry.Search(keyword);
        results.RemoveAll(t => CurrentMetadata.Tags.Contains(t));
        return results;
    }

    // ───────── 难度评分 ─────────

    /// <summary>
    /// 设置难度评分。0 = 清除评分，0.5-5 = 有效评分（0.5 步进）。
    /// </summary>
    public void SetDifficultyRating(float rating)
    {
        rating = Mathf.Round(rating * 2f) / 2f;
        rating = Mathf.Clamp(rating, 0f, 5f);
        CurrentMetadata.DifficultyRating = rating;
    }

    public float GetDifficultyRating()
    {
        return CurrentMetadata.DifficultyRating;
    }

    // ───────── 可通关标记 ─────────

    /// <summary>
    /// 标记关卡为已验证可通关。仅供 EditorValidateController 和 EditorSolverController 调用。
    /// </summary>
    public void MarkSolvable()
    {
        if (!CurrentMetadata.IsSolvable)
        {
            CurrentMetadata.IsSolvable = true;
            Debug.Log("[Metadata] 关卡已标记为可通关");
        }
    }

    /// <summary>
    /// 清除可通关标记（例如关卡内容被修改后应重置）。
    /// </summary>
    public void ClearSolvable()
    {
        if (CurrentMetadata.IsSolvable)
        {
            CurrentMetadata.IsSolvable = false;
            Debug.Log("[Metadata] 可通关标记已清除（关卡内容已变更）");
        }
    }

    public bool GetIsSolvable()
    {
        return CurrentMetadata.IsSolvable;
    }

    // ───────── 显示名 ─────────

    public string GetDisplayName()
    {
        return CurrentMetadata.DisplayName ?? "";
    }

    public void SetDisplayName(string name)
    {
        CurrentMetadata.DisplayName = name ?? "";
    }

    // ───────── BGM ─────────

    public string GetBgmPath()
    {
        if (string.IsNullOrWhiteSpace(CurrentMetadata.BgmPath))
            return LevelMetadata.DefaultBgmPath;
        return CurrentMetadata.BgmPath;
    }

    public void SetBgmPath(string bgmPath)
    {
        CurrentMetadata.BgmPath = string.IsNullOrWhiteSpace(bgmPath)
            ? LevelMetadata.DefaultBgmPath
            : bgmPath.Trim();
    }

    public string GetLevelCompleteSfxPath()
    {
        if (_audioSettings == null)
            _audioSettings = AudioSettingsLoader.Load();
        if (string.IsNullOrWhiteSpace(_audioSettings.LevelCompleteSfxPath))
            return AudioSettingsData.DefaultLevelCompleteSfxPath;
        return _audioSettings.LevelCompleteSfxPath;
    }

    public void SetLevelCompleteSfxPath(string sfxPath)
    {
        if (_audioSettings == null)
            _audioSettings = AudioSettingsLoader.Load();

        _audioSettings.LevelCompleteSfxPath = string.IsNullOrWhiteSpace(sfxPath)
            ? AudioSettingsData.DefaultLevelCompleteSfxPath
            : sfxPath.Trim();
    }

    public void SaveGlobalAudioSettings()
    {
        if (_audioSettings == null)
            _audioSettings = AudioSettingsLoader.Load();
        AudioSettingsLoader.Save(_audioSettings);
    }

    // ───────── 评论 ─────────

    public void SetComment(string comment)
    {
        CurrentMetadata.Comment = comment ?? "";
    }

    public string GetComment()
    {
        return CurrentMetadata.Comment ?? "";
    }
}
