using System.Collections.Generic;

/// <summary>
/// 只读访问持久化标签注册表（补全、搜索等）。
/// </summary>
public interface ITagRegistryReader
{
    /// <summary>
    /// 返回包含关键词的已知标签列表（不区分大小写）；关键词为空则返回全部副本。
    /// </summary>
    List<string> Search(string keyword);
}

/// <summary>
/// 可写入并持久化标签注册表。核心玩法 SceneContext 默认不绑定；当前由 LevelEditor 通过 <see cref="TagRegistry.Load"/> 使用。
/// </summary>
public interface ITagRegistryWriter : ITagRegistryReader
{
    /// <summary>
    /// 注册一个标签。如果是新标签则自动持久化。返回是否为新增。
    /// </summary>
    bool RegisterTag(string tag);

    void Save();
}
