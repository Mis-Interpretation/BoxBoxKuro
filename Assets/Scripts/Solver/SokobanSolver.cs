using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// A* 推箱子求解器。纯 C# 逻辑，可在后台线程运行。
/// </summary>
public class SokobanSolver
{
    private static readonly Vector2Int[] Dirs =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    /// <summary>
    /// 求解推箱子关卡。
    /// </summary>
    /// <param name="level">关卡数据</param>
    /// <param name="ct">取消令牌</param>
    /// <param name="maxNodes">最大展开节点数</param>
    /// <param name="progress">进度报告（可选，线程安全）</param>
    /// <param name="reportInterval">每隔多少节点报告一次进度</param>
    public SolverResult Solve(LevelDataModel level, CancellationToken ct = default,
        int maxNodes = 1000000, SolverProgress progress = null, int reportInterval = 100)
    {
        progress?.Start();

        var board = SolverBoard.FromLevelData(level);

        // 基本校验
        if (board.Goals.Length == 0)
        {
            progress?.Finish(SolverProgress.SolveStatus.Failed);
            return SolverResult.Failure("没有目标点", 0);
        }
        if (board.BoxesStart.Length == 0)
        {
            progress?.Finish(SolverProgress.SolveStatus.Failed);
            return SolverResult.Failure("没有箱子", 0);
        }
        if (board.BoxesStart.Length != board.Goals.Length)
        {
            progress?.Finish(SolverProgress.SolveStatus.Failed);
            return SolverResult.Failure($"箱子数({board.BoxesStart.Length})与目标数({board.Goals.Length})不匹配", 0);
        }

        // 检查初始箱子是否在死格上
        foreach (var box in board.BoxesStart)
        {
            if (board.IsDead(box))
            {
                progress?.Finish(SolverProgress.SolveStatus.Failed);
                return SolverResult.Failure($"箱子初始位置 ({box.x},{box.y}) 在死格上，无解", 0);
            }
        }

        // 初始状态
        var startState = new SolverState(board.PlayerStart, board.BoxesStart, board);
        startState.GCost = 0;
        startState.FCost = SolverHeuristic.Compute(startState.Boxes, board.Goals);

        // 检查是否已经解决
        if (IsGoal(startState, board))
        {
            progress?.Finish(SolverProgress.SolveStatus.Success);
            return SolverResult.FromSolution(startState, board, 0);
        }

        // A* 开放列表（优先队列）和关闭列表
        var openList = new SortedSet<SolverState>(new StateComparer());
        var closedSet = new HashSet<SolverState>();

        openList.Add(startState);
        int nodesExpanded = 0;

        while (openList.Count > 0)
        {
            if (ct.IsCancellationRequested)
            {
                progress?.Finish(SolverProgress.SolveStatus.Cancelled);
                return SolverResult.Failure($"求解被取消，已展开 {nodesExpanded} 个节点", nodesExpanded);
            }

            if (nodesExpanded >= maxNodes)
            {
                progress?.Finish(SolverProgress.SolveStatus.Failed);
                return SolverResult.Failure($"达到节点上限 {maxNodes}，未找到解", nodesExpanded);
            }

            // 取 FCost 最小的状态
            var current = GetMin(openList);
            openList.Remove(current);

            if (closedSet.Contains(current))
                continue;

            closedSet.Add(current);
            nodesExpanded++;

            // 定期报告进度
            if (progress != null && nodesExpanded % reportInterval == 0)
            {
                progress.ReportNodes(nodesExpanded, openList.Count, closedSet.Count);
                progress.ReportBestFCost(current.FCost);
            }

            // 生成后继状态：对每个箱子尝试 4 个方向的推动（支持链式推动）
            var boxSet = new HashSet<Vector2Int>(current.Boxes);

            for (int i = 0; i < current.Boxes.Length; i++)
            {
                var boxPos = current.Boxes[i];

                foreach (var dir in Dirs)
                {
                    Vector2Int newBoxPos = boxPos + dir;
                    if (board.IsWall(newBoxPos)) continue;

                    // 沿推动方向跟踪连续箱子链
                    Vector2Int chainEnd = newBoxPos;
                    int chainCount = 0;
                    while (boxSet.Contains(chainEnd))
                    {
                        chainEnd += dir;
                        chainCount++;
                    }

                    // 链末端（第一个空位）不能是墙
                    if (board.IsWall(chainEnd)) continue;

                    // 只有链末端是新位置，需要检查死格
                    if (board.IsDead(chainEnd)) continue;

                    // 玩家需要站在箱子的反方向
                    Vector2Int playerNeedPos = boxPos - dir;
                    if (board.IsWall(playerNeedPos)) continue;
                    if (boxSet.Contains(playerNeedPos)) continue;

                    if (!SolverState.CanPlayerReach(current.ActualPlayerPos, playerNeedPos, board, boxSet))
                        continue;

                    // 构建新箱子数组：所有链上的箱子都沿 dir 平移一格
                    var newBoxes = (Vector2Int[])current.Boxes.Clone();
                    newBoxes[i] = newBoxPos;
                    for (int c = 0; c < chainCount; c++)
                    {
                        Vector2Int chainBoxPos = newBoxPos + dir * c;
                        for (int j = 0; j < newBoxes.Length; j++)
                        {
                            if (j != i && current.Boxes[j] == chainBoxPos)
                            {
                                newBoxes[j] = chainBoxPos + dir;
                                break;
                            }
                        }
                    }

                    var newState = new SolverState(boxPos, newBoxes, board);
                    newState.GCost = current.GCost + 1;
                    newState.FCost = newState.GCost + SolverHeuristic.Compute(newState.Boxes, board.Goals);
                    newState.Parent = current;
                    newState.PushDirection = dir;
                    newState.PushedBoxFrom = boxPos;
                    newState.ActualPlayerPos = boxPos;
                    newState.ChainLength = chainCount + 1;

                    if (closedSet.Contains(newState))
                        continue;

                    if (DeadlockDetector.IsDeadlocked(newState.Boxes, board))
                        continue;

                    if (IsGoal(newState, board))
                    {
                        progress?.ReportNodes(nodesExpanded, openList.Count, closedSet.Count);
                        progress?.Finish(SolverProgress.SolveStatus.Success);
                        return SolverResult.FromSolution(newState, board, nodesExpanded);
                    }

                    openList.Add(newState);
                }
            }
        }

        progress?.Finish(SolverProgress.SolveStatus.Failed);
        return SolverResult.Failure($"无解，已展开 {nodesExpanded} 个节点", nodesExpanded);
    }

    private static bool IsGoal(SolverState state, SolverBoard board)
    {
        var goalSet = new HashSet<Vector2Int>(board.Goals);
        foreach (var box in state.Boxes)
        {
            if (!goalSet.Contains(box))
                return false;
        }
        return true;
    }

    private static SolverState GetMin(SortedSet<SolverState> set)
    {
        return set.Min;
    }

    /// <summary>
    /// 比较器：先比 FCost，再比 GCost（偏好深度更深的），最后用 SequenceId 打破平局。
    /// </summary>
    private class StateComparer : IComparer<SolverState>
    {
        public int Compare(SolverState a, SolverState b)
        {
            if (a.Equals(b)) return 0;

            int cmp = a.FCost.CompareTo(b.FCost);
            if (cmp != 0) return cmp;

            cmp = b.GCost.CompareTo(a.GCost);
            if (cmp != 0) return cmp;

            return a.SequenceId.CompareTo(b.SequenceId);
        }
    }
}
