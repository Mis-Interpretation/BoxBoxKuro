using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 选择模式控制器：处理框选、拖动移动、Delete 删除、Shift 多选。
/// 仅在 EditorMode.Select 时激活。
/// </summary>
public class EditorSelectionController : MonoBehaviour
{
    private EditorStateModel _state;
    private EditorPlacementController _placement;
    private EditorMetadataController _metadata;
    private EditorUndoController _undo;

    private Vector2Int _selectDragStart;
    private Vector2Int _moveDragStart;

    // 右键擦除拖拽状态
    private bool _isEraseDragging;
    private Vector2Int _eraseDragStart;
    private Vector2Int _lastEraseTrackedCell;
    private readonly List<Vector2Int> _erasePreviewCells = new List<Vector2Int>();
    private readonly HashSet<Vector2Int> _erasePreviewSet = new HashSet<Vector2Int>();

    /// <summary>擦除拖拽预览格子（供 EditorGridView 读取）。</summary>
    public IReadOnlyList<Vector2Int> ErasePreviewCells => _erasePreviewCells;

    /// <summary>是否正在擦除拖拽。</summary>
    public bool IsEraseDragging => _isEraseDragging;

    private void Awake()
    {
        _state = FindAnyObjectByType<EditorStateModel>();
        _placement = FindAnyObjectByType<EditorPlacementController>();
        _metadata = FindAnyObjectByType<EditorMetadataController>();
        _undo = FindAnyObjectByType<EditorUndoController>();
    }

    private void Update()
    {
        if (_state.CurrentEditorMode != EditorMode.Select) return;

        // Delete 键删除选中
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            DeleteSelected();
            return;
        }

        // 鼠标在 UI 上时不处理
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2Int cell = EditorShapeHelper.GetMouseGridCell(Camera.main);
        var sel = _state.Selection;
        int w = _state.CurrentLevel.Width;
        int h = _state.CurrentLevel.Height;

        // ── 左键：选择 / 拖动 ──
        if (Input.GetMouseButtonDown(0))
        {
            // 左键按下时取消擦除拖拽
            if (_isEraseDragging)
            {
                _isEraseDragging = false;
                _erasePreviewCells.Clear();
                _erasePreviewSet.Clear();
            }
            OnMouseDown(cell, sel);
        }

        if (sel.IsDraggingSelection)
            UpdateSelectionDrag(cell, sel);
        else if (sel.IsDraggingMove)
            UpdateMoveDrag(cell, sel);

        if (Input.GetMouseButtonUp(0))
            OnMouseUp(cell, sel);

        // ── 右键：擦除（和笔刷模式行为一致） ──
        if (Input.GetMouseButtonDown(1) && EditorShapeHelper.IsInBounds(cell, w, h))
        {
            // 右键按下时取消左键拖拽
            if (sel.IsDraggingSelection) sel.ClearPreview();
            if (sel.IsDraggingMove) sel.ClearMove();

            if (_state.CurrentSelectShapeMode == DrawingMode.Point)
            {
                // 单点直接删除
                var cmd = _placement.RemoveEntity(cell);
                if (cmd != null) _undo?.Record(cmd);
            }
            else
            {
                _isEraseDragging = true;
                _eraseDragStart = cell;
                _lastEraseTrackedCell = cell;
                _erasePreviewCells.Clear();
                _erasePreviewSet.Clear();

                if (_state.CurrentSelectShapeMode == DrawingMode.Line)
                {
                    _erasePreviewSet.Add(cell);
                    _erasePreviewCells.Add(cell);
                }
            }
        }

        // 擦除拖拽中更新预览
        if (_isEraseDragging)
        {
            if (_state.CurrentSelectShapeMode == DrawingMode.Line)
            {
                EditorShapeHelper.TraceLineCells(_lastEraseTrackedCell, cell,
                    _erasePreviewCells, _erasePreviewSet, w, h);
                _lastEraseTrackedCell = cell;
            }
            else
            {
                _erasePreviewCells.Clear();
                EditorShapeHelper.ComputeShapeCells(_eraseDragStart, cell,
                    _state.CurrentSelectShapeMode, _erasePreviewCells, w, h);
            }
        }

        // 右键松开：提交擦除
        if (Input.GetMouseButtonUp(1) && _isEraseDragging)
        {
            var eraseCmds = new List<IEditorCommand>();
            foreach (var c in _erasePreviewCells)
            {
                var cmd = _placement.RemoveEntity(c);
                if (cmd != null) eraseCmds.Add(cmd);
            }
            if (eraseCmds.Count > 0)
                _undo?.Record(new CompositeCommand(eraseCmds));

            _isEraseDragging = false;
            _erasePreviewCells.Clear();
            _erasePreviewSet.Clear();

            // 擦除后也清除对应的选中格子
            sel.SelectedCells.RemoveWhere(c =>
                !_state.PlacedObjects.ContainsKey(c) || _state.PlacedObjects[c].Count == 0);
        }
    }

    private void OnMouseDown(Vector2Int cell, SelectionModel sel)
    {
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool cellHasObject = _state.TryResolveSelectionAnchor(cell, out var anchorCell)
            && _state.PlacedObjects.ContainsKey(anchorCell)
            && _state.PlacedObjects[anchorCell].Count > 0;
        bool cellIsSelected = cellHasObject && sel.SelectedCells.Contains(anchorCell);

        if (_state.CurrentSelectShapeMode == DrawingMode.Point)
        {
            // 单点模式：点击已选区域 → 拖动；点击未选区域 → 选中后可拖动
            if (cellIsSelected && cellHasObject)
            {
                BeginMove(anchorCell, sel);
            }
            else
            {
                if (!shift) sel.ClearSelection();
                if (cellHasObject)
                {
                    sel.SelectedCells.Add(anchorCell);
                    // 单点模式长按自动视为拖拽
                    BeginMove(anchorCell, sel);
                }
            }
        }
        else
        {
            // 非单点模式：点击已选区域 → 拖动；点击未选区域 → 开始框选
            if (cellIsSelected && cellHasObject)
            {
                BeginMove(anchorCell, sel);
            }
            else
            {
                if (!shift) sel.ClearSelection();
                BeginSelectionDrag(cell, sel);
            }
        }
    }

    private void BeginMove(Vector2Int cell, SelectionModel sel)
    {
        sel.IsDraggingMove = true;
        sel.MoveOffset = Vector2Int.zero;
        _moveDragStart = cell;
    }

    private void BeginSelectionDrag(Vector2Int cell, SelectionModel sel)
    {
        sel.IsDraggingSelection = true;
        _selectDragStart = cell;
        sel.SelectionPreviewCells.Clear();
        sel.SelectionPreviewSet.Clear();
        sel.LastTrackedCell = cell;

        if (_state.CurrentSelectShapeMode == DrawingMode.Line)
        {
            sel.SelectionPreviewSet.Add(cell);
            sel.SelectionPreviewCells.Add(cell);
        }
    }

    private void UpdateSelectionDrag(Vector2Int cell, SelectionModel sel)
    {
        int w = _state.CurrentLevel.Width;
        int h = _state.CurrentLevel.Height;

        if (_state.CurrentSelectShapeMode == DrawingMode.Line)
        {
            EditorShapeHelper.TraceLineCells(sel.LastTrackedCell, cell,
                sel.SelectionPreviewCells, sel.SelectionPreviewSet, w, h);
            sel.LastTrackedCell = cell;
        }
        else
        {
            sel.SelectionPreviewCells.Clear();
            EditorShapeHelper.ComputeShapeCells(_selectDragStart, cell,
                _state.CurrentSelectShapeMode, sel.SelectionPreviewCells, w, h);
        }
    }

    private void UpdateMoveDrag(Vector2Int cell, SelectionModel sel)
    {
        sel.MoveOffset = cell - _moveDragStart;
    }

    private void OnMouseUp(Vector2Int cell, SelectionModel sel)
    {
        if (sel.IsDraggingSelection)
        {
            // 提交框选：将预览格子中有实体的加入选中集合
            foreach (var c in sel.SelectionPreviewCells)
            {
                if (_state.TryResolveSelectionAnchor(c, out var anchorCell))
                    sel.SelectedCells.Add(anchorCell);
            }
            sel.ClearPreview();
        }
        else if (sel.IsDraggingMove)
        {
            if (sel.MoveOffset != Vector2Int.zero)
            {
                CommitMove(sel);
            }
            sel.ClearMove();
        }
    }

    private void CommitMove(SelectionModel sel)
    {
        var moved = new List<SelectMoveCommand.MovedEntity>();
        var displaced = new List<PlaceEntityCommand.DisplacedEntity>();

        // 记录移动前的选中格子，用于 Undo 时恢复
        var selectedBeforeMove = new HashSet<Vector2Int>(sel.SelectedCells);

        // 收集所有要移动的实体信息
        var entitiesToMove = new List<(int typeIndex, Vector2Int fromCell, TextEntityPayload textPayload)>();
        foreach (var fromCell in sel.SelectedCells)
        {
            if (!_state.PlacedObjects.TryGetValue(fromCell, out var objects)) continue;
            foreach (var obj in objects)
            {
                if (obj == null) continue;
                var config = _state.ConfigReader.GetConfig(obj.name);
                if (config == null) continue;
                var runtime = obj.GetComponent<TextEntityRuntimeModel>();
                entitiesToMove.Add((config.TypeIndex, fromCell, TextEntityUtility.ClonePayload(runtime?.Payload)));
            }
        }

        // 目标格子集合（排除自身移动的格子）
        var targetCells = new HashSet<Vector2Int>();
        foreach (var fromCell in sel.SelectedCells)
            targetCells.Add(fromCell + sel.MoveOffset);

        // 检查目标位置是否在界内
        int w = _state.CurrentLevel.Width;
        int h = _state.CurrentLevel.Height;
        foreach (var tc in targetCells)
        {
            if (!EditorShapeHelper.IsInBounds(tc, w, h)) return; // 有超界的，取消移动
        }

        // Text footprint 额外边界与冲突检查（仅编辑器占格，不影响玩法）。
        var movingText = new List<(Vector2Int toCell, TextEntityPayload payload, Vector2Int fromCell)>();
        foreach (var (_, fromCell, textPayload) in entitiesToMove)
        {
            if (textPayload == null) continue;
            movingText.Add((fromCell + sel.MoveOffset, textPayload, fromCell));
        }

        foreach (var (toCell, payload, _) in movingText)
        {
            foreach (var covered in TextEntityUtility.EnumerateFootprint(toCell, payload))
            {
                if (!EditorShapeHelper.IsInBounds(covered, w, h))
                    return;
            }
        }

        for (int i = 0; i < movingText.Count; i++)
        {
            foreach (var kvp in _state.PlacedObjects)
            {
                if (sel.SelectedCells.Contains(kvp.Key)) continue;
                foreach (var obj in kvp.Value)
                {
                    if (obj == null) continue;
                    var runtime = obj.GetComponent<TextEntityRuntimeModel>();
                    if (runtime == null) continue;

                    foreach (var covered in TextEntityUtility.EnumerateFootprint(movingText[i].toCell, movingText[i].payload))
                    {
                        if (TextEntityUtility.ContainsCell(kvp.Key, runtime.Payload, covered))
                            return;
                    }
                }
            }
        }

        for (int i = 0; i < movingText.Count; i++)
        {
            for (int j = i + 1; j < movingText.Count; j++)
            {
                foreach (var covered in TextEntityUtility.EnumerateFootprint(movingText[i].toCell, movingText[i].payload))
                {
                    if (TextEntityUtility.ContainsCell(movingText[j].toCell, movingText[j].payload, covered))
                        return;
                }
            }
        }

        // 先删除原位置所有选中实体的 GameObject 和数据
        foreach (var fromCell in sel.SelectedCells)
        {
            if (!_state.PlacedObjects.TryGetValue(fromCell, out var objects)) continue;
            foreach (var obj in objects)
            {
                if (obj != null) Object.Destroy(obj);
            }
            _state.PlacedObjects.Remove(fromCell);
        }

        // 从 Entities 中删除对应数据
        foreach (var (typeIndex, fromCell, _) in entitiesToMove)
        {
            RemoveEntityData(typeIndex, fromCell);
        }

        // 在目标位置检测并记录被覆盖的实体，然后放置
        foreach (var (typeIndex, fromCell, textPayload) in entitiesToMove)
        {
            var toCell = fromCell + sel.MoveOffset;
            var config = _state.ConfigReader.GetConfigByIndex(typeIndex);
            if (config == null) continue;

            bool newCanOverlap = _state.ConfigReader.HasComponent(config.Id, "OverlappableModel");

            // 处理重叠
            if (!newCanOverlap && _state.PlacedObjects.TryGetValue(toCell, out var existing))
            {
                for (int i = existing.Count - 1; i >= 0; i--)
                {
                    var obj = existing[i];
                    if (obj == null) { existing.RemoveAt(i); continue; }
                    if (obj.GetComponent<OverlappableModel>() != null) continue;

                    var oldConfig = _state.ConfigReader.GetConfig(obj.name);
                    int oldTypeIndex = oldConfig?.TypeIndex ?? 0;
                    var oldRuntime = obj.GetComponent<TextEntityRuntimeModel>();
                    displaced.Add(new PlaceEntityCommand.DisplacedEntity
                    {
                        TypeIndex = oldTypeIndex,
                        Cell = toCell,
                        TextPayload = TextEntityUtility.ClonePayload(oldRuntime?.Payload)
                    });
                    Object.Destroy(obj);
                    existing.RemoveAt(i);
                    RemoveEntityData(oldTypeIndex, toCell);
                }
            }

            // 创建新实体
            var entityData = new EntityData
            {
                Type = typeIndex,
                X = toCell.x,
                Y = toCell.y,
                Text = TextEntityUtility.ClonePayload(textPayload)
            };

            GameObject instance = _state.CreateEntity(entityData);
            if (instance == null) continue;

            if (!_state.PlacedObjects.ContainsKey(toCell))
                _state.PlacedObjects[toCell] = new List<GameObject>();
            _state.PlacedObjects[toCell].Add(instance);

            _state.CurrentLevel.Entities.Add(entityData);

            moved.Add(new SelectMoveCommand.MovedEntity
            {
                TypeIndex = typeIndex,
                FromCell = fromCell,
                ToCell = toCell,
                TextPayload = TextEntityUtility.ClonePayload(textPayload)
            });
        }

        if (moved.Count > 0)
        {
            _undo?.Record(new SelectMoveCommand(_state, moved, displaced.Count > 0 ? displaced : null, selectedBeforeMove));
            _metadata?.ClearSolvable();
        }

        // 更新选中集合为移动后的位置
        var newSelected = new HashSet<Vector2Int>();
        foreach (var c in sel.SelectedCells)
            newSelected.Add(c + sel.MoveOffset);
        sel.SelectedCells = newSelected;
    }

    private void DeleteSelected()
    {
        var sel = _state.Selection;
        if (sel.SelectedCells.Count == 0) return;

        var cmds = new List<IEditorCommand>();

        foreach (var cell in sel.SelectedCells)
        {
            if (!_state.PlacedObjects.TryGetValue(cell, out var objects)) continue;

            // 倒序删除该格子上所有实体
            for (int i = objects.Count - 1; i >= 0; i--)
            {
                var obj = objects[i];
                if (obj == null) continue;

                var config = _state.ConfigReader.GetConfig(obj.name);
                int typeIndex = config?.TypeIndex ?? 0;
                var runtime = obj.GetComponent<TextEntityRuntimeModel>();

                Object.Destroy(obj);
                objects.RemoveAt(i);
                RemoveEntityData(typeIndex, cell);

                cmds.Add(new RemoveEntityCommand(_state, typeIndex, cell, TextEntityUtility.ClonePayload(runtime?.Payload)));
            }

            if (objects.Count == 0)
                _state.PlacedObjects.Remove(cell);
        }

        if (cmds.Count > 0)
        {
            _undo?.Record(new CompositeCommand(cmds));
            _metadata?.ClearSolvable();
        }

        sel.ClearSelection();
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
}
