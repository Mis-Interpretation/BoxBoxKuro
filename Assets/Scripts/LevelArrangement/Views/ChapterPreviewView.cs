using System;
using UnityEngine.UIElements;

/// <summary>
/// 右面板：选中章节（而非关卡）时的详情预览。
/// </summary>
public class ChapterPreviewView
{
    private readonly VisualElement _chapterDetails;
    private readonly Label _nameLabel;
    private readonly Label _commentLabel;
    private readonly Label _unlockLabel;
    private readonly Label _levelCountLabel;
    private readonly Label _warningLabel;
    private readonly Button _editBtn;

    public event Action OnEditRequested;

    public ChapterPreviewView(VisualElement root)
    {
        _chapterDetails = root.Q("chapter-details");
        _nameLabel = root.Q<Label>("chapter-detail-name");
        _commentLabel = root.Q<Label>("chapter-detail-comment");
        _unlockLabel = root.Q<Label>("chapter-detail-unlock");
        _levelCountLabel = root.Q<Label>("chapter-detail-level-count");
        _warningLabel = root.Q<Label>("chapter-detail-warning");
        _editBtn = root.Q<Button>("chapter-edit-btn");

        if (_editBtn != null)
            _editBtn.clicked += () => OnEditRequested?.Invoke();
    }

    public void Show(ChapterData chapter, int index, ArrangementStateModel state)
    {
        if (_chapterDetails == null || chapter == null) return;

        _chapterDetails.RemoveFromClassList("hidden");

        if (_nameLabel != null)
            _nameLabel.text = chapter.ChapterName;

        if (_commentLabel != null)
            _commentLabel.text = string.IsNullOrWhiteSpace(chapter.Comment) ? "（无备注）" : chapter.Comment;

        if (_unlockLabel != null)
            _unlockLabel.text = FormatUnlock(chapter.Unlock);

        if (_levelCountLabel != null)
        {
            string onlineTag = chapter.IsOnline ? "已上线" : "未上线（草稿）";
            _levelCountLabel.text = $"包含 {chapter.Levels.Count} 个关卡 · {onlineTag}";
        }

        if (_warningLabel != null)
        {
            int unverified = state != null ? state.CountUnverifiedLevelsInChapter(chapter) : 0;
            if (unverified > 0)
            {
                _warningLabel.text = $"\u26A0 含 {unverified} 个未验证关卡";
                _warningLabel.RemoveFromClassList("hidden");
            }
            else
            {
                _warningLabel.text = "";
                _warningLabel.AddToClassList("hidden");
            }
        }
    }

    public void Hide()
    {
        _chapterDetails?.AddToClassList("hidden");
    }

    private static string FormatUnlock(UnlockCondition unlock)
    {
        if (unlock == null) return "始终开放";

        switch (unlock.Type)
        {
            case UnlockType.AlwaysOpen:
                return "解锁条件: 始终开放";
            case UnlockType.ClearPreviousChapter:
                return "解锁条件: 通关前一章";
            case UnlockType.StarCount:
                return $"解锁条件: 累计通关 {unlock.RequiredStars} 关";
            default:
                return "解锁条件: 未知";
        }
    }
}
