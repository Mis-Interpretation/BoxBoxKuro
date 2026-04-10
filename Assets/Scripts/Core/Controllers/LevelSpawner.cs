using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 共享的关卡实体生成逻辑。
/// 编辑器加载、游戏场景加载、编辑器试玩均使用此类来实例化实体。
/// </summary>
public static class LevelSpawner
{
    /// <summary>
    /// 根据关卡数据通过工厂实例化所有实体，返回生成的 GameObject 列表。
    /// </summary>
    public static List<GameObject> SpawnEntities(LevelDataModel levelData, IEntityFactory factory)
    {
        var spawned = new List<GameObject>();

        foreach (var entity in levelData.Entities)
        {
            GameObject instance = factory.Create(entity);
            if (instance == null)
            {
                Debug.LogWarning($"工厂创建实体失败: {entity.Type}");
                continue;
            }

            spawned.Add(instance);
        }

        return spawned;
    }
}
