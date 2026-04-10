# BoxBoxBox 项目术语表

> 本文件统一项目内的命名 — 包括场景、UI、功能、数据结构等 — 与其通俗含义。
> 与 AI agent / 协作者沟通时请优先使用左列的"项目内名词"。
> 详细架构请见 `Architecture.md`。

## 1. 场景 (Scenes)

| 项目内名词 | 通俗含义 |
| --- | --- |
| MainScene | 游戏主菜单场景（入口） |
| LevelSelectScene | 关卡选择场景（玩家挑选要玩的关卡） |
| LevelArrangementScene | 关卡编排场景（设计师把关卡组织成 Campaign / Chapter） |
| LevelEditorScene | 关卡编辑器场景（设计师拖拽实体编辑单个关卡） |
| EntityConfigScene | 实体配置场景（编辑 EntityConfig.json — 即可用实体类型清单） |
| PlayLevel | 玩游戏的运行时场景（实际加载并游玩一个关卡） |
| PrefabScene | Prefab 工作区场景 |

## 2. 核心游戏运行时 (Core Runtime)

| 项目内名词 | 通俗含义 |
| --- | --- |
| Entity | 关卡里的一个游戏对象（玩家/箱子/障碍/终点等） |
| EntityType | 内置实体类型枚举（Player/Block/Box/Endpoint），向后兼容字段 |
| EntityConfig | 实体配置数据 — 在 JSON 里描述某种实体由哪些 Component 组成 |
| EntityConfig.json | StreamingAssets 下的实体配置文件，定义全部可用实体 |
| EntityFactory | 根据 EntityConfig 动态生成实体 GameObject 的工厂 |
| EntityComponentRegistry | "组件名字符串 → C# Type"的静态查表（让 JSON 能引用 C# 组件） |
| LevelDataModel | 单个关卡的可序列化数据（宽高、实体列表、Metadata） |
| LevelMetadata | 关卡的元数据（Tags、难度评分、IsSolvable、Comment、DisplayName） |
| LevelLoaderController | 游戏场景里负责"读 JSON → 生成关卡"的控制器 |
| LevelSpawner | 给定 LevelData 后真正实例化场景实体的静态工具 |
| MoveController | 处理移动 / 推动 / 阻挡判定的控制器（递归推箱子核心） |
| MoveRecord | 单次移动记录（用于撤销栈） |
| InputController | 键盘输入 → 派发给 Controllable 实体 |
| GameRuleController | 胜利条件检测（所有箱子是否在终点上） |
| CameraFitController | 让正交相机自动适配关卡大小 |
| LevelCompleteController | 关卡通关流程控制器（弹通关 UI、推进 Campaign） |
| LevelCompleteView | 通关 UI 视图 (UI Toolkit) |
| LevelCompleteModel | 通关流程相关的数据 |
| SceneNameModel | 跨场景共享的"目标场景名"载体 |
| MainMenuController | MainScene 的主菜单按钮逻辑 |
| EntityView | 实体的视图组件，把网格坐标平滑同步到世界坐标 |
| PositionModel | 实体的网格坐标 (Vector2Int)，所有实体共有 |
| MovableModel | 标记组件：实体可以移动 |
| BlockingModel | 标记组件：实体阻挡其他实体进入 |
| ControllableModel | 标记组件：响应玩家输入 |
| PushableModel | 标记组件：可被推动（如箱子） |
| OverlappableModel | 标记组件：允许其他实体重叠（如终点） |

## 3. 求解器 (Solver)

| 项目内名词 | 通俗含义 |
| --- | --- |
| SokobanSolver | A* 推箱子求解器主循环 |
| SolverBoard | 把 LevelDataModel 转换为求解用的棋盘表示，含死格预计算 |
| SolverState | 求解中的一个状态节点（含哈希、可达性） |
| SolverHeuristic | 启发式估值函数 |
| DeadlockDetector | 死锁剪枝（冻结、2x2 死角等） |
| SolverResult | 求解结果 DTO（含完整移动序列） |
| SolverSettings | ScriptableObject — 节点上限/超时/进度上报间隔等 |
| SolverProgress | 求解进度快照（供 UI 轮询） |
| SolverProgressView | 编辑器内的求解进度 UI |
| SolverPlaybackController | 按 SolverResult 自动回放每一步移动（禁用玩家输入） |

## 4. 关卡编辑器 (LevelEditor)

| 项目内名词 | 通俗含义 |
| --- | --- |
| LevelEditor | 整个关卡编辑器模块（设计师拖拽放置实体的工具） |
| EditorStateModel | 编辑器运行时状态：当前关卡、笔刷、已放置对象、模式等 |
| EditorMode (枚举) | 编辑器顶层模式：Brush / Select |
| BrushMode | 编辑器的"放置/擦除"大模式（俗称：笔刷模式） |
| SelectMode | 编辑器的"框选/移动/删除"大模式（与笔刷模式平级） |
| DrawingMode (枚举) | 子模式：Point / Line / RectFill / RectEdge（点/线/面/框） |
| SelectionModel | 当前选区数据 |
| EditorPlacementController | 鼠标 → 放置 / 移除实体的核心控制器（笔刷模式下） |
| EditorSelectionController | 选择模式下的框选 / 拖动 / 多选 / 删除 |
| EditorUndoController | 撤销 / 重做栈管理 |
| EditorFileController | 关卡 JSON 的保存 / 加载 / 清空 |
| EditorMetadataController | 关卡 Metadata（Tags / 评分 / Comment）+ TagRegistry 操作 |
| EditorValidateController | "试玩"模式：临时切到游戏运行实体，可中途退出 |
| EditorSolverController | 后台调用 Solver 并把结果交给 SolverPlayback 回放 |
| EditorGridView | 编辑器网格绘制 + 鼠标悬停高亮 + 选区指示 |
| EditorPaletteView | 实体类型面板（左/右侧栏可选实体列表） |
| EditorHUDView | 编辑器顶部/侧边按钮 HUD（保存/加载/求解/试玩/模式切换等入口） |
| EditorMetadataView | 关卡元数据编辑面板（Tag / 星级 / Comment） |
| EditorCameraPanView | 编辑器相机平移控制 |
| EditorShapeHelper | 把 DrawingMode + 起止点转换为格子集合的工具 |
| LevelEditorPendingLoad | 跨场景传参 — 进入编辑器时要打开哪个关卡 |
| TagRegistry | 全局 Tag 注册表，持久化所有已知 tag 字符串 |
| IEditorCommand | 可撤销编辑操作的命令接口 |
| PlaceEntityCommand | 命令：放置一个实体（撤销时移除） |
| RemoveEntityCommand | 命令：移除一个实体 |
| SelectMoveCommand | 命令：选区拖动 |
| GridResizeCommand | 命令：调整网格尺寸 |
| CenterEntitiesCommand | 命令：把所有实体居中到网格中央 |
| CompositeCommand | 命令：聚合多个子命令为一次撤销单元 |

## 5. 关卡编排 / Campaign (LevelArrangement)

| 项目内名词 | 通俗含义 |
| --- | --- |
| Campaign | 游戏的"战役/关卡集"概念 — 由多个 Chapter 组成 |
| campaign.json | StreamingAssets 下的 Campaign 数据文件 |
| CampaignDataModel | Campaign 数据结构（Chapters 列表） |
| Chapter | 一个大关卡 / 章节（包含多个 Level） |
| ChapterData | Chapter 的数据结构（名称、Comment、解锁条件、IsOnline、Level 列表） |
| ChapterItem | Chapter 在 UI 里的条目控件（左栏每一行就是一个 ChapterItem） |
| LevelItem | Chapter 内某个关卡在 UI 里的条目控件（中栏可拖拽排序） |
| UnassignedItem | 未分配关卡条目（底部栏显示尚未编入任何 Chapter 的关卡） |
| UnlockCondition | 章节解锁条件结构 |
| UnlockType | 解锁条件类型枚举：AlwaysOpen / ClearPreviousChapter / StarCount |
| IsOnline | Chapter 是否对玩家可见（false = 草稿，仅编辑器可见） |
| LevelArrangement | 关卡编排功能（整体模块） |
| ArrangementStateModel | 编排页面运行时状态（当前选中章节/关卡、元数据缓存等） |
| ArrangementController | 编排页面主控（增删改 Chapter / Level，绑定 UI 事件） |
| ArrangementFileController | campaign.json 读写 + 扫描磁盘上所有关卡文件 |
| ChapterListView | 左面板：Chapter 列表 UI 逻辑 |
| LevelListView | 中面板：当前 Chapter 的 Level 列表（含 ListView 拖拽排序） |
| LevelPreviewView | 右面板：单关卡元数据预览 |
| ChapterPreviewView | 右面板：Chapter 信息预览 |
| ChapterEditModalView | 编辑/新增 Chapter 的弹窗 |
| UnassignedLevelView | 底部：未分配关卡列表 |
| CampaignProgressController | 游戏内 Campaign 推进（静态类，PlayerPrefs 持久化） |
| CampaignProgress | 玩家在 Campaign 中的当前位置（CurrentChapter / CurrentLevel） |
| LevelMetadataSummary | 关卡元数据的轻量缓存（编排/选关页面预览用） |
| MetadataDisplayHelper | 把元数据格式化为字符串的工具（如 "★★★☆☆"） |

## 6. 实体配置工具 (EntityConfig 编辑器)

| 项目内名词 | 通俗含义 |
| --- | --- |
| EntityConfigController | EntityConfig 编辑器主控（挂在 UIDocument 同 GameObject） |
| EntityConfigFileController | EntityConfig.json 的读写 |
| EntityConfigListView | 左侧：当前 EntityConfig 中所有实体的列表 |
| EntityConfigInspectorView | 右侧：选中实体的详细字段编辑面板 |
| EntityConfigNewEntityModalView | "新建实体"弹窗 |
| EntityConfigDeleteModalView | "删除实体"确认弹窗 |

## 7. 关卡选择 (LevelSelect)

| 项目内名词 | 通俗含义 |
| --- | --- |
| LevelSelect | 关卡选择功能（玩家选择要玩的关卡） |
| LevelSelectController | 关卡选择场景的主控（每页 8 关，含章节解锁状态） |
| LevelSelectMain.uxml/uss | 关卡选择场景的 UI Toolkit 主布局 / 样式 |

## 8. UI Toolkit 资源 (UXML / USS)

| 项目内名词 | 通俗含义 |
| --- | --- |
| UIDocument | Unity 组件 — 把 UXML 加载到场景中 |
| PanelSettings | UI Toolkit 的全局面板设置（缩放、主题等） |
| ArrangePanelSettings.asset | LevelArrangement 用的 PanelSettings |
| ArrangementMain.uxml/uss | LevelArrangementScene 的主布局 / 样式 |
| ChapterItem.uxml | Chapter 条目模板 |
| LevelItem.uxml | Chapter 内 Level 条目模板（可拖拽） |
| UnassignedItem.uxml | 未分配关卡条目模板 |
| EditorMain.uxml | LevelEditorScene 主布局 |
| EditorHUD.uxml/uss | 编辑器 HUD 布局 / 样式 |
| EditorPalette.uxml/uss | 编辑器实体面板布局 / 样式 |
| EditorMetadata.uxml/uss | 元数据面板布局 / 样式 |
| EntityConfigMain.uxml/uss | EntityConfig 编辑器主布局 / 样式 |
| LevelSelectMain.uxml/uss | LevelSelect 主布局 / 样式 |
| LevelComplete.uxml/uss | 通关 UI 布局 / 样式 |

## 9. 数据 / 文件路径

| 项目内名词 | 通俗含义 |
| --- | --- |
| StreamingAssets/Levels/{name}.json | 单个关卡文件 |
| StreamingAssets/EntityConfig.json | 全部可用实体的配置文件 |
| StreamingAssets/campaign.json | Campaign（章节 + 关卡顺序）数据 |
| StreamingAssets/tag_registry.json | 所有已知 Tag 的持久化注册表 |
| Resources/Sprites/{Player,Block,Box,Endpoint} | 实体图像资源 |
| PlayerPrefs: Campaign_CurrentChapter / Campaign_CurrentLevel | 玩家 Campaign 进度 |

## 10. 架构通用术语

| 项目内名词 | 通俗含义 |
| --- | --- |
| MVC | 项目代码组织模式：Model(数据) / View(UI) / Controller(逻辑) |
| Zenject | 项目使用的依赖注入框架（仅 Core 玩法场景使用） |
| CoreInstaller | Zenject 安装器 — 仅绑定 IEntityConfigReader / IEntityFactory |
| IEntityConfigReader | 实体配置只读接口（由 JsonEntityConfigProvider 实现） |
| IEntityFactory | 实体工厂接口（由 EntityFactory 实现） |
| ITagRegistryReader / ITagRegistryWriter | Tag 注册表的读 / 写接口 |
