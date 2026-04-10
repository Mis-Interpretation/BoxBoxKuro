using UnityEngine;

/// <summary>
/// 全局规则：检测胜利条件。
/// 当所有 OverlappableModel（终点）都被箱子覆盖时，游戏胜利。
/// </summary>
public class GameRuleController : MonoBehaviour
{
    public bool IsLevelComplete { get; private set; }

    public event System.Action OnLevelComplete;

    /// <summary>
    /// 重置通关状态（撤销时调用，允许玩家继续操作）。
    /// </summary>
    public void ResetCompletion()
    {
        IsLevelComplete = false;
    }

    private void LateUpdate()
    {
        if (IsLevelComplete) return;

        var goals = FindObjectsByType<OverlappableModel>(FindObjectsSortMode.None);
        if (goals.Length == 0) return;

        foreach (var goal in goals)
        {
            var goalPos = goal.GetComponent<PositionModel>();
            if (goalPos == null) continue;

            bool covered = false;
            foreach (var pos in FindObjectsByType<PositionModel>(FindObjectsSortMode.None))
            {
                if (pos == goalPos) continue;
                if (pos.GridPosition == goalPos.GridPosition && pos.GetComponent<PushableModel>() != null)
                {
                    covered = true;
                    break;
                }
            }

            if (!covered) return;
        }

        // 所有终点都被覆盖
        IsLevelComplete = true;
        OnLevelComplete?.Invoke();
        Debug.Log("Level Complete!");
    }
}
