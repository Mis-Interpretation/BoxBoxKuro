using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// 实体配置编辑器场景的入口 MonoBehaviour。
/// 挂载在场景的 UIDocument 同一 GameObject 上。
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class EntityConfigController : MonoBehaviour
{
    private UIDocument _uiDocument;
    private VisualElement _root;

    private EntityConfigFileController _fileController;
    private EntityConfigListView _listView;
    private EntityConfigInspectorView _inspectorView;
    private EntityConfigNewEntityModalView _newEntityModal;
    private EntityConfigDeleteModalView _deleteModal;
    private VisualElement _exitConfirmOverlay;

    private List<EntityConfigData> _entities;
    private string _selectedEntityId;
    private List<string> _availableSprites;
    private List<string> _availableComponents;
    private List<string> _availableSfxPaths;

    private void OnEnable()
    {
        EnsureEventSystem();

        _uiDocument = GetComponent<UIDocument>();
        _root = _uiDocument.rootVisualElement;

        if (_root == null)
        {
            Debug.LogError("EntityConfigController: rootVisualElement 为 null，请检查 UIDocument 的 Source Asset 是否已赋值。");
            return;
        }

        _fileController = new EntityConfigFileController();
        _availableSprites = ScanAvailableSprites();
        _availableComponents = new List<string>(EntityComponentRegistry.AllKeys());
        _availableSfxPaths = ScanAvailableSfxPaths();

        InitViews();
        BindToolbar();
        LoadEntities();
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        Debug.Log("[EntityConfig] 已自动创建 EventSystem + InputSystemUIInputModule");
    }

    // ════════════════════════════════════════
    //  初始化
    // ════════════════════════════════════════

    private void InitViews()
    {
        // 左侧列表
        var listContainer = _root.Q("entity-list-container");
        _listView = new EntityConfigListView(listContainer);
        _listView.OnEntitySelected += OnEntitySelected;

        // 右侧 Inspector
        _inspectorView = new EntityConfigInspectorView(
            _root.Q("inspector-panel"),
            _availableSprites,
            _availableComponents,
            _availableSfxPaths);
        _inspectorView.OnApplyRequested += OnApplyChanges;
        _inspectorView.OnCancelRequested += OnCancelEdit;
        _inspectorView.OnDeleteRequested += OnDeleteRequested;

        // 新建实体弹窗
        _newEntityModal = new EntityConfigNewEntityModalView(_root);
        _newEntityModal.OnConfirmed += OnNewEntityConfirmed;

        // 删除确认弹窗
        _deleteModal = new EntityConfigDeleteModalView(_root);
        _deleteModal.OnConfirmed += OnDeleteConfirmed;
    }

    private void BindToolbar()
    {
        var saveBtn = _root.Q<Button>("save-btn");
        if (saveBtn != null) saveBtn.clicked += OnSave;

        var newBtn = _root.Q<Button>("new-entity-btn");
        if (newBtn != null) newBtn.clicked += OnNewEntity;

        var backBtn = _root.Q<Button>("back-btn");
        if (backBtn != null) backBtn.clicked += ShowExitConfirm;

        _exitConfirmOverlay = _root.Q("exit-confirm-overlay");
        var exitSaveBtn = _root.Q<Button>("exit-save-btn");
        var exitDiscardBtn = _root.Q<Button>("exit-discard-btn");
        var exitCancelBtn = _root.Q<Button>("exit-cancel-btn");
        exitSaveBtn?.RegisterCallback<ClickEvent>(_ => OnExitSave());
        exitDiscardBtn?.RegisterCallback<ClickEvent>(_ => OnExitDiscard());
        exitCancelBtn?.RegisterCallback<ClickEvent>(_ => HideExitConfirm());
    }

    // ════════════════════════════════════════
    //  数据加载
    // ════════════════════════════════════════

    private void LoadEntities()
    {
        _entities = _fileController.LoadAll();
        _selectedEntityId = null;
        RefreshAll();
    }

    private void RefreshAll()
    {
        _listView.Refresh(_entities, _selectedEntityId);
        RefreshInspector();
    }

    private void RefreshInspector()
    {
        if (string.IsNullOrEmpty(_selectedEntityId))
        {
            _inspectorView.ShowEmpty();
            return;
        }

        var entity = _entities.Find(e => e.Id == _selectedEntityId);
        if (entity == null)
        {
            _inspectorView.ShowEmpty();
            return;
        }

        _inspectorView.Show(entity);
    }

    // ════════════════════════════════════════
    //  实体选择
    // ════════════════════════════════════════

    private void OnEntitySelected(string entityId)
    {
        _selectedEntityId = entityId;
        RefreshAll();
    }

    // ════════════════════════════════════════
    //  编辑操作
    // ════════════════════════════════════════

    private void OnApplyChanges(EntityConfigData updated)
    {
        if (updated == null) return;

        int index = _entities.FindIndex(e => e.Id == updated.Id);
        if (index < 0) return;

        var existing = _entities[index];
        if (existing.TypeIndex < 4)
        {
            // 基础实体：允许修改名称、Sprite、组件音效覆盖，其余字段保持不变
            existing.DisplayName = updated.DisplayName;
            existing.SpritePath = updated.SpritePath;
            existing.ComponentSfxOverrides = EntityConfigFileController.DeepCopy(updated).ComponentSfxOverrides;
            _entities[index] = existing;
        }
        else
        {
            _entities[index] = updated;
        }

        Debug.Log($"[EntityConfig] 已应用修改: {updated.Id} (TypeIndex={existing.TypeIndex})");
        RefreshAll();
    }

    private void OnCancelEdit()
    {
        RefreshInspector();
    }

    // ════════════════════════════════════════
    //  删除操作
    // ════════════════════════════════════════

    private void OnDeleteRequested(string entityId)
    {
        var entity = _entities.Find(e => e.Id == entityId);
        if (entity == null) return;

        if (entity.TypeIndex < 4)
        {
            Debug.LogWarning($"[EntityConfig] 基础实体 {entityId}（TypeIndex={entity.TypeIndex}）不可删除。");
            return;
        }

        string displayName = string.IsNullOrEmpty(entity.DisplayName) ? entity.Id : entity.DisplayName;
        _deleteModal.Show(entityId, displayName);
    }

    private void OnDeleteConfirmed(string entityId)
    {
        _entities.RemoveAll(e => e.Id == entityId);

        if (_selectedEntityId == entityId)
            _selectedEntityId = null;

        Debug.Log($"[EntityConfig] 已删除实体: {entityId}");
        RefreshAll();
    }

    // ════════════════════════════════════════
    //  新建实体
    // ════════════════════════════════════════

    private void OnNewEntity()
    {
        _newEntityModal.Show(_availableSprites);
    }

    private void OnNewEntityConfirmed(string id, string displayName, string spritePath)
    {
        // 校验 Id 唯一性
        if (_entities.Any(e => e.Id == id))
        {
            _newEntityModal.ShowError($"Id \"{id}\" 已存在，请使用不同的 Id。");
            return;
        }

        // 分配 TypeIndex
        int maxIndex = _entities.Count > 0 ? _entities.Max(e => e.TypeIndex) : -1;
        int newTypeIndex = maxIndex + 1;

        var newEntity = new EntityConfigData
        {
            Id = id,
            DisplayName = string.IsNullOrEmpty(displayName) ? id : displayName,
            TypeIndex = newTypeIndex,
            SpritePath = spritePath ?? "",
            OrderInLayer = 1,
            Components = new List<string>(),
            ComponentSfx = new List<ComponentSfxEntry>(),
            ComponentSfxOverrides = new List<ComponentSfxEntry>(),
            IsPureDecoration = false,
            IsTextEntity = false
        };

        _entities.Add(newEntity);
        _selectedEntityId = id;

        _newEntityModal.Hide();
        Debug.Log($"[EntityConfig] 已新建实体: {id} (TypeIndex={newTypeIndex})");
        RefreshAll();
    }

    // ════════════════════════════════════════
    //  保存 / 导航
    // ════════════════════════════════════════

    private void OnSave()
    {
        _fileController.Save(_entities);
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
        OnSave();
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

    // ════════════════════════════════════════
    //  工具方法
    // ════════════════════════════════════════

    private List<string> ScanAvailableSprites()
    {
        var textures = Resources.LoadAll<Texture2D>("Sprites");
        var paths = new List<string> { "none" };
        foreach (var t in textures)
            paths.Add("Sprites/" + t.name);
        return paths;
    }

    private List<string> ScanAvailableSfxPaths()
    {
        var clips = Resources.LoadAll<AudioClip>("Sound/SFX");
        var paths = new List<string> { "none" };
        foreach (var clip in clips)
            paths.Add("Sound/SFX/" + clip.name);
        return paths;
    }
}
