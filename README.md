# 欢迎来到BoxBoxBox

作者：Jerry郑炜城

这是一个简易的推箱子游戏关卡编辑器。你可以随时进入Play模式，来游玩我准备的样板关卡。你也可以进入level editor来设计新关卡，甚至是Entity Config来设计新的关卡元素。

# 最简单的使用方法是。。。

只需运行MainScene，然后跟着UI指引进行探索就好了。

## 分辨率

本编辑器是以2560x1440 (16:9)为目标分辨率设计的。最低也请以1080p运行哦。分辨率和比例的不同可能会导致UI长得比较怪。

# 功能介绍

## 关卡编辑器 Level Editor

你可以用一个宛如绘图软件的UI进行关卡编辑

- Level Editor 超短版：[Assets/UI/LevelEditor/README_LevelEditor_QuickStart_CN.md](Assets/UI/LevelEditor/README_LevelEditor_QuickStart_CN.md)
- Level Editor 完整说明：[Assets/UI/LevelEditor/README_LevelEditor_Usage_CN.md](Assets/UI/LevelEditor/README_LevelEditor_Usage_CN.md)

## 关卡编排器 Level Arrangement

你可以把做好的一系列关卡编排成一个大章节，以及决定哪些关卡/大章节要上线。

- Level Arrange 超短版：[Assets/UI/Arrangement/README_LevelArrange_QuickStart_CN.md](Assets/UI/Arrangement/README_LevelArrange_QuickStart_CN.md)

## 实体编辑器 Entity Config

关卡由一个个实体构成（像是玩家，箱子，目标点），你也可以追根溯源，去修改/创造实体，为关卡设计带来新变化
PS：别忘了有四个维持游戏运作的基础实体是无法被修改的哦

- Entity Config 超短版：[Assets/UI/EntityConfig/README_EntityConfig_QuickStart_CN.md](Assets/UI/EntityConfig/README_EntityConfig_QuickStart_CN.md)

## 如果你通关了，想要重置进度。。。

可以在Unity Editor里面，选择`Tools/BoxBoxBox/Clear Campaign Completion`来清空你的存档。请放心，这不会清空你的关卡设计。

# 其他信息

- 素材来源：[素材来源说明（Sprites / Sound）](Assets/Art/asset_reference.md)
- 开发架构：这个demo 95%的代码由AI生成，AI&Jerry review。使用MVC（Model-View-Controller）显著提高了代码质量和功能添加/修改效率。详见 [Architecture.md](Assets/Scripts/Architecture.md)。
- 关卡设计&Auto Solver参考：https://dangarfield.github.io/sokoban-solver/
- 为什么commit history这么短：因为这个项目是从Jerry的另一个Unity6.3的项目急头白脸地迁移过来的（没看清楚版本要求，悲）。https://github.com/Mis-Interpretation/BoxBoxBox 