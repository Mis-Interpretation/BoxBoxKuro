using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

/// <summary>
/// 编辑器主 UI 视图（UI Toolkit 版）。
/// 本脚本持有唯一的 UIDocument（Source Asset 指向 EditorMain.uxml），
/// 管理顶部 HUD 栏（关卡名、网格尺寸、操作按钮、退出确认），
/// 并向 <see cref="EditorPaletteView"/> 和 <see cref="EditorMetadataView"/>
/// 提供共享的 <see cref="RootVisualElement"/>。
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class EditorHUDView : MonoBehaviour
{
    /// <summary>
    /// UIDocument 的根 VisualElement，供其他 View 查询子元素。
    /// </summary>
    public VisualElement RootVisualElement { get; private set; }

    private VisualElement _root;

    private Label _levelNameDisplay;
    private IntegerField _gridWidth;
    private IntegerField _gridHeight;

    private Button _saveBtn;
    private Button _clearBtn;
    private Button _validateBtn;
    private Button _solveBtn;
    private Button _metadataBtn;
    private Button _exitBtn;

    private VisualElement _exitOverlay;
    private Button _exitSaveBtn;
    private Button _exitDiscardBtn;
    private Button _exitCancelBtn;
    private VisualElement _validateTipsRoot;

    private EditorFileController _fileController;
    private EditorValidateController _validateController;
    private EditorSolverController _solverController;
    private EditorMetadataView _metadataView;
    private EditorStateModel _state;
    private EditorUndoController _undo;

    private void OnEnable()
    {
        var doc = GetComponent<UIDocument>();
        RootVisualElement = doc.rootVisualElement;

        _root = RootVisualElement.Q("hud-root");

        _levelNameDisplay = _root.Q<Label>("level-name-display");
        _gridWidth = _root.Q<IntegerField>("grid-width");
        _gridHeight = _root.Q<IntegerField>("grid-height");

        _saveBtn = _root.Q<Button>("save-btn");
        _clearBtn = _root.Q<Button>("clear-btn");
        _validateBtn = _root.Q<Button>("validate-btn");
        _solveBtn = _root.Q<Button>("solve-btn");
        _metadataBtn = _root.Q<Button>("metadata-btn");
        _exitBtn = _root.Q<Button>("exit-btn");

        _exitOverlay = RootVisualElement.Q("exit-confirm-overlay");
        _exitSaveBtn = RootVisualElement.Q<Button>("exit-save-btn");
        _exitDiscardBtn = RootVisualElement.Q<Button>("exit-discard-btn");
        _exitCancelBtn = RootVisualElement.Q<Button>("exit-cancel-btn");
        _validateTipsRoot = RootVisualElement.Q("validate-tips-root");

        _saveBtn?.RegisterCallback<ClickEvent>(_ => OnSave());
        _clearBtn?.RegisterCallback<ClickEvent>(_ => OnClear());
        _validateBtn?.RegisterCallback<ClickEvent>(_ => OnValidate());
        _solveBtn?.RegisterCallback<ClickEvent>(_ => OnSolve());
        _metadataBtn?.RegisterCallback<ClickEvent>(_ => OnMetadata());
        _exitBtn?.RegisterCallback<ClickEvent>(_ => ShowExitConfirm());

        _exitSaveBtn?.RegisterCallback<ClickEvent>(_ => OnExitSave());
        _exitDiscardBtn?.RegisterCallback<ClickEvent>(_ => OnExitDiscard());
        _exitCancelBtn?.RegisterCallback<ClickEvent>(_ => HideExitConfirm());

        _gridWidth?.RegisterValueChangedCallback(OnGridWidthChanged);
        _gridHeight?.RegisterValueChangedCallback(OnGridHeightChanged);
    }

    public void SetValidateTipsVisible(bool visible)
    {
        if (_validateTipsRoot == null) return;

        if (visible)
            _validateTipsRoot.RemoveFromClassList("hidden");
        else
            _validateTipsRoot.AddToClassList("hidden");
    }

    public void SetMainHudVisible(bool visible)
    {
        if (_root == null) return;

        _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void Start()
    {
        _fileController = FindAnyObjectByType<EditorFileController>();
        _validateController = FindAnyObjectByType<EditorValidateController>();
        _solverController = FindAnyObjectByType<EditorSolverController>();
        _metadataView = FindAnyObjectByType<EditorMetadataView>();
        _state = FindAnyObjectByType<EditorStateModel>();
        _undo = FindAnyObjectByType<EditorUndoController>();

        RefreshGridSizeUI();

        if (LevelEditorPendingLoad.TryConsumePendingLevel(out var pendingName) && _fileController != null)
            _fileController.LoadLevel(pendingName);
        else
            RefreshLevelNameDisplay();
    }

    public void RefreshGridSizeUI()
    {
        if (_state == null) return;
        _gridWidth?.SetValueWithoutNotify(_state.CurrentLevel.Width);
        _gridHeight?.SetValueWithoutNotify(_state.CurrentLevel.Height);
    }

    public void RefreshLevelNameDisplay()
    {
        if (_levelNameDisplay == null || _state == null) return;

        string name = _state.CurrentLevel.LevelName;
        _levelNameDisplay.text = string.IsNullOrWhiteSpace(name) ? "（未命名）" : name;
    }

    private void OnGridWidthChanged(ChangeEvent<int> evt)
    {
        int w = Mathf.Clamp(evt.newValue, 1, 50);
        if (w != evt.newValue)
            _gridWidth.SetValueWithoutNotify(w);

        int oldW = _state.CurrentLevel.Width;
        int oldH = _state.CurrentLevel.Height;
        _state.CurrentLevel.Width = w;
        _undo?.Record(new GridResizeCommand(_state, this, oldW, oldH));
    }

    private void OnGridHeightChanged(ChangeEvent<int> evt)
    {
        int h = Mathf.Clamp(evt.newValue, 1, 50);
        if (h != evt.newValue)
            _gridHeight.SetValueWithoutNotify(h);

        int oldW = _state.CurrentLevel.Width;
        int oldH = _state.CurrentLevel.Height;
        _state.CurrentLevel.Height = h;
        _undo?.Record(new GridResizeCommand(_state, this, oldW, oldH));
    }

    private void OnSave()
    {
        if (_fileController != null && _state != null)
            _fileController.SaveLevel(_state.CurrentLevel.LevelName);
    }

    private void OnClear()
    {
        if (_fileController != null)
        {
            _fileController.ClearLevel();
            RefreshGridSizeUI();
        }
    }

    private void OnValidate()
    {
        if (_validateController == null) return;

        if (_validateController.IsValidating)
            _validateController.StopValidation();
        else
            _validateController.StartValidation();
    }

    private void OnSolve()
    {
        if (_solverController != null)
            _solverController.OnSolve();
    }

    private void OnMetadata()
    {
        if (_metadataView != null)
            _metadataView.TogglePanel();
    }

    // ════════════════════════════════════════
    //  退出确认
    // ════════════════════════════════════════

    private void ShowExitConfirm()
    {
        if (_exitOverlay != null)
            _exitOverlay.RemoveFromClassList("hidden");
    }

    private void HideExitConfirm()
    {
        if (_exitOverlay != null)
            _exitOverlay.AddToClassList("hidden");
    }

    private void OnExitSave()
    {
        if (_fileController != null && _state != null)
            _fileController.SaveLevel(_state.CurrentLevel.LevelName);

        SceneManager.LoadScene(SceneNameModel.ArrangeScene);
    }

    private void OnExitDiscard()
    {
        SceneManager.LoadScene(SceneNameModel.ArrangeScene);
    }
}
