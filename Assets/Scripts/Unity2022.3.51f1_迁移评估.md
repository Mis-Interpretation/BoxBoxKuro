# BoxBoxBox 迁移至 Unity 2022.3.51f1 可行性评估

## 0. 一句话结论

**这是一次从 Unity 6.3（6000.3.9f1）向 Unity 2022.3 LTS 的"降级"迁移，而不是常规升级。** 技术上可行，但成本远高于升级，**强烈建议优先考虑其他方案**（见 §5）。如果业务上必须回到 2022.3 LTS，请先在独立分支上做 POC，按 §4 的步骤推进。

---

## 1. 现状快照

| 项目 | 当前值 |
|---|---|
| Unity 版本 | `6000.3.9f1`（Unity 6.3） |
| 目标版本 | `2022.3.51f1`（LTS） |
| 渲染管线 | URP `17.3.0` |
| 输入系统 | Input System `1.18.0` |
| UI 方案 | UI Toolkit（UXML/USS） + Input System UI Module |
| DI 框架 | Zenject/Extenject `9.2.0`（Plugins 目录，源码导入） |
| 第三方代码库 | 仅 Zenject；**未使用** DOTween、UniTask、Newtonsoft.Json、Burst、Jobs、ECS |
| 脚本 API 兼容级别 | .NET Standard 2.1（apiCompatibilityLevel: 6） |
| 脚本资源规模 | 约 80 个 `.cs`，12 个 `.uxml`，场景 7 个 |

---

## 2. 可行性评估（总体：⚠️ 中等风险，技术可行）

### 2.1 对迁移**有利**的因素

- **运行时 C# 代码几乎是干净的 2022 兼容写法。**
  - 没有使用 C# 11 `required`、file-scoped class、raw string literal；没有 record 类型；没有 file-scoped namespace（使用传统 `namespace { }` 甚至全局命名空间）。
  - `FindAnyObjectByType` / `FindObjectsByType` 自 2022.2 起就存在，可直接用。
  - 没有使用 Unity 6 新引入的 `Awaitable`、`AsyncInstantiate`、Render Graph API。
  - `UnityEditor.EditorUtility.DisplayDialog` 的两处调用均已被 `#if UNITY_EDITOR` 正确包裹（`EditorSolverController.cs:215`、`EditorMetadataView.cs:387`）。
- **没有依赖任何商用第三方库**（DOTween/Odin/UniTask 之类），减少版本链锁死风险。
- **Zenject 9.2.0**（`Assets/Plugins/Zenject/package.json` 标注 `unity: 2019.3`）在 2022.3 完全兼容。
- **UXML 用的是基础命名空间** `xmlns:ui="UnityEngine.UIElements"`，没有使用 Unity 6 特有的 `[UxmlElement]` 属性标注或 UI Toolkit 新序列化模型。
- **纯 2D 游戏 + URP 2D Renderer**，未用后处理栈、Volume Profile、Shader Graph 自定义节点等"鸡毛蒜皮会出问题"的模块。
- **代码组织解耦较好**：已经有 `CoreInstaller`（Zenject）、MVC 层次，迁移后脚本层几乎不用改。

### 2.2 对迁移**不利**的核心障碍

**这些是"降级"本身带来的硬性问题：**

#### A. 资源序列化的单向性（**最高风险**）
Unity 的场景、Prefab、ScriptableObject、材质、URP Asset 一旦被高版本编辑器保存过，其 YAML 中的 `m_SerializedVersion`、字段结构、GUID 引用都可能向前演化。**Unity 官方不保证资源能被低版本读取**。常见表现：
- 场景里的某些组件被降级版本识别为 "missing script / component"；
- Prefab override 丢失；
- URP Asset / Renderer2D 的字段因 URP 14↔17 结构不同被清零；
- Shader Graph 图表在低版本 SG 中打不开或被"修复"成空图。

BoxBoxBox 的 `PlayLevel.unity`、`LevelEditorScene.unity` 等 7 个场景，以及 `UniversalRenderPipelineGlobalSettings.asset`、`Settings/UniversalRP.asset`、`Settings/Renderer2D.asset`，全部可能踩到。

#### B. 包版本存在巨大跨度（见表）
`Packages/manifest.json` 里多个包的当前版本**远高于**它们在 2022.3 分支的最新版本。强行降版本时，包内部的序列化结构同样可能回不去。

| 包 | 当前版本 | 2022.3 最新可用 | 差距 |
|---|---|---|---|
| `com.unity.render-pipelines.universal` | 17.3.0 | 14.0.x | **跨 3 个主版本**，含 Render Graph 引入前后 |
| `com.unity.inputsystem` | 1.18.0 | 1.11.x 左右 | 次版本差距，影响 `InputSystemUIInputModule` 序列化 |
| `com.unity.ugui` | 2.0.0 | 1.0.0（内置模块） | 大版本重构，manifest 引用方式不同 |
| `com.unity.2d.animation` | 13.0.4 | 10.x | 影响 SpriteSkin、骨骼数据格式 |
| `com.unity.2d.psdimporter` | 12.0.1 | 9.x | 影响 PSD 资源导入设置 |
| `com.unity.2d.spriteshape` | 13.0.0 | 10.x | 影响 SpriteShape 数据 |
| `com.unity.2d.tilemap.extras` | 6.0.1 | 3.x / 4.x | RuleTile 资源可能需要重建 |
| `com.unity.2d.aseprite` | 3.0.1 | 1.x（部分版本无该包） | **可能整个包在 2022.3 对应时间线不存在** |
| `com.unity.test-framework` | 1.6.0 | 1.4.x | |
| `com.unity.timeline` | 1.8.10 | 1.8.x | 兼容 |
| `com.unity.visualscripting` | 1.9.9 | 1.9.x | 兼容 |
| `com.unity.modules.accessibility` | 1.0.0 | **2022.3 不存在此内置模块** | 需从 manifest 删除 |
| `com.unity.multiplayer.center` | 1.0.1 | **2022.3 不存在此包** | 需从 manifest 删除 |

#### C. URP 14 ↔ URP 17 断档
URP 17（Unity 6）引入了 Render Graph 作为默认管线、2D Renderer 内部结构重写、部分 Renderer Feature API 变更。你当前用的是简单 2D Renderer，影响面相对有限，但 `UniversalRP.asset` 与 `Renderer2D.asset` **必须在 2022.3 中重建**，不能指望直接读。

#### D. Unity 6 独占特性无法迁回
即便当前代码未直接使用，项目元数据/默认设置可能已经引用：
- Accessibility Module（`com.unity.modules.accessibility`）——2022.3 不存在；
- Multiplayer Center（`com.unity.multiplayer.center`）——2022.3 不存在；
- 新的 `UniversalRenderPipelineGlobalSettings` 字段。

---

## 3. 风险清单（按影响从高到低）

| # | 风险 | 影响面 | 概率 | 缓解策略 |
|---|---|---|---|---|
| R1 | 场景 / Prefab / SO 资源在 2022.3 打开后出现 missing script、字段清零 | 🟥 阻塞 | 高 | §4 步骤 5：在 2022.3 里用"新建 + 重绑"替代"直接打开"；关键对象用脚本重建 |
| R2 | URP Asset / Renderer2D / Global Settings 无法被 2022.3 解析 | 🟥 阻塞 | **确定** | §4 步骤 4：删掉旧 URP asset，在 2022.3 中新建，重新指向 Graphics/Quality |
| R3 | `manifest.json` 里的包版本不存在于 2022.3 分支，`Package Manager` 解析失败 | 🟥 阻塞 | **确定** | §4 步骤 3：迁移前把 manifest 换成 2022.3 已知可用版本清单 |
| R4 | UXML `editor-extension-mode="False"` 及 UI Toolkit runtime 面板在 2022.3 行为微差 | 🟧 需验证 | 中 | 逐个场景跑一遍 Play，出现渲染异常时用 UI Debugger 对比 |
| R5 | Input System 1.18 → 1.11 后 `.inputactions` 文件含有新增属性被丢弃 | 🟧 功能性 | 中 | 备份 `InputSystem_Actions.inputactions`，在 2022.3 里重新生成/手动清洗 |
| R6 | 2D Animation/PSD Importer/SpriteShape/Tilemap Extras 的数据格式降级后损坏 | 🟧 美术资产 | 中高 | 如果用到了这几个包的资产，做一次资源清点；没有用到就直接改 manifest |
| R7 | ShaderGraph / Lit2D 模板着色器在 2022.3 里着色结果不同 | 🟨 视觉差异 | 中 | 以 2022.3 默认 URP 2D 模板为准，重建自定义 shader |
| R8 | `com.unity.modules.accessibility`、`multiplayer.center` 在 2022.3 里不存在，manifest 解析报错 | 🟥 阻塞 | **确定** | §4 步骤 3：从 manifest 删除这两行 |
| R9 | Zenject `Resources/DefaultSceneContextConfig` 在低版本里行为变化（极小） | 🟨 低 | 低 | 验证 `CoreInstaller` 能正常 bind 即可 |
| R10 | `Library/` 缓存被污染导致编辑器启动即崩 | 🟧 可恢复 | 中 | 每次切版本前删除 `Library/`、`Temp/`、`obj/` 再打开 |
| R11 | `apiCompatibilityLevel: 6`（.NET Standard 2.1）在 2022.3 下仍然可用，但需要确认 | 🟨 低 | 低 | 2022.3 支持 .NET Standard 2.1，直接保留 |
| R12 | Shader 中可能存在的 Unity 6 专有 include / keyword | 🟨 未知 | 低 | 当前未检出自定义 shader，若后续添加需单独评估 |
| R13 | 代码签名/IL2CPP 在 2022.3 的 C# 功能子集与 6.3 不同 | 🟨 低 | 低 | 2022.3 支持到 C# 9，本项目未用更新语法，无需修改 |

---

## 4. 迁移实施步骤（分阶段，强制顺序）

> **原则：全程在独立 git 分支上操作；每步完成后提交 commit；Library/ 每次切版本前清空。**

### 阶段 1：降级前准备（在 Unity 6.3 里做）
1. **开 `migration/unity-2022.3` 分支**，并对当前可运行状态打 tag：`git tag pre-downgrade-snapshot`。
2. **强制文本序列化**已经是默认，确认 `ProjectSettings/EditorSettings.asset` 里 `m_SerializationMode: 2`（Force Text）。
3. **导出核心数据**：把 `Assets/Resources/` 下的关卡 JSON、实体配置 JSON、ArrangementStateModel 等原始数据单独备份一份到仓库外——这些是纯文本，降版本不会坏。
4. **列出所有美术资产依赖**：如果没有用到 2D Animation（骨骼 / SpriteSkin）、PSD Importer 的图层导入、SpriteShape、RuleTile，就可以在下一步放心把这些包降到最低版本甚至移除。
5. **记录当前每个 Shader / Material 的外观**（截图一批对比图），便于降级后比对。

### 阶段 2：改 manifest 与 ProjectSettings（仍在 Unity 6.3 里关闭编辑器后改）
6. 把 `Packages/manifest.json` 替换成下面这份 **已在 Unity 2022.3.51f1（Hub 安装版）上解析、编译通过的参考清单**。工程内**以仓库根目录 [`Packages/manifest.json`](../../Packages/manifest.json) 为唯一真源**；下文 JSON 与其保持一致，便于文档内联阅读。

```json
{
  "dependencies": {
    "com.unity.2d.animation": "9.1.2",
    "com.unity.2d.common": "8.0.3",
    "com.unity.2d.psdimporter": "8.0.5",
    "com.unity.2d.sprite": "1.0.0",
    "com.unity.2d.spriteshape": "9.0.4",
    "com.unity.2d.tilemap": "1.0.0",
    "com.unity.2d.tilemap.extras": "3.1.2",
    "com.unity.collab-proxy": "2.5.2",
    "com.unity.ide.rider": "3.0.31",
    "com.unity.ide.visualstudio": "2.0.22",
    "com.unity.inputsystem": "1.11.2",
    "com.unity.render-pipelines.universal": "14.0.11",
    "com.unity.test-framework": "1.4.5",
    "com.unity.textmeshpro": "3.0.7",
    "com.unity.timeline": "1.7.6",
    "com.unity.ugui": "1.0.0",
    "com.unity.visualscripting": "1.9.4",
    "com.unity.modules.ai": "1.0.0",
    "com.unity.modules.androidjni": "1.0.0",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.assetbundle": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.cloth": "1.0.0",
    "com.unity.modules.director": "1.0.0",
    "com.unity.modules.imageconversion": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.particlesystem": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.physics2d": "1.0.0",
    "com.unity.modules.screencapture": "1.0.0",
    "com.unity.modules.terrain": "1.0.0",
    "com.unity.modules.terrainphysics": "1.0.0",
    "com.unity.modules.tilemap": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.umbra": "1.0.0",
    "com.unity.modules.unityanalytics": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0",
    "com.unity.modules.unitywebrequestassetbundle": "1.0.0",
    "com.unity.modules.unitywebrequestaudio": "1.0.0",
    "com.unity.modules.unitywebrequesttexture": "1.0.0",
    "com.unity.modules.unitywebrequestwww": "1.0.0",
    "com.unity.modules.vehicles": "1.0.0",
    "com.unity.modules.video": "1.0.0",
    "com.unity.modules.vr": "1.0.0",
    "com.unity.modules.wind": "1.0.0",
    "com.unity.modules.xr": "1.0.0"
  }
}
```

**注意（相对旧版“拍脑袋”清单的修正）：**
- **2D 包不要用 Unity 6 时代的 10.x / 13.x 主版本**：否则 UPM 可能解析出 **`com.unity.2d.common@9.x`**，其代码依赖 **Unity 6 引擎 API**（如 `SpriteRenderer.SetBoneTransforms` 等），在 2022.3 上会整包编译失败。本清单的 **2D 版本与 Hub 内嵌的 `Editor/Data/Resources/PackageManager/Editor/manifest.json` 中 `mustBeBundled` 默认值一致**（例如 `2d.animation` **9.1.2**、`2d.common` **8.0.3**、`tilemap.extras` **3.1.2**），并 **显式写上 `com.unity.2d.common`**，避免被高版本依赖拽飞。
- **`com.unity.modules.vectorgraphics` 不要写进 manifest**：在 2022.3.51f1 上常见表现为 **UPM 无法解析**（`Package [...] cannot be found`）。需要矢量模块时再在目标编辑器里用 Package Manager 按官方指引处理。
- **`com.unity.textmeshpro`**：凡项目代码使用 `TMPro` / `TextMeshPro`，必须在 manifest 中声明（本仓库为 **3.0.7**）；否则 Assembly-CSharp 会报找不到命名空间。
- **`com.unity.timeline`**：与 Hub 捆绑默认对齐为 **1.7.6**（若你坚持 1.8.x，需自行确认与 2022.3.51f1 的解析结果）。
- 仍须 **删除** Unity 6 独有依赖（与旧说明相同）：`com.unity.modules.accessibility`、`com.unity.multiplayer.center`、`com.unity.2d.tooling` 等；**UGUI 2.0.0 → 1.0.0**；`com.unity.modules.adaptiveperformance`、`com.unity.2d.aseprite` 等按项目需要另加。
- 若首次打开后仍有 **Zenject OptionalExtras 示例**编译错误：Unity 6 的 `Rigidbody.linearVelocity` 在 2022.3 中应改回 **`velocity`**（仅示例，不影响本体游戏逻辑）。
- 若 **UI Toolkit** 使用了 Unity 6 才有的 API（例如 `TextField.verticalScrollerVisibility`），在 2022.3 下需 **`#if UNITY_6000_0_OR_NEWER`** 包一层或删该行。

**可选自动化（本仓库已加，迁移稳定后可删）：** `Assets/Editor/MigrationUrpBootstrap.cs`（batchmode `-executeMethod MigrationUrpBootstrap.CreateAndWireUrp` 生成 `Assets/Settings/2022/*` 并绑定 Graphics/Quality）、`Assets/Editor/MigrationPlayLevelVerify.cs`（`-executeMethod MigrationPlayLevelVerify.VerifyPlayLevelScene` 递归检查 `PlayLevel` missing script）。

7. 删除 `ProjectSettings/` 下的 Unity 6 特有文件（**在新分支上**，保留备份）：
   - `MultiplayerManager.asset`
   - `URPProjectSettings.asset`（2022.3 里也有同名文件但结构不同）
   - `UniversalRenderPipelineGlobalSettings.asset`（位于 `Assets/`，也删）
8. 修改 `ProjectSettings/ProjectVersion.txt`（**revision 须与本机安装的 2022.3.51f1 一致**；可从该版本编辑器启动日志中的 `Version is '2022.3.51f1 (…)'` 复制，或用手动下载页 Changeset 对应哈希）：
   ```
   m_EditorVersion: 2022.3.51f1
   m_EditorVersionWithRevision: 2022.3.51f1 (9f9d16c45e54)
   ```
   示例行对应官方 Windows 包常见 changeset；若 Hub 安装的 revision 不同，以你机器为准。

### 阶段 3：在 Unity 2022.3.51f1 里首次打开
9. **确保已清空 `Library/`、`Temp/`、`obj/`、所有 `.csproj`、`.sln`**。
10. 用 2022.3.51f1 打开工程，让 Package Manager 做第一次解析。**这一步会出大量错误**，正常现象，按下面逐项修：
    - 如有任何包解析失败：去 Unity 2022.3 官方 Package 文档确认当时的版本号再改。
    - **C# 编译错误**：按行修。根据本评估第 2.1 节，当前运行时代码应当全部可编译，大概率零报错。
11. **重建 URP 资产**：
    - 新建 `Assets/Settings/2022/` 下的 **URP Asset** 与 **Renderer 2D**（本仓库脚本生成文件名为 `UniversalRP_2022.asset`、`Renderer2D_2022.asset`，名称可自定，以 Graphics/Quality 引用为准）；
    - Project Settings → Graphics：把默认 RP Asset 指向新的；
    - Project Settings → Quality：所有 Quality Level 的 Render Pipeline Asset 指向新的；
    - 删除旧的 `Settings/UniversalRP.asset`、`Renderer2D.asset`（及根目录 `Assets/UniversalRenderPipelineGlobalSettings.asset`，若来自 Unity 6）。

### 阶段 4：场景与 Prefab 逐个验证
12. **不要直接打开 7 个场景就按 Ctrl+S**。先跑编译，再**只打开 `MainScene.unity`** 观察：
    - 有无 missing script？
    - UIDocument 能否正确加载 UXML？
    - Camera 下的 URP 渲染是否正常（比如透明排序、Pixel Perfect）？
13. 按 `MainScene → LevelSelectScene → LevelEditorScene → PlayLevel → LevelArrangementScene → EntityConfigScene → PrefabScene` 顺序逐场景修复。每修完一个就提交。
14. **如果某个场景损毁到 missing script 大面积爆炸**，方案：
    - 从仓库外备份里拷回运行时数据（关卡 JSON），
    - 在 2022.3 里**新建**一个空场景，
    - 按 `LevelEditorScene` 为例：手工添加 `EditorStateModel`、`EditorHUDView`、`EditorPaletteView` 等 GameObject，配 `UIDocument` 绑定 `EditorMain.uxml`。
    - 因为项目架构解耦得好（`FindAnyObjectByType` + Zenject），重建场景的成本主要在"摆对象"，不在改代码。

### 阶段 5：功能回归
15. 按顺序跑通：主菜单 → 关卡选择 → 播放一关（PlayLevel）→ 编辑器创建 → 编辑器求解 → 关卡编排；每条路径记录一次对比。
16. **Input System Actions 验证**：`InputSystem_Actions.inputactions` 用文本 diff 对比迁移前后，观察是否有字段被 2022.3 版本的 Input System 剔除。
17. **UI Toolkit 验证**：重点看 `EditorMetadata.uxml` 的 `SliderInt`、`DropdownField`、`TextField` 在 2022.3 UI Toolkit 下事件回调是否一致（`RegisterValueChangedCallback` 有过签名变化史）。

### 阶段 6：构建验证
18. 跑一次 Windows Standalone Build；
19. 确认 StreamingAssets 里的 JSON 打包正常；
20. 合并分支前，跑一次完整 Play Mode 冒烟。

---

## 5. 强烈推荐先考虑的替代方案

**在实施迁移之前，请先确认为什么一定要回到 2022.3 LTS。** 常见原因对应替代方案：

| 原因 | 替代方案 |
|---|---|
| "Unity 6 太新不稳定" | 留在 Unity 6 LTS（`6000.0.x` 或 `6000.2.x` LTS 线），而不是回 2022.3 |
| "某第三方 SDK 只支持 2022.3" | 先确认该 SDK 是否真的不支持 Unity 6；Unity 6 对 2022 API 高度兼容 |
| "团队其他项目还在 2022.3" | 新项目保留在 Unity 6；工具链/脚手架共享时用包方式拆出 |
| "目标平台编辑器崩溃" | 升级到 Unity 6 LTS 而不是降到 2022.3 |
| "需要长期支持" | Unity 6 LTS (`6000.0` 系列) 已经是 LTS |

**降级迁移的工作量**（按本项目规模估计，仅供参考量级，不做时间承诺）：
- 顺利路径（零美术资产损坏）：**若干工作日**用于资源重建与回归；
- 常规路径（场景部分重建、URP 资产重建、逐 bug 修复）：**一到两周等级**；
- 失败路径（多个场景序列化彻底损坏）：可能需要**重做场景骨架**。

与之相比，**升级到 Unity 6 LTS** 几乎是零成本（本项目代码本身就在 Unity 6 上运行）。

---

## 6. 如果决定继续迁移，先做的"两小时 POC"

在投入正式迁移前，花两小时做一次 Proof of Concept，用来**提前暴露 R1/R2/R6 风险**：

1. `git worktree add ../BoxBoxBox-migration-poc migration/poc`
2. 在 POC 副本里完成本评估 §4 的阶段 2（改 manifest + 删 Unity 6 文件）
3. 用 Unity 2022.3.51f1 打开 POC 副本
4. **只看一件事：`PlayLevel.unity` 在新版本编辑器里能否无 missing script 打开，并能进入 Play Mode 把主角走一步**
5. 结果判定：
   - ✅ 能走一步 → 按本文档 §4 正式迁移；
   - ⚠️ 进 Play Mode 报错但场景加载了 → 按 §4，但预估多花 50% 的时间在场景修复；
   - ❌ 场景打开就一片红 → **放弃迁移**，执行 §5 的替代方案。

POC 做完即可 `git worktree remove` 丢弃，不污染主仓库。

---

## 7. 额外发现的"顺手可以清理"的遗留问题
（与迁移本身无关，但做 POC 的时候可能会暴露，列在这里备忘）

- 运行时代码里存在大量 `FindAnyObjectByType<T>()` 的样板（见 `EditorFileController.cs`、`EditorValidateController.cs` 等），`Assets/Scripts/重构方案.md` 已经提到要用 Zenject 绑定替换——迁移是顺手做这件事的好时机。
- `EditorSolverController.cs:215` 与 `EditorMetadataView.cs:387` 虽然已被 `#if UNITY_EDITOR` 正确包裹，但在运行时场景 (`LevelEditorScene`) 的 View/Controller 里弹编辑器 Dialog 不是好做法；建议抽成 `IAlertService` 之类，构建版用 UI Toolkit 自己的 Modal 实现。

---

## 8. TL;DR

- **是"降级"不是"升级"**，Unity 不保证资源向下兼容。
- **代码层面几乎无风险**（没用 C# 11、没用 Render Graph、没用 Awaitable、没用 UxmlElement）。
- **资源层面是主要风险**（场景 / Prefab / URP Asset 重建成本）。
- **Package 清单必须手动重写**（Unity 6 独有包要删，URP/InputSystem/UGUI/2D 系列大版本回退）。
- **先花两小时做 POC**（§6），再决定是否正式迁移。
- **在决定迁移前请先评估替代方案**（§5）——"升级到 Unity 6 LTS" 通常是更好的答案。
