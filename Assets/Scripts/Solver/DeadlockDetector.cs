using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 死锁检测：冻结死锁和 2x2 死锁（支持链式推动）。
/// 死格检测在 SolverBoard 中预计算。
/// </summary>
public static class DeadlockDetector
{
    /// <summary>
    /// 检查当前状态是否存在死锁（冻结死锁或 2x2 死锁）。
    /// </summary>
    public static bool IsDeadlocked(Vector2Int[] boxes, SolverBoard board)
    {
        var boxSet = new HashSet<Vector2Int>(boxes);

        if (Has2x2Deadlock(boxSet, board))
            return true;

        if (HasFreezeDeadlock(boxes, boxSet, board))
            return true;

        return false;
    }

    /// <summary>
    /// 2x2 死锁：任何 2x2 区域内全是墙/箱子，且至少一个箱子不在目标上，
    /// 且没有箱子能通过链式推动逃出该 2x2。
    /// </summary>
    private static bool Has2x2Deadlock(HashSet<Vector2Int> boxSet, SolverBoard board)
    {
        foreach (var box in boxSet)
        {
            if (Check2x2(box, Vector2Int.right, Vector2Int.up, boxSet, board))
                return true;
            if (Check2x2(box, Vector2Int.left, Vector2Int.up, boxSet, board))
                return true;
            if (Check2x2(box, Vector2Int.right, Vector2Int.down, boxSet, board))
                return true;
            if (Check2x2(box, Vector2Int.left, Vector2Int.down, boxSet, board))
                return true;
        }
        return false;
    }

    private static bool Check2x2(Vector2Int origin, Vector2Int dx, Vector2Int dy,
        HashSet<Vector2Int> boxSet, SolverBoard board)
    {
        Vector2Int p1 = origin;
        Vector2Int p2 = origin + dx;
        Vector2Int p3 = origin + dy;
        Vector2Int p4 = origin + dx + dy;

        if (!IsWallOrBox(p1, boxSet, board)) return false;
        if (!IsWallOrBox(p2, boxSet, board)) return false;
        if (!IsWallOrBox(p3, boxSet, board)) return false;
        if (!IsWallOrBox(p4, boxSet, board)) return false;

        bool hasBoxNotOnGoal = false;
        if (boxSet.Contains(p1) && !board.IsGoal(p1)) hasBoxNotOnGoal = true;
        if (boxSet.Contains(p2) && !board.IsGoal(p2)) hasBoxNotOnGoal = true;
        if (boxSet.Contains(p3) && !board.IsGoal(p3)) hasBoxNotOnGoal = true;
        if (boxSet.Contains(p4) && !board.IsGoal(p4)) hasBoxNotOnGoal = true;

        if (!hasBoxNotOnGoal) return false;

        // 检查是否有箱子能通过链式推动逃出 2x2。
        // 每个角的箱子可尝试向 2x2 内部推（玩家从外部推入），
        // 链末端若为非墙则可逃出。
        if (CanChainEscape(p1, dx, boxSet, board)) return false;
        if (CanChainEscape(p1, dy, boxSet, board)) return false;
        if (CanChainEscape(p2, -dx, boxSet, board)) return false;
        if (CanChainEscape(p2, dy, boxSet, board)) return false;
        if (CanChainEscape(p3, dx, boxSet, board)) return false;
        if (CanChainEscape(p3, -dy, boxSet, board)) return false;
        if (CanChainEscape(p4, -dx, boxSet, board)) return false;
        if (CanChainEscape(p4, -dy, boxSet, board)) return false;

        return true;
    }

    /// <summary>
    /// 检查位于 pos 的箱子能否被玩家从 pos-dir 方向推入，通过链式推动逃出。
    /// </summary>
    private static bool CanChainEscape(Vector2Int pos, Vector2Int dir,
        HashSet<Vector2Int> boxSet, SolverBoard board)
    {
        if (!boxSet.Contains(pos)) return false;
        if (board.IsWall(pos - dir)) return false;

        Vector2Int end = pos + dir;
        while (boxSet.Contains(end))
            end += dir;

        return !board.IsWall(end);
    }

    private static bool IsWallOrBox(Vector2Int pos, HashSet<Vector2Int> boxSet, SolverBoard board)
    {
        return board.IsWall(pos) || boxSet.Contains(pos);
    }

    /// <summary>
    /// 冻结死锁：箱子在两个轴上的链式推动都被墙阻挡，且不在目标上。
    /// </summary>
    private static bool HasFreezeDeadlock(Vector2Int[] boxes, HashSet<Vector2Int> boxSet, SolverBoard board)
    {
        foreach (var box in boxes)
        {
            if (board.IsGoal(box)) continue;

            if (IsChainFrozen(box, boxSet, board))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 判断箱子是否链式冻结：两个轴上的链末端都是墙。
    /// </summary>
    private static bool IsChainFrozen(Vector2Int box, HashSet<Vector2Int> boxSet, SolverBoard board)
    {
        bool frozenH = IsChainBlockedOnAxis(box, Vector2Int.left, Vector2Int.right, boxSet, board);
        bool frozenV = IsChainBlockedOnAxis(box, Vector2Int.down, Vector2Int.up, boxSet, board);
        return frozenH && frozenV;
    }

    private static bool IsChainBlockedOnAxis(Vector2Int box, Vector2Int negDir, Vector2Int posDir,
        HashSet<Vector2Int> boxSet, SolverBoard board)
    {
        return ChainEndsAtWall(box, negDir, boxSet, board)
            && ChainEndsAtWall(box, posDir, boxSet, board);
    }

    /// <summary>
    /// 从 box 沿 dir 方向跟踪连续箱子链，返回链末端是否为墙。
    /// </summary>
    private static bool ChainEndsAtWall(Vector2Int box, Vector2Int dir,
        HashSet<Vector2Int> boxSet, SolverBoard board)
    {
        Vector2Int pos = box + dir;
        while (boxSet.Contains(pos))
            pos += dir;
        return board.IsWall(pos);
    }
}
