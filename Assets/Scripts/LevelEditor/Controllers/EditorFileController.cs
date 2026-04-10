using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 关卡文件的保存/加载/清空。JSON 存储在 StreamingAssets/Levels/ 下。
/// </summary>
public class EditorFileController : MonoBehaviour
{
    private EditorStateModel _state;
    private EditorPlacementController _placement;
    private EditorHUDView _hud;
    private EditorMetadataView _metadataView;
    private EditorUndoController _undo;

    private string LevelsDirectory => Path.Combine(Application.streamingAssetsPath, "Levels");

    private void Awake()
    {
        _state = FindAnyObjectByType<EditorStateModel>();
        _placement = FindAnyObjectByType<EditorPlacementController>();
        _hud = FindAnyObjectByType<EditorHUDView>();
        _metadataView = FindAnyObjectByType<EditorMetadataView>();
        _undo = FindAnyObjectByType<EditorUndoController>();
    }

    public void SaveLevel(string levelName)
    {
        if (string.IsNullOrWhiteSpace(levelName))
        {
            Debug.LogWarning("关卡名不能为空！");
            return;
        }

        _state.CurrentLevel.LevelName = levelName;
        _state.CurrentLevel.EnsureMetadata();

        string directory = LevelsDirectory;
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string json = JsonUtility.ToJson(_state.CurrentLevel, true);
        string filePath = Path.Combine(directory, levelName + ".json");
        File.WriteAllText(filePath, json);

        Debug.Log($"关卡已保存: {filePath}");
        RefreshLevelNameHud();
    }

    private static bool LevelFilePathsAreSame(string pathA, string pathB)
    {
        try
        {
            return string.Equals(Path.GetFullPath(pathA), Path.GetFullPath(pathB), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(pathA, pathB, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// 将关卡文件从当前名改为新名：写新文件、删旧文件；若新名已有其他文件则失败。
    /// 同步更新 campaign.json 中的关卡 id 引用。
    /// </summary>
    public bool TryRenameLevel(string newName, out string errorMessage)
    {
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(newName))
        {
            errorMessage = "关卡名不能为空。";
            return false;
        }

        newName = newName.Trim();
        string oldName = (_state.CurrentLevel.LevelName ?? "").Trim();
        string oldPath = Path.Combine(LevelsDirectory, oldName + ".json");
        string newPath = Path.Combine(LevelsDirectory, newName + ".json");

        bool oldExists = File.Exists(oldPath);
        bool newExists = File.Exists(newPath);

        if (string.Equals(oldName, newName, StringComparison.Ordinal))
        {
            _state.CurrentLevel.LevelName = newName;
            RefreshLevelNameHud();
            return true;
        }

        if (newExists && (!oldExists || !LevelFilePathsAreSame(oldPath, newPath)))
        {
            errorMessage = "已存在同名关卡文件，请换一个名称。";
            return false;
        }

        _state.CurrentLevel.LevelName = newName;
        _state.CurrentLevel.EnsureMetadata();
        string json = JsonUtility.ToJson(_state.CurrentLevel, true);

        if (oldExists && LevelFilePathsAreSame(oldPath, newPath))
        {
            File.WriteAllText(oldPath, json);
            if (!string.Equals(oldName, newName, StringComparison.Ordinal))
                File.Move(oldPath, newPath);
        }
        else if (oldExists)
        {
            File.WriteAllText(newPath, json);
            File.Delete(oldPath);
        }
        else
        {
            File.WriteAllText(newPath, json);
        }

        UpdateCampaignLevelId(oldName, newName);
        RefreshLevelNameHud();
        if (_metadataView != null) _metadataView.RefreshUI();

        Debug.Log($"关卡已重命名: {oldName} → {newName}");
        return true;
    }

    private void UpdateCampaignLevelId(string oldName, string newName)
    {
        if (string.Equals(oldName, newName, StringComparison.Ordinal))
            return;

        var io = new ArrangementFileController();
        var campaign = io.LoadCampaign();
        bool changed = false;
        foreach (var ch in campaign.Chapters)
        {
            if (ch.Levels == null) continue;
            for (int i = 0; i < ch.Levels.Count; i++)
            {
                if (string.Equals(ch.Levels[i], oldName, StringComparison.Ordinal))
                {
                    ch.Levels[i] = newName;
                    changed = true;
                }
            }
        }

        if (changed)
            io.SaveCampaign(campaign);
    }

    private void RefreshLevelNameHud()
    {
        if (_hud != null) _hud.RefreshLevelNameDisplay();
    }

    /// <summary>
    /// 仅将内存中的 Metadata 写回磁盘上的关卡 JSON，不改动文件中已存的关卡设计（实体、尺寸等）。
    /// 若对应文件不存在则无法执行（需先用「保存关卡」创建文件）。
    /// </summary>
    public void SaveMetadataOnly(string levelName)
    {
        if (string.IsNullOrWhiteSpace(levelName))
        {
            Debug.LogWarning("关卡名不能为空！");
            return;
        }

        string filePath = Path.Combine(LevelsDirectory, levelName + ".json");
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"关卡文件不存在，无法仅保存元数据（请先保存关卡整体设计）: {filePath}");
            return;
        }

        string json = File.ReadAllText(filePath);
        var onDisk = JsonUtility.FromJson<LevelDataModel>(json);
        if (onDisk == null)
        {
            Debug.LogWarning("解析关卡 JSON 失败，无法仅保存元数据");
            return;
        }

        _state.CurrentLevel.EnsureMetadata();
        onDisk.Metadata = _state.CurrentLevel.Metadata.DeepCopy();

        string outJson = JsonUtility.ToJson(onDisk, true);
        File.WriteAllText(filePath, outJson);

        Debug.Log($"已仅更新关卡元数据: {filePath}");
    }

    public void LoadLevel(string levelName)
    {
        if (string.IsNullOrWhiteSpace(levelName))
        {
            Debug.LogWarning("关卡名不能为空！");
            return;
        }

        string filePath = Path.Combine(LevelsDirectory, levelName + ".json");
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"关卡文件不存在: {filePath}");
            return;
        }

        // 先清空当前场景
        _placement.ClearAll();

        // 读取并反序列化（旧版 JSON 可能缺少 Metadata，EnsureMetadata 负责补齐）
        string json = File.ReadAllText(filePath);
        _state.CurrentLevel = JsonUtility.FromJson<LevelDataModel>(json);
        _state.CurrentLevel.EnsureMetadata();

        // 使用共享生成器实例化所有实体
        var spawned = LevelSpawner.SpawnEntities(_state.CurrentLevel, _state.EntityFactory);

        // 将生成的对象注册到 PlacedObjects 以便编辑器追踪
        foreach (var obj in spawned)
        {
            var posModel = obj.GetComponent<PositionModel>();
            if (posModel == null) continue;

            var cell = posModel.GridPosition;
            if (!_state.PlacedObjects.ContainsKey(cell))
                _state.PlacedObjects[cell] = new System.Collections.Generic.List<GameObject>();
            _state.PlacedObjects[cell].Add(obj);
        }

        if (_hud != null) _hud.RefreshGridSizeUI();
        RefreshLevelNameHud();
        if (_metadataView != null) _metadataView.RefreshUI();

        _undo?.Clear();

        Debug.Log($"关卡已加载: {filePath}");
    }

    public void ClearLevel()
    {
        _placement.ClearAll();
        string levelName = _state.CurrentLevel.LevelName;
        var metadata = _state.CurrentLevel.Metadata;
        _state.CurrentLevel = new LevelDataModel();
        _state.CurrentLevel.LevelName = levelName;
        _state.CurrentLevel.Metadata = metadata;
        RefreshLevelNameHud();
        if (_metadataView != null) _metadataView.RefreshUI();
        _undo?.Clear();
        Debug.Log("关卡已清空");
    }
}
