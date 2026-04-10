using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 新建实体弹窗视图。
/// </summary>
public class EntityConfigNewEntityModalView
{
    private readonly VisualElement _modal;
    private readonly TextField _idField;
    private readonly TextField _displayNameField;
    private readonly DropdownField _spriteDropdown;
    private readonly Label _errorLabel;

    /// <summary>
    /// 确认新建：参数为 (id, displayName, spritePath)
    /// </summary>
    public event Action<string, string, string> OnConfirmed;

    public EntityConfigNewEntityModalView(VisualElement root)
    {
        _modal = root.Q("new-entity-modal");
        _idField = root.Q<TextField>("new-entity-id-field");
        _displayNameField = root.Q<TextField>("new-entity-displayname-field");
        _spriteDropdown = root.Q<DropdownField>("new-entity-sprite-dropdown");
        _errorLabel = root.Q<Label>("new-entity-error");

        var confirmBtn = root.Q<Button>("new-entity-confirm");
        var cancelBtn = root.Q<Button>("new-entity-cancel");

        confirmBtn.clicked += OnConfirmClicked;
        cancelBtn.clicked += Hide;

        ApplyInputTextStyles(_idField);
        ApplyInputTextStyles(_displayNameField);
    }

    public void Show(List<string> availableSprites)
    {
        _spriteDropdown.choices = availableSprites;
        _idField.value = "";
        _displayNameField.value = "";
        _errorLabel.text = "";
        if (availableSprites.Count > 0)
            _spriteDropdown.value = availableSprites[0];

        _modal.RemoveFromClassList("hidden");

        _idField.schedule.Execute(() =>
        {
            ApplyInputTextStylesImmediate(_idField);
            ApplyInputTextStylesImmediate(_displayNameField);
            _idField.Focus();
        }).ExecuteLater(0);
    }

    public void Hide()
    {
        _modal.AddToClassList("hidden");
    }

    public void ShowError(string message)
    {
        _errorLabel.text = message;
    }

    private void OnConfirmClicked()
    {
        string id = _idField.value?.Trim();
        string displayName = _displayNameField.value?.Trim();
        string spritePath = _spriteDropdown.value;

        if (string.IsNullOrEmpty(id))
        {
            _errorLabel.text = "Id 不能为空";
            return;
        }

        _errorLabel.text = "";
        OnConfirmed?.Invoke(id, displayName, spritePath);
    }

    private static void ApplyInputTextStyles(TextField field)
    {
        if (field == null) return;
        field.RegisterCallback<AttachToPanelEvent>(_ => ApplyInputTextStylesImmediate(field));
    }

    private static void ApplyInputTextStylesImmediate(TextField field)
    {
        if (field == null) return;
        var black = Color.black;
        field.Query<TextElement>().ForEach(te => te.style.color = black);
    }
}
