using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 中面板：关卡列表 UI 逻辑。利用 ListView 的内置拖拽排序。
/// </summary>
public class LevelListView
{
    private sealed class LevelRowUserData
    {
        public EventCallback<ClickEvent> ClickHandler;
    }

    private readonly ListView _listView;
    private readonly Label _headerLabel;
    private readonly VisualTreeAsset _levelItemTemplate;
    private ArrangementStateModel _state;
    private List<string> _currentLevels = new List<string>();
    private int _chapterIndex = -1;

    public event Action<string> OnLevelSelected;
    public event Action<int> OnLevelRemoveRequested;
    public event Action<int, int> OnLevelReordered;

    public LevelListView(ListView listView, Label headerLabel, VisualTreeAsset levelItemTemplate)
    {
        _listView = listView;
        _headerLabel = headerLabel;
        _levelItemTemplate = levelItemTemplate;

        _listView.makeItem = MakeItem;
        _listView.bindItem = BindItem;
        _listView.selectionType = SelectionType.Single;
        _listView.selectionChanged += OnSelectionChanged;
        _listView.reorderable = true;
        _listView.reorderMode = ListViewReorderMode.Animated;
        _listView.itemIndexChanged += HandleItemIndexChanged;
        _listView.fixedItemHeight = 52;
    }

    public void Bind(ArrangementStateModel state)
    {
        _state = state;
    }

    public void Refresh(int chapterIndex)
    {
        _chapterIndex = chapterIndex;

        if (_state == null || chapterIndex < 0 || chapterIndex >= _state.Campaign.Chapters.Count)
        {
            _headerLabel.text = "请选择一个大关卡";
            _currentLevels = new List<string>();
            _listView.itemsSource = _currentLevels;
            _listView.Rebuild();
            return;
        }

        var chapter = _state.Campaign.Chapters[chapterIndex];
        _headerLabel.text = $"第 {chapterIndex + 1} 章: {chapter.ChapterName}";
        _currentLevels = chapter.Levels;
        _listView.itemsSource = _currentLevels;
        _listView.Rebuild();
    }

    private static bool IsClickUnderRemoveButton(EventBase evt)
    {
        if (evt.target is not VisualElement ve)
            return false;
        for (var p = ve; p != null; p = p.parent)
        {
            if (p is Button b && b.name == "level-remove-btn")
                return true;
        }
        return false;
    }

    private VisualElement MakeItem()
    {
        if (_levelItemTemplate != null)
            return _levelItemTemplate.Instantiate();

        var row = new VisualElement();
        row.AddToClassList("level-item");
        row.Add(new Label { name = "level-handle", text = "\u2261" });
        row.Add(new Label { name = "level-index" });
        var main = new VisualElement { name = "level-main" };
        main.AddToClassList("level-item__main");
        main.Add(new Label { name = "level-name" });
        var tags = new VisualElement { name = "level-tags" };
        tags.AddToClassList("tag-container");
        tags.AddToClassList("level-item__tags");
        main.Add(tags);
        row.Add(main);
        row.Add(new Label { name = "level-stars" });
        row.Add(new Label { name = "level-solvable" });
        var removeBtn = new Button { name = "level-remove-btn", text = "\u2715" };
        row.Add(removeBtn);
        return row;
    }

    private void BindItem(VisualElement element, int index)
    {
        if (index < 0 || index >= _currentLevels.Count) return;

        string levelName = _currentLevels[index];
        bool isMissing = _state != null && _state.MetadataCache.ContainsKey(levelName)
            && _state.MetadataCache[levelName].Comment == "[文件不存在]";

        var root = element.Q(className: "level-item") ?? element;

        if (root.userData is LevelRowUserData oldUd && oldUd.ClickHandler != null)
            root.UnregisterCallback(oldUd.ClickHandler);

        int rowIndex = index;
        EventCallback<ClickEvent> clickHandler = evt =>
        {
            if (IsClickUnderRemoveButton(evt))
                return;
            if (rowIndex < 0 || rowIndex >= _currentLevels.Count)
                return;
            string name = _currentLevels[rowIndex];
            _listView.schedule.Execute(() => OnLevelSelected?.Invoke(name)).ExecuteLater(0);
        };
        root.userData = new LevelRowUserData { ClickHandler = clickHandler };
        root.RegisterCallback(clickHandler, TrickleDown.TrickleDown);

        // 序号
        var indexLabel = element.Q<Label>("level-index");
        if (indexLabel != null)
            indexLabel.text = $"{_chapterIndex + 1}-{index + 1}";

        // 关卡名
        var nameLabel = element.Q<Label>("level-name");
        if (nameLabel != null)
            nameLabel.text = levelName;

        // 标记缺失文件
        if (isMissing)
            root.AddToClassList("level-item--missing");
        else
            root.RemoveFromClassList("level-item--missing");

        // 元数据摘要
        var tagsContainer = element.Q("level-tags");
        if (tagsContainer != null)
            tagsContainer.Clear();

        if (_state != null && _state.MetadataCache.TryGetValue(levelName, out var meta))
        {
            var starsLabel = element.Q<Label>("level-stars");
            if (starsLabel != null)
                starsLabel.text = meta.DifficultyRating > 0 ? MetadataDisplayHelper.BuildStarText(meta.DifficultyRating) : "";

            var solvableLabel = element.Q<Label>("level-solvable");
            if (solvableLabel != null)
                solvableLabel.text = meta.IsSolvable ? "\u2714" : "";

            if (tagsContainer != null && meta.Tags != null)
            {
                foreach (var tag in meta.Tags)
                {
                    var chip = new Label(tag);
                    chip.AddToClassList("tag-chip");
                    tagsContainer.Add(chip);
                }
            }
        }
        else
        {
            var starsLabel = element.Q<Label>("level-stars");
            if (starsLabel != null)
                starsLabel.text = "";
            var solvableLabel = element.Q<Label>("level-solvable");
            if (solvableLabel != null)
                solvableLabel.text = "";
        }

        // 选中高亮
        if (levelName == _state?.SelectedLevelName)
            root.AddToClassList("level-item--selected");
        else
            root.RemoveFromClassList("level-item--selected");

        // 移除按钮
        var removeBtn = element.Q<Button>("level-remove-btn");
        if (removeBtn != null)
        {
            removeBtn.clickable = new Clickable(() => OnLevelRemoveRequested?.Invoke(index));
        }
    }

    private void OnSelectionChanged(IEnumerable<object> _)
    {
        // 不能 foreach 参数里的集合：它与 ListView 内部 selectedItems 共享枚举状态；
        // 回调里 Refresh/Rebuild 会改 itemsSource，导致 “Collection was modified”。
        // 用 selectedItem + schedule 延后执行，避免在 NotifyOfSelectionChange 栈内 Rebuild。
        if (_listView.selectedItem is not string levelName)
            return;

        var captured = levelName;
        _listView.schedule.Execute(() => OnLevelSelected?.Invoke(captured)).ExecuteLater(0);
    }

    private void HandleItemIndexChanged(int oldIndex, int newIndex)
    {
        var o = oldIndex;
        var n = newIndex;
        _listView.schedule.Execute(() => OnLevelReordered?.Invoke(o, n)).ExecuteLater(0);
    }
}
