using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 记录单个实体在一次移动中的位置变化。
/// </summary>
[System.Serializable]
public struct EntityMove
{
    public PositionModel Entity;
    public Vector2Int FromPosition;

    public EntityMove(PositionModel entity, Vector2Int fromPosition)
    {
        Entity = entity;
        FromPosition = fromPosition;
    }
}

/// <summary>
/// 记录一次玩家操作中所有被移动实体的信息（玩家 + 可能被推的箱子）。
/// </summary>
public class MoveRecord
{
    public List<EntityMove> Moves = new List<EntityMove>();

    public void Add(PositionModel entity, Vector2Int fromPosition)
    {
        Moves.Add(new EntityMove(entity, fromPosition));
    }
}
