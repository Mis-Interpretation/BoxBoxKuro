using UnityEngine;

/// <summary>
/// View：根据 PositionModel 的网格坐标同步 GameObject 的世界坐标。
/// </summary>
public class EntityView : MonoBehaviour
{
    [Tooltip("每个网格单元的世界大小")]
    public float CellSize = 1f;

    [Tooltip("移动的平滑速度，0 表示瞬移")]
    public float SmoothSpeed = 10f;

    [Tooltip("指定 SpriteRenderer，留空则自动从 GameObject 获取")]
    public SpriteRenderer SpriteRenderer;

    private PositionModel _position;
    private bool _initialized;

    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;
        _position = GetComponent<PositionModel>();
        if (SpriteRenderer == null)
            SpriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 设置 Sprite 并按 CellSize 缩放到网格大小。
    /// </summary>
    public void InitSprite(Sprite sprite, int orderInLayer = 0)
    {
        EnsureInitialized();
        if (SpriteRenderer == null || sprite == null) return;

        SpriteRenderer.sprite = sprite;
        SpriteRenderer.sortingOrder = orderInLayer;
        Vector2 spriteSize = SpriteRenderer.bounds.size;
        SpriteRenderer.transform.localScale = new Vector3(
            CellSize / spriteSize.x,
            CellSize / spriteSize.y,
            1f
        );
    }

    private void LateUpdate()
    {
        EnsureInitialized();
        if (_position == null) return;

        Vector3 targetWorld = new Vector3(
            _position.GridPosition.x * CellSize,
            _position.GridPosition.y * CellSize,
            0f
        );

        if (SmoothSpeed <= 0f)
        {
            transform.position = targetWorld;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetWorld, SmoothSpeed * Time.deltaTime);
        }
    }
}
