using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 求解器回放控制器：按序列逐步执行移动，复用 MoveController。
/// </summary>
public class SolverPlaybackController : MonoBehaviour
{
    public float MoveInterval = 0.15f;
    public float StartDelay = 0.5f;

    private List<Vector2Int> _moves;
    private MoveController _moveController;
    private bool _isPlaying;

    public bool IsPlaying => _isPlaying;

    /// <summary>
    /// 开始回放解法。
    /// </summary>
    public void PlaySolution(List<Vector2Int> moves)
    {
        _moves = moves;
        _moveController = FindAnyObjectByType<MoveController>();

        if (_moveController == null)
        {
            Debug.LogError("SolverPlaybackController: 未找到 MoveController");
            return;
        }

        StartCoroutine(PlaybackCoroutine());
    }

    private IEnumerator PlaybackCoroutine()
    {
        _isPlaying = true;

        // 禁用玩家输入，避免冲突
        var inputController = FindAnyObjectByType<InputController>();
        if (inputController != null)
            inputController.enabled = false;

        yield return new WaitForSeconds(StartDelay); // 短暂延迟让玩家看到初始状态

        foreach (var direction in _moves)
        {
            // 找到玩家实体
            var player = FindPlayer();
            if (player == null)
            {
                Debug.LogError("SolverPlaybackController: 未找到玩家实体");
                break;
            }

            _moveController.TryMove(player, direction);
            yield return new WaitForSeconds(MoveInterval);
        }

        _isPlaying = false;
        Debug.Log("求解器回放完成");
    }

    private PositionModel FindPlayer()
    {
        foreach (var c in FindObjectsByType<ControllableModel>(FindObjectsSortMode.None))
        {
            var pos = c.GetComponent<PositionModel>();
            if (pos != null) return pos;
        }
        return null;
    }
}
