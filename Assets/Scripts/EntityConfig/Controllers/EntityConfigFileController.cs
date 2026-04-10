using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// EntityConfig.json 的读写控制器（纯 C# 类）。
/// </summary>
public class EntityConfigFileController
{
    private string ConfigPath => Path.Combine(Application.streamingAssetsPath, "EntityConfig.json");

    public List<EntityConfigData> LoadAll()
    {
        string json = File.ReadAllText(ConfigPath);
        var root = JsonUtility.FromJson<EntityConfigRoot>(json);
        var result = new List<EntityConfigData>();
        foreach (var src in root.Entities)
            result.Add(DeepCopy(src));
        return result;
    }

    public void Save(List<EntityConfigData> entities)
    {
        ProtectBaseEntitiesBeforeSave(entities);
        var root = new EntityConfigRoot { Entities = entities };
        string json = JsonUtility.ToJson(root, true);
        File.WriteAllText(ConfigPath, json);
        Debug.Log($"[EntityConfig] 已保存 {entities.Count} 个实体到 {ConfigPath}");
    }

    private void ProtectBaseEntitiesBeforeSave(List<EntityConfigData> entities)
    {
        if (entities == null) return;

        List<EntityConfigData> originalEntities;
        try
        {
            originalEntities = LoadAll();
        }
        catch
        {
            // 若读取失败，则至少保证不在此处抛异常阻止用户保存其他内容
            return;
        }

        var originalByTypeIndex = originalEntities
            .Where(e => e != null && e.TypeIndex >= 0 && e.TypeIndex < 4)
            .ToDictionary(e => e.TypeIndex, e => e);

        // 补回缺失的基础实体
        for (int ti = 0; ti < 4; ti++)
        {
            if (!originalByTypeIndex.TryGetValue(ti, out var original)) continue;
            bool exists = entities.Any(e => e != null && e.TypeIndex == ti);
            if (!exists)
                entities.Add(DeepCopy(original));
        }

        // 回滚基础实体的受保护字段：
        // 仅允许 DisplayName / SpritePath / 组件音效覆盖 变化
        for (int i = 0; i < entities.Count; i++)
        {
            var e = entities[i];
            if (e == null) continue;
            if (e.TypeIndex < 0 || e.TypeIndex >= 4) continue;
            if (!originalByTypeIndex.TryGetValue(e.TypeIndex, out var original)) continue;

            e.Id = original.Id;
            e.TypeIndex = original.TypeIndex;
            e.OrderInLayer = original.OrderInLayer;
            e.Components = new List<string>(original.Components);
            e.IsTextEntity = original.IsTextEntity;
            entities[i] = e;
        }
    }

    public static EntityConfigData DeepCopy(EntityConfigData source)
    {
        return new EntityConfigData
        {
            Id = source.Id,
            DisplayName = source.DisplayName,
            TypeIndex = source.TypeIndex,
            SpritePath = source.SpritePath,
            OrderInLayer = source.OrderInLayer,
            Components = new List<string>(source.Components),
            ComponentSfx = DeepCopySfxEntries(source.ComponentSfx),
            ComponentSfxOverrides = DeepCopySfxEntries(source.ComponentSfxOverrides),
            IsPureDecoration = source.IsPureDecoration,
            IsTextEntity = source.IsTextEntity
        };
    }

    private static List<ComponentSfxEntry> DeepCopySfxEntries(List<ComponentSfxEntry> source)
    {
        var result = new List<ComponentSfxEntry>();
        if (source == null) return result;
        foreach (var entry in source)
        {
            if (entry == null) continue;
            result.Add(new ComponentSfxEntry
            {
                ComponentName = entry.ComponentName ?? "",
                SfxPath = entry.SfxPath ?? ""
            });
        }
        return result;
    }
}
