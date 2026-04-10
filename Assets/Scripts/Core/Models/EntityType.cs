/// <summary>
/// 内置实体类型枚举。用于向后兼容现有关卡 JSON 和 Solver。
/// 自定义实体类型通过 string Id 扩展，不需要在此枚举中添加。
/// </summary>
public enum EntityType
{
    Player,
    Block,
    Box,
    Endpoint
}
