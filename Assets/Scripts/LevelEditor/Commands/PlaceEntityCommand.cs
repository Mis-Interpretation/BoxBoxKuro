using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 记录一次放置操作，可撤销：销毁放置的实体，恢复被覆盖的实体。
/// </summary>
public class PlaceEntityCommand : IEditorCommand
{
    private readonly EditorStateModel _state;
    private readonly int _typeIndex;
    private readonly Vector2Int _cell;
    private readonly GameObject _placed;
    private readonly List<DisplacedEntity> _displaced;
    private readonly TextEntityPayload _textPayload;

    public struct DisplacedEntity
    {
        public int TypeIndex;
        public Vector2Int Cell;
        public TextEntityPayload TextPayload;
    }

    public PlaceEntityCommand(
        EditorStateModel state,
        int typeIndex,
        Vector2Int cell,
        GameObject placed,
        List<DisplacedEntity> displaced,
        TextEntityPayload textPayload)
    {
        _state = state;
        _typeIndex = typeIndex;
        _cell = cell;
        _placed = placed;
        _displaced = displaced;
        _textPayload = TextEntityUtility.ClonePayload(textPayload);
    }

    public void Undo()
    {
        var config = _state.ConfigReader.GetConfigByIndex(_typeIndex);
        string entityId = config?.Id ?? "";

        // 销毁该格子上对应类型的对象（不依赖原始引用，因为它可能已被后续删除操作销毁）
        if (_state.PlacedObjects.TryGetValue(_cell, out var list))
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] != null && list[i].name == entityId)
                {
                    Object.Destroy(list[i]);
                    list.RemoveAt(i);
                    break;
                }
            }
            if (list.Count == 0)
                _state.PlacedObjects.Remove(_cell);
        }

        // 从关卡数据移除
        RemoveEntityData(_typeIndex, _cell);

        // 恢复被覆盖的实体
        if (_displaced != null)
        {
            foreach (var d in _displaced)
                RestoreEntity(d.TypeIndex, d.Cell, d.TextPayload);
        }
    }

    private void RemoveEntityData(int typeIndex, Vector2Int cell)
    {
        var entities = _state.CurrentLevel.Entities;
        for (int i = entities.Count - 1; i >= 0; i--)
        {
            var e = entities[i];
            if (e.X == cell.x && e.Y == cell.y && e.Type == typeIndex)
            {
                entities.RemoveAt(i);
                break;
            }
        }
    }

    private void RestoreEntity(int typeIndex, Vector2Int cell, TextEntityPayload payloadOverride = null)
    {
        GameObject instance = _state.CreateEntity(new EntityData
        {
            Type = typeIndex,
            X = cell.x,
            Y = cell.y,
            Text = TextEntityUtility.ClonePayload(payloadOverride ?? (typeIndex == _typeIndex ? _textPayload : null))
        });
        if (instance == null) return;

        if (!_state.PlacedObjects.ContainsKey(cell))
            _state.PlacedObjects[cell] = new List<GameObject>();
        _state.PlacedObjects[cell].Add(instance);

        _state.CurrentLevel.Entities.Add(new EntityData
        {
            Type = typeIndex,
            X = cell.x,
            Y = cell.y,
            Text = TextEntityUtility.ClonePayload(payloadOverride ?? (typeIndex == _typeIndex ? _textPayload : null))
        });
    }
}
