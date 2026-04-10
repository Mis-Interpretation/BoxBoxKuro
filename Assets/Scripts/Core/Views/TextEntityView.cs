using TMPro;
using UnityEngine;

/// <summary>
/// Text 实体视图：同步网格位置并应用实例级文本参数。
/// </summary>
public class TextEntityView : MonoBehaviour
{
    public float CellSize = 1f;
    public float SmoothSpeed = 10f;

    private PositionModel _position;
    private TextMeshPro _tmp;
    private bool _initialized;

    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        _position = GetComponent<PositionModel>();
        _tmp = GetComponent<TextMeshPro>();
        if (_tmp == null)
            _tmp = gameObject.AddComponent<TextMeshPro>();
    }

    public void ApplyText(TextEntityPayload payload, int orderInLayer)
    {
        EnsureInitialized();
        if (payload == null || _tmp == null) return;

        payload.EnsureValid();

        _tmp.text = payload.Content;
        _tmp.fontSize = payload.FontSize * 2f;
        _tmp.alignment = TextAlignmentOptions.Center;
        _tmp.enableWordWrapping = false;
        _tmp.sortingOrder = orderInLayer;

        // Text 固定占 1x1 格，视觉溢出由字体大小自然产生。
        transform.localScale = new Vector3(CellSize, CellSize, 1f);
    }

    private void LateUpdate()
    {
        EnsureInitialized();
        if (_position == null) return;

        Vector3 targetWorld = new Vector3(_position.GridPosition.x * CellSize, _position.GridPosition.y * CellSize, 0f);
        if (SmoothSpeed <= 0f)
            transform.position = targetWorld;
        else
            transform.position = Vector3.Lerp(transform.position, targetWorld, SmoothSpeed * Time.deltaTime);
    }
}
