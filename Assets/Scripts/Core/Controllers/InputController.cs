using UnityEngine;

/// <summary>
/// 读取玩家输入，找到 Controllable 实体，请求 MoveController 执行移动。
/// </summary>
public class InputController : MonoBehaviour
{
    public bool InputEnabled { get; set; } = true;

    private MoveController _moveController;
    private GameRuleController _gameRuleController;
    private IEntityConfigReader _entityConfigReader;

    private void Awake()
    {
        _moveController = FindAnyObjectByType<MoveController>();
        _gameRuleController = FindAnyObjectByType<GameRuleController>();
        _entityConfigReader = new JsonEntityConfigProvider();
    }

    private void Update()
    {
        if (!InputEnabled) return;

        // R 键重新开始关卡
        if (Input.GetKeyDown(KeyCode.R))
        {
            _moveController.RestoreInitialPositions();
            if (_gameRuleController != null)
                _gameRuleController.ResetCompletion();
            return;
        }

        // Z 键撤销上一步
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (_moveController.Undo())
            {
                if (_gameRuleController != null)
                    _gameRuleController.ResetCompletion();
            }
            return;
        }

        Vector2Int direction = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            direction = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            direction = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            direction = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            direction = Vector2Int.right;

        if (direction == Vector2Int.zero) return;

        foreach (var controllable in FindObjectsByType<ControllableModel>(FindObjectsSortMode.None))
        {
            var position = controllable.GetComponent<PositionModel>();
            if (position != null)
            {
                _moveController.BeginRecordingMove();
                bool moved = _moveController.TryMove(position, direction);
                if (moved)
                {
                    PlayComponentSfx(controllable.gameObject.name, "ControllableModel");
                    if (_moveController.LastMoveHadPush)
                        PlayComponentSfx(_moveController.LastPushedEntityId, "PushableModel");
                    _moveController.CommitMove();
                }
                else
                    _moveController.DiscardMove();
            }
        }
    }

    private void PlayComponentSfx(string entityId, string componentName)
    {
        if (string.IsNullOrWhiteSpace(entityId) || _entityConfigReader == null) return;
        if (_entityConfigReader.TryGetComponentSfx(entityId, componentName, out var sfxPath))
            AudioController.Instance.PlaySfx(sfxPath);
    }
}
