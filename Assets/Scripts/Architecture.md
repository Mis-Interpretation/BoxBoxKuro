# BoxBoxBox 架构文档

## 概览

BoxBoxBox 是基于 Unity 6 (UI Toolkit) 的推箱子游戏，包含：

- **核心玩法** (`Core/`)：JSON 驱动的实体系统 + 推箱子规则。
- **求解器** (`Solver/`)：纯 C# A* 推箱子求解器，跑在后台 Task。
- **关卡编辑器** (`LevelEditor/`)：可视化关卡设计 + 命令式撤销栈 + Brush/Select 双模式。
- **关卡编排** (`LevelArrangement/`)：把关卡组织成 Campaign / Chapter，可拖拽排序。
- **实体配置编辑器** (`EntityConfig/`)：编辑 `EntityConfig.json`，新增/删除可用实体类型。
- **关卡选择** (`LevelSelect/`)：玩家侧的章节 + 关卡选择界面。

## 设计模式

本项目使用 MVC（Model–View–Controller）组织代码。Core 玩法通过 Zenject 进行依赖注入；编辑器/工具场景多自管 DI 或直接 `new`。

## 场景一览

| 场景 | 用途 |
| --- | --- |
| `MainScene` | 主菜单入口（`MainMenuController`） |
| `LevelSelectScene` | 玩家选关（章节列表 + 每页 8 关，含解锁状态） |
| `LevelArrangementScene` | 关卡编排（Campaign / Chapter 编辑） |
| `LevelEditorScene` | 关卡编辑器（拖拽放置实体 + 元数据 + 求解 + 试玩） |
| `EntityConfigScene` | 实体配置编辑器（编辑 `EntityConfig.json`） |
| `PlayLevel` | 实际游玩场景，被 `LevelLoaderController` 加载 |
| `PrefabScene` | Prefab 工作区 |

## Core Model 分层（JSON + Zenject DI）

| 类型 | 说明 |
| --- | --- |
| `JsonEntityConfigProvider` + `IEntityConfigReader` | 从 `StreamingAssets/EntityConfig.json` 加载实体配置（组件列表、Sprite 路径、排序层）。Zenject 绑定为 `IEntityConfigReader`。 |
| `EntityFactory` + `IEntityFactory` | 根据 JSON 配置动态创建实体 GameObject，添加组件（通过 `EntityComponentRegistry` 查表）。Zenject 绑定。 |
| `EntityComponentRegistry` | 静态注册表，映射组件名称字符串到 `System.Type`。 |
| `TagRegistry` + `ITagRegistryReader` / `ITagRegistryWriter` | 文件持久化的标签全集；`LevelEditor` 使用 `TagRegistry.Load()`。 |
| `LevelDataModel` 等 DTO、`MoveRecord` | 非场景单例，不做 Zenject 全局注册。 |
| `CampaignDataModel` / `ChapterData` / `UnlockCondition` | Campaign 数据；存于 `StreamingAssets/campaign.json`。 |
| `LevelCompleteModel` | 通关流程数据载体。 |
| `SceneNameModel` | 跨场景共享的场景名字符串（主菜单 /游玩 / 编辑器等）。 |
| `CampaignCompletionSaveModel` | 通关记录列表；与 `campaign_completion.json`（`persistentDataPath`）对应。 |
| `AudioSettingsData` + `AudioSettingsLoader` | 默认 BGM、通关 SFX 等；`StreamingAssets/audio_settings.json`。 |
| `TextEntityPayload` / `TextEntityRuntimeModel` / `TextEntityUtility` | 文本类实体数据与运行时；关卡 JSON 的 `EntityData.Text`。 |
| `PositionModel` 等实体组件 | 每实体生命周期，由 `EntityFactory` 动态添加。 |

> `CoreInstaller` 当前**只**绑定 `IEntityConfigReader` 与 `IEntityFactory`，其他 Model 不走全局 DI。

## 目录结构

```
Scripts/
├── Core/                                            # 运行时核心（MVC）
│   ├── Models/
│   │   ├── PositionModel.cs                         — 网格坐标（Vector2Int），所有实体共用
│   │   ├── MovableModel.cs                          — 标记：可移动
│   │   ├── BlockingModel.cs                         — 标记：阻挡其他实体进入
│   │   ├── ControllableModel.cs                     — 标记：响应玩家输入
│   │   ├── PushableModel.cs                         — 标记：可被推动
│   │   ├── OverlappableModel.cs                     — 标记：允许重叠（终点）
│   │   ├── EntityType.cs                            — 内置实体类型枚举（向后兼容）
│   │   ├── EntityConfigData.cs                      — JSON 配置数据类
│   │   ├── EntityComponentRegistry.cs               — 组件名 → Type 静态注册表
│   │   ├── JsonEntityConfigProvider.cs              — JSON 配置读取器（实现 IEntityConfigReader）
│   │   ├── IEntityConfigReader.cs                   — 实体配置只读接口
│   │   ├── ITagRegistry.cs                          — ITagRegistryReader / ITagRegistryWriter
│   │   ├── LevelDataModel.cs                        — 关卡 JSON DTO（含 EntityData、TextEntityPayload）
│   │   ├── LevelMetadata.cs                         — 元数据（Tags/难度/IsSolvable/Comment/DisplayName/BgmPath）
│   │   ├── TagRegistry.cs                           — 全局标签注册表（持久化）
│   │   ├── MoveRecord.cs                            — 单次移动记录（撤销栈）
│   │   ├── CampaignDataModel.cs                     — Campaign / ChapterData / UnlockCondition / UnlockType
│   │   ├── LevelCompleteModel.cs                    — 通关流程数据
│   │   ├── SceneNameModel.cs                        — 跨场景场景名常量
│   │   ├── CampaignCompletionSaveModel.cs             — 通关存档 DTO
│   │   ├── AudioSettingsData.cs                     — 音频默认配置 DTO
│   │   ├── AudioSettingsLoader.cs                   — 读取 audio_settings.json
│   │   ├── TextEntityRuntimeModel.cs                — 文本实体运行时
│   │   └── TextEntityUtility.cs                     — 文本实体辅助
│   ├── Installers/
│   │   └── CoreInstaller.cs                         — Zenject：仅绑定 IEntityConfigReader + IEntityFactory
│   ├── Controllers/
│   │   ├── InputController.cs                       — 键盘输入 → Controllable 实体
│   │   ├── MoveController.cs                        — 移动/推动/阻挡判定（递归推箱子）
│   │   ├── GameRuleController.cs                    — 胜利条件检测
│   │   ├── LevelLoaderController.cs                 — 游戏场景：读 JSON → LevelSpawner（LevelToLoad 为空时回退 CampaignProgress）
│   │   ├── LevelSpawner.cs                          — 静态：按 LevelData + IEntityFactory 实例化实体
│   │   ├── EntityFactory.cs                         — IEntityFactory 实现：动态组装实体
│   │   ├── CameraFitController.cs                   — 正交相机按关卡范围适配
│   │   ├── SolverPlaybackController.cs              — 协程按解法逐步调用 MoveController（禁用输入）
│   │   ├── LevelCompleteController.cs               — 通关流程：弹通关 UI、推进 Campaign
│   │   ├── CampaignProgressController.cs            — 静态：进度 PlayerPrefs + 通关记录 JSON
│   │   ├── MainMenuController.cs                    — MainScene 的主菜单按钮逻辑
│   │   ├── AudioController.cs                       — 全局 BGM/SFX（单例，Resources 加载）
│   │   └── PlayPauseMenuController.cs               —游玩场景 ESC 暂停菜单（UI Toolkit）
│   ├── Views/
│   │   ├── EntityView.cs                            — 网格坐标 → 世界坐标同步（带平滑）
│   │   ├── TextEntityView.cs                        — 文本实体 UI Toolkit 呈现
│   │   └── LevelCompleteView.cs                     — 通关 UI 视图（UI Toolkit）
│   └── Utils/
│       └── MetadataDisplayHelper.cs                 — 把元数据格式化为字符串（"★★★☆☆" 等）
│
├── Solver/                                          # A* 求解（纯 C#，后台 Task）
│   ├── SokobanSolver.cs                             — 主循环（推箱子动作、节点上限）
│   ├── SolverBoard.cs                               — 从 LevelDataModel 建棋盘、死格预计算
│   ├── SolverState.cs                               — 状态、哈希、可达性
│   ├── SolverHeuristic.cs                           — 启发式
│   ├── DeadlockDetector.cs                          — 冻结 / 2x2 等死锁剪枝
│   ├── SolverResult.cs                              — 结果 DTO（含完整移动序列）
│   ├── SolverSettings.cs                            — SO：节点上限、超时、进度上报间隔等
│   └── SolverProgress.cs                            — 求解进度快照（UI 轮询）
│
├── LevelEditor/                                     # 关卡编辑器
│   ├── Models/
│   │   ├── EditorStateModel.cs                      — 当前关卡、笔刷、模式、已放置对象（自管 DiContainer）
│   │   └── SelectionModel.cs                        — 选择模式下的当前选区
│   ├── Controllers/
│   │   ├── EditorPlacementController.cs             — 鼠标 → 放置/移除实体（笔刷模式）
│   │   ├── EditorSelectionController.cs             — 框选 / 拖动 / 多选 / 删除（选择模式）
│   │   ├── EditorUndoController.cs                  — 命令撤销 / 重做栈管理
│   │   ├── EditorFileController.cs                  — JSON 保存/加载/清空
│   │   ├── EditorMetadataController.cs              — 元数据 + TagRegistry
│   │   ├── EditorValidateController.cs              — 试玩：生成游玩实体、切相机、退出时清理
│   │   └── EditorSolverController.cs                — 后台求解 → 试玩宿主上回放
│   ├── Views/
│   │   ├── EditorGridView.cs                        — GL 网格 + 悬停高亮 + 选区指示
│   │   ├── EditorPaletteView.cs                     — 实体类型面板（从 AllConfigs 构建）
│   │   ├── EditorHUDView.cs                         — 保存/加载/求解/模式切换等入口
│   │   ├── EditorMetadataView.cs                    — 元数据面板
│   │   ├── EditorCameraPanView.cs                   — 编辑器相机平移
│   │   └── SolverProgressView.cs                    — 求解进度 UI
│   ├── Commands/
│   │   ├── IEditorCommand.cs                        — 可撤销命令接口
│   │   ├── PlaceEntityCommand.cs                    — 放置实体
│   │   ├── RemoveEntityCommand.cs                   — 移除实体
│   │   ├── SelectMoveCommand.cs                     — 选区拖动
│   │   ├── GridResizeCommand.cs                     — 调整网格尺寸
│   │   ├── CenterEntitiesCommand.cs                 — 居中所有实体
│   │   └── CompositeCommand.cs                      — 多命令聚合为一次撤销
│   ├── EditorShapeHelper.cs                         — DrawingMode + 起止点 → 格子集合
│   └── LevelEditorPendingLoad.cs                    — 跨场景传参：要打开的关卡名
│
├── EntityConfig/                                    # 实体配置编辑器
│   ├── Controllers/
│   │   ├── EntityConfigController.cs                — 主入口（挂在 UIDocument 同 GameObject）
│   │   └── EntityConfigFileController.cs            — EntityConfig.json 读写
│   └── Views/
│       ├── EntityConfigListView.cs                  — 左：实体列表
│       ├── EntityConfigInspectorView.cs             — 右：选中实体的字段编辑
│       ├── EntityConfigNewEntityModalView.cs        — 新建实体弹窗
│       └── EntityConfigDeleteModalView.cs           — 删除实体确认弹窗
│
├── LevelArrangement/                                # Campaign 编排
│   ├── Models/
│   │   ├── ArrangementStateModel.cs                 — 当前选中章节/关卡 + 元数据缓存（纯 C#）
│   │   └── LevelMetadataSummary.cs                  — 关卡元数据轻量缓存
│   ├── Controllers/
│   │   ├── ArrangementController.cs                 — 主控（增删改 Chapter/Level + UI 事件）
│   │   └── ArrangementFileController.cs             — campaign.json 读写 + 扫描关卡目录
│   └── Views/
│       ├── ChapterListView.cs                       — 左面板：Chapter 列表
│       ├── LevelListView.cs                         — 中面板：当前 Chapter 的 Level 列表（ListView 拖拽）
│       ├── LevelPreviewView.cs                      — 右面板：关卡元数据预览
│       ├── ChapterPreviewView.cs                    — 右面板：Chapter 信息预览
│       ├── ChapterEditModalView.cs                  — 编辑/新增 Chapter 弹窗
│       └── UnassignedLevelView.cs                   — 底部：未分配关卡列表
│
├── LevelSelect/                                     # 玩家选关
│   └── LevelSelectController.cs                     — 章节列表 + 关卡网格（每页 8 关）
│
└── Editor/                                          # 仅 Unity Editor
    └── CampaignProgressEditorTools.cs               — 菜单 Tools/BoxBoxBox/Clear Campaign Completion
```

## 实体组合（JSON 驱动）

实体组件由 `StreamingAssets/EntityConfig.json` 定义，运行时由 `EntityFactory` 动态添加。

- **默认精灵实体**：`PositionModel` + `SpriteRenderer` + `EntityView`（按 JSON 附加行为组件）。
- **`IsTextEntity`**：`PositionModel` + `TextEntityRuntimeModel` + `TextEntityView`（关卡里 `EntityData.Text` 提供文案）；不走 `EntityView`。
- **`IsPureDecoration`**：仅保留 `SpriteRenderer`与表现，**忽略** `Components` 行为列表。

配置里还可选 `ComponentSfx` / `ComponentSfxOverrides`（按行为组件名绑定 SFX 路径）、`DisplayName` 等，详见下文「实体配置格式」。

| 实体（示意） | JSON Components 列表（随 EntityConfig 可变） |
| ------------ | ---------------------------------------------- |
| 玩家         | MovableModel, ControllableModel, BlockingModel |
| 障碍物       | BlockingModel                                  |
| 箱子         | MovableModel, PushableModel, BlockingModel     |
| 终点         | OverlappableModel                              |

## LevelEditor 模式系统

`EditorStateModel` 中定义两层枚举：

```csharp
public enum EditorMode  { Brush, Select }
public enum DrawingMode { Point, Line, RectFill, RectEdge }
```

- **EditorMode** 是顶层模式（笔刷 vs 选择），与 [README_SelectMode.md](LevelEditor/README_SelectMode.md) 描述一致。
- **DrawingMode** 是子模式（点/线/面/框），同时被 Brush 和 Select 使用。
- 切换模式由 `EditorHUDView` 触发，`EditorPlacementController` 与 `EditorSelectionController` 各自只在自己模式下接收鼠标事件。
- 所有破坏性操作（放置/移除/拖动/网格尺寸/居中）都通过 `IEditorCommand` 走 `EditorUndoController`，支持 Undo/Redo。
- `CompositeCommand` 把一次手势内的多个子命令聚合为单次撤销单元。

## LevelArrangement (Campaign 编排)

四面板布局（`UI/Arrangement/ArrangementMain.uxml`）：

- **左**：`ChapterListView` —— Chapter 列表，可增删改
- **中**：`LevelListView` —— 当前 Chapter 内的 Level 列表，使用 `ListView.reorderable` 原生拖拽排序
- **右**：`LevelPreviewView` / `ChapterPreviewView` —— 元数据预览
- **底**：`UnassignedLevelView` —— 尚未编入任何 Chapter 的关卡

`ArrangementController` 是整体主控；`ArrangementFileController` 负责 `campaign.json` 读写并扫描 `StreamingAssets/Levels/` 目录。`ArrangementStateModel` 缓存 `LevelMetadataSummary`，供预览面板快速展示。

游戏侧由静态类 **`CampaignProgressController`** 推进进度：

- `CurrentChapter` / `CurrentLevel` 通过 `PlayerPrefs` 持久化（键 `Campaign_CurrentChapter` / `Campaign_CurrentLevel`）。
- 各关是否已通关由 **`campaign_completion.json`** 记录（`Application.persistentDataPath`，结构为 `CampaignCompletionSaveModel`），供选关界面统计与解锁展示。
- `GetCurrentLevelName()` → 当前应加载的关卡文件名。
- `AdvanceToNext()` → 通关后跳到下一关，跨章自动切换。
- `LevelLoaderController` 在 `LevelToLoad` 为空时调用 `GetCurrentLevelName()` 回退到 Campaign 进度。

`ChapterData.IsOnline` 控制 Chapter 是否对玩家可见（false = 草稿，仅编辑器可见）。

## EntityConfig 编辑器

`EntityConfigController` 挂在 `EntityConfigScene` 的 `UIDocument` 同 GameObject 上，负责协调：

- `EntityConfigFileController` —— 读写 `StreamingAssets/EntityConfig.json`
- `EntityConfigListView` —— 左侧实体列表
- `EntityConfigInspectorView` —— 右侧字段编辑（Id / TypeIndex / SpritePath / OrderInLayer / Components）
- `EntityConfigNewEntityModalView` / `EntityConfigDeleteModalView` —— 新增 / 删除弹窗

## LevelSelect

`LevelSelectController` 直接读取 `campaign.json` 与各关卡元数据缓存：

- 显示 Chapter 列表，含解锁状态（基于 `UnlockCondition`）与累计通关数（读 completion 存档）。
- 关卡区域每页 8 个，支持翻页。
- 选中关卡时设置 `CampaignProgressController.CurrentChapter` / `CurrentLevel`，再 `SceneManager.LoadScene(SceneNameModel.LevelScene)`（即 `PlayLevel`）。`LevelLoaderController` 在 `LevelToLoad` 为空时用当前进度解析具体 JSON 文件名。

（从关卡编辑器「试玩」打开关卡时仍可使用 `LevelEditorPendingLoad` 等路径；选关流程不依赖它。）

## 关卡数据格式

存储路径：`StreamingAssets/Levels/{关卡名}.json`

```json
{
  "LevelName": "Level_01",
  "Width": 10,
  "Height": 10,
  "Entities": [
    { "Type": 1, "X": 0, "Y": 0 },
    { "Type": 0, "X": 2, "Y": 3 }
  ],
  "Metadata": {
    "Tags": ["tutorial"],
    "DifficultyRating": 3.5,
    "IsSolvable": true,
    "Comment": "入门关卡，适合新手",
    "DisplayName": "起步",
    "BgmPath": "Sound/BGM/Baba Is You"
  }
}
```

`Type` 对应 `EntityConfig.json` 里该类型的 `TypeIndex`（内置示意：`Player=0, Block=1, Box=2, Endpoint=3`）。
文本实体可在条目中包含 `"Text": { "Content": "...", "FontSize": 1 }`（见 `EntityData`）。
`Metadata.DisplayName` 为空时，玩家界面使用文件名作为显示名。
`Metadata.BgmPath` 为 `Resources` 相对路径（无扩展名）；空则回退默认 BGM（见 `LevelMetadata.DefaultBgmPath` 与 `audio_settings.json`）。

## 实体配置格式

存储路径：`StreamingAssets/EntityConfig.json`

```json
{
  "Entities": [
    {
      "Id": "Player",
      "DisplayName": "玩家",
      "TypeIndex": 0,
      "SpritePath": "Sprites/Player",
      "OrderInLayer": 1,
      "Components": ["MovableModel", "ControllableModel", "BlockingModel"],
      "ComponentSfx": [],
      "ComponentSfxOverrides": [],
      "IsPureDecoration": false,
      "IsTextEntity": false
    }
  ]
}
```

`SpritePath` 为 `Resources/` 下的相对路径，通过 `Resources.Load<Sprite>()` 加载。
`Components` 列出行为标记组件名称，通过 `EntityComponentRegistry` 映射到具体类型。
`ComponentSfx` / `ComponentSfxOverrides` 为 `{ "ComponentName", "SfxPath" }` 列表，`SfxPath` 同为 `Resources` 相对路径、无扩展名。

## Campaign 数据格式

存储路径：`StreamingAssets/campaign.json`

```json
{
  "Chapters": [
    {
      "ChapterName": "入门",
      "Comment": "教学关卡",
      "IsOnline": true,
      "Unlock": { "Type": 0, "RequiredStars": 0 },
      "Levels": ["Level_01", "Level_02", "Level_03"]
    }
  ]
}
```

- 关卡序号由数组下标自动生成：`Chapter[0].Levels[2]` → `"1-3"`
- `Levels` 元素为关卡文件名（不含 `.json`），与 `LevelLoaderController.LevelToLoad` 一致
- `IsOnline = false` 时玩家选关界面不显示该 Chapter
- `UnlockType`：`AlwaysOpen=0`、`ClearPreviousChapter=1`、`StarCount=2`（预留）

## UI Toolkit 资源

```
Assets/UI/
├── Arrangement/      ArrangementMain.uxml/uss + ChapterItem/LevelItem/UnassignedItem.uxml
├── EntityConfig/     EntityConfigMain.uxml/uss
├── Game/             LevelComplete.uxml/uss
├── LevelEditor/      EditorMain.uxml + EditorHUD/EditorPalette/EditorMetadata.uxml/uss
├── LevelSelect/      LevelSelectMain + PlayPauseMenu.uxml/uss（暂停菜单）
└── ArrangePanelSettings.asset   — LevelArrangement 用的 PanelSettings
```

## 依赖关系

```
                    EntityType (enum, 向后兼容)
                         │
                    EntityConfig.json
                         │
                  JsonEntityConfigProvider → IEntityConfigReader (Zenject)
                         │
                    EntityFactory → IEntityFactory (Zenject)
                    ┌────┴────┐
                    │         │
              LevelDataModel  │
              ┌─────┼─────┐  │
              │     │     │  │
              ▼     ▼     ▼  ▼
┌─────────────────────────────────────────────────────────────┐
│  Core（游戏）                                                │
│                                                              │
│  InputController ──▶ MoveController ◀── SolverPlaybackController │
│       │                   │                                  │
│       ▼                   ▼                                  │
│  ControllableModel   PositionModel ◀── EntityView            │
│                      …（其余 Model）                          │
│                           ▲                                  │
│  GameRuleController ──────┘                                  │
│             │                                                │
│             ▼                                                │
│  LevelCompleteController ──▶ LevelCompleteView               │
│             │                                                │
│             ▼                                                │
│  CampaignProgressController（PlayerPrefs + completion 存档）   │
│             │                                                │
│             ▼                                                │
│  LevelLoaderController ──▶ LevelSpawner ──▶ 场景实体        │
│       └── IEntityFactory (JSON 配置 + 动态组装)              │
│       └── AudioController（BGM / SFX）                       │
│                                                              │
│  CameraFitController（按 LevelDataModel 适配相机）            │
│  PlayPauseMenuController（ESC 暂停）                         │
└─────────────────────────────────────────────────────────────┘
```

## 数据流

```
[键盘输入] → InputController → MoveController → PositionModel → EntityView / TextEntityView → [画面]
                                     │
                               GameRuleController → LevelCompleteController → CampaignProgressController.AdvanceToNext()
                                                          │
                                                          ▼
                                                     [下一关 / 章节完成]

[campaign.json] → CampaignProgressController.GetCurrentLevelName()
                          │
                          ▼
[JSON] → LevelLoaderController → LevelSpawner → EntityFactory.Create() → [场景实体]
         └─ IEntityFactory（CoreInstaller 注入）
         └─ AudioController：按 Metadata.BgmPath / audio_settings.json 播 BGM
```

## 模块边界

- **Core 不引用** LevelEditor / LevelArrangement / EntityConfig / LevelSelect。
- **`Scripts/Editor/`** 仅调用 Core 静态 API（如清空进度），不反向引用玩法模块。
- **LevelEditor / LevelArrangement / EntityConfig / LevelSelect** 只单向依赖 Core。
- **Solver** 是纯 C#，只依赖 `LevelDataModel` 这类 Core DTO。
- **Zenject** 仅在 Core 玩法场景生效；编辑器/工具场景多自管 DI 或直接 `new`（参见 `EditorStateModel.Awake` 自建 `DiContainer`）。
