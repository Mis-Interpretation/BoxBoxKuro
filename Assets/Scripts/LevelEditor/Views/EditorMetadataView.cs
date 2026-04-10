using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 关卡元数据面板 UI（UI Toolkit 版）：标签管理、难度评分（半星）、通关状态、评论。
/// 不再自带 UIDocument，改为从 <see cref="EditorHUDView.RootVisualElement"/> 获取共享根。
/// </summary>
public class EditorMetadataView : MonoBehaviour
{
    private VisualElement _root;

    // 关卡名
    private Label _levelNameDisplay;
    private TextField _levelNameInput;
    private Button _levelNameEditBtn;

    // 显示名
    private Label _displayNameDisplay;
    private TextField _displayNameInput;
    private Button _displayNameEditBtn;
    private DropdownField _bgmDropdown;
    private DropdownField _levelCompleteSfxDropdown;

    // 标签
    private VisualElement _tagChipContainer;
    private TextField _tagInput;
    private Button _tagAddBtn;
    private DropdownField _tagDropdown;

    // 难度
    private SliderInt _ratingSlider;
    private VisualElement _starContainer;
    private Label _ratingNumber;

    // 通关
    private Label _solvableText;

    // 评论
    private Label _commentDisplay;
    private TextField _commentInput;
    private Button _commentEditBtn;

    // 操作
    private Button _saveCloseBtn;
    private Button _discardCloseBtn;

    // 依赖
    private EditorMetadataController _controller;
    private EditorFileController _fileController;
    private EditorStateModel _state;

    // 状态
    private bool _isLevelNameEditing;
    private bool _isDisplayNameEditing;
    private bool _isCommentEditing;
    private readonly List<string> _dropdownTagList = new List<string>();
    private readonly List<string> _availableBgmPaths = new List<string>();
    private readonly List<string> _availableLevelCompleteSfxPaths = new List<string>();

    private void Start()
    {
        _controller = FindAnyObjectByType<EditorMetadataController>();
        _fileController = FindAnyObjectByType<EditorFileController>();
        _state = FindAnyObjectByType<EditorStateModel>();

        var hud = FindAnyObjectByType<EditorHUDView>();
        if (hud == null || hud.RootVisualElement == null) return;
        _root = hud.RootVisualElement.Q("metadata-root");
        if (_root == null) return;

        // 查询元素
        _levelNameDisplay = _root.Q<Label>("level-name-display");
        _levelNameInput = _root.Q<TextField>("level-name-input");
        _levelNameEditBtn = _root.Q<Button>("level-name-edit-btn");

        _displayNameDisplay = _root.Q<Label>("display-name-display");
        _displayNameInput = _root.Q<TextField>("display-name-input");
        _displayNameEditBtn = _root.Q<Button>("display-name-edit-btn");
        _bgmDropdown = _root.Q<DropdownField>("bgm-dropdown");
        _levelCompleteSfxDropdown = _root.Q<DropdownField>("level-complete-sfx-dropdown");

        _tagChipContainer = _root.Q("tag-chip-container");
        _tagInput = _root.Q<TextField>("tag-input");
        _tagAddBtn = _root.Q<Button>("tag-add-btn");
        _tagDropdown = _root.Q<DropdownField>("tag-dropdown");

        _ratingSlider = _root.Q<SliderInt>("rating-slider");
        _starContainer = _root.Q("star-container");
        _ratingNumber = _root.Q<Label>("rating-number");

        _solvableText = _root.Q<Label>("solvable-text");

        _commentDisplay = _root.Q<Label>("comment-display");
        _commentInput = _root.Q<TextField>("comment-input");
        _commentEditBtn = _root.Q<Button>("comment-edit-btn");

        _saveCloseBtn = _root.Q<Button>("save-close-btn");
        _discardCloseBtn = _root.Q<Button>("discard-close-btn");

#if UNITY_6000_0_OR_NEWER
        if (_commentInput != null)
            _commentInput.verticalScrollerVisibility = ScrollerVisibility.Auto;
#endif

        // 注册回调
        _levelNameEditBtn?.RegisterCallback<ClickEvent>(_ => OnLevelNameEditClicked());
        _displayNameEditBtn?.RegisterCallback<ClickEvent>(_ => OnDisplayNameEditClicked());

        _tagAddBtn?.RegisterCallback<ClickEvent>(_ => AddTagFromInput());
        _tagInput?.RegisterCallback<KeyDownEvent>(e =>
        {
            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                AddTagFromInput();
                e.PreventDefault();
                e.StopPropagation();
            }
        });
        _tagDropdown?.RegisterValueChangedCallback(OnTagDropdownChanged);
        _bgmDropdown?.RegisterValueChangedCallback(OnBgmDropdownChanged);
        _levelCompleteSfxDropdown?.RegisterValueChangedCallback(OnLevelCompleteSfxDropdownChanged);

        _ratingSlider?.RegisterValueChangedCallback(OnRatingSliderChanged);

        _commentEditBtn?.RegisterCallback<ClickEvent>(_ => OnCommentEditClicked());

        _saveCloseBtn?.RegisterCallback<ClickEvent>(_ => OnSaveAndClose());
        _discardCloseBtn?.RegisterCallback<ClickEvent>(_ => OnDiscardAndClose());

        InitBgmDropdown();
        InitLevelCompleteSfxDropdown();

        // 初始隐藏
        _root.style.display = DisplayStyle.None;
    }

    private void Update()
    {
        if (_root == null || _root.style.display == DisplayStyle.None) return;
        UpdateSolvableDisplay();
    }

    // ════════════════════════════════════════
    //  面板开关
    // ════════════════════════════════════════

    public void TogglePanel()
    {
        if (_root == null) return;

        bool isVisible = _root.style.display == DisplayStyle.Flex;
        _root.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;

        if (!isVisible)
            RefreshUI();
    }

    private void OnSaveAndClose()
    {
        if (_isLevelNameEditing)
            SaveLevelName();
        if (_isDisplayNameEditing)
            SaveDisplayName();
        if (_isCommentEditing)
            SaveComment();

        if (_fileController != null && _state != null)
            _fileController.SaveMetadataOnly(_state.CurrentLevel.LevelName);
        _controller?.SaveGlobalAudioSettings();

        if (_root != null)
            _root.style.display = DisplayStyle.None;
    }

    private void OnDiscardAndClose()
    {
        _isLevelNameEditing = false;
        _isDisplayNameEditing = false;
        _isCommentEditing = false;

        if (_root != null)
            _root.style.display = DisplayStyle.None;
    }

    // ════════════════════════════════════════
    //  整体刷新
    // ════════════════════════════════════════

    public void RefreshUI()
    {
        if (_controller == null) return;

        RefreshTagChips();
        RefreshTagDropdown();
        RefreshRatingDisplay();
        UpdateSolvableDisplay();
        RefreshLevelNameDisplay();
        RefreshDisplayNameDisplay();
        RefreshCommentDisplay();
        RefreshBgmDropdown();
        RefreshLevelCompleteSfxDropdown();
        SetLevelNameEditMode(false);
        SetDisplayNameEditMode(false);
        SetCommentEditMode(false);
    }

    // ════════════════════════════════════════
    //  标签 —— Chips
    // ════════════════════════════════════════

    private void RefreshTagChips()
    {
        if (_tagChipContainer == null || _controller == null) return;

        _tagChipContainer.Clear();

        var tags = _controller.GetTags();
        foreach (var tag in tags)
            CreateTagChip(tag);
    }

    private void CreateTagChip(string tag)
    {
        var chip = new VisualElement();
        chip.AddToClassList("tag-chip");

        var label = new Label(tag);
        label.AddToClassList("tag-chip__label");
        chip.Add(label);

        var removeBtn = new Button(() =>
        {
            _controller.RemoveTag(tag);
            RefreshTagChips();
            RefreshTagDropdown();
        });
        removeBtn.text = "\u00D7";
        removeBtn.AddToClassList("tag-chip__remove");
        chip.Add(removeBtn);

        _tagChipContainer.Add(chip);
    }

    // ════════════════════════════════════════
    //  标签 —— 输入 & Dropdown
    // ════════════════════════════════════════

    private void AddTagFromInput()
    {
        if (_tagInput == null || _controller == null) return;

        string text = _tagInput.value;
        if (string.IsNullOrWhiteSpace(text)) return;

        _controller.AddTag(text);
        _tagInput.value = "";

        RefreshTagChips();
        RefreshTagDropdown();
    }

    private void OnTagDropdownChanged(ChangeEvent<string> evt)
    {
        if (_tagInput == null || string.IsNullOrEmpty(evt.newValue)) return;

        // 跳过占位项
        if (evt.newValue == "选择已有标签...") return;

        _tagInput.value = evt.newValue;

        // 重置回占位项
        _tagDropdown?.SetValueWithoutNotify("选择已有标签...");
    }

    private void RefreshTagDropdown()
    {
        if (_tagDropdown == null || _controller == null) return;

        _dropdownTagList.Clear();

        var available = _controller.SearchKnownTags("");
        _dropdownTagList.AddRange(available);

        var choices = new List<string> { "选择已有标签..." };
        choices.AddRange(available);

        _tagDropdown.choices = choices;
        _tagDropdown.SetValueWithoutNotify("选择已有标签...");
    }

    // ════════════════════════════════════════
    //  难度评分
    // ════════════════════════════════════════

    private void OnRatingSliderChanged(ChangeEvent<int> evt)
    {
        if (_controller == null) return;

        float rating = evt.newValue / 2f;
        _controller.SetDifficultyRating(rating);
        RefreshRatingDisplay();
    }

    private void RefreshRatingDisplay()
    {
        if (_controller == null) return;

        float rating = _controller.GetDifficultyRating();

        _ratingSlider?.SetValueWithoutNotify(Mathf.RoundToInt(rating * 2f));

        // 星星
        if (_starContainer != null)
        {
            _starContainer.Clear();
            for (int i = 1; i <= 5; i++)
            {
                var star = new Label();
                star.AddToClassList("star");

                if (rating >= i)
                {
                    star.text = "\u2605";
                    star.AddToClassList("star--full");
                }
                else if (rating >= i - 0.5f)
                {
                    star.text = "\u2605";
                    star.AddToClassList("star--half");
                }
                else
                {
                    star.text = "\u2606";
                    star.AddToClassList("star--empty");
                }

                _starContainer.Add(star);
            }
        }

        if (_ratingNumber != null)
        {
            _ratingNumber.text = rating <= 0f ? "未评分" : $"{rating:0.#}/5";
        }
    }

    // ════════════════════════════════════════
    //  关卡名称
    // ════════════════════════════════════════

    private void OnLevelNameEditClicked()
    {
        if (_isLevelNameEditing)
            SaveLevelName();
        else
            SetLevelNameEditMode(true);
    }

    private void SetLevelNameEditMode(bool editing)
    {
        _isLevelNameEditing = editing;

        if (_levelNameDisplay != null)
            SetVisible(_levelNameDisplay, !editing);
        if (_levelNameInput != null)
        {
            SetVisible(_levelNameInput, editing);
            if (editing && _state != null)
                _levelNameInput.value = _state.CurrentLevel.LevelName ?? "";
        }
        if (_levelNameEditBtn != null)
            _levelNameEditBtn.text = editing ? "保存" : "编辑";
    }

    private void SaveLevelName()
    {
        if (_fileController == null || _levelNameInput == null || _state == null)
        {
            SetLevelNameEditMode(false);
            return;
        }

        string proposed = _levelNameInput.value;
        if (!_fileController.TryRenameLevel(proposed, out string error))
        {
            ShowAlert(error);
            return;
        }

        SetLevelNameEditMode(false);
        RefreshLevelNameDisplay();
    }

    private void RefreshLevelNameDisplay()
    {
        if (_levelNameDisplay == null || _state == null) return;

        string name = _state.CurrentLevel.LevelName;
        _levelNameDisplay.text = string.IsNullOrWhiteSpace(name) ? "（未命名）" : name;
    }

    private static void ShowAlert(string message)
    {
#if UNITY_EDITOR
        EditorUtility.DisplayDialog("提示", message, "确定");
#else
        Debug.LogWarning(message);
#endif
    }

    // ════════════════════════════════════════
    //  显示名
    // ════════════════════════════════════════

    private void OnDisplayNameEditClicked()
    {
        if (_isDisplayNameEditing)
            SaveDisplayName();
        else
            SetDisplayNameEditMode(true);
    }

    private void SetDisplayNameEditMode(bool editing)
    {
        _isDisplayNameEditing = editing;

        if (_displayNameDisplay != null)
            SetVisible(_displayNameDisplay, !editing);
        if (_displayNameInput != null)
        {
            SetVisible(_displayNameInput, editing);
            if (editing && _controller != null)
                _displayNameInput.value = _controller.GetDisplayName();
        }
        if (_displayNameEditBtn != null)
            _displayNameEditBtn.text = editing ? "保存" : "编辑";
    }

    private void SaveDisplayName()
    {
        if (_controller != null && _displayNameInput != null)
            _controller.SetDisplayName(_displayNameInput.value);

        SetDisplayNameEditMode(false);
        RefreshDisplayNameDisplay();
    }

    private void RefreshDisplayNameDisplay()
    {
        if (_displayNameDisplay == null || _controller == null) return;

        string displayName = _controller.GetDisplayName();
        _displayNameDisplay.text = string.IsNullOrWhiteSpace(displayName) ? "（使用文件名）" : displayName;
    }

    private void InitBgmDropdown()
    {
        if (_bgmDropdown == null) return;

        _availableBgmPaths.Clear();
        var clips = Resources.LoadAll<AudioClip>("Sound/BGM");
        foreach (var clip in clips)
            _availableBgmPaths.Add("Sound/BGM/" + clip.name);

        if (!_availableBgmPaths.Contains(LevelMetadata.DefaultBgmPath))
            _availableBgmPaths.Insert(0, LevelMetadata.DefaultBgmPath);

        _bgmDropdown.choices = _availableBgmPaths;
    }

    private void RefreshBgmDropdown()
    {
        if (_bgmDropdown == null || _controller == null) return;

        string current = _controller.GetBgmPath();
        if (!_availableBgmPaths.Contains(current))
        {
            _availableBgmPaths.Add(current);
            _bgmDropdown.choices = _availableBgmPaths;
        }
        _bgmDropdown.SetValueWithoutNotify(current);
    }

    private void OnBgmDropdownChanged(ChangeEvent<string> evt)
    {
        if (_controller == null) return;
        _controller.SetBgmPath(evt.newValue);
    }

    private void InitLevelCompleteSfxDropdown()
    {
        if (_levelCompleteSfxDropdown == null) return;

        _availableLevelCompleteSfxPaths.Clear();
        var clips = Resources.LoadAll<AudioClip>("Sound/SFX");
        foreach (var clip in clips)
            _availableLevelCompleteSfxPaths.Add("Sound/SFX/" + clip.name);

        if (!_availableLevelCompleteSfxPaths.Contains(AudioSettingsData.DefaultLevelCompleteSfxPath))
            _availableLevelCompleteSfxPaths.Insert(0, AudioSettingsData.DefaultLevelCompleteSfxPath);

        _levelCompleteSfxDropdown.choices = _availableLevelCompleteSfxPaths;
    }

    private void RefreshLevelCompleteSfxDropdown()
    {
        if (_levelCompleteSfxDropdown == null || _controller == null) return;

        string current = _controller.GetLevelCompleteSfxPath();
        if (!_availableLevelCompleteSfxPaths.Contains(current))
        {
            _availableLevelCompleteSfxPaths.Add(current);
            _levelCompleteSfxDropdown.choices = _availableLevelCompleteSfxPaths;
        }
        _levelCompleteSfxDropdown.SetValueWithoutNotify(current);
    }

    private void OnLevelCompleteSfxDropdownChanged(ChangeEvent<string> evt)
    {
        if (_controller == null) return;
        _controller.SetLevelCompleteSfxPath(evt.newValue);
    }

    // ════════════════════════════════════════
    //  通关状态
    // ════════════════════════════════════════

    private void UpdateSolvableDisplay()
    {
        if (_solvableText == null || _controller == null) return;

        bool solvable = _controller.GetIsSolvable();

        _solvableText.text = solvable ? "\u221A 可通关" : "\u00D7 未验证";
        _solvableText.EnableInClassList("solvable--yes", solvable);
        _solvableText.EnableInClassList("solvable--no", !solvable);
    }

    // ════════════════════════════════════════
    //  评论
    // ════════════════════════════════════════

    private void OnCommentEditClicked()
    {
        if (_isCommentEditing)
            SaveComment();
        else
            SetCommentEditMode(true);
    }

    private void SetCommentEditMode(bool editing)
    {
        _isCommentEditing = editing;

        if (_commentDisplay != null)
            SetVisible(_commentDisplay, !editing);
        if (_commentInput != null)
        {
            SetVisible(_commentInput, editing);
            if (editing && _controller != null)
                _commentInput.value = _controller.GetComment();
        }
        if (_commentEditBtn != null)
            _commentEditBtn.text = editing ? "保存" : "编辑";
    }

    private void SaveComment()
    {
        if (_controller != null && _commentInput != null)
            _controller.SetComment(_commentInput.value);

        SetCommentEditMode(false);
        RefreshCommentDisplay();
    }

    private void RefreshCommentDisplay()
    {
        if (_commentDisplay == null || _controller == null) return;

        string comment = _controller.GetComment();
        _commentDisplay.text = string.IsNullOrWhiteSpace(comment) ? "（无评论）" : comment;
    }

    // ════════════════════════════════════════
    //  工具方法
    // ════════════════════════════════════════

    private static void SetVisible(VisualElement element, bool visible)
    {
        if (visible)
            element.RemoveFromClassList("hidden");
        else
            element.AddToClassList("hidden");
    }
}
