using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 全局标签注册表：记录项目中所有已使用过的标签。
/// 持久化为 StreamingAssets/tag_registry.json，供标签下拉补全使用。
/// 实现 <see cref="ITagRegistryWriter"/> 供 Zenject 按接口注入；游戏关卡场景通常不绑定本服务，仍使用 <see cref="Load"/> 即可。
/// </summary>
[System.Serializable]
public class TagRegistry : ITagRegistryWriter
{
    public List<string> AllTags = new List<string>();

    private static string FilePath =>
        Path.Combine(Application.streamingAssetsPath, "tag_registry.json");

    public static TagRegistry Load()
    {
        if (!File.Exists(FilePath))
            return new TagRegistry();

        string json = File.ReadAllText(FilePath);
        var registry = JsonUtility.FromJson<TagRegistry>(json);
        return registry ?? new TagRegistry();
    }

    public void Save()
    {
        string dir = Path.GetDirectoryName(FilePath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string json = JsonUtility.ToJson(this, true);
        File.WriteAllText(FilePath, json);
    }

    /// <summary>
    /// 注册一个标签。如果是新标签则自动持久化。返回是否为新增。
    /// </summary>
    public bool RegisterTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return false;

        tag = tag.Trim().ToLowerInvariant();
        if (AllTags.Contains(tag)) return false;

        AllTags.Add(tag);
        AllTags.Sort();
        Save();
        return true;
    }

    /// <summary>
    /// 返回包含关键词的已知标签列表（不区分大小写）。
    /// </summary>
    public List<string> Search(string keyword)
    {
        var results = new List<string>();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            results.AddRange(AllTags);
            return results;
        }

        keyword = keyword.Trim().ToLowerInvariant();
        foreach (var tag in AllTags)
        {
            if (tag.Contains(keyword))
                results.Add(tag);
        }
        return results;
    }
}
