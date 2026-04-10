using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 处理鼠标点击 → 在网格上放置/移除实体。
/// 支持 Point / Line / RectFill / RectEdge 四种绘制模式。
/// </summary>
public class EditorPlacementController : MonoBehaviour
{
    public readonly struct CenterResult
    {
        public readonly bool HasEntities;
        public readonly int OffsetX;
        public readonly int OffsetY;

        public bool Applied => HasEntities && (OffsetX != 0 || OffsetY != 0);

        public CenterResult(bool hasEntities, int offsetX, int offsetY)
        {
            HasEntities = hasEntities;
            OffsetX = offsetX;
            OffsetY = offsetY;
        }
    }

    private EditorStateModel _state;
    private EditorMetadataController _metadata;
    private EditorUndoController _undo;

    private static int RoundHalfAwayFromZero(int value)
    {
        if (value >= 0)
            return (value + 1) / 2;

        int abs = -value;
        return -((abs + 1) / 2);
    }

    private bool _isDragging;
    private Vector2Int _dragStartCell;
    private Vector2Int _lastTrackedCell;
    private readonly List<Vector2Int> _previewCells = new List<Vector2Int>();
    private readonly HashSet<Vector2Int> _previewSet = new HashSet<Vector2Int>();
    private readonly List<IEditorCommand> _dragCommands = new List<IEditorCommand>();

    private bool _isEraseDragging;
    private Vector2Int _eraseDragStartCell;
    private Vector2Int _lastEraseTrackedCell;
    private readonly List<Vector2Int> _erasePreviewCells = new List<Vector2Int>();
    private readonly HashSet<Vector2Int> _erasePreviewSet = new HashSet<Vector2Int>();
    private readonly List<IEditorCommand> _eraseCommands = new List<IEditorCommand>();

    /// <summary>
    /// 拖拽预览中的格子列表（供 EditorGridView 读取渲染）。
    /// </summary>
    public IReadOnlyList<Vector2Int> PreviewCells => _previewCells;

    /// <summary>
    /// 当前是否正在拖拽绘制。
    /// </summary>
    public bool IsDragging => _isDragging;

    /// <summary>
    /// 擦除拖拽预览中的格子列表（供 EditorGridView 读取渲染）。
    /// </summary>
    public IReadOnlyList<Vector2Int> ErasePreviewCells => _erasePreviewCells;

    /// <summary>
    /// 当前是否正在擦除拖拽。
    /// </summary>
    public bool IsEraseDragging => _isEraseDragging;

    private void Awake()
    {
        _state = FindAnyObjectByType<EditorStateModel>();
        _metadata = FindAnyObjectByType<EditorMetadataController>();
        _undo = FindAnyObjectByType<EditorUndoController>();
    }

    private void Update()
    {
        // 仅在笔刷模式下激活
        if (_state.CurrentEditorMode != EditorMode.Brush) return;

        // 鼠标在 UI 上时不处理
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2Int cell = GetMouseGridCell();

        if (_state.CurrentDrawingMode == DrawingMode.Point)
        {
            UpdatePointMode(cell);
        }
        else
        {
            UpdateDragMode(cell);
        }
    }

    private void UpdatePointMode(Vector2Int cell)
    {
        if (!IsInBounds(cell)) return;

        // 左键放置
        if (Input.GetMouseButtonDown(0))
        {
            var cmd = PlaceEntity(_state.SelectedTypeIndex, cell);
            if (cmd != null) _undo?.Record(cmd);
        }

        // 右键移除
        if (Input.GetMouseButtonDown(1))
        {
            var cmd = RemoveEntity(cell);
            if (cmd != null) _undo?.Record(cmd);
        }
    }

    private void UpdateDragMode(Vector2Int cell)
    {
        // 左键按下 → 开始放置拖拽（如果正在擦除拖拽则取消）
        if (Input.GetMouseButtonDown(0) && IsInBounds(cell))
        {
            if (_isEraseDragging)
            {
                _isEraseDragging = false;
                _erasePreviewCells.Clear();
                _erasePreviewSet.Clear();
            }

            _isDragging = true;
            _dragStartCell = cell;
            _lastTrackedCell = cell;
            _previewCells.Clear();
            _previewSet.Clear();

            if (_state.CurrentDrawingMode == DrawingMode.Line)
            {
                _previewSet.Add(cell);
                _previewCells.Add(cell);
            }
        }

        // 右键按下 → 开始擦除拖拽（如果正在放置拖拽则取消）
        if (Input.GetMouseButtonDown(1) && IsInBounds(cell))
        {
            if (_isDragging)
            {
                _isDragging = false;
                _previewCells.Clear();
                _previewSet.Clear();
            }

            _isEraseDragging = true;
            _eraseDragStartCell = cell;
            _lastEraseTrackedCell = cell;
            _erasePreviewCells.Clear();
            _erasePreviewSet.Clear();

            if (_state.CurrentDrawingMode == DrawingMode.Line)
            {
                _erasePreviewSet.Add(cell);
                _erasePreviewCells.Add(cell);
            }
        }

        // 放置拖拽中 → 更新预览
        if (_isDragging)
        {
            if (_state.CurrentDrawingMode == DrawingMode.Line)
            {
                TraceLineCells(_lastTrackedCell, cell, _previewCells, _previewSet);
                _lastTrackedCell = cell;
            }
            else
            {
                _previewCells.Clear();
                ComputeShapeCells(_dragStartCell, cell, _state.CurrentDrawingMode, _previewCells);
            }
        }

        // 擦除拖拽中 → 更新预览
        if (_isEraseDragging)
        {
            if (_state.CurrentDrawingMode == DrawingMode.Line)
            {
                TraceLineCells(_lastEraseTrackedCell, cell, _erasePreviewCells, _erasePreviewSet);
                _lastEraseTrackedCell = cell;
            }
            else
            {
                _erasePreviewCells.Clear();
                ComputeShapeCells(_eraseDragStartCell, cell, _state.CurrentDrawingMode, _erasePreviewCells);
            }
        }

        // 左键松开 → 提交放置
        if (Input.GetMouseButtonUp(0) && _isDragging)
        {
            _dragCommands.Clear();
            foreach (var c in _previewCells)
            {
                var cmd = PlaceEntity(_state.SelectedTypeIndex, c);
                if (cmd != null) _dragCommands.Add(cmd);
            }
            if (_dragCommands.Count > 0)
                _undo?.Record(new CompositeCommand(new List<IEditorCommand>(_dragCommands)));
            _dragCommands.Clear();
            _isDragging = false;
            _previewCells.Clear();
            _previewSet.Clear();
        }

        // 右键松开 → 提交擦除
        if (Input.GetMouseButtonUp(1) && _isEraseDragging)
        {
            _eraseCommands.Clear();
            foreach (var c in _erasePreviewCells)
            {
                var cmd = RemoveEntity(c);
                if (cmd != null) _eraseCommands.Add(cmd);
            }
            if (_eraseCommands.Count > 0)
                _undo?.Record(new CompositeCommand(new List<IEditorCommand>(_eraseCommands)));
            _eraseCommands.Clear();
            _isEraseDragging = false;
            _erasePreviewCells.Clear();
            _erasePreviewSet.Clear();
        }
    }

    private void ComputeShapeCells(Vector2Int start, Vector2Int end, DrawingMode mode, List<Vector2Int> result)
    {
        switch (mode)
        {
            case DrawingMode.Line:
                ComputeLineCells(start, end, result);
                break;
            case DrawingMode.RectFill:
                ComputeRectFillCells(start, end, result);
                break;
            case DrawingMode.RectEdge:
                ComputeRectEdgeCells(start, end, result);
                break;
        }
    }

    /// <summary>
    /// 从 from 到 to 逐格追踪，将经过的格子加入预览（去重）。
    /// 使用 Bresenham 风格遍历，防止鼠标快速移动时跳格。
    /// </summary>
    private void TraceLineCells(Vector2Int from, Vector2Int to, List<Vector2Int> cells, HashSet<Vector2Int> set)
    {
        int dx = Mathf.Abs(to.x - from.x);
        int dy = Mathf.Abs(to.y - from.y);
        int sx = from.x < to.x ? 1 : -1;
        int sy = from.y < to.y ? 1 : -1;
        int err = dx - dy;

        int cx = from.x, cy = from.y;
        while (true)
        {
            var c = new Vector2Int(cx, cy);
            if (IsInBounds(c) && set.Add(c))
                cells.Add(c);

            if (cx == to.x && cy == to.y) break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; cx += sx; }
            if (e2 < dx) { err += dx; cy += sy; }
        }
    }

    private void ComputeLineCells(Vector2Int start, Vector2Int end, List<Vector2Int> result)
    {
        int dx = Mathf.Abs(end.x - start.x);
        int dy = Mathf.Abs(end.y - start.y);

        if (dx >= dy)
        {
            // 水平线
            int minX = Mathf.Min(start.x, end.x);
            int maxX = Mathf.Max(start.x, end.x);
            for (int x = minX; x <= maxX; x++)
            {
                var c = new Vector2Int(x, start.y);
                if (IsInBounds(c)) result.Add(c);
            }
        }
        else
        {
            // 垂直线
            int minY = Mathf.Min(start.y, end.y);
            int maxY = Mathf.Max(start.y, end.y);
            for (int y = minY; y <= maxY; y++)
            {
                var c = new Vector2Int(start.x, y);
                if (IsInBounds(c)) result.Add(c);
            }
        }
    }

    private void ComputeRectFillCells(Vector2Int start, Vector2Int end, List<Vector2Int> result)
    {
        int minX = Mathf.Min(start.x, end.x);
        int maxX = Mathf.Max(start.x, end.x);
        int minY = Mathf.Min(start.y, end.y);
        int maxY = Mathf.Max(start.y, end.y);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var c = new Vector2Int(x, y);
                if (IsInBounds(c)) result.Add(c);
            }
        }
    }

    private void ComputeRectEdgeCells(Vector2Int start, Vector2Int end, List<Vector2Int> result)
    {
        int minX = Mathf.Min(start.x, end.x);
        int maxX = Mathf.Max(start.x, end.x);
        int minY = Mathf.Min(start.y, end.y);
        int maxY = Mathf.Max(start.y, end.y);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (x == minX || x == maxX || y == minY || y == maxY)
                {
                    var c = new Vector2Int(x, y);
                    if (IsInBounds(c)) result.Add(c);
                }
            }
        }
    }

    private bool IsInBounds(Vector2Int cell)
    {
        int halfW = _state.CurrentLevel.Width / 2;
        int halfH = _state.CurrentLevel.Height / 2;
        return cell.x >= -halfW && cell.x < _state.CurrentLevel.Width - halfW &&
               cell.y >= -halfH && cell.y < _state.CurrentLevel.Height - halfH;
    }

    /// <summary>
    /// 在指定格子放置一个实体。
    /// 同类型不重复放置。新实体会覆盖旧的不可重叠实体。
    /// 挂有 OverlappableModel 的实体可与其他实体共存。
    /// </summary>
    public IEditorCommand PlaceEntity(int typeIndex, Vector2Int cell, TextEntityPayload explicitTextPayload = null)
    {
        var config = _state.ConfigReader.GetConfigByIndex(typeIndex);
        if (config == null) return null;
        bool isTextEntity = config.IsTextEntity;

        TextEntityPayload textPayload = null;
        if (isTextEntity)
        {
            textPayload = TextEntityUtility.ClonePayload(explicitTextPayload ?? _state.BrushTextPayload) ?? new TextEntityPayload();
            textPayload.EnsureValid();

            if (HasTextFootprintConflict(cell, textPayload))
                return null;
        }

        string entityId = config.Id;
        bool newCanOverlap = _state.ConfigReader.HasComponent(entityId, "OverlappableModel");
        List<PlaceEntityCommand.DisplacedEntity> displaced = null;

        if (_state.PlacedObjects.TryGetValue(cell, out var existing))
        {
            // 同类型已存在，跳过
            foreach (var obj in existing)
            {
                if (obj != null && obj.name == entityId)
                    return null;
            }

            // 新实体不可重叠 → 移除格子上所有不可重叠的旧实体
            if (!newCanOverlap)
            {
                for (int i = existing.Count - 1; i >= 0; i--)
                {
                    var obj = existing[i];
                    if (obj == null) { existing.RemoveAt(i); continue; }
                    if (obj.GetComponent<OverlappableModel>() != null)
                        continue; // 保留可重叠的旧实体

                    var oldConfig = _state.ConfigReader.GetConfig(obj.name);
                    int oldTypeIndex = oldConfig?.TypeIndex ?? 0;
                    var oldText = obj.GetComponent<TextEntityRuntimeModel>();
                    if (displaced == null) displaced = new List<PlaceEntityCommand.DisplacedEntity>();
                    displaced.Add(new PlaceEntityCommand.DisplacedEntity
                    {
                        TypeIndex = oldTypeIndex,
                        Cell = cell,
                        TextPayload = TextEntityUtility.ClonePayload(oldText?.Payload)
                    });
                    Destroy(obj);
                    existing.RemoveAt(i);
                    RemoveEntityData(cell, oldTypeIndex);
                }
            }
        }

        var entityData = new EntityData
        {
            Type = typeIndex,
            X = cell.x,
            Y = cell.y,
            Text = isTextEntity ? TextEntityUtility.ClonePayload(textPayload) : null
        };

        GameObject instance = _state.CreateEntity(entityData);
        if (instance == null) return null;

        // 记录到 PlacedObjects
        if (!_state.PlacedObjects.ContainsKey(cell))
            _state.PlacedObjects[cell] = new List<GameObject>();
        _state.PlacedObjects[cell].Add(instance);

        // 记录到关卡数据
        _state.CurrentLevel.Entities.Add(entityData);

        if (_metadata != null) _metadata.ClearSolvable();

        return new PlaceEntityCommand(_state, typeIndex, cell, instance, displaced, entityData.Text);
    }

    /// <summary>
    /// 移除指定格子上最后放置的实体。
    /// </summary>
    public IEditorCommand RemoveEntity(Vector2Int cell)
    {
        Vector2Int anchorCell = cell;
        if (!_state.PlacedObjects.TryGetValue(anchorCell, out var objects) || objects.Count == 0)
        {
            if (!_state.TryResolveSelectionAnchor(cell, out anchorCell) ||
                !_state.PlacedObjects.TryGetValue(anchorCell, out objects) ||
                objects.Count == 0)
                return null;
        }

        if (objects.Count == 0)
            return null;

        // 移除最后一个
        int lastIndex = objects.Count - 1;
        GameObject obj = objects[lastIndex];

        var config = _state.ConfigReader.GetConfig(obj.name);
        int typeIndex = config?.TypeIndex ?? 0;

        Destroy(obj);
        objects.RemoveAt(lastIndex);

        if (objects.Count == 0)
            _state.PlacedObjects.Remove(anchorCell);

        var removedTextPayload = RemoveEntityData(anchorCell, typeIndex);

        if (_metadata != null) _metadata.ClearSolvable();

        return new RemoveEntityCommand(_state, typeIndex, anchorCell, removedTextPayload);
    }

    /// <summary>
    /// 将所有实体整体平移，使包围盒中心对齐画布中心。
    /// </summary>
    public CenterResult CenterAllEntities()
    {
        var entities = _state.CurrentLevel.Entities;
        if (entities.Count == 0) return new CenterResult(false, 0, 0);

        // 计算包围盒
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;
        foreach (var e in entities)
        {
            if (e.X < minX) minX = e.X;
            if (e.X > maxX) maxX = e.X;
            if (e.Y < minY) minY = e.Y;
            if (e.Y > maxY) maxY = e.Y;
        }

        // X 轴保留原截断策略，避免横向在半格场景下出现“过冲”观感。
        int offsetX = -(minX + maxX) / 2;
        // Y 轴在半格时向远离 0 方向取整，避免 odd-span 场景出现“看起来没反应”。
        int offsetY = -RoundHalfAwayFromZero(minY + maxY);

        if (offsetX == 0 && offsetY == 0) return new CenterResult(true, 0, 0);

        _undo?.Record(new CenterEntitiesCommand(_state, offsetX, offsetY));

        // 更新 EntityData
        foreach (var e in entities)
        {
            e.X += offsetX;
            e.Y += offsetY;
        }

        // 重建 PlacedObjects 字典并更新 GameObject
        var oldPlaced = new Dictionary<Vector2Int, List<GameObject>>(_state.PlacedObjects);
        _state.PlacedObjects.Clear();

        foreach (var kvp in oldPlaced)
        {
            var newKey = new Vector2Int(kvp.Key.x + offsetX, kvp.Key.y + offsetY);
            foreach (var obj in kvp.Value)
            {
                if (obj == null) continue;
                obj.transform.position = new Vector3(newKey.x, newKey.y, 0f);
                var posModel = obj.GetComponent<PositionModel>();
                if (posModel != null) posModel.GridPosition = newKey;
            }
            _state.PlacedObjects[newKey] = kvp.Value;
        }

        return new CenterResult(true, offsetX, offsetY);
    }

    private bool HasTextFootprintConflict(Vector2Int anchorCell, TextEntityPayload payload)
    {
        foreach (var kvp in _state.PlacedObjects)
        {
            foreach (var obj in kvp.Value)
            {
                if (obj == null) continue;
                var runtime = obj.GetComponent<TextEntityRuntimeModel>();
                if (runtime == null) continue;

                foreach (var candidate in TextEntityUtility.EnumerateFootprint(anchorCell, payload))
                {
                    if (TextEntityUtility.ContainsCell(kvp.Key, runtime.Payload, candidate))
                        return true;
                }
            }
        }

        return false;
    }

    private TextEntityPayload RemoveEntityData(Vector2Int cell, int typeIndex)
    {
        for (int i = _state.CurrentLevel.Entities.Count - 1; i >= 0; i--)
        {
            var entity = _state.CurrentLevel.Entities[i];
            if (entity.X == cell.x && entity.Y == cell.y && entity.Type == typeIndex)
            {
                var removedText = TextEntityUtility.ClonePayload(entity.Text);
                _state.CurrentLevel.Entities.RemoveAt(i);
                return removedText;
            }
        }

        return null;
    }

    /// <summary>
    /// 清空所有已放置的实体。
    /// </summary>
    public void ClearAll()
    {
        // 记录所有实体以便撤销
        var cmds = new List<IEditorCommand>();
        foreach (var entity in _state.CurrentLevel.Entities)
        {
            cmds.Add(new RemoveEntityCommand(
                _state,
                entity.Type,
                new Vector2Int(entity.X, entity.Y),
                TextEntityUtility.ClonePayload(entity.Text)));
        }
        if (cmds.Count > 0)
            _undo?.Record(new CompositeCommand(cmds));

        foreach (var kvp in _state.PlacedObjects)
        {
            foreach (var obj in kvp.Value)
            {
                if (obj != null) Destroy(obj);
            }
        }
        _state.PlacedObjects.Clear();
        _state.CurrentLevel.Entities.Clear();
    }

    private Vector2Int GetMouseGridCell()
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
    }
}
