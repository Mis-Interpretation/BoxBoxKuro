using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 运行时只读访问实体配置表（JSON 驱动）。
/// </summary>
public interface IEntityConfigReader
{
    EntityConfigData GetConfig(string entityId);
    EntityConfigData GetConfigByIndex(int typeIndex);
    Sprite GetSprite(string entityId);
    bool HasComponent(string entityId, string componentName);
    bool TryGetComponentSfx(string entityId, string componentName, out string sfxPath);
    IReadOnlyList<EntityConfigData> AllConfigs { get; }
}
