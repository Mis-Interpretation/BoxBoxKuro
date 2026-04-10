using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 左侧实体列表视图：显示所有实体的 sprite 图标 + 显示名称。
/// </summary>
public class EntityConfigListView
{
    private readonly VisualElement _container;
    public event Action<string> OnEntitySelected;

    public EntityConfigListView(VisualElement container)
    {
        _container = container;
    }

    public void Refresh(List<EntityConfigData> entities, string selectedId)
    {
        _container.Clear();

        foreach (var entity in entities)
        {
            var item = new VisualElement();
            item.AddToClassList("entity-list-item");
            if (entity.Id == selectedId)
                item.AddToClassList("entity-list-item--selected");

            // sprite 图标
            var icon = new VisualElement();
            icon.AddToClassList("entity-list-item__icon");
            var sprite = Resources.Load<Sprite>(entity.SpritePath);
            if (sprite != null)
                icon.style.backgroundImage = new StyleBackground(sprite);
            item.Add(icon);

            // 信息列
            var info = new VisualElement();
            info.AddToClassList("entity-list-item__info");

            var nameLabel = new Label(string.IsNullOrEmpty(entity.DisplayName) ? entity.Id : entity.DisplayName);
            nameLabel.AddToClassList("entity-list-item__name");
            info.Add(nameLabel);

            var idLabel = new Label($"Id: {entity.Id}  |  TypeIndex: {entity.TypeIndex}");
            idLabel.AddToClassList("entity-list-item__id");
            info.Add(idLabel);

            item.Add(info);

            // 点击事件
            string capturedId = entity.Id;
            item.RegisterCallback<PointerDownEvent>(evt =>
            {
                OnEntitySelected?.Invoke(capturedId);
            });

            _container.Add(item);
        }
    }
}
