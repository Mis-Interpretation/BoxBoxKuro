using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 右侧 Inspector 视图：显示和编辑选中实体的详细信息。
/// </summary>
public class EntityConfigInspectorView
{
    private readonly VisualElement _root;
    private readonly List<string> _availableSprites;
    private readonly List<string> _availableComponents;
    private readonly List<string> _availableSfxPaths;

    // UI 元素缓存
    private readonly Label _placeholder;
    private readonly VisualElement _details;
    private readonly Label _readOnlyHint;
    private readonly VisualElement _spritePreview;
    private readonly Label _idLabel;
    private readonly Label _typeIndexLabel;
    private readonly TextField _displayNameField;
    private readonly DropdownField _spriteDropdown;
    private readonly IntegerField _orderField;
    private readonly Toggle _pureDecorationToggle;
    private readonly Label _pureDecorationHint;
    private readonly VisualElement _componentsContainer;
    private readonly VisualElement _componentSfxContainer;
    private readonly ScrollView _scrollView;
    private readonly Button _applyBtn;
    private readonly Button _cancelBtn;
    private readonly Button _deleteBtn;
    private readonly Dictionary<string, DropdownField> _sfxDropdownByComponent = new Dictionary<string, DropdownField>();

    // 当前编辑的实体（原始数据，用于取消时恢复）
    private EntityConfigData _currentEntity;
    private bool _isBaseEntityReadOnly;

    public event Action<EntityConfigData> OnApplyRequested;
    public event Action OnCancelRequested;
    public event Action<string> OnDeleteRequested;

    public EntityConfigInspectorView(
        VisualElement root,
        List<string> availableSprites,
        List<string> availableComponents,
        List<string> availableSfxPaths)
    {
        _root = root;
        _availableSprites = availableSprites;
        _availableComponents = availableComponents;
        _availableSfxPaths = availableSfxPaths;

        _placeholder = root.Q<Label>("inspector-placeholder");
        _details = root.Q("inspector-details");
        _readOnlyHint = root.Q<Label>("inspector-readonly-hint");
        _spritePreview = root.Q("inspector-sprite-preview");
        _idLabel = root.Q<Label>("inspector-id");
        _typeIndexLabel = root.Q<Label>("inspector-type-index");
        _displayNameField = root.Q<TextField>("inspector-displayname-field");
        _spriteDropdown = root.Q<DropdownField>("inspector-sprite-dropdown");
        _orderField = root.Q<IntegerField>("inspector-order-field");
        _pureDecorationToggle = root.Q<Toggle>("inspector-pure-decoration-toggle");
        _pureDecorationHint = root.Q<Label>("inspector-pure-decoration-hint");
        _componentsContainer = root.Q("inspector-components-container");
        _componentSfxContainer = root.Q("inspector-component-sfx-container");
        _scrollView = root.Q<ScrollView>("inspector-scroll");
        _applyBtn = root.Q<Button>("inspector-apply-btn");
        _cancelBtn = root.Q<Button>("inspector-cancel-btn");
        _deleteBtn = root.Q<Button>("inspector-delete-btn");

        // 配置 sprite 下拉框选项
        _spriteDropdown.choices = _availableSprites;

        // sprite 下拉框变更时更新预览
        _spriteDropdown.RegisterValueChangedCallback(evt => UpdateSpritePreview(evt.newValue));
        _pureDecorationToggle?.RegisterValueChangedCallback(_ => RefreshDecorationUiState());

        // 按钮事件
        _applyBtn.clicked += () => OnApplyRequested?.Invoke(GatherEdited());
        _cancelBtn.clicked += () => OnCancelRequested?.Invoke();
        _deleteBtn.clicked += () =>
        {
            if (_currentEntity != null)
                OnDeleteRequested?.Invoke(_currentEntity.Id);
        };

        // 修复 TextField 内部文字颜色
        ApplyInputTextStyles(_displayNameField);
        ApplyDropdownStyles(_spriteDropdown);
    }

    public void ShowEmpty()
    {
        _placeholder.style.display = DisplayStyle.Flex;
        _details.AddToClassList("hidden");
        ScrollToTop();
        _currentEntity = null;
        _isBaseEntityReadOnly = false;
        if (_readOnlyHint != null) _readOnlyHint.AddToClassList("hidden");
        if (_pureDecorationHint != null) _pureDecorationHint.AddToClassList("hidden");
    }

    public void Show(EntityConfigData entity)
    {
        _currentEntity = EntityConfigFileController.DeepCopy(entity);
        _isBaseEntityReadOnly = entity.TypeIndex < 4;
        ScrollToTop();

        _placeholder.style.display = DisplayStyle.None;
        _details.RemoveFromClassList("hidden");

        // 头部信息
        _idLabel.text = entity.Id;
        _typeIndexLabel.text = $"TypeIndex: {entity.TypeIndex}";
        if (_readOnlyHint != null)
        {
            if (_isBaseEntityReadOnly) _readOnlyHint.RemoveFromClassList("hidden");
            else _readOnlyHint.AddToClassList("hidden");
        }

        // 显示名称
        _displayNameField.value = entity.DisplayName ?? "";
        ApplyInputTextStyles(_displayNameField);

        // Sprite 下拉框：若当前路径不在可用列表中，默认选第一个
        if (_availableSprites.Contains(entity.SpritePath))
            _spriteDropdown.value = entity.SpritePath;
        else if (_availableSprites.Count > 0)
            _spriteDropdown.value = _availableSprites[0];
        else
            _spriteDropdown.value = null;
        UpdateSpritePreview(_spriteDropdown.value);

        // OrderInLayer
        _orderField.value = entity.OrderInLayer;
        if (_pureDecorationToggle != null)
            _pureDecorationToggle.value = entity.IsPureDecoration;

        // 组件 toggles
        BuildComponentToggles(entity.Components);
        BuildComponentSfxEditors(entity);

        // TypeIndex 0-3 不可删除
        _deleteBtn.SetEnabled(entity.TypeIndex >= 4);

        // TypeIndex 0-3 只允许改名称和 Sprite：禁用其余编辑项
        _orderField.SetEnabled(!_isBaseEntityReadOnly);
        if (_pureDecorationToggle != null) _pureDecorationToggle.SetEnabled(!_isBaseEntityReadOnly);
        RefreshDecorationUiState();
    }

    private void RefreshDecorationUiState()
    {
        bool isPureDecoration = _pureDecorationToggle != null && _pureDecorationToggle.value;
        bool canEditComponents = !_isBaseEntityReadOnly && !isPureDecoration;
        _componentsContainer.SetEnabled(canEditComponents);
        _componentSfxContainer?.SetEnabled(!isPureDecoration);

        if (_pureDecorationHint != null)
        {
            if (isPureDecoration) _pureDecorationHint.RemoveFromClassList("hidden");
            else _pureDecorationHint.AddToClassList("hidden");
        }
    }

    private void BuildComponentToggles(List<string> activeComponents)
    {
        _componentsContainer.Clear();
        foreach (var compName in _availableComponents)
        {
            var toggle = new Toggle(compName);
            toggle.AddToClassList("component-toggle");
            toggle.value = activeComponents.Contains(compName);
            _componentsContainer.Add(toggle);
        }
    }

    private void BuildComponentSfxEditors(EntityConfigData entity)
    {
        if (_componentSfxContainer == null) return;
        _componentSfxContainer.Clear();
        _sfxDropdownByComponent.Clear();

        foreach (var componentName in _availableComponents)
        {
            if (!entity.Components.Contains(componentName)) continue;

            var row = new VisualElement();
            row.AddToClassList("component-sfx-row");

            var componentLabel = new Label(componentName);
            componentLabel.AddToClassList("component-sfx-label");
            row.Add(componentLabel);

            var dropdown = new DropdownField(_availableSfxPaths, 0);
            dropdown.AddToClassList("inspector-dropdown");
            dropdown.AddToClassList("component-sfx-dropdown");
            string overrideValue = NormalizeChoice(GetOverrideValue(entity, componentName));
            dropdown.value = overrideValue;
            ApplyDropdownStyles(dropdown);
            row.Add(dropdown);
            _sfxDropdownByComponent[componentName] = dropdown;

            _componentSfxContainer.Add(row);
        }
    }

    private void UpdateSpritePreview(string spritePath)
    {
        if (string.IsNullOrWhiteSpace(spritePath) || string.Equals(spritePath.Trim(), "none", StringComparison.OrdinalIgnoreCase))
        {
            _spritePreview.style.backgroundImage = StyleKeyword.None;
            return;
        }
        var sprite = Resources.Load<Sprite>(spritePath);
        if (sprite != null)
            _spritePreview.style.backgroundImage = new StyleBackground(sprite);
        else
            _spritePreview.style.backgroundImage = StyleKeyword.None;
    }

    private EntityConfigData GatherEdited()
    {
        if (_currentEntity == null) return null;

        var edited = new EntityConfigData
        {
            Id = _currentEntity.Id,
            TypeIndex = _currentEntity.TypeIndex,
            DisplayName = _displayNameField.value,
            SpritePath = _spriteDropdown.value ?? "",
            OrderInLayer = _isBaseEntityReadOnly ? _currentEntity.OrderInLayer : _orderField.value,
            Components = new List<string>(),
            ComponentSfx = new List<ComponentSfxEntry>(),
            ComponentSfxOverrides = new List<ComponentSfxEntry>(),
            IsPureDecoration = _pureDecorationToggle != null && _pureDecorationToggle.value,
            IsTextEntity = _currentEntity.IsTextEntity
        };

        if (_isBaseEntityReadOnly)
            edited.Components = new List<string>(_currentEntity.Components);
        else
        {
            // 收集选中的组件
            foreach (var child in _componentsContainer.Children())
            {
                if (child is Toggle toggle && toggle.value)
                    edited.Components.Add(toggle.label);
            }
        }

        foreach (string componentName in edited.Components)
        {
            if (_sfxDropdownByComponent.TryGetValue(componentName, out var dropdown))
                edited.SetComponentSfx(componentName, dropdown.value, true);
        }

        return edited;
    }

    private void ScrollToTop()
    {
        if (_scrollView == null) return;
        _scrollView.scrollOffset = Vector2.zero;
    }

    private static string GetOverrideValue(EntityConfigData entity, string componentName)
    {
        if (entity.ComponentSfxOverrides == null) return "";
        for (int i = 0; i < entity.ComponentSfxOverrides.Count; i++)
        {
            var entry = entity.ComponentSfxOverrides[i];
            if (entry == null) continue;
            if (entry.ComponentName != componentName) continue;
            return entry.SfxPath ?? "";
        }
        return "";
    }

    private string NormalizeChoice(string sfxPath)
    {
        if (string.IsNullOrWhiteSpace(sfxPath)) return "none";
        if (_availableSfxPaths.Contains(sfxPath)) return sfxPath;
        return "none";
    }

    private static void ApplyInputTextStyles(TextField field)
    {
        if (field == null) return;
        field.RegisterCallback<AttachToPanelEvent>(_ =>
        {
            var black = Color.black;
            field.Query<TextElement>().ForEach(te => te.style.color = black);
        });
    }

    private static void ApplyDropdownStyles(DropdownField field)
    {
        if (field == null) return;

        void ApplyNow()
        {
            var black = Color.black;
            var white = Color.white;

            // 折叠态显示文本与箭头区域都强制为白底黑字，避免主题继承导致灰字。
            field.Query<TextElement>().ForEach(te => te.style.color = black);

            var input = field.Q(className: "unity-base-field__input")
                ?? field.Q(className: "unity-popup-field__input")
                ?? field.Q(className: "unity-enum-field__input");
            if (input != null)
            {
                input.style.backgroundColor = white;
                input.style.color = black;
            }
        }

        field.RegisterCallback<AttachToPanelEvent>(_ => ApplyNow());
        ApplyNow();
    }
}
