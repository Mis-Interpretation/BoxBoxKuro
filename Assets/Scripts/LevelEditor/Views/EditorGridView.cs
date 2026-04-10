using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 绘制编辑器网格线和鼠标悬停/预览/选择高亮。需要挂在 Camera 上。
/// 通过普通 MeshRenderer + URP 正常管线渲染（而非 GL 立即模式 + endCameraRendering 回调），
/// 这样 Screen Space Overlay 的 UI（UI Toolkit HUD 等）可以自然遮挡网格。
/// 注意：渲染不再依赖相机的投影矩阵；但鼠标 -> 网格坐标仍使用 Camera.ScreenToWorldPoint，
/// 这是两件正交的事情。
/// </summary>
[RequireComponent(typeof(Camera))]
public class EditorGridView : MonoBehaviour
{
    public Color GridColor = new Color(1f, 1f, 1f, 0.3f);
    public Color HoverColor = new Color(1f, 1f, 0f, 0.3f);
    public Color PreviewColor = new Color(0f, 1f, 0f, 0.25f);
    public Color ErasePreviewColor = new Color(1f, 0f, 0f, 0.25f);
    public Color SelectionColor = new Color(0.3f, 0.5f, 1f, 0.3f);
    public Color SelectionPreviewColor = new Color(0.3f, 0.5f, 1f, 0.15f);
    public Color MovePreviewColor = new Color(0.3f, 1f, 0.5f, 0.2f);

    [SerializeField] private float _lineThickness = 0.03f;
    [SerializeField] private float _gridLinesZ = 0.2f;
    [SerializeField] private float _highlightsZ = 0.1f;
    [SerializeField] private int _gridLinesSortingOrder = -2;
    [SerializeField] private int _highlightsSortingOrder = -1;

    private EditorStateModel _state;
    private EditorPlacementController _placement;
    private EditorSelectionController _selection;
    private EditorSolverController _solver;
    private Camera _camera;

    private Material _meshMaterial;
    private GameObject _gridLinesGO;
    private GameObject _highlightsGO;
    private Mesh _gridLinesMesh;
    private Mesh _highlightsMesh;

    private int _cachedGridW = -1;
    private int _cachedGridH = -1;

    // 每帧复用以避免 GC
    private readonly List<Vector3> _vertices = new List<Vector3>();
    private readonly List<Color> _colors = new List<Color>();
    private readonly List<int> _indices = new List<int>();

    /// <summary>
    /// 当前鼠标悬停的网格坐标。
    /// </summary>
    public Vector2Int HoveredCell { get; private set; }

    private void Awake()
    {
        _state = FindAnyObjectByType<EditorStateModel>();
        _placement = FindAnyObjectByType<EditorPlacementController>();
        _selection = FindAnyObjectByType<EditorSelectionController>();
        _solver = FindAnyObjectByType<EditorSolverController>();
        _camera = GetComponent<Camera>();

        _meshMaterial = new Material(Shader.Find("Sprites/Default"))
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        _gridLinesMesh = new Mesh { name = "EditorGridLinesMesh" };
        _gridLinesMesh.MarkDynamic();
        _gridLinesGO = CreateMeshChild("EditorGridLines", _gridLinesMesh, _gridLinesZ, _gridLinesSortingOrder);

        _highlightsMesh = new Mesh { name = "EditorGridHighlightsMesh" };
        _highlightsMesh.MarkDynamic();
        _highlightsGO = CreateMeshChild("EditorGridHighlights", _highlightsMesh, _highlightsZ, _highlightsSortingOrder);
    }

    private GameObject CreateMeshChild(string name, Mesh mesh, float z, int sortingOrder)
    {
        var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
        go.transform.position = new Vector3(0f, 0f, z);

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = _meshMaterial;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = LightProbeUsage.Off;
        mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        mr.sortingOrder = sortingOrder;

        return go;
    }

    private void OnEnable()
    {
        // 组件启用时（如退出试玩）恢复网格显示
        SetRenderersActive(true);
    }

    private void OnDisable()
    {
        // 组件禁用时（试玩模式下 EditorValidateController 会 disable 本组件）隐藏网格
        SetRenderersActive(false);
    }

    private void Update()
    {
        if (_camera == null) return;
        Vector3 worldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
        HoveredCell = new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
    }

    private void LateUpdate()
    {
        // 求解模式下隐藏整个网格（验证模式由 OnDisable 处理，因为组件本身会被禁用）
        bool hidden = _solver != null && _solver.IsSolving;
        SetRenderersActive(!hidden);
        if (hidden) return;

        if (_state == null || _state.CurrentLevel == null)
        {
            if (_gridLinesMesh != null) _gridLinesMesh.Clear();
            if (_highlightsMesh != null) _highlightsMesh.Clear();
            _cachedGridW = -1;
            _cachedGridH = -1;
            return;
        }

        int w = _state.CurrentLevel.Width;
        int h = _state.CurrentLevel.Height;

        if (w != _cachedGridW || h != _cachedGridH)
        {
            RebuildGridLinesMesh(w, h);
            _cachedGridW = w;
            _cachedGridH = h;
        }

        RebuildHighlightsMesh(w, h);
    }

    private void RebuildGridLinesMesh(int w, int h)
    {
        _vertices.Clear();
        _colors.Clear();
        _indices.Clear();

        if (w <= 0 || h <= 0)
        {
            _gridLinesMesh.Clear();
            return;
        }

        int halfW = w / 2;
        int halfH = h / 2;
        float minX = -halfW - 0.5f;
        float maxX = w - halfW - 0.5f;
        float minY = -halfH - 0.5f;
        float maxY = h - halfH - 0.5f;
        float t = _lineThickness * 0.5f;

        // 竖线
        for (int x = 0; x <= w; x++)
        {
            float xp = x - halfW - 0.5f;
            AddQuad(xp - t, minY - t, xp + t, maxY + t, GridColor);
        }

        // 横线（在 X 方向延伸 t，让拐角闭合）
        for (int y = 0; y <= h; y++)
        {
            float yp = y - halfH - 0.5f;
            AddQuad(minX - t, yp - t, maxX + t, yp + t, GridColor);
        }

        CommitMesh(_gridLinesMesh);
    }

    private void RebuildHighlightsMesh(int w, int h)
    {
        _vertices.Clear();
        _colors.Clear();
        _indices.Clear();

        int halfW = w / 2;
        int halfH = h / 2;

        // 1. Hover 高亮
        if (HoveredCell.x >= -halfW && HoveredCell.x < w - halfW &&
            HoveredCell.y >= -halfH && HoveredCell.y < h - halfH)
        {
            AddCellQuad(HoveredCell.x, HoveredCell.y, HoverColor);
        }

        // 2. 放置拖拽预览
        if (_placement != null && _placement.IsDragging && _placement.PreviewCells.Count > 0)
        {
            foreach (var pc in _placement.PreviewCells)
                AddCellQuad(pc.x, pc.y, PreviewColor);
        }

        // 3. 放置模式的擦除拖拽预览
        if (_placement != null && _placement.IsEraseDragging && _placement.ErasePreviewCells.Count > 0)
        {
            foreach (var pc in _placement.ErasePreviewCells)
                AddCellQuad(pc.x, pc.y, ErasePreviewColor);
        }

        // 4. 选择模式的擦除拖拽预览
        if (_selection != null && _selection.IsEraseDragging && _selection.ErasePreviewCells.Count > 0)
        {
            foreach (var pc in _selection.ErasePreviewCells)
                AddCellQuad(pc.x, pc.y, ErasePreviewColor);
        }

        // 5. 选中格子高亮（含移动拖拽双显示）
        var sel = _state.Selection;
        if (sel != null && sel.SelectedCells.Count > 0)
        {
            if (sel.IsDraggingMove && sel.MoveOffset != Vector2Int.zero)
            {
                Color faded = new Color(SelectionColor.r, SelectionColor.g, SelectionColor.b, 0.1f);
                foreach (var sc in sel.SelectedCells)
                    AddCellQuad(sc.x, sc.y, faded);

                foreach (var sc in sel.SelectedCells)
                    AddCellQuad(sc.x + sel.MoveOffset.x, sc.y + sel.MoveOffset.y, MovePreviewColor);
            }
            else
            {
                foreach (var sc in sel.SelectedCells)
                    AddCellQuad(sc.x, sc.y, SelectionColor);
            }
        }

        // 6. 框选预览
        if (sel != null && sel.IsDraggingSelection && sel.SelectionPreviewCells.Count > 0)
        {
            foreach (var pc in sel.SelectionPreviewCells)
                AddCellQuad(pc.x, pc.y, SelectionPreviewColor);
        }

        CommitMesh(_highlightsMesh);
    }

    private void AddCellQuad(int cx, int cy, Color c)
    {
        AddQuad(cx - 0.5f, cy - 0.5f, cx + 0.5f, cy + 0.5f, c);
    }

    private void AddQuad(float x0, float y0, float x1, float y1, Color c)
    {
        int baseIdx = _vertices.Count;
        _vertices.Add(new Vector3(x0, y0, 0f));
        _vertices.Add(new Vector3(x1, y0, 0f));
        _vertices.Add(new Vector3(x1, y1, 0f));
        _vertices.Add(new Vector3(x0, y1, 0f));

        _colors.Add(c);
        _colors.Add(c);
        _colors.Add(c);
        _colors.Add(c);

        _indices.Add(baseIdx + 0);
        _indices.Add(baseIdx + 1);
        _indices.Add(baseIdx + 2);
        _indices.Add(baseIdx + 0);
        _indices.Add(baseIdx + 2);
        _indices.Add(baseIdx + 3);
    }

    private void CommitMesh(Mesh mesh)
    {
        mesh.Clear();
        if (_vertices.Count == 0) return;

        mesh.indexFormat = _vertices.Count > 65000
            ? IndexFormat.UInt32
            : IndexFormat.UInt16;
        mesh.SetVertices(_vertices);
        mesh.SetColors(_colors);
        mesh.SetIndices(_indices, MeshTopology.Triangles, 0);
        mesh.RecalculateBounds();
    }

    private void SetRenderersActive(bool active)
    {
        if (_gridLinesGO != null && _gridLinesGO.activeSelf != active)
            _gridLinesGO.SetActive(active);
        if (_highlightsGO != null && _highlightsGO.activeSelf != active)
            _highlightsGO.SetActive(active);
    }

    private void OnDestroy()
    {
        if (_gridLinesGO != null) DestroyImmediate(_gridLinesGO);
        if (_highlightsGO != null) DestroyImmediate(_highlightsGO);
        if (_gridLinesMesh != null) DestroyImmediate(_gridLinesMesh);
        if (_highlightsMesh != null) DestroyImmediate(_highlightsMesh);
        if (_meshMaterial != null) DestroyImmediate(_meshMaterial);
    }
}
