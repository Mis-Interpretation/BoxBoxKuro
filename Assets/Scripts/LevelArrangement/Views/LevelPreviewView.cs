using UnityEngine.UIElements;

/// <summary>
/// 右面板：选中关卡的元数据详细预览。
/// </summary>
public class LevelPreviewView
{
    private readonly VisualElement _previewContent;
    private readonly Label _placeholder;
    private readonly VisualElement _details;
    private readonly Label _nameLabel;
    private readonly Label _indexLabel;
    private readonly Label _sizeLabel;
    private readonly Label _entityCountLabel;
    private readonly VisualElement _tagsContainer;
    private readonly Label _starsLabel;
    private readonly Label _solvableLabel;
    private readonly Label _commentLabel;

    public LevelPreviewView(VisualElement root)
    {
        _previewContent = root.Q("preview-content");
        _placeholder = root.Q<Label>("preview-placeholder");
        _details = root.Q("preview-details");
        _nameLabel = root.Q<Label>("preview-name");
        _indexLabel = root.Q<Label>("preview-index");
        _sizeLabel = root.Q<Label>("preview-size");
        _entityCountLabel = root.Q<Label>("preview-entity-count");
        _tagsContainer = root.Q("preview-tags");
        _starsLabel = root.Q<Label>("preview-stars");
        _solvableLabel = root.Q<Label>("preview-solvable");
        _commentLabel = root.Q<Label>("preview-comment");
    }

    public void ShowEmpty()
    {
        if (_placeholder != null)
            _placeholder.style.display = DisplayStyle.Flex;
        if (_details != null)
            _details.style.display = DisplayStyle.None;
    }

    /// <summary>
    /// 显示指定关卡的元数据预览。
    /// </summary>
    /// <param name="meta">关卡元数据摘要</param>
    /// <param name="displayIndex">展示用序号，如 "1-3" 或 "未分配"</param>
    public void Show(LevelMetadataSummary meta, string displayIndex)
    {
        if (meta == null)
        {
            ShowEmpty();
            return;
        }

        if (_placeholder != null)
            _placeholder.style.display = DisplayStyle.None;
        if (_details != null)
            _details.style.display = DisplayStyle.Flex;
        // Ensure the hidden class is removed so the details are visible
        _details?.RemoveFromClassList("hidden");

        if (_nameLabel != null)
        {
            string displayName = string.IsNullOrEmpty(meta.DisplayName) ? meta.LevelName : meta.DisplayName;
            _nameLabel.text = displayName;
        }

        if (_indexLabel != null)
            _indexLabel.text = displayIndex;

        if (_sizeLabel != null)
            _sizeLabel.text = $"尺寸: {meta.Width} \u00d7 {meta.Height}";

        if (_entityCountLabel != null)
            _entityCountLabel.text = $"实体数: {meta.EntityCount}";

        // 标签芯片
        if (_tagsContainer != null)
        {
            _tagsContainer.Clear();
            if (meta.Tags != null)
            {
                foreach (var tag in meta.Tags)
                {
                    var chip = new Label(tag);
                    chip.AddToClassList("tag-chip");
                    _tagsContainer.Add(chip);
                }
            }
        }

        if (_starsLabel != null)
            _starsLabel.text = MetadataDisplayHelper.BuildStarText(meta.DifficultyRating);

        if (_solvableLabel != null)
            _solvableLabel.text = MetadataDisplayHelper.BuildSolvableText(meta.IsSolvable);

        if (_commentLabel != null)
            _commentLabel.text = string.IsNullOrWhiteSpace(meta.Comment) ? "（无备注）" : meta.Comment;
    }
}
