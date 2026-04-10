using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 从 StreamingAssets/EntityConfig.json 加载实体配置，替代 ScriptableObject 方案。
/// </summary>
public class JsonEntityConfigProvider : IEntityConfigReader
{
    private static readonly HashSet<string> GameplayComponents = new HashSet<string>
    {
        "BlockingModel",
        "PushableModel",
        "MovableModel",
        "ControllableModel",
        "OverlappableModel"
    };

    private readonly Dictionary<string, EntityConfigData> _byId = new Dictionary<string, EntityConfigData>();
    private readonly Dictionary<int, EntityConfigData> _byIndex = new Dictionary<int, EntityConfigData>();
    private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
    private readonly List<EntityConfigData> _allConfigs;

    public IReadOnlyList<EntityConfigData> AllConfigs => _allConfigs;

    public JsonEntityConfigProvider()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "EntityConfig.json");
        string json = File.ReadAllText(path);
        var root = JsonUtility.FromJson<EntityConfigRoot>(json);

        _allConfigs = root.Entities;

        foreach (var config in root.Entities)
        {
            if (!config.IsTextEntity && string.Equals(config.Id, "Text", System.StringComparison.OrdinalIgnoreCase))
                config.IsTextEntity = true;
            if (config.Components == null) config.Components = new List<string>();
            if (config.ComponentSfx == null) config.ComponentSfx = new List<ComponentSfxEntry>();
            if (config.ComponentSfxOverrides == null) config.ComponentSfxOverrides = new List<ComponentSfxEntry>();

            ValidateDecorationConfig(config);
            _byId[config.Id] = config;
            _byIndex[config.TypeIndex] = config;
        }
    }

    private static void ValidateDecorationConfig(EntityConfigData config)
    {
        if (config == null || !config.IsPureDecoration || config.Components == null) return;

        for (int i = 0; i < config.Components.Count; i++)
        {
            string component = config.Components[i];
            if (!GameplayComponents.Contains(component)) continue;

            Debug.LogWarning(
                $"[EntityConfig] 纯装饰物 '{config.Id}' 配置了玩法组件 '{component}'，运行时将忽略该组件。");
        }
    }

    public EntityConfigData GetConfig(string entityId)
    {
        return _byId.TryGetValue(entityId, out var config) ? config : null;
    }

    public EntityConfigData GetConfigByIndex(int typeIndex)
    {
        return _byIndex.TryGetValue(typeIndex, out var config) ? config : null;
    }

    public Sprite GetSprite(string entityId)
    {
        if (_spriteCache.TryGetValue(entityId, out var cached))
            return cached;

        var config = GetConfig(entityId);
        if (config == null) return null;

        string spritePath = config.SpritePath?.Trim();
        bool hasSprite = !string.IsNullOrWhiteSpace(spritePath)
            && !string.Equals(spritePath, "none", System.StringComparison.OrdinalIgnoreCase);
        if (!hasSprite)
        {
            _spriteCache[entityId] = null;
            return null;
        }

        var sprite = Resources.Load<Sprite>(spritePath);
        if (sprite == null)
            Debug.LogWarning($"找不到 Sprite: {config.SpritePath}");

        _spriteCache[entityId] = sprite;
        return sprite;
    }

    public bool HasComponent(string entityId, string componentName)
    {
        var config = GetConfig(entityId);
        if (config == null) return false;
        if (config.IsPureDecoration) return false;
        return config.Components.Contains(componentName);
    }

    public bool TryGetComponentSfx(string entityId, string componentName, out string sfxPath)
    {
        sfxPath = "";
        var config = GetConfig(entityId);
        if (config == null) return false;
        if (!HasComponent(entityId, componentName)) return false;

        string value = "";
        if (config.ComponentSfxOverrides != null)
        {
            for (int i = 0; i < config.ComponentSfxOverrides.Count; i++)
            {
                var entry = config.ComponentSfxOverrides[i];
                if (entry == null) continue;
                if (entry.ComponentName != componentName) continue;
                value = entry.SfxPath ?? "";
                break;
            }
        }
        if (string.IsNullOrWhiteSpace(value)) return false;
        sfxPath = value;
        return true;
    }
}
