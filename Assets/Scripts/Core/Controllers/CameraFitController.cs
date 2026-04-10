using UnityEngine;

/// <summary>
/// 自动调整正交相机的位置和大小，使整个关卡可见。
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFitController : MonoBehaviour
{
    [Tooltip("关卡四周预留的额外格子数")]
    public int Margin = 1;

    public void FitToLevel(LevelDataModel levelData)
    {
        Vector2 center = CalculateCenter(levelData, out float spanX, out float spanY);
        transform.position = new Vector3(center.x, center.y, transform.position.z);

        float totalWidth = spanX + 1 + Margin * 2;
        float totalHeight = spanY + 1 + Margin * 2;

        Camera cam = GetComponent<Camera>();
        float verticalSize = totalHeight / 2f;
        float horizontalSize = totalWidth / (2f * cam.aspect);
        cam.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
    }

    Vector2 CalculateCenter(LevelDataModel levelData, out float spanX, out float spanY)
    {
        if (levelData.Entities.Count == 0)
        {
            spanX = levelData.Width;
            spanY = levelData.Height;
            return new Vector2((levelData.Width - 1) / 2f, (levelData.Height - 1) / 2f);
        }

        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var e in levelData.Entities)
        {
            if (e.X < minX) minX = e.X;
            if (e.X > maxX) maxX = e.X;
            if (e.Y < minY) minY = e.Y;
            if (e.Y > maxY) maxY = e.Y;
        }

        spanX = maxX - minX;
        spanY = maxY - minY;
        return new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
    }
}
