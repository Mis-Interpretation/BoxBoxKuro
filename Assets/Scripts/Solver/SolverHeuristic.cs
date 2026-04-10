using UnityEngine;

/// <summary>
/// 启发式函数：贪心最小匹配，箱子到目标的 Manhattan 距离。
/// </summary>
public static class SolverHeuristic
{
    /// <summary>
    /// 贪心最小匹配：每次选距离最近的(箱子,目标)配对，求总 Manhattan 距离下界。
    /// </summary>
    public static int Compute(Vector2Int[] boxes, Vector2Int[] goals)
    {
        int n = boxes.Length;
        if (n == 0) return 0;

        // 标记已分配的箱子和目标
        var usedBox = new bool[n];
        var usedGoal = new bool[goals.Length];
        int totalCost = 0;

        for (int round = 0; round < n; round++)
        {
            int bestCost = int.MaxValue;
            int bestBox = -1;
            int bestGoal = -1;

            for (int b = 0; b < n; b++)
            {
                if (usedBox[b]) continue;
                for (int g = 0; g < goals.Length; g++)
                {
                    if (usedGoal[g]) continue;
                    int dist = Manhattan(boxes[b], goals[g]);
                    if (dist < bestCost)
                    {
                        bestCost = dist;
                        bestBox = b;
                        bestGoal = g;
                    }
                }
            }

            if (bestBox < 0) break;

            usedBox[bestBox] = true;
            usedGoal[bestGoal] = true;
            totalCost += bestCost;
        }

        return totalCost;
    }

    private static int Manhattan(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
