using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A* 搜索状态：归一化玩家位置 + 排序后的箱子位置。
/// </summary>
public class SolverState : IEquatable<SolverState>
{
    /// <summary>
    /// 归一化的玩家位置（可达区域中最小坐标）。
    /// </summary>
    public readonly Vector2Int NormalizedPlayerPos;

    /// <summary>
    /// 箱子位置数组（按坐标排序，保证唯一性）。
    /// </summary>
    public readonly Vector2Int[] Boxes;

    /// <summary>
    /// A* 的 g 值（推箱子次数）。
    /// </summary>
    public int GCost;

    /// <summary>
    /// A* 的 f = g + h。
    /// </summary>
    public int FCost;

    /// <summary>
    /// 回溯用的父状态。
    /// </summary>
    public SolverState Parent;

    /// <summary>
    /// 产生此状态的推动：玩家推动方向。
    /// </summary>
    public Vector2Int PushDirection;

    /// <summary>
    /// 被推箱子推动前的位置。
    /// </summary>
    public Vector2Int PushedBoxFrom;

    /// <summary>
    /// 产生此状态时玩家的实际位置（推之前站的位置）。
    /// </summary>
    public Vector2Int ActualPlayerPos;

    /// <summary>
    /// 本次推动涉及的箱子总数（含链式推动）。1 = 单箱推动。
    /// </summary>
    public int ChainLength = 1;

    /// <summary>
    /// 全局自增序列号，用于 SortedSet 比较器打破平局，避免哈希碰撞丢状态。
    /// </summary>
    public readonly int SequenceId;

    private static int _nextSequenceId;
    private readonly int _hashCode;

    private static readonly Vector2Int[] Dirs =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    public SolverState(Vector2Int actualPlayerPos, Vector2Int[] boxes, SolverBoard board)
    {
        SequenceId = System.Threading.Interlocked.Increment(ref _nextSequenceId);
        Boxes = SortBoxes(boxes);
        ActualPlayerPos = actualPlayerPos;

        // 归一化：flood-fill 玩家可达区域，取最小坐标
        var boxSet = new HashSet<Vector2Int>(Boxes);
        NormalizedPlayerPos = NormalizePlayerPos(actualPlayerPos, board, boxSet);

        _hashCode = ComputeHash();
    }

    /// <summary>
    /// 对箱子坐标排序（先 X 后 Y）。
    /// </summary>
    private static Vector2Int[] SortBoxes(Vector2Int[] boxes)
    {
        var sorted = (Vector2Int[])boxes.Clone();
        Array.Sort(sorted, (a, b) =>
        {
            int cmp = a.x.CompareTo(b.x);
            return cmp != 0 ? cmp : a.y.CompareTo(b.y);
        });
        return sorted;
    }

    /// <summary>
    /// Flood-fill 玩家可达区域，返回区域中坐标最小的格子作为归一化位置。
    /// </summary>
    private static Vector2Int NormalizePlayerPos(Vector2Int playerPos, SolverBoard board, HashSet<Vector2Int> boxSet)
    {
        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(playerPos);
        visited.Add(playerPos);

        Vector2Int minPos = playerPos;

        while (queue.Count > 0)
        {
            var pos = queue.Dequeue();

            // 取坐标最小的位置（先比 Y 再比 X，保证一致性）
            if (pos.y < minPos.y || (pos.y == minPos.y && pos.x < minPos.x))
                minPos = pos;

            foreach (var dir in Dirs)
            {
                var next = pos + dir;
                if (visited.Contains(next)) continue;
                if (board.IsWall(next)) continue;
                if (boxSet.Contains(next)) continue;

                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        return minPos;
    }

    /// <summary>
    /// 检查玩家是否能从当前位置到达目标位置（不穿过墙和箱子）。
    /// </summary>
    public static bool CanPlayerReach(Vector2Int from, Vector2Int to, SolverBoard board, HashSet<Vector2Int> boxSet)
    {
        if (from == to) return true;

        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(from);
        visited.Add(from);

        while (queue.Count > 0)
        {
            var pos = queue.Dequeue();

            foreach (var dir in Dirs)
            {
                var next = pos + dir;
                if (next == to) return true;
                if (visited.Contains(next)) continue;
                if (board.IsWall(next)) continue;
                if (boxSet.Contains(next)) continue;

                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        return false;
    }

    /// <summary>
    /// BFS 求玩家从 from 到 to 的路径（方向列表）。
    /// </summary>
    public static List<Vector2Int> FindPlayerPath(Vector2Int from, Vector2Int to, SolverBoard board, HashSet<Vector2Int> boxSet)
    {
        if (from == to) return new List<Vector2Int>();

        var visited = new Dictionary<Vector2Int, Vector2Int>(); // pos -> cameFromDir 的反向
        var parent = new Dictionary<Vector2Int, Vector2Int>();   // pos -> parent pos
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(from);
        visited[from] = Vector2Int.zero;
        parent[from] = from;

        while (queue.Count > 0)
        {
            var pos = queue.Dequeue();

            foreach (var dir in Dirs)
            {
                var next = pos + dir;
                if (visited.ContainsKey(next)) continue;
                if (board.IsWall(next)) continue;
                if (boxSet.Contains(next)) continue;

                visited[next] = dir;
                parent[next] = pos;

                if (next == to)
                {
                    // 回溯路径
                    var path = new List<Vector2Int>();
                    var cur = to;
                    while (cur != from)
                    {
                        path.Add(visited[cur]);
                        cur = parent[cur];
                    }
                    path.Reverse();
                    return path;
                }

                queue.Enqueue(next);
            }
        }

        return null; // 不可达
    }

    private int ComputeHash()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + NormalizedPlayerPos.x;
            hash = hash * 31 + NormalizedPlayerPos.y;
            for (int i = 0; i < Boxes.Length; i++)
            {
                hash = hash * 31 + Boxes[i].x;
                hash = hash * 31 + Boxes[i].y;
            }
            return hash;
        }
    }

    public bool Equals(SolverState other)
    {
        if (other == null) return false;
        if (_hashCode != other._hashCode) return false;
        if (NormalizedPlayerPos != other.NormalizedPlayerPos) return false;
        if (Boxes.Length != other.Boxes.Length) return false;

        for (int i = 0; i < Boxes.Length; i++)
        {
            if (Boxes[i] != other.Boxes[i]) return false;
        }

        return true;
    }

    public override bool Equals(object obj) => Equals(obj as SolverState);
    public override int GetHashCode() => _hashCode;
}
