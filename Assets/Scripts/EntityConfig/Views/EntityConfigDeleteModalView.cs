using System;
using UnityEngine.UIElements;

/// <summary>
/// 删除实体确认弹窗视图。
/// </summary>
public class EntityConfigDeleteModalView
{
    private readonly VisualElement _modal;
    private readonly Label _messageLabel;
    private string _pendingEntityId;

    public event Action<string> OnConfirmed;

    public EntityConfigDeleteModalView(VisualElement root)
    {
        _modal = root.Q("delete-confirm-modal");
        _messageLabel = root.Q<Label>("delete-confirm-message");

        var confirmBtn = root.Q<Button>("delete-confirm-btn");
        var cancelBtn = root.Q<Button>("delete-cancel-btn");

        confirmBtn.clicked += () =>
        {
            if (!string.IsNullOrEmpty(_pendingEntityId))
                OnConfirmed?.Invoke(_pendingEntityId);
            Hide();
        };
        cancelBtn.clicked += Hide;
    }

    public void Show(string entityId, string displayName)
    {
        _pendingEntityId = entityId;
        _messageLabel.text = $"确定要删除实体 \"{displayName}\"（Id: {entityId}）吗？\n此操作不可撤销。";
        _modal.RemoveFromClassList("hidden");
    }

    public void Hide()
    {
        _modal.AddToClassList("hidden");
        _pendingEntityId = null;
    }
}
