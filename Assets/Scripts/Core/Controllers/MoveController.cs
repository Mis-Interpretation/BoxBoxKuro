using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 处理移动、推动、阻挡判定的核心逻辑，并管理撤销历史。
/// </summary>
public class MoveController : MonoBehaviour
{
    private Stack<MoveRecord> _history = new Stack<MoveRecord>();
    private MoveRecord _currentRecord;
    private Dictionary<PositionModel, Vector2Int> _initialPositions = new Dictionary<PositionModel, Vector2Int>();
    public bool LastMoveHadPush { get; private set; }
    public string LastPushedEntityId { get; private set; } = "";

    /// <summary>
    /// 保存所有实体的初始位置，用于重新开始关卡。
    /// </summary>
    public void SaveInitialPositions()
    {
        _initialPositions.Clear();
        foreach (var pos in FindObjectsByType<PositionModel>(FindObjectsSortMode.None))
            _initialPositions[pos] = pos.GridPosition;
    }

    /// <summary>
    /// 恢复所有实体到初始位置，清空历史记录。
    /// </summary>
    public void RestoreInitialPositions()
    {
        foreach (var kvp in _initialPositions)
        {
            if (kvp.Key != null)
                kvp.Key.GridPosition = kvp.Value;
        }
        ClearHistory();
        _currentRecord = null;
    }

    /// <summary>
    /// 开始记录一次操作（在 TryMove 之前调用）。
    /// </summary>
    public void BeginRecordingMove()
    {
        _currentRecord = new MoveRecord();
        LastMoveHadPush = false;
        LastPushedEntityId = "";
    }

    /// <summary>
    /// 提交本次操作记录到历史栈（移动成功时调用）。
    /// </summary>
    public void CommitMove()
    {
        if (_currentRecord != null && _currentRecord.Moves.Count > 0)
            _history.Push(_currentRecord);
        _currentRecord = null;
    }

    /// <summary>
    /// 丢弃本次操作记录（移动失败时调用）。
    /// </summary>
    public void DiscardMove()
    {
        _currentRecord = null;
    }

    /// <summary>
    /// 撤销上一步操作，逆序恢复所有实体位置。
    /// </summary>
    public bool Undo()
    {
        if (_history.Count == 0) return false;

        var record = _history.Pop();
        // 逆序恢复，先恢复后移动的实体（如箱子先恢复，再恢复玩家）
        for (int i = record.Moves.Count - 1; i >= 0; i--)
        {
            var move = record.Moves[i];
            if (move.Entity != null)
                move.Entity.GridPosition = move.FromPosition;
        }
        return true;
    }

    /// <summary>
    /// 清空撤销历史。
    /// </summary>
    public void ClearHistory()
    {
        _history.Clear();
    }

    /// <summary>
    /// 尝试将实体沿 direction 移动一格。
    /// 返回是否移动成功。
    /// </summary>
    public bool TryMove(PositionModel mover, Vector2Int direction)
    {
        // 自身必须是 Movable
        if (mover.GetComponent<MovableModel>() == null) return false;

        Vector2Int targetPos = mover.GridPosition + direction;

        // 检查目标格子上的所有实体
        foreach (var other in FindObjectsByType<PositionModel>(FindObjectsSortMode.None))
        {
            if (other == mover) continue;
            if (other.GridPosition != targetPos) continue;

            // 目标格子有 Overlappable 实体（如终点），允许进入
            if (other.GetComponent<OverlappableModel>() != null)
                continue;

            // 目标格子有 Pushable + Movable 实体（如箱子），尝试推动
            if (other.GetComponent<PushableModel>() != null)
            {
                bool pushed = TryMove(other, direction);
                if (!pushed) return false;
                LastMoveHadPush = true;
                if (string.IsNullOrEmpty(LastPushedEntityId))
                    LastPushedEntityId = other.gameObject.name;
                continue;
            }

            // 目标格子有 Blocking 实体（如墙壁），阻止移动
            if (other.GetComponent<BlockingModel>() != null)
                return false;
        }

        // 记录移动前的位置
        if (_currentRecord != null)
            _currentRecord.Add(mover, mover.GridPosition);

        // 移动成功
        mover.GridPosition = targetPos;
        return true;
    }
}
