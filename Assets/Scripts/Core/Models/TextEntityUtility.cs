using System.Collections.Generic;
using UnityEngine;

public static class TextEntityUtility
{
    public static TextEntityPayload ClonePayload(TextEntityPayload payload)
    {
        if (payload == null) return null;

        return new TextEntityPayload
        {
            Content = payload.Content,
            FontSize = payload.FontSize,
            WidthInCells = payload.WidthInCells,
            HeightInCells = payload.HeightInCells
        };
    }

    public static IEnumerable<Vector2Int> EnumerateFootprint(Vector2Int anchorCell, TextEntityPayload payload)
    {
        if (payload == null) yield break;
        payload.EnsureValid();

        int minX = anchorCell.x - (payload.WidthInCells - 1) / 2;
        int minY = anchorCell.y - (payload.HeightInCells - 1) / 2;

        for (int x = minX; x < minX + payload.WidthInCells; x++)
        {
            for (int y = minY; y < minY + payload.HeightInCells; y++)
                yield return new Vector2Int(x, y);
        }
    }

    public static bool ContainsCell(Vector2Int anchorCell, TextEntityPayload payload, Vector2Int cell)
    {
        if (payload == null) return false;
        payload.EnsureValid();

        int minX = anchorCell.x - (payload.WidthInCells - 1) / 2;
        int minY = anchorCell.y - (payload.HeightInCells - 1) / 2;
        int maxX = minX + payload.WidthInCells - 1;
        int maxY = minY + payload.HeightInCells - 1;
        return cell.x >= minX && cell.x <= maxX && cell.y >= minY && cell.y <= maxY;
    }
}
