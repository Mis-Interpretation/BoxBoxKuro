using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 选择模式的运行时状态：已选中格子、选择预览、拖动状态。
/// </summary>
public class SelectionModel
{
    /// <summary>已选中的格子集合。</summary>
    public HashSet<Vector2Int> SelectedCells = new HashSet<Vector2Int>();

    /// <summary>当前正在框选的预览格子。</summary>
    public List<Vector2Int> SelectionPreviewCells = new List<Vector2Int>();

    /// <summary>是否正在框选。</summary>
    public bool IsDraggingSelection;

    /// <summary>是否正在拖动已选物体。</summary>
    public bool IsDraggingMove;

    /// <summary>拖动起始格子。</summary>
    public Vector2Int DragStartCell;

    /// <summary>当前拖动偏移量。</summary>
    public Vector2Int MoveOffset;

    /// <summary>Line 模式追踪用的去重集合。</summary>
    public HashSet<Vector2Int> SelectionPreviewSet = new HashSet<Vector2Int>();

    /// <summary>Line 模式追踪用的上一个格子。</summary>
    public Vector2Int LastTrackedCell;

    public void ClearSelection()
    {
        SelectedCells.Clear();
    }

    public void ClearPreview()
    {
        SelectionPreviewCells.Clear();
        SelectionPreviewSet.Clear();
        IsDraggingSelection = false;
    }

    public void ClearMove()
    {
        IsDraggingMove = false;
        MoveOffset = Vector2Int.zero;
    }

    public void ClearAll()
    {
        ClearSelection();
        ClearPreview();
        ClearMove();
    }
}
