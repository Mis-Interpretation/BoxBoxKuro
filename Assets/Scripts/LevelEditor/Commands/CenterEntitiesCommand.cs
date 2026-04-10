using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 记录居中操作的偏移量，可撤销。
/// </summary>
public class CenterEntitiesCommand : IEditorCommand
{
    private readonly EditorStateModel _state;
    private readonly int _offsetX;
    private readonly int _offsetY;

    public CenterEntitiesCommand(EditorStateModel state, int offsetX, int offsetY)
    {
        _state = state;
        _offsetX = offsetX;
        _offsetY = offsetY;
    }

    public void Undo()
    {
        // 反向偏移
        foreach (var e in _state.CurrentLevel.Entities)
        {
            e.X -= _offsetX;
            e.Y -= _offsetY;
        }

        var oldPlaced = new Dictionary<Vector2Int, List<GameObject>>(_state.PlacedObjects);
        _state.PlacedObjects.Clear();

        foreach (var kvp in oldPlaced)
        {
            var newKey = new Vector2Int(kvp.Key.x - _offsetX, kvp.Key.y - _offsetY);
            foreach (var obj in kvp.Value)
            {
                if (obj == null) continue;
                obj.transform.position = new Vector3(newKey.x, newKey.y, 0f);
                var posModel = obj.GetComponent<PositionModel>();
                if (posModel != null) posModel.GridPosition = newKey;
            }
            _state.PlacedObjects[newKey] = kvp.Value;
        }
    }
}
