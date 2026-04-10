using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// 玩家关卡选择界面控制器。
/// 显示章节列表（含解锁状态）和关卡网格（每页最多8个）。
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class LevelSelectController : MonoBehaviour
{
    private const int LevelsPerPage = 8;

    private UIDocument _uiDocument;
    private VisualElement _root;

    private CampaignDataModel _campaign;
    private Dictionary<string, LevelMetadataSummary> _metadataCache;

    private int _selectedChapterIndex = -1;
    private int _currentPage;

    // UI 元素
    private VisualElement _chapterContainer;
    private VisualElement _levelGrid;
    private Label _levelHeader;
    private Label _chapterUnlockCondition;
    private Label _chapterProgressSummary;
    private Label _pageIndicator;
    private Button _prevBtn;
    private Button _nextBtn;

    private void OnEnable()
    {
        EnsureEventSystem();

        _uiDocument = GetComponent<UIDocument>();
        _root = _uiDocument.rootVisualElement;

        _chapterContainer = _root.Q("chapter-container");
        _levelGrid = _root.Q("level-grid");
        _levelHeader = _root.Q<Label>("level-header");
        _chapterUnlockCondition = _root.Q<Label>("chapter-unlock-condition");
        _chapterProgressSummary = _root.Q<Label>("chapter-progress-summary");
        _pageIndicator = _root.Q<Label>("page-indicator");
        _prevBtn = _root.Q<Button>("page-prev-btn");
        _nextBtn = _root.Q<Button>("page-next-btn");

        var backBtn = _root.Q<Button>("back-btn");
        if (backBtn != null) backBtn.clicked += OnBack;
        if (_prevBtn != null) _prevBtn.clicked += () => ChangePage(-1);
        if (_nextBtn != null) _nextBtn.clicked += () => ChangePage(1);

        LoadData();
        PopulateChapters();
        RefreshProgressSummary();
        RefreshLevelGrid();
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    // ════════════════════════════════════════
    //  数据加载
    // ════════════════════════════════════════

    private void LoadData()
    {
        _campaign = CampaignProgressController.LoadCampaign() ?? new CampaignDataModel();
        _metadataCache = new Dictionary<string, LevelMetadataSummary>();

        string levelsDir = Path.Combine(Application.streamingAssetsPath, "Levels");
        if (!Directory.Exists(levelsDir)) return;

        // 收集所有被引用的关卡名
        var levelNames = new HashSet<string>();
        foreach (var ch in _campaign.Chapters)
            foreach (var lv in ch.Levels)
                levelNames.Add(lv);

        // 加载 metadata
        foreach (var name in levelNames)
        {
            string filePath = Path.Combine(levelsDir, name + ".json");
            if (!File.Exists(filePath)) continue;

            string json = File.ReadAllText(filePath);
            var data = JsonUtility.FromJson<LevelDataModel>(json);
            if (data == null) continue;
            data.EnsureMetadata();

            _metadataCache[name] = new LevelMetadataSummary
            {
                LevelName = data.LevelName,
                Width = data.Width,
                Height = data.Height,
                EntityCount = data.Entities?.Count ?? 0,
                Tags = data.Metadata.Tags ?? new List<string>(),
                DifficultyRating = data.Metadata.DifficultyRating,
                IsSolvable = data.Metadata.IsSolvable,
                Comment = data.Metadata.Comment ?? "",
                DisplayName = data.Metadata.DisplayName ?? ""
            };
        }
    }

    // ════════════════════════════════════════
    //  章节列表
    // ════════════════════════════════════════

    private void PopulateChapters()
    {
        if (_chapterContainer == null) return;
        _chapterContainer.Clear();

        for (int i = 0; i < _campaign.Chapters.Count; i++)
        {
            var chapter = _campaign.Chapters[i];
            if (!chapter.IsOnline) continue; // 玩家模式只显示已上线章节
            bool unlocked = CampaignProgressController.IsChapterUnlocked(i);
            int idx = i; // capture

            var item = new VisualElement();
            item.AddToClassList("chapter-select-item");
            if (!unlocked) item.AddToClassList("chapter-select-item--locked");

            var indexLabel = new Label($"{i + 1}");
            indexLabel.AddToClassList("chapter-select-index");
            item.Add(indexLabel);

            var nameLabel = new Label(chapter.ChapterName);
            nameLabel.AddToClassList("chapter-select-name");
            item.Add(nameLabel);

            if (!unlocked)
            {
                // U+1F512 🔒 依赖彩色表情字体，默认 UI 字体常缺字形 → 方框；改用「锁」与中文界面一致且必有字形
                var lockIcon = new Label("\u9501");
                lockIcon.AddToClassList("chapter-select-lock");
                item.Add(lockIcon);
            }

            item.userData = idx;
            item.RegisterCallback<ClickEvent>(_ => SelectChapter(idx));

            _chapterContainer.Add(item);
        }

        // 默认选中第一个"已上线 & 已解锁"的章节
        if (_selectedChapterIndex < 0)
        {
            for (int i = 0; i < _campaign.Chapters.Count; i++)
            {
                if (_campaign.Chapters[i].IsOnline && CampaignProgressController.IsChapterUnlocked(i))
                {
                    _selectedChapterIndex = i;
                    break;
                }
            }
        }

        UpdateChapterSelection();
    }

    private void SelectChapter(int index)
    {
        _selectedChapterIndex = index;
        _currentPage = 0;
        UpdateChapterSelection();
        RefreshLevelGrid();
    }

    private void UpdateChapterSelection()
    {
        if (_chapterContainer == null) return;

        for (int i = 0; i < _chapterContainer.childCount; i++)
        {
            var child = _chapterContainer[i];
            int chapterIndex = child.userData is int value ? value : -1;
            child.EnableInClassList("chapter-select-item--selected", chapterIndex == _selectedChapterIndex);
        }
    }

    // ════════════════════════════════════════
    //  关卡网格
    // ════════════════════════════════════════

    private void RefreshLevelGrid()
    {
        if (_levelGrid == null) return;
        _levelGrid.Clear();

        if (_selectedChapterIndex < 0 || _selectedChapterIndex >= _campaign.Chapters.Count)
        {
            if (_levelHeader != null) _levelHeader.text = "请选择一个章节";
            if (_chapterUnlockCondition != null) _chapterUnlockCondition.text = "解锁条件：-";
            UpdatePagination(0);
            return;
        }

        var chapter = _campaign.Chapters[_selectedChapterIndex];
        if (_levelHeader != null) _levelHeader.text = chapter.ChapterName;
        if (_chapterUnlockCondition != null) _chapterUnlockCondition.text = FormatUnlockCondition(chapter.Unlock);

        bool unlocked = CampaignProgressController.IsChapterUnlocked(_selectedChapterIndex);
        if (!unlocked)
        {
            _levelGrid.Add(BuildLockedHint(chapter.Unlock));
            UpdatePagination(0);
            return;
        }

        int totalLevels = chapter.Levels.Count;
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)totalLevels / LevelsPerPage));
        _currentPage = Mathf.Clamp(_currentPage, 0, totalPages - 1);

        int startIndex = _currentPage * LevelsPerPage;
        int endIndex = Mathf.Min(startIndex + LevelsPerPage, totalLevels);

        for (int i = startIndex; i < endIndex; i++)
        {
            string levelName = chapter.Levels[i];
            int chapterIdx = _selectedChapterIndex;
            int levelIdx = i;

            var card = new VisualElement();
            card.AddToClassList("level-card");

            // 编号
            var indexLabel = new Label($"{chapterIdx + 1}-{levelIdx + 1}");
            indexLabel.AddToClassList("level-card__index");
            card.Add(indexLabel);

            // 显示名
            string displayName = levelName;
            if (_metadataCache.TryGetValue(levelName, out var meta))
            {
                if (!string.IsNullOrEmpty(meta.DisplayName))
                    displayName = meta.DisplayName;
            }
            var nameLabel = new Label(displayName);
            nameLabel.AddToClassList("level-card__name");
            card.Add(nameLabel);

            // 已通关角标
            if (CampaignProgressController.IsLevelCompleted(chapterIdx, levelIdx))
            {
                var clearedBadge = new Label("\u221A");
                clearedBadge.AddToClassList("level-card__cleared-badge");
                card.Add(clearedBadge);
            }

            card.RegisterCallback<ClickEvent>(_ => LoadLevel(chapterIdx, levelIdx));
            _levelGrid.Add(card);
        }

        UpdatePagination(totalPages);
    }

    private void UpdatePagination(int totalPages)
    {
        if (totalPages <= 1)
        {
            if (_prevBtn != null) _prevBtn.style.display = DisplayStyle.None;
            if (_nextBtn != null) _nextBtn.style.display = DisplayStyle.None;
            if (_pageIndicator != null) _pageIndicator.style.display = DisplayStyle.None;
            return;
        }

        if (_prevBtn != null)
        {
            _prevBtn.style.display = DisplayStyle.Flex;
            _prevBtn.SetEnabled(_currentPage > 0);
        }
        if (_nextBtn != null)
        {
            _nextBtn.style.display = DisplayStyle.Flex;
            _nextBtn.SetEnabled(_currentPage < totalPages - 1);
        }
        if (_pageIndicator != null)
        {
            _pageIndicator.style.display = DisplayStyle.Flex;
            _pageIndicator.text = $"{_currentPage + 1} / {totalPages}";
        }
    }

    private void RefreshProgressSummary()
    {
        if (_chapterProgressSummary == null) return;
        int completedCount = CampaignProgressController.GetTotalCompletedLevelCountInOnlineChapters();
        _chapterProgressSummary.text = $"累计通关：{completedCount} 关";
    }

    private static string FormatUnlockCondition(UnlockCondition unlock)
    {
        if (unlock == null) return "解锁条件：始终开放";

        switch (unlock.Type)
        {
            case UnlockType.AlwaysOpen:
                return "解锁条件：始终开放";
            case UnlockType.ClearPreviousChapter:
                return "解锁条件：通关前一章";
            case UnlockType.StarCount:
                return $"解锁条件：累计通关 {unlock.RequiredStars} 关";
            default:
                return "解锁条件：未知";
        }
    }

    private static VisualElement BuildLockedHint(UnlockCondition unlock)
    {
        var hint = new Label("章节未解锁，仅可预览解锁条件");
        hint.AddToClassList("level-grid-hint");
        hint.tooltip = FormatUnlockCondition(unlock);
        return hint;
    }

    private void ChangePage(int delta)
    {
        _currentPage += delta;
        RefreshLevelGrid();
    }

    // ════════════════════════════════════════
    //  导航
    // ════════════════════════════════════════

    private void LoadLevel(int chapterIndex, int levelIndex)
    {
        CampaignProgressController.CurrentChapter = chapterIndex;
        CampaignProgressController.CurrentLevel = levelIndex;
        SceneManager.LoadScene(SceneNameModel.LevelScene);
    }

    private void OnBack()
    {
        SceneManager.LoadScene(SceneNameModel.StartScene);
    }
}
