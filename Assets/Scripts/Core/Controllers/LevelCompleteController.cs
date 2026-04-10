using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 通关控制器：监听通关事件，禁用输入，显示通关 UI，处理玩家选择。
/// </summary>
public class LevelCompleteController : MonoBehaviour
{
    private LevelCompleteView _view;
    private GameRuleController _gameRule;
    private InputController _inputController;

    private void Start()
    {
        _view = GetComponent<LevelCompleteView>();
        _gameRule = FindAnyObjectByType<GameRuleController>();
        _inputController = FindAnyObjectByType<InputController>();

        if (_gameRule != null)
            _gameRule.OnLevelComplete += HandleLevelComplete;

        _view.OnNextLevelClicked += HandleNextLevel;
        _view.OnBackToSelectClicked += HandleBackToSelect;
    }

    private void HandleLevelComplete()
    {
        CampaignProgressController.MarkCurrentLevelCompleted();
        AudioController.Instance.PlayLevelCompleteSfx();

        if (_inputController != null)
            _inputController.InputEnabled = false;

        bool hasNext = CampaignProgressController.HasNextLevel();
        _view.Show(hasNext);
    }

    private void HandleNextLevel()
    {
        CampaignProgressController.AdvanceToNext();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void HandleBackToSelect()
    {
        AudioController.Instance.StopBgm();
        SceneManager.LoadScene(SceneNameModel.SelectScene);
    }
}
