using System.IO;
using UnityEngine;
using Zenject;

/// <summary>
/// 游戏场景用：启动时从 JSON 文件加载关卡，实例化所有预制体。
/// 核心游戏脚本通过 FindObjectsByType 自动发现这些实体。
/// 依赖 <see cref="IEntityConfigReader"/>，需场景中存在 SceneContext + <see cref="CoreInstaller"/>。
/// </summary>
public class LevelLoaderController : MonoBehaviour
{
    IEntityFactory _entityFactory;

    [Inject]
    private void Construct(IEntityFactory entityFactory)
    {
        _entityFactory = entityFactory;
    }

    [Header("要加载的关卡名（不含 .json 后缀）")]
    public string LevelToLoad;

    private void Start()
    {
        // 优先使用 Inspector 指定的关卡名；留空时从 campaign.json 读取当前进度
        string levelName = LevelToLoad;
        if (string.IsNullOrWhiteSpace(levelName))
        {
            levelName = CampaignProgressController.GetCurrentLevelName();
            if (!string.IsNullOrEmpty(levelName))
                Debug.Log($"从战役加载关卡: {CampaignProgressController.GetDisplayLabel()} → {levelName}");
        }

        if (string.IsNullOrWhiteSpace(levelName))
        {
            Debug.LogWarning("未指定要加载的关卡名，且 campaign.json 中无有效关卡！");
            return;
        }

        string filePath = Path.Combine(Application.streamingAssetsPath, "Levels", levelName + ".json");
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"关卡文件不存在: {filePath}");
            return;
        }

        string json = File.ReadAllText(filePath);
        LevelDataModel levelData = JsonUtility.FromJson<LevelDataModel>(json);
        levelData.EnsureMetadata();
        if (!string.IsNullOrWhiteSpace(levelData.Metadata.BgmPath))
            AudioController.Instance.PlayBgm(levelData.Metadata.BgmPath);
        else
            AudioController.Instance.PlayDefaultBgm();

        LevelSpawner.SpawnEntities(levelData, _entityFactory);

        var moveController = FindAnyObjectByType<MoveController>();
        if (moveController != null)
            moveController.SaveInitialPositions();

        var cameraFit = FindAnyObjectByType<CameraFitController>();
        if (cameraFit != null)
            cameraFit.FitToLevel(levelData);

        Debug.Log($"关卡已加载: {levelData.LevelName} ({levelData.Entities.Count} 个实体)");

        var titleHud = FindAnyObjectByType<LevelTitleHudView>();
        if (titleHud != null)
        {
            string numLabel = CampaignProgressController.TryGetDisplayLabelForLevelFile(levelName);
            if (string.IsNullOrEmpty(numLabel))
                numLabel = levelName;

            string namePart = levelData.Metadata != null && !string.IsNullOrWhiteSpace(levelData.Metadata.DisplayName)
                ? levelData.Metadata.DisplayName.Trim()
                : levelName;

            titleHud.SetTitleText($"{numLabel} {namePart}");
        }

        // 订阅通关事件以推进战役进度
        var gameRule = FindAnyObjectByType<GameRuleController>();
        if (gameRule != null)
        {
            gameRule.OnLevelComplete += OnLevelComplete;
        }
    }

    private void OnLevelComplete()
    {
        Debug.Log("通关！等待玩家选择下一步操作。");
    }
}
