using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// 编排场景的入口 MonoBehaviour。挂载在场景的 UIDocument 同一 GameObject 上。
/// 负责初始化所有 View、绑定 UI 事件、协调数据操作。
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class ArrangementController : MonoBehaviour
{
    [Header("UXML 模板")]
    [SerializeField] private VisualTreeAsset _chapterItemTemplate;
    [SerializeField] private VisualTreeAsset _levelItemTemplate;
    [SerializeField] private VisualTreeAsset _unassignedItemTemplate;

    private UIDocument _uiDocument;
    private VisualElement _root;

    // MVC 组件
    private ArrangementStateModel _state;
    private ArrangementFileController _fileController;
    private ChapterListView _chapterListView;
    private LevelListView _levelListView;
    private LevelPreviewView _previewView;
    private ChapterPreviewView _chapterPreviewView;
    private ChapterEditModalView _chapterEditModalView;
    private UnassignedLevelView _unassignedView;
    private Button _previewEditBtn;
    private VisualElement _newLevelModal;
    private TextField _newLevelNameField;
    private Label _newLevelErrorLabel;

    private VisualElement _exitConfirmOverlay;
    private VisualElement _chapterDeleteConfirmOverlay;
    private Label _chapterDeleteConfirmMessage;
    private int _pendingChapterDeleteIndex = -1;

    // 使用 OnEnable 确保 UIDocument.rootVisualElement 已就绪
    private void OnEnable()
    {
        EnsureEventSystem();

        _uiDocument = GetComponent<UIDocument>();
        _root = _uiDocument.rootVisualElement;

        if (_root == null)
        {
            Debug.LogError("ArrangementController: rootVisualElement 为 null，请检查 UIDocument 的 Source Asset 是否已赋值。");
            return;
        }

        _state = new ArrangementStateModel();
        _fileController = new ArrangementFileController();

        InitViews();
        BindToolbar();
        LoadAll();
    }

    /// <summary>
    /// 确保场景中有 EventSystem + InputSystemUIInputModule。
    /// Unity 6 + New Input System 环境下 UI Toolkit 运行时面板必须依赖它来接收点击/拖拽事件。
    /// </summary>
    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();

        // New Input System 的输入模块
        var inputModule = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        Debug.Log("[Arrangement] 已自动创建 EventSystem + InputSystemUIInputModule");
    }

    // ════════════════════════════════════════
    //  初始化
    // ════════════════════════════════════════

    private void InitViews()
    {
        // 左面板：章节列表
        var chapterList = _root.Q<ListView>("chapter-list");
        if (chapterList == null)
        {
            Debug.LogError("ArrangementController: 未找到 ListView#chapter-list，请检查 ArrangementMain.uxml。");
            return;
        }
        _chapterListView = new ChapterListView(chapterList, _chapterItemTemplate);
        _chapterListView.OnChapterSelected += OnChapterSelected;
        _chapterListView.OnChapterDeleteRequested += OnChapterDeleteRequested;
        _chapterListView.OnChapterRenamed += OnChapterRenamed;
        _chapterListView.OnChapterReordered += OnChapterReordered;

        // 中面板：关卡列表
        var levelList = _root.Q<ListView>("level-list");
        var chapterHeader = _root.Q<Label>("chapter-header");
        _levelListView = new LevelListView(levelList, chapterHeader, _levelItemTemplate);
        _levelListView.OnLevelSelected += OnLevelSelected;
        _levelListView.OnLevelRemoveRequested += OnLevelRemove;
        _levelListView.OnLevelReordered += OnLevelReordered;

        // 右面板：预览
        _previewView = new LevelPreviewView(_root.Q("preview-panel"));

        // 右面板：章节预览
        _chapterPreviewView = new ChapterPreviewView(_root.Q("preview-panel"));
        _chapterPreviewView.OnEditRequested += OnChapterEditRequested;

        // 章节编辑模态框
        _chapterEditModalView = new ChapterEditModalView(_root);
        _chapterEditModalView.OnConfirmed += OnChapterEditConfirmed;

        // 底部：未分配关卡
        var unassignedContainer = _root.Q("unassigned-container");
        _unassignedView = new UnassignedLevelView(unassignedContainer, _unassignedItemTemplate);
        _unassignedView.OnAddToChapterRequested += OnAddLevelToCurrentChapter;
        _unassignedView.OnLevelSelected += OnLevelSelected;

        // 绑定 state
        _chapterListView.Bind(_state);
        _levelListView.Bind(_state);
        _unassignedView.Bind(_state);

        _previewEditBtn = _root.Q<Button>("preview-edit-btn");
        if (_previewEditBtn != null)
            _previewEditBtn.clicked += OnPreviewEditClicked;
    }

    private void BindToolbar()
    {
        var saveBtn = _root.Q<Button>("save-btn");
        Debug.Log($"[Arrangement] save-btn found: {saveBtn != null}");
        if (saveBtn != null) saveBtn.clicked += () => { Debug.Log("[Arrangement] 保存按钮被点击"); SaveCampaign(); };

        var addChapterBtn = _root.Q<Button>("add-chapter-btn");
        Debug.Log($"[Arrangement] add-chapter-btn found: {addChapterBtn != null}");
        if (addChapterBtn != null) addChapterBtn.clicked += () => { Debug.Log("[Arrangement] +大关卡按钮被点击"); AddChapter(); };

        var backBtn = _root.Q<Button>("back-btn");
        Debug.Log($"[Arrangement] back-btn found: {backBtn != null}");
        if (backBtn != null) backBtn.clicked += () => { Debug.Log("[Arrangement] 返回按钮被点击"); ShowExitConfirm(); };

        _exitConfirmOverlay = _root.Q("exit-confirm-overlay");
        var exitSaveBtn = _root.Q<Button>("exit-save-btn");
        var exitDiscardBtn = _root.Q<Button>("exit-discard-btn");
        var exitCancelBtn = _root.Q<Button>("exit-cancel-btn");
        exitSaveBtn?.RegisterCallback<ClickEvent>(_ => OnExitSave());
        exitDiscardBtn?.RegisterCallback<ClickEvent>(_ => OnExitDiscard());
        exitCancelBtn?.RegisterCallback<ClickEvent>(_ => HideExitConfirm());

        _chapterDeleteConfirmOverlay = _root.Q("chapter-delete-confirm-overlay");
        _chapterDeleteConfirmMessage = _root.Q<Label>("chapter-delete-confirm-message");
        var chapterDeleteConfirmBtn = _root.Q<Button>("chapter-delete-confirm-btn");
        var chapterDeleteCancelBtn = _root.Q<Button>("chapter-delete-cancel-btn");
        chapterDeleteConfirmBtn?.RegisterCallback<ClickEvent>(_ => OnChapterDeleteConfirmed());
        chapterDeleteCancelBtn?.RegisterCallback<ClickEvent>(_ => HideChapterDeleteConfirm());

        var unassignedNewBtn = _root.Q<Button>("unassigned-new-btn");
        if (unassignedNewBtn != null)
            unassignedNewBtn.clicked += ShowNewLevelDialog;

        BindNewLevelModal();

        // 诊断：在 root 上注册全局指针事件，检测鼠标事件是否到达 UI Toolkit 面板
        _root.RegisterCallback<PointerDownEvent>(evt =>
        {
            Debug.Log($"[Arrangement] PointerDown 到达 root, target={evt.target.GetType().Name}, name={((VisualElement)evt.target).name}");
        });
    }

    // ════════════════════════════════════════
    //  数据加载
    // ════════════════════════════════════════

    private void LoadAll()
    {
        _state.Campaign = _fileController.LoadCampaign();
        _state.AllLevelFiles = _fileController.ScanAllLevels();
        _state.MetadataCache = _fileController.LoadAllMetadata(_state.AllLevelFiles);

        // 校验引用完整性
        var existingSet = new HashSet<string>(_state.AllLevelFiles);
        var missing = _fileController.ValidateReferences(_state.Campaign, existingSet);
        if (missing.Count > 0)
            Debug.LogWarning($"campaign.json 引用了 {missing.Count} 个不存在的关卡文件: {string.Join(", ", missing)}");

        _state.SelectedChapterIndex = _state.Campaign.Chapters.Count > 0 ? 0 : -1;
        _state.SelectedLevelName = "";

        RefreshAll();
    }

    private void RefreshAll()
    {
        _chapterListView.Refresh();
        _levelListView.Refresh(_state.SelectedChapterIndex);
        _unassignedView.Refresh();
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        if (string.IsNullOrEmpty(_state.SelectedLevelName))
        {
            // 未选中关卡，检查是否选中了章节
            if (_state.SelectedChapterIndex >= 0 &&
                _state.SelectedChapterIndex < _state.Campaign.Chapters.Count)
            {
                _previewView.ShowEmpty();
                _chapterPreviewView.Show(
                    _state.Campaign.Chapters[_state.SelectedChapterIndex],
                    _state.SelectedChapterIndex,
                    _state);
            }
            else
            {
                _previewView.ShowEmpty();
                _chapterPreviewView.Hide();
            }
            UpdatePreviewEditButton();
            return;
        }

        _chapterPreviewView.Hide();

        if (!_state.MetadataCache.TryGetValue(_state.SelectedLevelName, out var meta))
        {
            _previewView.ShowEmpty();
            UpdatePreviewEditButton();
            return;
        }

        string displayIndex = GetLevelDisplayIndex(_state.SelectedLevelName);
        _previewView.Show(meta, displayIndex);
        UpdatePreviewEditButton();
    }

    private void UpdatePreviewEditButton()
    {
        if (_previewEditBtn == null) return;

        if (string.IsNullOrEmpty(_state.SelectedLevelName))
        {
            _previewEditBtn.style.display = DisplayStyle.None;
            return;
        }

        _previewEditBtn.style.display = DisplayStyle.Flex;

        bool canEdit = _state.MetadataCache.TryGetValue(_state.SelectedLevelName, out var meta)
            && meta.Comment != "[文件不存在]";
        _previewEditBtn.SetEnabled(canEdit);
    }

    private void OnPreviewEditClicked()
    {
        if (string.IsNullOrEmpty(_state.SelectedLevelName)) return;
        if (!_state.MetadataCache.TryGetValue(_state.SelectedLevelName, out var meta))
        {
            Debug.LogWarning("无法编辑：未找到该关卡的元数据缓存。");
            return;
        }

        if (meta.Comment == "[文件不存在]")
        {
            Debug.LogWarning($"无法编辑：关卡文件不存在 ({_state.SelectedLevelName})。");
            return;
        }

        if (!TryAutoSaveCampaignBeforeOpenEditor())
            return;

        LevelEditorPendingLoad.SetPendingLevel(_state.SelectedLevelName);
        OpenLevelEditorScene();
    }

    /// <summary>
    /// 从编排场景跳转到编辑器前自动保存 campaign，避免未手动点击保存导致数据丢失。
    /// </summary>
    private bool TryAutoSaveCampaignBeforeOpenEditor()
    {
        try
        {
            _fileController.SaveCampaign(_state.Campaign);
            Debug.Log("[Arrangement] 已在进入 LevelEditor 前自动保存 campaign。");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Arrangement] 自动保存 campaign 失败，已取消进入 LevelEditor：{e.Message}");
            return false;
        }
    }

    private static void OpenLevelEditorScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNameModel.EditorScene);
    }

    // ════════════════════════════════════════
    //  章节操作
    // ════════════════════════════════════════

    private void AddChapter()
    {
        var chapter = new ChapterData
        {
            ChapterName = $"Chapter {_state.Campaign.Chapters.Count + 1}"
        };
        _state.Campaign.Chapters.Add(chapter);
        _state.SelectedChapterIndex = _state.Campaign.Chapters.Count - 1;
        RefreshAll();
    }

    private void OnChapterSelected(int index)
    {
        _state.SelectedChapterIndex = index;
        _state.SelectedLevelName = "";
        RefreshAll();
    }

    private void OnChapterDeleteRequested(int index)
    {
        if (index < 0 || index >= _state.Campaign.Chapters.Count) return;

        _pendingChapterDeleteIndex = index;
        string name = _state.Campaign.Chapters[index].ChapterName ?? "";
        if (_chapterDeleteConfirmMessage != null)
            _chapterDeleteConfirmMessage.text = $"确定要删除大关卡「{name}」吗？关卡文件不会被删除，仅从本战役中移除该章节。";

        _chapterDeleteConfirmOverlay?.RemoveFromClassList("hidden");
    }

    private void HideChapterDeleteConfirm()
    {
        _pendingChapterDeleteIndex = -1;
        _chapterDeleteConfirmOverlay?.AddToClassList("hidden");
    }

    private void OnChapterDeleteConfirmed()
    {
        int index = _pendingChapterDeleteIndex;
        HideChapterDeleteConfirm();
        PerformChapterDelete(index);
    }

    private void PerformChapterDelete(int index)
    {
        if (index < 0 || index >= _state.Campaign.Chapters.Count) return;

        _state.Campaign.Chapters.RemoveAt(index);

        if (_state.SelectedChapterIndex >= _state.Campaign.Chapters.Count)
            _state.SelectedChapterIndex = _state.Campaign.Chapters.Count - 1;

        _state.SelectedLevelName = "";
        RefreshAll();
    }

    private void OnChapterEditRequested()
    {
        if (_state.SelectedChapterIndex < 0 ||
            _state.SelectedChapterIndex >= _state.Campaign.Chapters.Count) return;

        _chapterEditModalView.Show(_state.Campaign.Chapters[_state.SelectedChapterIndex]);
    }

    private void OnChapterEditConfirmed(string name, string comment, UnlockCondition unlock, bool isOnline)
    {
        if (_state.SelectedChapterIndex < 0 ||
            _state.SelectedChapterIndex >= _state.Campaign.Chapters.Count) return;

        var chapter = _state.Campaign.Chapters[_state.SelectedChapterIndex];
        chapter.ChapterName = name;
        chapter.Comment = comment;
        chapter.Unlock = unlock;
        chapter.IsOnline = isOnline;
        RefreshAll();
    }

    private void OnChapterRenamed(int index, string newName)
    {
        if (index < 0 || index >= _state.Campaign.Chapters.Count) return;
        _state.Campaign.Chapters[index].ChapterName = newName;
        RefreshAll();
    }

    private void OnChapterReordered(int oldIndex, int newIndex)
    {
        int selected = _state.SelectedChapterIndex;
        if (selected < 0)
        {
            RefreshAll();
            return;
        }

        // ListView 已经改动了 Chapters 顺序；这里仅修正选中索引，使“同一个章节”仍保持选中。
        if (selected == oldIndex)
            selected = newIndex;
        else if (oldIndex < newIndex)
        {
            // oldIndex -> newIndex（向下拖）
            if (selected > oldIndex && selected <= newIndex)
                selected -= 1;
        }
        else if (newIndex < oldIndex)
        {
            // oldIndex -> newIndex（向上拖）
            if (selected >= newIndex && selected < oldIndex)
                selected += 1;
        }

        _state.SelectedChapterIndex = selected;
        RefreshAll();
    }

    // ════════════════════════════════════════
    //  关卡操作
    // ════════════════════════════════════════

    private void OnLevelSelected(string levelName)
    {
        _state.SelectedLevelName = levelName;
        RefreshPreview();
        // 刷新列表以更新选中高亮
        _levelListView.Refresh(_state.SelectedChapterIndex);
    }

    private void OnLevelRemove(int levelIndex)
    {
        if (_state.SelectedChapterIndex < 0) return;
        var chapter = _state.Campaign.Chapters[_state.SelectedChapterIndex];
        if (levelIndex < 0 || levelIndex >= chapter.Levels.Count) return;

        string removedLevel = chapter.Levels[levelIndex];
        chapter.Levels.RemoveAt(levelIndex);

        if (_state.SelectedLevelName == removedLevel)
            _state.SelectedLevelName = "";

        RefreshAll();
    }

    private void OnLevelReordered(int oldIndex, int newIndex)
    {
        // ListView 的 reorderable 已经修改了 itemsSource 列表中的顺序，
        // 只需刷新显示即可。
        _levelListView.Refresh(_state.SelectedChapterIndex);
    }

    private void OnAddLevelToCurrentChapter(string levelName)
    {
        if (_state.SelectedChapterIndex < 0)
        {
            Debug.LogWarning("请先选择一个大关卡！");
            return;
        }

        var chapter = _state.Campaign.Chapters[_state.SelectedChapterIndex];
        if (chapter.Levels.Contains(levelName))
        {
            Debug.LogWarning($"关卡 {levelName} 已在该章节中！");
            return;
        }

        chapter.Levels.Add(levelName);
        RefreshAll();
    }

    private void BindNewLevelModal()
    {
        _newLevelModal = _root.Q("new-level-modal");
        _newLevelNameField = _root.Q<TextField>("new-level-name-field");
        _newLevelErrorLabel = _root.Q<Label>("new-level-error");

        var confirmBtn = _root.Q<Button>("new-level-confirm");
        var cancelBtn = _root.Q<Button>("new-level-cancel");
        if (confirmBtn != null)
            confirmBtn.clicked += OnConfirmNewLevelDialog;
        if (cancelBtn != null)
            cancelBtn.clicked += HideNewLevelDialog;

        _newLevelNameField?.RegisterCallback<KeyDownEvent>(OnNewLevelNameKeyDown, TrickleDown.TrickleDown);

        if (_newLevelNameField != null)
        {
            _newLevelNameField.RegisterCallback<AttachToPanelEvent>(_ => ApplyNewLevelTextFieldStyles(_newLevelNameField));
            ApplyNewLevelTextFieldStyles(_newLevelNameField);
        }
    }

    /// <summary>
    /// TextField 内文字由 TextElement 绘制；全局 USS 的 * 颜色会继承到 TextElement，需显式设为黑色。
    /// </summary>
    private static void ApplyNewLevelTextFieldStyles(TextField field)
    {
        if (field == null) return;

        var black = Color.black;
        field.Query<TextElement>().ForEach(te => te.style.color = black);

        var input = field.Q(className: "unity-base-field__input")
            ?? field.Q(className: "unity-text-field__input");
        if (input != null)
            input.style.color = black;
    }

    private void OnNewLevelNameKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
            return;
        evt.StopImmediatePropagation();
        OnConfirmNewLevelDialog();
    }

    private void ShowNewLevelDialog()
    {
        if (_newLevelModal == null)
            return;

        if (_newLevelErrorLabel != null)
            _newLevelErrorLabel.text = "";
        if (_newLevelNameField != null)
            _newLevelNameField.value = "";

        _newLevelModal.RemoveFromClassList("hidden");
        _newLevelNameField?.schedule.Execute(() =>
        {
            ApplyNewLevelTextFieldStyles(_newLevelNameField);
            _newLevelNameField.Focus();
        }).ExecuteLater(0);
    }

    private void HideNewLevelDialog()
    {
        _newLevelModal?.AddToClassList("hidden");
    }

    private void OnConfirmNewLevelDialog()
    {
        if (_newLevelNameField == null)
            return;

        string input = _newLevelNameField.value;
        if (!_fileController.TryCreateNewEmptyLevel(input, out var created, out var err))
        {
            if (_newLevelErrorLabel != null)
                _newLevelErrorLabel.text = err ?? "无法创建关卡";
            return;
        }

        HideNewLevelDialog();
        _state.AllLevelFiles = _fileController.ScanAllLevels();
        _state.MetadataCache = _fileController.LoadAllMetadata(_state.AllLevelFiles);
        _state.SelectedLevelName = created;
        RefreshAll();
    }

    // ════════════════════════════════════════
    //  工具方法
    // ════════════════════════════════════════

    private string GetLevelDisplayIndex(string levelName)
    {
        for (int c = 0; c < _state.Campaign.Chapters.Count; c++)
        {
            var levels = _state.Campaign.Chapters[c].Levels;
            for (int l = 0; l < levels.Count; l++)
            {
                if (levels[l] == levelName)
                    return $"{c + 1}-{l + 1}";
            }
        }
        return "未分配";
    }

    // ════════════════════════════════════════
    //  工具栏
    // ════════════════════════════════════════

    private void SaveCampaign()
    {
        _fileController.SaveCampaign(_state.Campaign);
    }

    private void ShowExitConfirm()
    {
        _exitConfirmOverlay?.RemoveFromClassList("hidden");
    }

    private void HideExitConfirm()
    {
        _exitConfirmOverlay?.AddToClassList("hidden");
    }

    private void OnExitSave()
    {
        SaveCampaign();
        LoadStartScene();
    }

    private void OnExitDiscard()
    {
        LoadStartScene();
    }

    private static void LoadStartScene()
    {
        SceneManager.LoadScene(SceneNameModel.StartScene);
    }
}
