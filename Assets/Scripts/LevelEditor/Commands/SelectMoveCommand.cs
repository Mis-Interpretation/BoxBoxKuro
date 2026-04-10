using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 记录一次选择模式下的移动操作，可撤销：将实体移回原位并恢复被覆盖的实体。
/// </summary>
public class SelectMoveCommand : IEditorCommand
{
    public struct MovedEntity
    {
        public int TypeIndex;
        public Vector2Int FromCell;
        public Vector2Int ToCell;
        public TextEntityPayload TextPayload;
    }

    private readonly EditorStateModel _state;
    private readonly List<MovedEntity> _moved;
    private readonly List<PlaceEntityCommand.DisplacedEntity> _displaced;
    private readonly HashSet<Vector2Int> _selectedAfterMove;

    public SelectMoveCommand(
        EditorStateModel state,
        List<MovedEntity> moved,
        List<PlaceEntityCommand.DisplacedEntity> displaced,
        HashSet<Vector2Int> selectedBeforeMove)
    {
        _state = state;
        _moved = moved;
        _displaced = displaced;
        // 记录移动前的选中格子，用于 Undo 时恢复
        _selectedAfterMove = selectedBeforeMove;
    }

    public void Undo()
    {
        // 1. 删除移动后位置的实体
        foreach (var m in _moved)
        {
            DestroyAt(m.TypeIndex, m.ToCell);
            RemoveEntityData(m.TypeIndex, m.ToCell);
        }

        // 2. 恢复移动前位置的实体
        foreach (var m in _moved)
        {
            RestoreEntity(m.TypeIndex, m.FromCell, m.TextPayload);
        }

        // 3. 恢复被覆盖的实体
        if (_displaced != null)
        {
            foreach (var d in _displaced)
                RestoreEntity(d.TypeIndex, d.Cell, d.TextPayload);
        }

        // 4. 将选中格子恢复到移动前的位置
        if (_selectedAfterMove != null)
        {
            _state.Selection.SelectedCells = new HashSet<Vector2Int>(_selectedAfterMove);
        }
    }

    private void DestroyAt(int typeIndex, Vector2Int cell)
    {
        var config = _state.ConfigReader.GetConfigByIndex(typeIndex);
        string entityId = config?.Id ?? "";

        if (_state.PlacedObjects.TryGetValue(cell, out var list))
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
                _state.PlacedObjects.Remove(cell);
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

    private void RestoreEntity(int typeIndex, Vector2Int cell, TextEntityPayload textPayload)
    {
        var entityData = new EntityData
        {
            Type = typeIndex,
            X = cell.x,
            Y = cell.y,
            Text = TextEntityUtility.ClonePayload(textPayload)
        };

        GameObject instance = _state.CreateEntity(entityData);
        if (instance == null) return;

        if (!_state.PlacedObjects.ContainsKey(cell))
            _state.PlacedObjects[cell] = new List<GameObject>();
        _state.PlacedObjects[cell].Add(instance);

        _state.CurrentLevel.Entities.Add(entityData);
    }
}
