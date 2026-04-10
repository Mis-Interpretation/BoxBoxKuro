using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 形状计算和网格边界检查的静态工具类，供笔刷模式和选择模式共用。
/// </summary>
public static class EditorShapeHelper
{
    public static bool IsInBounds(Vector2Int cell, int width, int height)
    {
        int halfW = width / 2;
        int halfH = height / 2;
        return cell.x >= -halfW && cell.x < width - halfW &&
               cell.y >= -halfH && cell.y < height - halfH;
    }

    public static void ComputeShapeCells(Vector2Int start, Vector2Int end, DrawingMode mode,
        List<Vector2Int> result, int width, int height)
    {
        switch (mode)
        {
            case DrawingMode.Line:
                ComputeLineCells(start, end, result, width, height);
                break;
            case DrawingMode.RectFill:
                ComputeRectFillCells(start, end, result, width, height);
                break;
            case DrawingMode.RectEdge:
                ComputeRectEdgeCells(start, end, result, width, height);
                break;
        }
    }

    public static void TraceLineCells(Vector2Int from, Vector2Int to,
        List<Vector2Int> cells, HashSet<Vector2Int> set, int width, int height)
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
            if (IsInBounds(c, width, height) && set.Add(c))
                cells.Add(c);

            if (cx == to.x && cy == to.y) break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; cx += sx; }
            if (e2 < dx) { err += dx; cy += sy; }
        }
    }

    public static void ComputeLineCells(Vector2Int start, Vector2Int end,
        List<Vector2Int> result, int width, int height)
    {
        int dx = Mathf.Abs(end.x - start.x);
        int dy = Mathf.Abs(end.y - start.y);

        if (dx >= dy)
        {
            int minX = Mathf.Min(start.x, end.x);
            int maxX = Mathf.Max(start.x, end.x);
            for (int x = minX; x <= maxX; x++)
            {
                var c = new Vector2Int(x, start.y);
                if (IsInBounds(c, width, height)) result.Add(c);
            }
        }
        else
        {
            int minY = Mathf.Min(start.y, end.y);
            int maxY = Mathf.Max(start.y, end.y);
            for (int y = minY; y <= maxY; y++)
            {
                var c = new Vector2Int(start.x, y);
                if (IsInBounds(c, width, height)) result.Add(c);
            }
        }
    }

    public static void ComputeRectFillCells(Vector2Int start, Vector2Int end,
        List<Vector2Int> result, int width, int height)
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
                if (IsInBounds(c, width, height)) result.Add(c);
            }
        }
    }

    public static void ComputeRectEdgeCells(Vector2Int start, Vector2Int end,
        List<Vector2Int> result, int width, int height)
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
                    if (IsInBounds(c, width, height)) result.Add(c);
                }
            }
        }
    }

    public static Vector2Int GetMouseGridCell(Camera camera)
    {
        Vector3 worldPos = camera.ScreenToWorldPoint(Input.mousePosition);
        return new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
    }
}
