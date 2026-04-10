using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 求解结果：是否成功、完整移动路径、统计信息。
/// </summary>
public class SolverResult
{
    /// <summary>
    /// 是否找到解。
    /// </summary>
    public bool Success;

    /// <summary>
    /// 完整移动路径（包括玩家走路 + 推箱子，每步一个方向向量）。
    /// </summary>
    public List<Vector2Int> Moves;

    /// <summary>
    /// 推箱子次数。
    /// </summary>
    public int PushCount;

    /// <summary>
    /// 搜索展开的节点数。
    /// </summary>
    public int NodesExpanded;

    /// <summary>
    /// 错误/状态信息。
    /// </summary>
    public string Message;

    /// <summary>
    /// 从 A* 搜索的推箱子序列还原完整 LURD 路径。
    /// </summary>
    public static SolverResult FromSolution(SolverState goalState, SolverBoard board, int nodesExpanded)
    {
        var result = new SolverResult();
        result.NodesExpanded = nodesExpanded;

        // 回溯推箱子序列
        var pushSequence = new List<SolverState>();
        var current = goalState;
        while (current.Parent != null)
        {
            pushSequence.Add(current);
            current = current.Parent;
        }
        pushSequence.Reverse();

        var initialState = current; // 根状态
        result.PushCount = pushSequence.Count;

        // 还原完整路径
        var moves = new List<Vector2Int>();
        var playerPos = initialState.ActualPlayerPos;
        var boxPositions = new HashSet<Vector2Int>(initialState.Boxes);

        foreach (var state in pushSequence)
        {
            Vector2Int pushFrom = state.PushedBoxFrom - state.PushDirection;

            var walkPath = SolverState.FindPlayerPath(playerPos, pushFrom, board, boxPositions);
            if (walkPath != null)
            {
                moves.AddRange(walkPath);
            }

            moves.Add(state.PushDirection);
            playerPos = state.PushedBoxFrom;

            // 链式推动：从最远的箱子开始向后更新，避免位置覆盖冲突
            for (int c = state.ChainLength - 1; c >= 0; c--)
            {
                Vector2Int from = state.PushedBoxFrom + state.PushDirection * c;
                boxPositions.Remove(from);
                boxPositions.Add(from + state.PushDirection);
            }
        }

        result.Success = true;
        result.Moves = moves;
        result.Message = $"解出! {moves.Count} 步移动, {result.PushCount} 次推箱子, 展开 {nodesExpanded} 个节点";
        return result;
    }

    public static SolverResult Failure(string message, int nodesExpanded)
    {
        return new SolverResult
        {
            Success = false,
            Moves = null,
            PushCount = 0,
            NodesExpanded = nodesExpanded,
            Message = message
        };
    }
}
