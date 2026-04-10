using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 章节元数据编辑模态框。
/// </summary>
public class ChapterEditModalView
{
    private readonly VisualElement _modal;
    private readonly TextField _nameField;
    private readonly TextField _commentField;
    private readonly Toggle _onlineToggle;
    private readonly DropdownField _unlockTypeDropdown;
    private readonly IntegerField _requiredStarsField;
    private readonly Label _starsLabel;

    private static readonly List<string> UnlockTypeChoices = new List<string>
    {
        "始终开放",
        "通关前一章",
        "累计关数"
    };

    public event Action<string, string, UnlockCondition, bool> OnConfirmed;

    public ChapterEditModalView(VisualElement root)
    {
        _modal = root.Q("chapter-edit-modal");
        _nameField = root.Q<TextField>("chapter-edit-name-field");
        _commentField = root.Q<TextField>("chapter-edit-comment-field");
        _onlineToggle = root.Q<Toggle>("chapter-edit-online-toggle");
        _unlockTypeDropdown = root.Q<DropdownField>("chapter-edit-unlock-type");
        _requiredStarsField = root.Q<IntegerField>("chapter-edit-required-stars");
        _starsLabel = root.Q<Label>("chapter-edit-stars-label");

        if (_unlockTypeDropdown != null)
        {
            _unlockTypeDropdown.choices = UnlockTypeChoices;
            _unlockTypeDropdown.RegisterValueChangedCallback(OnUnlockTypeChanged);
        }

        var confirmBtn = root.Q<Button>("chapter-edit-confirm");
        var cancelBtn = root.Q<Button>("chapter-edit-cancel");
        if (confirmBtn != null) confirmBtn.clicked += OnConfirmClicked;
        if (cancelBtn != null) cancelBtn.clicked += Hide;

        // 确保输入框文字颜色正确
        ApplyTextFieldStyles(_nameField);
        ApplyTextFieldStyles(_commentField);
    }

    public void Show(ChapterData chapter)
    {
        if (_modal == null || chapter == null) return;

        if (_nameField != null) _nameField.value = chapter.ChapterName ?? "";
        if (_commentField != null) _commentField.value = chapter.Comment ?? "";
        if (_onlineToggle != null) _onlineToggle.value = chapter.IsOnline;

        var unlock = chapter.Unlock ?? new UnlockCondition();
        if (_unlockTypeDropdown != null)
            _unlockTypeDropdown.SetValueWithoutNotify(UnlockTypeChoices[(int)unlock.Type]);

        if (_requiredStarsField != null)
            _requiredStarsField.value = unlock.RequiredStars;

        UpdateStarsVisibility(unlock.Type);

        _modal.RemoveFromClassList("hidden");

        // 延迟重新应用样式
        _nameField?.schedule.Execute(() =>
        {
            ApplyTextFieldStyles(_nameField);
            ApplyTextFieldStyles(_commentField);
        }).ExecuteLater(0);
    }

    public void Hide()
    {
        _modal?.AddToClassList("hidden");
    }

    private void OnConfirmClicked()
    {
        string name = _nameField?.value ?? "New Chapter";
        string comment = _commentField?.value ?? "";
        bool isOnline = _onlineToggle?.value ?? true;

        var unlock = new UnlockCondition();
        if (_unlockTypeDropdown != null)
        {
            int idx = UnlockTypeChoices.IndexOf(_unlockTypeDropdown.value);
            unlock.Type = idx >= 0 ? (UnlockType)idx : UnlockType.ClearPreviousChapter;
        }
        if (_requiredStarsField != null)
            unlock.RequiredStars = _requiredStarsField.value;

        OnConfirmed?.Invoke(name, comment, unlock, isOnline);
        Hide();
    }

    private void OnUnlockTypeChanged(ChangeEvent<string> evt)
    {
        int idx = UnlockTypeChoices.IndexOf(evt.newValue);
        var type = idx >= 0 ? (UnlockType)idx : UnlockType.ClearPreviousChapter;
        UpdateStarsVisibility(type);
    }

    private void UpdateStarsVisibility(UnlockType type)
    {
        bool showStars = type == UnlockType.StarCount;
        if (_requiredStarsField != null)
            _requiredStarsField.style.display = showStars ? DisplayStyle.Flex : DisplayStyle.None;
        if (_starsLabel != null)
            _starsLabel.style.display = showStars ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private static void ApplyTextFieldStyles(TextField field)
    {
        if (field == null) return;
        var black = Color.black;
        field.Query<TextElement>().ForEach(te => te.style.color = black);
    }
}
