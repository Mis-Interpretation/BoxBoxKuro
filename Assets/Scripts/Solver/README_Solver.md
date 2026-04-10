核心思路：用纯 C# 实现 A* 搜索，集成到你的关卡编辑器里。关键优化点：

只生成推箱子动作（不是玩家每一步移动），大幅缩小搜索空间
死格预计算 — 从目标反向 BFS，标记箱子永远无法到达目标的格子
冻结/2x2 死锁检测 — 剪掉明显无解的分支
贪心匹配启发式 — 箱子到目标的 Manhattan 距离最优分配作为 h(n)
状态归一化 — 玩家在同一可达区域内视为相同状态，避免重复搜索
计划新增 7 个文件（求解器核心 6 个 + 回放控制器 1 个），只需小改 EditorHUDView.cs 和 EditorValidateController.cs 两个现有文件。求解用 Task.Run 后台线程跑，不卡 Unity 主线程。解出来后复用你现有的 MoveController.TryMove() 自动回放。


新增 7 个文件
求解器核心 (Scripts/Solver/):

SolverBoard.cs — 从 LevelDataModel 转换棋盘，预计算死格（从目标反向 BFS 拉箱子）
SolverState.cs — A* 状态：归一化玩家位置 + 排序箱子坐标 + Zobrist 哈希 + BFS 可达性/寻路
SolverHeuristic.cs — 贪心最小匹配启发式（箱子-目标 Manhattan 距离）
DeadlockDetector.cs — 2x2 死锁 + 冻结死锁（递归判断两轴是否都被阻挡）
SolverResult.cs — 结果 DTO，从推箱子序列还原完整 LURD 路径
SokobanSolver.cs — A* 主循环，只生成推动动作，100 万节点上限
集成层:
7. SolverPlaybackController.cs — 协程逐步调用 MoveController.TryMove() 回放解法
8. EditorSolverController.cs — Task.Run 后台求解 → 进入试玩模式 → 自动回放