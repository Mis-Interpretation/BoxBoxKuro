using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

/// <summary>
/// 底部面板：未分配关卡列表 UI 逻辑。
/// </summary>
public class UnassignedLevelView
{
    private readonly VisualElement _container;
    private readonly VisualTreeAsset _itemTemplate;
    private ArrangementStateModel _state;

    public event Action<string> OnAddToChapterRequested;
    public event Action<string> OnLevelSelected;

    public UnassignedLevelView(VisualElement container, VisualTreeAsset itemTemplate)
    {
        _container = container;
        _itemTemplate = itemTemplate;
    }

    public void Bind(ArrangementStateModel state)
    {
        _state = state;
    }

    public void Refresh()
    {
        _container.Clear();

        if (_state == null) return;

        var unassigned = _state.GetUnassignedLevels();

        foreach (var levelName in unassigned)
        {
            var item = CreateItem(levelName);
            _container.Add(item);
        }
    }

    private VisualElement CreateItem(string levelName)
    {
        VisualElement item;

        if (_itemTemplate != null)
        {
            item = _itemTemplate.Instantiate();
        }
        else
        {
            item = new VisualElement();
            item.AddToClassList("unassigned-item");
            item.Add(new Label { name = "unassigned-name" });
            item.Add(new Label { name = "unassigned-meta" });
            item.Add(new Button { name = "unassigned-add-btn", text = "+ 添加" });
        }

        var nameLabel = item.Q<Label>("unassigned-name");
        if (nameLabel != null)
            nameLabel.text = levelName;

        // 元数据摘要
        var metaLabel = item.Q<Label>("unassigned-meta");
        if (metaLabel != null && _state.MetadataCache.TryGetValue(levelName, out var meta))
        {
            string stars = meta.DifficultyRating > 0 ? MetadataDisplayHelper.BuildStarText(meta.DifficultyRating) : "";
            string tags = meta.Tags != null && meta.Tags.Count > 0 ? string.Join(", ", meta.Tags) : "";
            metaLabel.text = string.Join(" ", new[] { stars, tags }).Trim();
        }

        // 点击选中预览（排除按钮点击）
        string capturedName = levelName;
        var root = item.Q(className: "unassigned-item") ?? item;
        root.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target is Button) return;
            OnLevelSelected?.Invoke(capturedName);
        });

        // 添加到当前章按钮 — 使用 clicked 事件
        var addBtn = item.Q<Button>("unassigned-add-btn");
        if (addBtn != null)
        {
            addBtn.clicked += () => OnAddToChapterRequested?.Invoke(capturedName);
        }

        return item;
    }
}
