using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 鼠标中键按住拖拽来平移编辑器相机视角，滚轮缩放。
/// 挂在 Editor Camera 上即可（与 EditorGridView 同一对象）。
/// </summary>
[RequireComponent(typeof(Camera))]
public class EditorCameraPanView : MonoBehaviour
{
    [Header("拖拽")]
    [Tooltip("拖拽灵敏度，1 表示鼠标移动距离与世界移动距离 1:1")]
    public float DragSpeed = 1f;

    [Header("缩放")]
    [Tooltip("每次滚轮滚动的缩放量")]
    public float ZoomSpeed = 1f;
    [Tooltip("最小 orthographicSize（最大放大）")]
    public float MinZoom = 2f;
    [Tooltip("最大 orthographicSize（最大缩小）")]
    public float MaxZoom = 20f;

    private Camera _camera;
    private bool _isDragging;
    private Vector3 _lastMouseWorldPos;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Update()
    {
        // 鼠标在 UI 上时不处理缩放和拖拽
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        // ── 滚轮缩放（朝鼠标位置缩放）──
        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0f && !overUI)
        {
            // 缩放前鼠标在世界中的位置
            Vector3 mouseWorldBefore = GetMouseWorldPos();

            float newSize = _camera.orthographicSize - scroll * ZoomSpeed;
            _camera.orthographicSize = Mathf.Clamp(newSize, MinZoom, MaxZoom);

            // 缩放后鼠标在世界中的位置
            Vector3 mouseWorldAfter = GetMouseWorldPos();

            // 补偿相机位置，使缩放朝鼠标位置进行
            transform.position += mouseWorldBefore - mouseWorldAfter;
        }

        // ── 中键拖拽 ──
        if (Input.GetMouseButtonDown(2) && !overUI)
        {
            _isDragging = true;
            _lastMouseWorldPos = GetMouseWorldPos();
        }

        if (Input.GetMouseButtonUp(2))
        {
            _isDragging = false;
        }

        if (_isDragging)
        {
            Vector3 currentMouseWorldPos = GetMouseWorldPos();
            Vector3 delta = _lastMouseWorldPos - currentMouseWorldPos;
            transform.position += delta * DragSpeed;
            _lastMouseWorldPos = GetMouseWorldPos();
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -_camera.transform.position.z; // 2D 场景，z 取反保证投影到 z=0 平面
        return _camera.ScreenToWorldPoint(mousePos);
    }
}
