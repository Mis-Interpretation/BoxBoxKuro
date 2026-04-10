using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 左面板：章节列表的 UI 逻辑。负责渲染章节条目、处理选择/删除/重命名。
/// </summary>
public class ChapterListView
{
    private sealed class ChapterRowUserData
    {
        public EventCallback<ClickEvent> ClickHandler;
        public Action DeleteHandler;
    }

    private readonly ListView _listView;
    private readonly VisualTreeAsset _chapterItemTemplate;
    private ArrangementStateModel _state;

    public event Action<int> OnChapterSelected;
    public event Action<int> OnChapterDeleteRequested;
    public event Action<int, string> OnChapterRenamed;
    public event Action<int, int> OnChapterReordered;

    public ChapterListView(ListView listView, VisualTreeAsset chapterItemTemplate)
    {
        _listView = listView;
        _chapterItemTemplate = chapterItemTemplate;

        _listView.makeItem = MakeItem;
        _listView.bindItem = BindItem;
        _listView.selectionType = SelectionType.Single;
        _listView.reorderable = true;
        _listView.reorderMode = ListViewReorderMode.Animated;
        _listView.itemIndexChanged += HandleItemIndexChanged;
        _listView.fixedItemHeight = 44;
    }

    public void Bind(ArrangementStateModel state)
    {
        _state = state;
    }

    public void Refresh()
    {
        if (_state == null)
        {
            _listView.itemsSource = new List<ChapterData>();
            _listView.Rebuild();
            return;
        }

        _listView.itemsSource = _state.Campaign.Chapters;
        _listView.Rebuild();
    }

    private VisualElement MakeItem()
    {
        if (_chapterItemTemplate != null)
            return _chapterItemTemplate.Instantiate();

        var item = new VisualElement();
        item.AddToClassList("chapter-item");
        item.Add(new Label { name = "chapter-index" });
        item.Add(new Label { name = "chapter-name" });
        var warn = new Label { name = "chapter-warning", text = "" };
        warn.AddToClassList("chapter-item__warning");
        item.Add(warn);
        item.Add(new Label { name = "chapter-count" });
        item.Add(new Button { name = "chapter-delete-btn", text = "\u00D7" });
        return item;
    }

    private void BindItem(VisualElement element, int index)
    {
        if (_state == null || index < 0 || index >= _state.Campaign.Chapters.Count)
            return;

        var chapter = _state.Campaign.Chapters[index];
        var root = element.Q(className: "chapter-item") ?? element;

        // 清理旧回调（避免 Rebuild 后叠加）
        if (root.userData is ChapterRowUserData oldUd)
        {
            if (oldUd.ClickHandler != null)
                root.UnregisterCallback(oldUd.ClickHandler);

            var oldDeleteBtn = element.Q<Button>("chapter-delete-btn");
            if (oldDeleteBtn != null && oldUd.DeleteHandler != null)
                oldDeleteBtn.clicked -= oldUd.DeleteHandler;
        }

        // 填充数据
        var indexLabel = element.Q<Label>("chapter-index");
        if (indexLabel != null)
            indexLabel.text = (index + 1).ToString();

        var nameLabel = element.Q<Label>("chapter-name");
        if (nameLabel != null)
            nameLabel.text = chapter.IsOnline ? chapter.ChapterName : $"[草稿] {chapter.ChapterName}";

        var countLabel = element.Q<Label>("chapter-count");
        if (countLabel != null)
            countLabel.text = $"({chapter.Levels.Count})";

        var warningLabel = element.Q<Label>("chapter-warning");
        if (warningLabel != null)
        {
            int unverified = _state.CountUnverifiedLevelsInChapter(chapter);
            warningLabel.text = unverified > 0 ? "\u26A0" : "";
        }

        // 样式
        if (index == _state.SelectedChapterIndex)
            root.AddToClassList("chapter-item--selected");
        else
            root.RemoveFromClassList("chapter-item--selected");

        if (!chapter.IsOnline)
            root.AddToClassList("chapter-item--offline");
        else
            root.RemoveFromClassList("chapter-item--offline");

        // 单击选中 / 双击重命名（整行可拖拽，不加 handle 限制）
        int capturedIndex = index;
        var capturedName = chapter.ChapterName;
        EventCallback<ClickEvent> clickHandler = evt =>
        {
            // 如果点击的是删除按钮，不触发选中
            if (evt.target is Button) return;

            if (evt.clickCount == 2)
                StartRename(root, capturedIndex, capturedName);
            else
                OnChapterSelected?.Invoke(capturedIndex);
        };
        root.RegisterCallback(clickHandler);

        // 删除按钮
        var deleteBtn = element.Q<Button>("chapter-delete-btn");
        Action deleteHandler = () => OnChapterDeleteRequested?.Invoke(capturedIndex);
        if (deleteBtn != null)
            deleteBtn.clicked += deleteHandler;

        root.userData = new ChapterRowUserData
        {
            ClickHandler = clickHandler,
            DeleteHandler = deleteHandler
        };
    }

    private void HandleItemIndexChanged(int oldIndex, int newIndex)
    {
        var o = oldIndex;
        var n = newIndex;
        _listView.schedule.Execute(() => OnChapterReordered?.Invoke(o, n)).ExecuteLater(0);
    }

    private void StartRename(VisualElement chapterRoot, int index, string currentName)
    {
        var nameLabel = chapterRoot.Q<Label>("chapter-name");
        if (nameLabel == null) return;

        nameLabel.style.display = DisplayStyle.None;

        var textField = new TextField { value = currentName };
        textField.AddToClassList("chapter-rename-field");
        chapterRoot.Insert(1, textField);
        textField.Focus();

        void CommitRename()
        {
            string newName = textField.value?.Trim();
            if (!string.IsNullOrEmpty(newName) && newName != currentName)
                OnChapterRenamed?.Invoke(index, newName);

            textField.RemoveFromHierarchy();
            nameLabel.style.display = DisplayStyle.Flex;
        }

        textField.RegisterCallback<FocusOutEvent>(evt => CommitRename());
        textField.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                CommitRename();
            else if (evt.keyCode == KeyCode.Escape)
            {
                textField.RemoveFromHierarchy();
                nameLabel.style.display = DisplayStyle.Flex;
            }
        });
    }
}
