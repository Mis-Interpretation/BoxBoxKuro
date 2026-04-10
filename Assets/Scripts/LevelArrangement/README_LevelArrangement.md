# 关卡编排系统 (Campaign Arrangement System)

## Context

当前 BoxBoxBox 推箱子游戏的关卡是独立 JSON 文件，没有编排/排序机制，也没有游戏内推进逻辑。本方案新增：
1. **campaign.json** — 定义大关卡(Chapter)和关卡顺序
2. **LevelArrangementScene** — 独立场景，用于编排关卡（拖拽排序、元数据预览）
3. **CampaignProgressController** — 游戏内关卡推进（通关 → 下一关）

---

## 一、数据模型

### 新文件: `Scripts/Core/Models/CampaignDataModel.cs`

```csharp
[System.Serializable]
public class CampaignDataModel
{
    public List<ChapterData> Chapters = new List<ChapterData>();
}

[System.Serializable]
public class ChapterData
{
    public string ChapterName = "New Chapter";
    public string Comment = "";
    public UnlockCondition Unlock = new UnlockCondition();
    public List<string> Levels = new List<string>(); // 关卡文件名，有序
}

[System.Serializable]
public class UnlockCondition
{
    public UnlockType Type = UnlockType.ClearPreviousChapter;
    public int RequiredStars = 0; // 预留：需要累计星数
}

[System.Serializable]
public enum UnlockType
{
    AlwaysOpen,           // 始终开放
    ClearPreviousChapter, // 通关前一章
    StarCount             // 累计星数（预留）
}
```

- 关卡序号由数组下标自动生成：Chapter[0].Levels[2] → "1-3"
- Level 引用的是文件名（不含 .json），与 `LevelLoaderController.LevelToLoad` 一致
- 不在任何 Chapter 中的关卡文件视为"未分配"

### 存储: `StreamingAssets/campaign.json`

遵循项目现有惯例（关卡 JSON、tag_registry.json 均在 StreamingAssets）。

---

## 二、编排场景 UI — UI Toolkit 实现

项目使用 Unity 6 (6000.3.9f1)，完全支持 UI Toolkit。编排界面用 UXML + USS + C# 构建，所有 UI 文件可直接文本编辑。

### 新场景: `Scenes/LevelArrangementScene.unity`

场景内挂载 `UIDocument` 组件，引用 UXML 和 PanelSettings。

### UI 文件结构

```
Assets/UI/
├── PanelSettings.asset              -- 共享 PanelSettings（需在 Unity 中创建）
├── Arrangement/
│   ├── ArrangementMain.uxml         -- 主布局
│   ├── ArrangementMain.uss          -- 主样式
│   ├── ChapterItem.uxml             -- 章节条目模板
│   ├── LevelItem.uxml               -- 关卡条目模板（可拖拽）
│   ├── UnassignedItem.uxml          -- 未分配关卡条目模板
│   └── LevelPreview.uxml            -- 右侧预览面板内容
```

### 主布局 `ArrangementMain.uxml`

```
<root>
├── TopBar (flex-direction: row)
│   ├── Label "关卡编排"
│   ├── Button#save-btn "保存"
│   ├── Button#add-chapter-btn "+ 大关卡"
│   └── Button#back-btn "返回"
├── MainArea (flex-direction: row, flex-grow: 1)
│   ├── ScrollView#chapter-list (width: 25%)
│   │   └── VisualElement#chapter-container  ← 动态填充 ChapterItem
│   ├── VisualElement#level-panel (width: 45%)
│   │   ├── Label#chapter-header "第 1 章: 入门"
│   │   └── ListView#level-list              ← 内置拖拽排序支持!
│   └── ScrollView#preview-panel (width: 30%)
│       └── VisualElement#preview-content    ← 动态填充预览信息
└── ScrollView#unassigned-panel (flex-direction: row, height: 120px)
    └── VisualElement#unassigned-container   ← 动态填充 UnassignedItem
```

### UI Toolkit 拖拽排序 — 利用 ListView 内置能力

**关键优势：UI Toolkit 的 `ListView` 原生支持 `reorderable = true`**，无需手写拖拽逻辑！

```csharp
var levelList = root.Q<ListView>("level-list");
levelList.reorderable = true;           // 启用拖拽排序
levelList.reorderMode = ListViewReorderMode.Animated;
levelList.makeItem = () => LoadUXML("LevelItem.uxml");
levelList.bindItem = (element, index) => BindLevelItem(element, index);
levelList.itemsSource = currentChapter.Levels;
levelList.itemIndexChanged += OnLevelReordered;  // 排序回调
```

这比 UGUI 手写 IBeginDragHandler/IDragHandler/IEndDragHandler + ScrollRect 冲突处理简单得多。

### 样式 `ArrangementMain.uss` 要点

```css
#top-bar { flex-direction: row; height: 40px; padding: 4px; background-color: #2d2d2d; }
#main-area { flex-direction: row; flex-grow: 1; }
#chapter-list { width: 25%; border-right-width: 1px; border-color: #555; }
#level-panel { width: 45%; }
#preview-panel { width: 30%; border-left-width: 1px; border-color: #555; }
#unassigned-panel { height: 120px; flex-direction: row; border-top-width: 1px; border-color: #555; }

.chapter-item { padding: 8px; margin: 2px; border-radius: 4px; }
.chapter-item--selected { background-color: #3a5fad; }
.level-item { flex-direction: row; padding: 6px; align-items: center; }
.level-item__handle { width: 20px; cursor: move-arrow; }
.level-item__index { width: 40px; color: #888; }
.level-item__name { flex-grow: 1; }
.level-item__stars { color: #f5a623; }
.level-item__solvable { width: 20px; }

.tag-chip { background-color: #3a3a5a; border-radius: 8px; padding: 2px 8px; margin: 2px; }
```

### 未分配关卡 → 章节：双击或按钮

- 未分配关卡条目上有 "+" 按钮，点击添加到当前选中章节末尾
- 也支持双击快速添加

---

## 三、MVC 文件结构

```
Scripts/LevelArrangement/
├── Models/
│   └── ArrangementStateModel.cs       -- 运行时状态（纯 C# 类，非 MonoBehaviour）
├── Controllers/
│   ├── ArrangementFileController.cs   -- campaign.json 读写 + 扫描关卡目录
│   └── ArrangementController.cs       -- 编排操作（增删改排）+ UI 事件绑定入口
└── Views/
    ├── ChapterListView.cs             -- 左面板：章节列表 UI 逻辑
    ├── LevelListView.cs               -- 中面板：关卡 ListView（含拖拽排序）
    ├── LevelPreviewView.cs            -- 右面板：元数据预览
    └── UnassignedLevelView.cs         -- 底部：未分配关卡

UI/Arrangement/
├── ArrangementMain.uxml               -- 主布局
├── ArrangementMain.uss                -- 主样式
├── ChapterItem.uxml                   -- 章节条目模板
├── LevelItem.uxml                     -- 关卡条目模板
└── UnassignedItem.uxml                -- 未分配关卡条目模板
```

场景中只需一个 GameObject 挂载：
- `UIDocument`（引用 ArrangementMain.uxml + PanelSettings）
- `ArrangementController`（MonoBehaviour，作为入口绑定所有 View 和 Controller）

### `ArrangementStateModel`（纯 C# 类）
```
- CampaignDataModel Campaign
- int SelectedChapterIndex
- string SelectedLevelName
- Dictionary<string, LevelMetadataSummary> MetadataCache  // 全部关卡元数据缓存
- List<string> AllLevelFiles  // 磁盘上所有 .json 文件名
```

### `ArrangementFileController` 关键方法
```
- LoadCampaign() → CampaignDataModel       // 读 campaign.json
- SaveCampaign(CampaignDataModel)           // 写 campaign.json
- ScanAllLevels() → List<string>            // 扫描 Levels/ 目录
- LoadLevelMetadata(string) → LevelMetadataSummary  // 读单个关卡元数据
- GetUnassignedLevels(campaign, allFiles) → List<string>
```

### `ArrangementController` 关键方法
```
- SelectChapter(int index)
- SelectLevel(string name)
- AddChapter(string name)
- RemoveChapter(int index)           // 关卡移入未分配
- RenameChapter(int index, string)
- SetUnlockCondition(int index, UnlockCondition)
- AddLevelToChapter(string level, int chapter, int insertAt)
- RemoveLevelFromChapter(int chapter, int levelIndex)
- ReorderLevel(int chapter, int from, int to)
```

---

## 四、共享元数据展示工具

### 新文件: `Scripts/Core/Utils/MetadataDisplayHelper.cs`

从 `EditorMetadataView.cs` 提取静态方法：
- `BuildStarText(float rating) → string`（如 "★★★☆☆"）
- `BuildSolvableText(bool isSolvable) → string`（如 "✔ 可通关" / "✘ 未验证"）

`EditorMetadataView`（UGUI 侧）和 `LevelPreviewView`（UI Toolkit 侧）共用纯字符串输出。

### `LevelMetadataSummary` 结构（轻量缓存用）
```csharp
public class LevelMetadataSummary
{
    public string LevelName;
    public int Width, Height, EntityCount;
    public List<string> Tags;
    public float DifficultyRating;
    public bool IsSolvable;
    public string Comment;
}
```

---

## 五、游戏推进系统

### 新文件: `Scripts/Core/Controllers/CampaignProgressController.cs`

```
- static int CurrentChapter, CurrentLevel   // 或用 PlayerPrefs 持久化
- LoadCampaign() → CampaignDataModel
- GetCurrentLevelName() → string
- AdvanceToNext() → bool                    // 推进到下一关，跨章时自动跳转
- HasNextLevel() → bool
- GetDisplayLabel() → string                // "1-3"
- IsChapterUnlocked(int index) → bool       // 检查解锁条件
```

### 修改: `Scripts/Core/Controllers/LevelLoaderController.cs`

```csharp
private void Start()
{
    string levelName = string.IsNullOrWhiteSpace(LevelToLoad)
        ? CampaignProgressController.GetCurrentLevelName()
        : LevelToLoad;
    // ... 现有加载逻辑不变
}
```

Inspector 的 `LevelToLoad` 字段留空时走 campaign 推进，填值时仍可测试指定关卡。

### 修改: 通关回调

在 `GameRuleController`（或等效胜利检测脚本）的通关逻辑中调用：
```csharp
CampaignProgressController.AdvanceToNext();
// 加载下一关或显示章节完成 UI
```

---

## 六、实现顺序

| 阶段 | 内容 | 涉及文件 |
|------|------|---------|
| **P1: 数据层** | CampaignDataModel + ChapterData + UnlockCondition + ArrangementFileController | `CampaignDataModel.cs`, `ArrangementFileController.cs` |
| **P2: UI 骨架** | UXML/USS 布局 + PanelSettings + ArrangementStateModel | `ArrangementMain.uxml`, `ArrangementMain.uss`, `ChapterItem.uxml`, `LevelItem.uxml`, `UnassignedItem.uxml`, `ArrangementStateModel.cs` |
| **P3: 场景 + 入口** | 创建场景 + UIDocument + ArrangementController（绑定 UI 事件） | `LevelArrangementScene.unity`（手动创建）, `ArrangementController.cs` |
| **P4: 章节管理** | ChapterListView：增删改章节 + 解锁条件编辑 | `ChapterListView.cs` |
| **P5: 关卡列表 + 拖拽** | LevelListView（利用 ListView.reorderable）+ UnassignedLevelView | `LevelListView.cs`, `UnassignedLevelView.cs` |
| **P6: 元数据预览** | MetadataDisplayHelper + LevelPreviewView | `MetadataDisplayHelper.cs`, `LevelPreviewView.cs`, 修改 `EditorMetadataView.cs` |
| **P7: 游戏推进** | CampaignProgressController + 修改 LevelLoaderController + 通关回调 | `CampaignProgressController.cs`, `LevelLoaderController.cs`, 通关相关脚本 |

> **注意**：场景文件 (.unity) 和 PanelSettings.asset 需要在 Unity 编辑器中手动创建，我会提供具体步骤。其余 .uxml/.uss/.cs 文件全部由代码直接生成。

---

## 七、验证方式

1. **P1-P2**: 运行场景，确认 campaign.json 正确读写、扫描到所有关卡文件
2. **P3-P4**: 添加/删除章节、将关卡分配到章节、从章节移除，保存后重新加载验证数据一致性
3. **P5**: 拖拽关卡改变顺序，保存后重新打开确认顺序持久化
4. **P6**: 点击关卡条目，右侧预览面板正确显示元数据
5. **P7**: 在 PlayLevel 场景中清空 LevelToLoad，运行游戏确认自动加载 campaign 第一关；通关后自动跳转到下一关

---

## 关键文件引用

| 现有文件 | 用途 |
|---------|------|
| `Scripts/Core/Models/LevelDataModel.cs` | 关卡数据结构，LevelMetadataSummary 参照此类 |
| `Scripts/Core/Models/LevelMetadata.cs` | 元数据结构 |
| `Scripts/Core/Controllers/LevelLoaderController.cs` | 需修改：增加 campaign 回退 |
| `Scripts/LevelEditor/Controllers/EditorFileController.cs` | ArrangementFileController 参照此模式 |
| `Scripts/LevelEditor/Views/EditorMetadataView.cs` | 提取 MetadataDisplayHelper |
| `Scripts/Core/Installers/CoreInstaller.cs` | 场景 Zenject 绑定参考 |
