using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 静态棋盘数据：墙壁、目标、死格预计算。从 LevelDataModel 转换而来。
/// </summary>
public class SolverBoard
{
    public int MinX { get; private set; }
    public int MinY { get; private set; }
    public int MaxX { get; private set; }
    public int MaxY { get; private set; }

    public HashSet<Vector2Int> Walls { get; private set; }
    public Vector2Int[] Goals { get; private set; }
    public Vector2Int PlayerStart { get; private set; }
    public Vector2Int[] BoxesStart { get; private set; }

    /// <summary>
    /// 死格：箱子推到这里永远无法到达任何目标。
    /// </summary>
    public HashSet<Vector2Int> DeadSquares { get; private set; }

    private static readonly Vector2Int[] Dirs =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    public static SolverBoard FromLevelData(LevelDataModel level)
    {
        var board = new SolverBoard();
        board.Walls = new HashSet<Vector2Int>();
        var goals = new List<Vector2Int>();
        var boxes = new List<Vector2Int>();
        Vector2Int player = Vector2Int.zero;
        bool hasPlayer = false;

        foreach (var e in level.Entities)
        {
            var pos = new Vector2Int(e.X, e.Y);
            switch (e.Type)
            {
                case 0: // Player
                    player = pos;
                    hasPlayer = true;
                    break;
                case 1: // Block
                    board.Walls.Add(pos);
                    break;
                case 2: // Box
                    boxes.Add(pos);
                    break;
                case 3: // Endpoint
                    goals.Add(pos);
                    break;
            }
        }

        board.PlayerStart = player;
        board.Goals = goals.ToArray();
        board.BoxesStart = boxes.ToArray();

        // 计算边界
        board.ComputeBounds(level);

        // 预计算死格
        board.DeadSquares = board.ComputeDeadSquares();

        return board;
    }

    private void ComputeBounds(LevelDataModel level)
    {
        // 优先使用会影响求解状态的实体（当前为 TypeIndex 0-3）来计算边界，
        // 避免纯装饰物把边界无意义放大。
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        bool hasRelevantEntity = false;

        foreach (var e in level.Entities)
        {
            if (!IsGameplayRelevantEntityType(e.Type))
                continue;

            hasRelevantEntity = true;
            if (e.X < minX) minX = e.X;
            if (e.X > maxX) maxX = e.X;
            if (e.Y < minY) minY = e.Y;
            if (e.Y > maxY) maxY = e.Y;
        }

        // 兜底：若没有核心实体，则退回到全部实体范围，避免边界失效。
        if (!hasRelevantEntity)
        {
            foreach (var e in level.Entities)
            {
                if (e.X < minX) minX = e.X;
                if (e.X > maxX) maxX = e.X;
                if (e.Y < minY) minY = e.Y;
                if (e.Y > maxY) maxY = e.Y;
            }
        }

        if (minX == int.MaxValue || minY == int.MaxValue)
        {
            MinX = -1;
            MinY = -1;
            MaxX = 1;
            MaxY = 1;
            return;
        }

        MinX = minX - 1;
        MinY = minY - 1;
        MaxX = maxX + 1;
        MaxY = maxY + 1;
    }

    private static bool IsGameplayRelevantEntityType(int type)
    {
        return type >= 0 && type <= 3;
    }

    /// <summary>
    /// 检查位置是否为墙壁。
    /// </summary>
    public bool IsWall(Vector2Int pos)
    {
        return Walls.Contains(pos);
    }

    /// <summary>
    /// 检查位置是否为死格。
    /// </summary>
    public bool IsDead(Vector2Int pos)
    {
        return DeadSquares.Contains(pos);
    }

    /// <summary>
    /// 检查位置是否为目标。
    /// </summary>
    public bool IsGoal(Vector2Int pos)
    {
        for (int i = 0; i < Goals.Length; i++)
            if (Goals[i] == pos) return true;
        return false;
    }

    /// <summary>
    /// 从每个目标反向 BFS（拉箱子），不可达的非墙格标记为死格。
    /// </summary>
    private HashSet<Vector2Int> ComputeDeadSquares()
    {
        // 收集所有可能的地板格（非墙壁且在边界内）
        var allFloor = new HashSet<Vector2Int>();
        for (int x = MinX; x <= MaxX; x++)
        {
            for (int y = MinY; y <= MaxY; y++)
            {
                var p = new Vector2Int(x, y);
                if (!Walls.Contains(p))
                    allFloor.Add(p);
            }
        }

        // 从每个目标反向拉箱子 BFS，找出箱子可以到达目标的所有位置
        var alive = new HashSet<Vector2Int>();

        foreach (var goal in Goals)
        {
            // BFS: 从 goal 出发，反向拉箱子
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(goal);
            visited.Add(goal);

            while (queue.Count > 0)
            {
                var boxPos = queue.Dequeue();
                alive.Add(boxPos);

                // 对每个方向：反向拉 = 箱子从 boxPos 被拉到 boxPos - dir，
                // 需要拉的人站在 boxPos + dir，拉之前箱子在 boxPos，拉之后在 boxPos - dir
                // 等价于：推的反向。人站在 boxPos - dir 的反方向，即 boxPos + dir
                // 反向操作：人在 boxPos + dir，箱子从 boxPos 被拉到 boxPos - dir 不对...
                //
                // 正确理解：正向推是人在 boxPos - dir 推箱子到 boxPos + dir 不对...
                // 正向：人站在 box - dir, 推箱子从 box 到 box + dir
                // 反向（拉回来）：箱子从 boxPos 回到 boxPos - dir，
                //   需要 boxPos - dir 不是墙，且 boxPos + dir 不是墙（人站的位置）
                foreach (var dir in Dirs)
                {
                    // 箱子要拉到的位置（反向=正向推的来源方向）
                    Vector2Int pullTo = boxPos + dir;
                    // 人需要站在的位置（拉时人在箱子的另一侧）
                    Vector2Int pullerPos = boxPos + dir + dir;

                    if (Walls.Contains(pullTo) || Walls.Contains(pullerPos))
                        continue;

                    if (visited.Contains(pullTo))
                        continue;

                    visited.Add(pullTo);
                    queue.Enqueue(pullTo);
                }
            }
        }

        // 死格 = 地板格 - 存活格
        var dead = new HashSet<Vector2Int>();
        foreach (var pos in allFloor)
        {
            if (!alive.Contains(pos))
                dead.Add(pos);
        }

        return dead;
    }
}
