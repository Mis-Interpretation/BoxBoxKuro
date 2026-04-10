# Entity Config 超短版

## 1 分钟流程

1. 左侧选中一个实体，看右侧 Inspector。
2. 修改显示名、Sprite、组件、音效覆盖等。
3. 点 `Apply` 应用当前实体改动。
4. 顶部 `save` 保存到 `StreamingAssets/EntityConfig.json`。

## 新增 / 删除

- 顶部 `new entity`：
  - 输入唯一 `Id`；
  - 可选显示名和 Sprite；
  - 系统自动分配新的 `TypeIndex`。
- Inspector `Delete`：
  - 仅 `TypeIndex >= 4` 可删除；
  - 删除弹窗确认后执行，且不可撤销。

## 基础实体限制（重要）

- `TypeIndex 0~3` 为基础实体，不可删除。
- 对基础实体只允许改：
  - `DisplayName`
  - `SpritePath`
  - 组件音效覆盖
- 其余关键字段保存时会被保护回滚（如 Id、TypeIndex、Components、OrderInLayer 等）。

## 退出

- `back` 会弹确认：
  - `保存并退出`：先保存配置；
  - `不保存退出`：直接返回。
