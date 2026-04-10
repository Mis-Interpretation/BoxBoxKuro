using Zenject;

/// <summary>
/// 核心玩法场景 Zenject 绑定：注册实体配置读取器和实体工厂。
/// </summary>
public class CoreInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<IEntityConfigReader>()
            .To<JsonEntityConfigProvider>()
            .AsSingle();

        Container.Bind<IEntityFactory>()
            .To<EntityFactory>()
            .AsSingle();
    }
}
