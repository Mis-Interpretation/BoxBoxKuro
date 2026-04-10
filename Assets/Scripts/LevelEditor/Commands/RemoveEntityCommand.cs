using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 记录一次移除操作，可撤销：重新生成被移除的实体。
/// </summary>
public class RemoveEntityCommand : IEditorCommand
{
    private readonly EditorStateModel _state;
    private readonly int _typeIndex;
    private readonly Vector2Int _cell;
    private readonly TextEntityPayload _textPayload;

    public RemoveEntityCommand(EditorStateModel state, int typeIndex, Vector2Int cell, TextEntityPayload textPayload = null)
    {
        _state = state;
        _typeIndex = typeIndex;
        _cell = cell;
        _textPayload = TextEntityUtility.ClonePayload(textPayload);
    }

    public void Undo()
    {
        var entityData = new EntityData
        {
            Type = _typeIndex,
            X = _cell.x,
            Y = _cell.y,
            Text = TextEntityUtility.ClonePayload(_textPayload)
        };

        GameObject instance = _state.CreateEntity(entityData);
        if (instance == null) return;

        if (!_state.PlacedObjects.ContainsKey(_cell))
            _state.PlacedObjects[_cell] = new List<GameObject>();
        _state.PlacedObjects[_cell].Add(instance);

        _state.CurrentLevel.Entities.Add(entityData);
    }
}
