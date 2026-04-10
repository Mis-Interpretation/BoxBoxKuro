using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using TMPro;

/// <summary>
/// 左侧工具栏面板（UI Toolkit 版）：
/// - 笔刷模式 / 选择模式 两个大模式切换
/// - 各模式有独立的 flyout 选择子模式（点/线/面/边缘）
/// - 实体类型选择（仅笔刷模式下显示）
/// 从 <see cref="EditorHUDView.RootVisualElement"/> 获取共享根。
/// </summary>
public class EditorPaletteView : MonoBehaviour
{
    private VisualElement _root;

    // 笔刷模式
    private VisualElement _brushModeItem;
    private Button _brushModeBtn;
    private VisualElement _brushFlyout;
    private Button[] _brushOptionButtons;
    private readonly DrawingMode[] _modes = { DrawingMode.Point, DrawingMode.Line, DrawingMode.RectFill, DrawingMode.RectEdge };
    private static readonly string[] ModeLabels = { "点", "线", "面", "边缘" };
    private bool _brushFlyoutOpen;

    // 选择模式
    private VisualElement _selectModeItem;
    private Button _selectModeBtn;
    private VisualElement _selectFlyout;
    private Button[] _selectOptionButtons;
    private bool _selectFlyoutOpen;

    // 实体类型
    private ScrollView _entityScroll;
    /// <summary>滚轮缩放（小于 1 则减慢）。UI Toolkit ScrollView 无对应 USS 属性。</summary>
    private const float EntityScrollWheelScale = 1f;
    private VisualElement _entitySection;
    private List<VisualElement> _entityButtons;
    private List<int> _entityTypeIndices;

    // 操作按钮
    private Button _centerBtn;

    // Text 实例属性
    private VisualElement _textInspector;
    private TextField _textContentField;
    private IntegerField _textFontField;
    private Button _textApplyBtn;
    private Vector2Int? _activeTextAnchor;
    private Vector2Int? _lastTextInspectorAnchor;

    // 依赖
    private EditorStateModel _state;
    private EditorPlacementController _placement;


    private void Start()
    {
        _state = FindAnyObjectByType<EditorStateModel>();
        _placement = FindAnyObjectByType<EditorPlacementController>();

        var hud = FindAnyObjectByType<EditorHUDView>();
        if (hud == null || hud.RootVisualElement == null) return;
        _root = hud.RootVisualElement.Q("palette-root");
        if (_root == null) return;

        // 笔刷模式
        _brushModeItem = _root.Q("brush-mode-item");
        _brushModeBtn = _root.Q<Button>("brush-mode-btn");
        _brushFlyout = _root.Q("brush-flyout");

        // 选择模式
        _selectModeItem = _root.Q("select-mode-item");
        _selectModeBtn = _root.Q<Button>("select-mode-btn");
        _selectFlyout = _root.Q("select-flyout");

        _entitySection = _root.Q("entity-section");
        _entityScroll = _root.Q<ScrollView>("entity-scroll");
        _centerBtn = _root.Q<Button>("center-btn");
        _textInspector = _root.Q("text-inspector");
        _textContentField = _root.Q<TextField>("text-content-field");
        _textFontField = _root.Q<IntegerField>("text-font-field");
        _textApplyBtn = _root.Q<Button>("text-apply-btn");
        if (_entityScroll != null)
        {
            _entityScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _entityScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _entityScroll.RegisterCallback<WheelEvent>(OnEntityScrollWheel, TrickleDown.TrickleDown);
        }

        // 笔刷按钮点击：切换到笔刷模式 + toggle flyout
        _brushModeBtn?.RegisterCallback<ClickEvent>(e =>
        {
            e.StopPropagation();
            if (_state.CurrentEditorMode != EditorMode.Brush)
            {
                _state.CurrentEditorMode = EditorMode.Brush;
                _state.Selection.ClearAll();
                CloseSelectFlyout();
            }
            ToggleBrushFlyout();
        });

        // 选择按钮点击：切换到选择模式 + toggle flyout
        _selectModeBtn?.RegisterCallback<ClickEvent>(e =>
        {
            e.StopPropagation();
            if (_state.CurrentEditorMode != EditorMode.Select)
            {
                _state.CurrentEditorMode = EditorMode.Select;
                CloseBrushFlyout();
            }
            ToggleSelectFlyout();
        });

        BuildBrushFlyout();
        BuildSelectFlyout();
        AddToolTriangle(_brushModeBtn);
        AddToolTriangle(_selectModeBtn);

        // 点击 root 任意位置关闭 flyout
        _root.RegisterCallback<PointerDownEvent>(e =>
        {
            if (_brushFlyoutOpen) CloseBrushFlyout();
            if (_selectFlyoutOpen) CloseSelectFlyout();
        });

        if (_state != null)
        {
            UpdateDisplays();
            BuildEntityButtons();
        }

        _centerBtn?.RegisterCallback<ClickEvent>(e =>
        {
            if (_placement == null) return;

            var result = _placement.CenterAllEntities();
            if (!result.HasEntities)
            {
                Debug.Log("[LevelEditor] Center skipped: no entities in current level.");
                return;
            }

            if (!result.Applied)
            {
                Debug.Log("[LevelEditor] Center no-op: computed offset is (0, 0).");
                return;
            }

            Debug.Log($"[LevelEditor] Center applied: offset=({result.OffsetX}, {result.OffsetY}).");
        });

        _textApplyBtn?.RegisterCallback<ClickEvent>(_ => ApplyTextInspectorChanges());
    }

    private void Update()
    {
        if (_state == null) return;
        UpdateDisplays();
        UpdateTextInspector();
    }

    // ════════════════════════════════════════
    //  显示/隐藏
    // ════════════════════════════════════════

    public void SetVisible(bool visible)
    {
        if (_root != null)
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ════════════════════════════════════════
    //  更新所有显示状态
    // ════════════════════════════════════════

    private void UpdateDisplays()
    {
        bool isBrush = _state.CurrentEditorMode == EditorMode.Brush;

        // 模式高亮
        _brushModeItem?.EnableInClassList("mode-item--active", isBrush);
        _selectModeItem?.EnableInClassList("mode-item--active", !isBrush);

        // 笔刷按钮显示当前子模式
        if (_brushModeBtn != null)
        {
            int idx = System.Array.IndexOf(_modes, _state.CurrentDrawingMode);
            _brushModeBtn.text = idx >= 0 ? ModeLabels[idx] : "?";
        }

        // 选择按钮显示当前子模式
        if (_selectModeBtn != null)
        {
            int idx = System.Array.IndexOf(_modes, _state.CurrentSelectShapeMode);
            _selectModeBtn.text = idx >= 0 ? ModeLabels[idx] : "?";
        }

        // flyout 选项高亮
        UpdateOptionHighlights(_brushOptionButtons, _state.CurrentDrawingMode);
        UpdateOptionHighlights(_selectOptionButtons, _state.CurrentSelectShapeMode);

        // 实体按钮高亮
        UpdateEntityHighlights();

        // 选择模式下隐藏实体选择区
        if (_entityScroll != null)
            _entityScroll.style.display = isBrush ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void UpdateTextInspector()
    {
        if (_textInspector == null || _state == null)
            return;

        if (_state.CurrentEditorMode != EditorMode.Select)
        {
            _activeTextAnchor = null;
            _lastTextInspectorAnchor = null;
            _textInspector.AddToClassList("hidden");
            return;
        }

        if (_state.Selection.SelectedCells.Count != 1)
        {
            _activeTextAnchor = null;
            _lastTextInspectorAnchor = null;
            _textInspector.AddToClassList("hidden");
            return;
        }

        Vector2Int anchor = _state.Selection.SelectedCells.First();
        if (!_state.PlacedObjects.TryGetValue(anchor, out var list) || list.Count == 0)
        {
            _activeTextAnchor = null;
            _lastTextInspectorAnchor = null;
            _textInspector.AddToClassList("hidden");
            return;
        }

        foreach (var obj in list)
        {
            if (obj == null) continue;
            var runtime = obj.GetComponent<TextEntityRuntimeModel>();
            if (runtime == null) continue;

            runtime.Payload.EnsureValid();
            _activeTextAnchor = anchor;
            if (_lastTextInspectorAnchor == null || _lastTextInspectorAnchor.Value != anchor)
            {
                _textContentField.value = runtime.Payload.Content;
                _textFontField.value = runtime.Payload.FontSize;
                _lastTextInspectorAnchor = anchor;
            }
            _textInspector.RemoveFromClassList("hidden");
            return;
        }

        _activeTextAnchor = null;
        _lastTextInspectorAnchor = null;
        _textInspector.AddToClassList("hidden");
    }

    private void ApplyTextInspectorChanges()
    {
        if (_activeTextAnchor == null || _state == null) return;

        Vector2Int anchor = _activeTextAnchor.Value;
        if (!_state.PlacedObjects.TryGetValue(anchor, out var list) || list.Count == 0) return;

        foreach (var obj in list)
        {
            if (obj == null) continue;

            var runtime = obj.GetComponent<TextEntityRuntimeModel>();
            var view = obj.GetComponent<TextEntityView>();
            if (runtime == null || view == null) continue;

            var newPayload = new TextEntityPayload
            {
                Content = _textContentField.value,
                FontSize = _textFontField.value,
                WidthInCells = 1,
                HeightInCells = 1
            };
            newPayload.EnsureValid();

            runtime.SetPayload(newPayload);
            view.ApplyText(runtime.Payload, obj.GetComponent<TextMeshPro>()?.sortingOrder ?? 0);

            var config = _state.ConfigReader.GetConfig(obj.name);
            int typeIndex = config?.TypeIndex ?? 0;
            for (int i = _state.CurrentLevel.Entities.Count - 1; i >= 0; i--)
            {
                var entity = _state.CurrentLevel.Entities[i];
                if (entity.Type == typeIndex && entity.X == anchor.x && entity.Y == anchor.y)
                {
                    entity.Text = TextEntityUtility.ClonePayload(runtime.Payload);
                    break;
                }
            }

            break;
        }
    }

    private void UpdateOptionHighlights(Button[] buttons, DrawingMode activeMode)
    {
        if (buttons == null) return;
        for (int i = 0; i < buttons.Length; i++)
            buttons[i].EnableInClassList("tool-option--active", _modes[i] == activeMode);
    }

    // ════════════════════════════════════════
    //  小三角
    // ════════════════════════════════════════

    private void AddToolTriangle(Button btn)
    {
        if (btn == null) return;
        var triangle = new VisualElement();
        triangle.AddToClassList("tool-triangle");
        btn.Add(triangle);
    }

    // ════════════════════════════════════════
    //  笔刷 Flyout
    // ════════════════════════════════════════

    private void BuildBrushFlyout()
    {
        if (_brushFlyout == null) return;
        _brushOptionButtons = new Button[_modes.Length];

        for (int i = 0; i < _modes.Length; i++)
        {
            var btn = new Button();
            btn.text = ModeLabels[i];
            btn.AddToClassList("tool-option");
            _brushOptionButtons[i] = btn;

            DrawingMode captured = _modes[i];
            btn.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                _state.CurrentDrawingMode = captured;
                CloseBrushFlyout();
            });
            _brushFlyout.Add(btn);
        }
    }

    private void ToggleBrushFlyout()
    {
        if (_brushFlyoutOpen) CloseBrushFlyout();
        else OpenBrushFlyout();
    }

    private void OpenBrushFlyout()
    {
        CloseSelectFlyout();
        _brushFlyoutOpen = true;
        _brushFlyout?.RemoveFromClassList("hidden");
    }

    private void CloseBrushFlyout()
    {
        _brushFlyoutOpen = false;
        _brushFlyout?.AddToClassList("hidden");
    }

    // ════════════════════════════════════════
    //  选择 Flyout
    // ════════════════════════════════════════

    private void BuildSelectFlyout()
    {
        if (_selectFlyout == null) return;
        _selectOptionButtons = new Button[_modes.Length];

        for (int i = 0; i < _modes.Length; i++)
        {
            var btn = new Button();
            btn.text = ModeLabels[i];
            btn.AddToClassList("tool-option");
            _selectOptionButtons[i] = btn;

            DrawingMode captured = _modes[i];
            btn.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                _state.CurrentSelectShapeMode = captured;
                CloseSelectFlyout();
            });
            _selectFlyout.Add(btn);
        }
    }

    private void ToggleSelectFlyout()
    {
        if (_selectFlyoutOpen) CloseSelectFlyout();
        else OpenSelectFlyout();
    }

    private void OpenSelectFlyout()
    {
        CloseBrushFlyout();
        _selectFlyoutOpen = true;
        _selectFlyout?.RemoveFromClassList("hidden");
    }

    private void CloseSelectFlyout()
    {
        _selectFlyoutOpen = false;
        _selectFlyout?.AddToClassList("hidden");
    }

    // ════════════════════════════════════════
    //  实体类型
    // ════════════════════════════════════════

    private void BuildEntityButtons()
    {
        if (_entitySection == null || _state.ConfigReader == null) return;

        _entitySection.Clear();

        var configs = _state.ConfigReader.AllConfigs;
        _entityTypeIndices = new List<int>(configs.Count);
        _entityButtons = new List<VisualElement>(configs.Count);

        var baseConfigs = new List<EntityConfigData>();
        var variantConfigs = new List<EntityConfigData>();
        var decorationConfigs = new List<EntityConfigData>();

        for (int i = 0; i < configs.Count; i++)
        {
            var config = configs[i];
            if (config == null) continue;

            if (config.TypeIndex < 4) baseConfigs.Add(config);
            else if (config.IsPureDecoration) decorationConfigs.Add(config);
            else variantConfigs.Add(config);
        }

        AddEntityGroup("基础物体", baseConfigs);
        AddEntityGroup("变种物体", variantConfigs);
        AddEntityGroup("装饰物体", decorationConfigs);
    }

    private void AddEntityGroup(string title, List<EntityConfigData> configs)
    {
        if (configs == null || configs.Count == 0) return;

        var group = new VisualElement();
        group.AddToClassList("entity-group");

        var titleLabel = new Label(title);
        titleLabel.AddToClassList("entity-group__title");
        group.Add(titleLabel);

        for (int i = 0; i < configs.Count; i++)
        {
            var btn = BuildEntityButton(configs[i]);
            if (btn != null) group.Add(btn);
        }

        _entitySection.Add(group);
    }

    private VisualElement BuildEntityButton(EntityConfigData config)
    {
        if (config == null) return null;

        _entityTypeIndices.Add(config.TypeIndex);

        var btn = new VisualElement();
        btn.AddToClassList("entity-btn");

        // 图标
        var sprite = _state.ConfigReader.GetSprite(config.Id);
        if (sprite != null)
        {
            var icon = new VisualElement();
            icon.AddToClassList("entity-btn__icon");
            icon.style.backgroundImage = new StyleBackground(sprite);
            btn.Add(icon);
        }

        // 文字
        string displayName = !string.IsNullOrEmpty(config.DisplayName) ? config.DisplayName : config.Id;
        var label = new Label(displayName);
        label.AddToClassList("entity-btn__label");
        btn.Add(label);

        _entityButtons.Add(btn);

        int capturedIndex = config.TypeIndex;
        btn.RegisterCallback<PointerDownEvent>(e =>
        {
            _state.SelectedTypeIndex = capturedIndex;
        });

        return btn;
    }

    private void OnEntityScrollWheel(WheelEvent evt)
    {
        if (_entityScroll == null) return;

        float contentH = _entityScroll.contentContainer.layout.height;
        float viewH = _entityScroll.contentViewport.layout.height;
        if (viewH <= 1f || float.IsNaN(contentH) || float.IsNaN(viewH))
        {
            contentH = _entityScroll.contentContainer.resolvedStyle.height;
            viewH = _entityScroll.contentViewport.resolvedStyle.height;
        }

        float maxY = Mathf.Max(0f, contentH - viewH);
        if (maxY <= 0f)
            return;

        evt.StopImmediatePropagation();

        float nextY = Mathf.Clamp(
            _entityScroll.scrollOffset.y + evt.delta.y * EntityScrollWheelScale,
            0f,
            maxY);
        _entityScroll.scrollOffset = new Vector2(_entityScroll.scrollOffset.x, nextY);
    }

    private void UpdateEntityHighlights()
    {
        if (_entityButtons == null || _state == null) return;

        for (int i = 0; i < _entityButtons.Count; i++)
        {
            if (_entityButtons[i] == null) continue;
            _entityButtons[i].EnableInClassList("entity-btn--selected", _entityTypeIndices[i] == _state.SelectedTypeIndex);
        }
    }
}
