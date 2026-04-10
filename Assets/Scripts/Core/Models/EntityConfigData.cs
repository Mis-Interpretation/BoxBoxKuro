using System.Collections.Generic;

/// <summary>
/// 单个实体类型的 JSON 配置数据。
/// </summary>
[System.Serializable]
public class EntityConfigData
{
    public string Id;
    public string DisplayName;
    public int TypeIndex;
    public string SpritePath;
    public int OrderInLayer;
    public List<string> Components;
    public List<ComponentSfxEntry> ComponentSfx = new List<ComponentSfxEntry>();
    public List<ComponentSfxEntry> ComponentSfxOverrides = new List<ComponentSfxEntry>();
    public bool IsPureDecoration;
    public bool IsTextEntity;

    public string GetComponentSfx(string componentName, bool preferOverride = true)
    {
        if (preferOverride)
        {
            string overrideValue = GetSfxFromEntries(ComponentSfxOverrides, componentName);
            if (!string.IsNullOrWhiteSpace(overrideValue))
                return overrideValue;
        }

        return GetSfxFromEntries(ComponentSfx, componentName);
    }

    public void SetComponentSfx(string componentName, string sfxPath, bool isOverride)
    {
        var target = isOverride ? ComponentSfxOverrides : ComponentSfx;
        SetSfxInEntries(target, componentName, sfxPath);
    }

    private static string GetSfxFromEntries(List<ComponentSfxEntry> entries, string componentName)
    {
        if (entries == null) return "";
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null) continue;
            if (entry.ComponentName != componentName) continue;
            return entry.SfxPath ?? "";
        }
        return "";
    }

    private static void SetSfxInEntries(List<ComponentSfxEntry> entries, string componentName, string sfxPath)
    {
        if (entries == null) return;

        string value = string.IsNullOrWhiteSpace(sfxPath) || sfxPath.Trim().ToLowerInvariant() == "none"
            ? ""
            : sfxPath.Trim();

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null) continue;
            if (entry.ComponentName != componentName) continue;

            if (string.IsNullOrEmpty(value))
                entries.RemoveAt(i);
            else
                entry.SfxPath = value;
            return;
        }

        if (!string.IsNullOrEmpty(value))
        {
            entries.Add(new ComponentSfxEntry
            {
                ComponentName = componentName,
                SfxPath = value
            });
        }
    }
}

[System.Serializable]
public class ComponentSfxEntry
{
    public string ComponentName;
    public string SfxPath;
}

/// <summary>
/// EntityConfig.json 的根结构。
/// </summary>
[System.Serializable]
public class EntityConfigRoot
{
    public List<EntityConfigData> Entities;
}
