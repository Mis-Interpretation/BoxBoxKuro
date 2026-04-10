using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 编辑器试玩（Validate）功能。
/// 进入试玩时：隐藏编辑器对象，用当前关卡数据生成可游玩实体并启用游戏逻辑。
/// 退出试玩时：销毁游玩实体，恢复编辑器状态。
/// </summary>
public class EditorValidateController : MonoBehaviour
{
    private EditorStateModel _state;
    private EditorPlacementController _placement;
    private EditorMetadataController _metadata;
    private EditorGridView _gridView;
    private EditorHUDView _hud;
    private EditorPaletteView _palette;
    private EditorCameraPanView _cameraPan;
    private CameraFitController _cameraFit;

    private bool _isValidating;
    private List<GameObject> _spawnedEntities;
    private GameObject _gameplayHost;

    private Vector3 _savedCameraPos;
    private float _savedOrthoSize;

    /// <summary>
    /// 当前是否处于试玩模式。
    /// </summary>
    public bool IsValidating => _isValidating;

    /// <summary>
    /// 试玩模式下的游戏逻辑宿主对象，供求解器附加回放组件。
    /// </summary>
    public GameObject GameplayHost => _gameplayHost;

    private void Awake()
    {
        _state = FindAnyObjectByType<EditorStateModel>();
        _placement = FindAnyObjectByType<EditorPlacementController>();
        _metadata = FindAnyObjectByType<EditorMetadataController>();
        _gridView = FindAnyObjectByType<EditorGridView>();
        _hud = FindAnyObjectByType<EditorHUDView>();
        _palette = FindAnyObjectByType<EditorPaletteView>();
        _cameraPan = FindAnyObjectByType<EditorCameraPanView>();
        _cameraFit = FindAnyObjectByType<CameraFitController>();
    }

    private void Update()
    {
        if (!_isValidating) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StopValidation();
        }
    }

    /// <summary>
    /// 开始试玩当前关卡。
    /// </summary>
    public void StartValidation()
    {
        if (_isValidating) return;

        if (_state.CurrentLevel.Entities.Count == 0)
        {
            Debug.LogWarning("当前关卡为空，无法试玩！");
            return;
        }

        _isValidating = true;

        // 隐藏编辑器对象
        SetEditorObjectsActive(false);

        // 禁用编辑器交互和 UI
        if (_placement != null) _placement.enabled = false;
        if (_gridView != null) _gridView.enabled = false;
        if (_hud != null) _hud.SetMainHudVisible(false);
        if (_palette != null) _palette.SetVisible(false);
        if (_cameraPan != null) _cameraPan.enabled = false;
        if (_hud != null) _hud.SetValidateTipsVisible(true);

        // 将相机居中到关卡
        if (_cameraFit != null)
        {
            var cam = _cameraFit.GetComponent<Camera>();
            _savedCameraPos = _cameraFit.transform.position;
            _savedOrthoSize = cam.orthographicSize;
            _cameraFit.FitToLevel(_state.CurrentLevel);
        }

        // 试玩时播放当前关卡 BGM（与正式游玩保持一致）
        if (_metadata != null)
            AudioController.Instance.PlayBgm(_metadata.GetBgmPath());
        else
            AudioController.Instance.PlayDefaultBgm();

        // 使用共享生成器创建可游玩的实体
        _spawnedEntities = LevelSpawner.SpawnEntities(_state.CurrentLevel, _state.EntityFactory);

        // 创建临时游戏逻辑宿主
        _gameplayHost = new GameObject("[ValidateGameplay]");
        var moveController = _gameplayHost.AddComponent<MoveController>();
        _gameplayHost.AddComponent<InputController>();
        var gameRule = _gameplayHost.AddComponent<GameRuleController>();
        gameRule.OnLevelComplete += OnLevelComplete;

        moveController.SaveInitialPositions();

        Debug.Log("开始试玩，按 Escape 退出");
    }

    /// <summary>
    /// 结束试玩，恢复编辑器状态。
    /// </summary>
    public void StopValidation()
    {
        if (!_isValidating) return;

        _isValidating = false;

        // 销毁游玩实体
        if (_spawnedEntities != null)
        {
            foreach (var obj in _spawnedEntities)
            {
                if (obj != null) Destroy(obj);
            }
            _spawnedEntities = null;
        }

        // 销毁游戏逻辑宿主
        if (_gameplayHost != null)
        {
            Destroy(_gameplayHost);
            _gameplayHost = null;
        }

        // 恢复相机状态
        if (_cameraFit != null)
        {
            _cameraFit.transform.position = _savedCameraPos;
            _cameraFit.GetComponent<Camera>().orthographicSize = _savedOrthoSize;
        }

        // 恢复编辑器对象
        SetEditorObjectsActive(true);

        // 恢复编辑器交互和 UI
        if (_placement != null) _placement.enabled = true;
        if (_gridView != null) _gridView.enabled = true;
        if (_hud != null) _hud.SetMainHudVisible(true);
        if (_palette != null) _palette.SetVisible(true);
        if (_cameraPan != null) _cameraPan.enabled = true;
        if (_hud != null) _hud.SetValidateTipsVisible(false);

        // 退出试玩后停止 BGM
        AudioController.Instance.StopBgm();

        Debug.Log("试玩结束，已恢复编辑器状态");
    }

    private void OnLevelComplete()
    {
        Debug.Log("试玩通关！");
        if (_metadata != null) _metadata.MarkSolvable();
        StopValidation();
    }

    /// <summary>
    /// 显示/隐藏编辑器中已放置的所有对象。
    /// </summary>
    private void SetEditorObjectsActive(bool active)
    {
        foreach (var kvp in _state.PlacedObjects)
        {
            foreach (var obj in kvp.Value)
            {
                if (obj != null) obj.SetActive(active);
            }
        }
    }
}
