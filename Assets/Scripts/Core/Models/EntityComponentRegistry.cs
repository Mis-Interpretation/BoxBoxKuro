using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// 组件名称 → Type 的静态注册表，用于从 JSON 配置动态添加组件。
/// </summary>
public static class EntityComponentRegistry
{
    private static readonly Dictionary<string, Type> _map = new Dictionary<string, Type>
    {
        ["MovableModel"] = typeof(MovableModel),
        ["BlockingModel"] = typeof(BlockingModel),
        ["PushableModel"] = typeof(PushableModel),
        ["ControllableModel"] = typeof(ControllableModel),
        ["OverlappableModel"] = typeof(OverlappableModel),
    };

    public static Type Get(string name)
    {
        return _map.TryGetValue(name, out var t) ? t : null;
    }

    public static void Register(string name, Type type)
    {
        _map[name] = type;
    }

    public static IReadOnlyCollection<string> AllKeys()
    {
        return new ReadOnlyCollection<string>(new List<string>(_map.Keys));
    }
}
