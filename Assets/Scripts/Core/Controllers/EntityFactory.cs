using UnityEngine;
using Zenject;

/// <summary>
/// 实体工厂接口：根据配置动态创建实体 GameObject。
/// </summary>
public interface IEntityFactory
{
    GameObject Create(string entityId, Vector2Int gridPosition);
    GameObject Create(int typeIndex, Vector2Int gridPosition);
    GameObject Create(EntityData entityData);
}

/// <summary>
/// 实体工厂实现：从 JSON 配置读取组件列表，动态组装 GameObject。
/// </summary>
public class EntityFactory : IEntityFactory
{
    private readonly DiContainer _container;
    private readonly IEntityConfigReader _configReader;

    public EntityFactory(DiContainer container, IEntityConfigReader configReader)
    {
        _container = container;
        _configReader = configReader;
    }

    public GameObject Create(string entityId, Vector2Int gridPosition)
    {
        var config = _configReader.GetConfig(entityId);
        if (config == null)
        {
            Debug.LogWarning($"找不到实体配置: {entityId}");
            return null;
        }

        return CreateFromConfig(config, gridPosition);
    }

    public GameObject Create(int typeIndex, Vector2Int gridPosition)
    {
        var config = _configReader.GetConfigByIndex(typeIndex);
        if (config == null)
        {
            Debug.LogWarning($"找不到实体配置 (TypeIndex={typeIndex})");
            return null;
        }

        return CreateFromConfig(config, gridPosition);
    }

    public GameObject Create(EntityData entityData)
    {
        if (entityData == null) return null;

        var config = _configReader.GetConfigByIndex(entityData.Type);
        if (config == null)
        {
            Debug.LogWarning($"找不到实体配置 (TypeIndex={entityData.Type})");
            return null;
        }

        return CreateFromConfig(config, new Vector2Int(entityData.X, entityData.Y), entityData);
    }

    private GameObject CreateFromConfig(EntityConfigData config, Vector2Int gridPosition)
    {
        return CreateFromConfig(config, gridPosition, null);
    }

    private GameObject CreateFromConfig(EntityConfigData config, Vector2Int gridPosition, EntityData entityData)
    {
        var go = new GameObject(config.Id);

        // 基础组件：所有实体都有
        var posModel = go.AddComponent<PositionModel>();
        posModel.GridPosition = gridPosition;

        if (config.IsTextEntity)
        {
            var runtime = go.AddComponent<TextEntityRuntimeModel>();
            runtime.SetPayload(entityData?.Text ?? new TextEntityPayload());

            var textView = go.AddComponent<TextEntityView>();
            textView.ApplyText(runtime.Payload, config.OrderInLayer);

            go.transform.position = new Vector3(gridPosition.x, gridPosition.y, 0f);
            _container.InjectGameObject(go);
            return go;
        }

        go.AddComponent<SpriteRenderer>();

        // 纯装饰物：忽略所有组件配置，只保留视觉表现。
        if (config.IsPureDecoration)
        {
            // no-op
        }
        else
        {
            // 行为组件：按 JSON 配置动态添加
            foreach (var componentName in config.Components)
            {
                var type = EntityComponentRegistry.Get(componentName);
                if (type != null)
                    go.AddComponent(type);
                else
                    Debug.LogWarning($"未注册的组件: {componentName}");
            }
        }

        // EntityView 最后添加，确保 PositionModel 已存在
        var entityView = go.AddComponent<EntityView>();

        // 应用 Sprite
        var sprite = _configReader.GetSprite(config.Id);
        entityView.InitSprite(sprite, config.OrderInLayer);

        // 设置世界坐标
        go.transform.position = new Vector3(gridPosition.x, gridPosition.y, 0f);

        // Zenject 注入
        _container.InjectGameObject(go);

        return go;
    }
}
