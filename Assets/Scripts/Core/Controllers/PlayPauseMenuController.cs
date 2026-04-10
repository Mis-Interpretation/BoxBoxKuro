using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// 玩家关卡中的 ESC 菜单控制：暂停/恢复、重玩、退出。
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class PlayPauseMenuController : MonoBehaviour
{
    private UIDocument _uiDocument;
    private VisualElement _overlay;
    private Button _continueBtn;
    private Button _restartBtn;
    private Button _exitBtn;

    private InputController _inputController;
    private GameRuleController _gameRuleController;

    private bool _isPaused;

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        var root = _uiDocument.rootVisualElement;

        _overlay = root.Q("pause-menu-overlay");
        _continueBtn = root.Q<Button>("pause-continue-btn");
        _restartBtn = root.Q<Button>("pause-restart-btn");
        _exitBtn = root.Q<Button>("pause-exit-btn");

        if (_continueBtn != null) _continueBtn.clicked += OnContinueClicked;
        if (_restartBtn != null) _restartBtn.clicked += OnRestartClicked;
        if (_exitBtn != null) _exitBtn.clicked += OnExitClicked;

        _inputController = FindAnyObjectByType<InputController>();
        _gameRuleController = FindAnyObjectByType<GameRuleController>();

        HideMenu();
        EnsureRunningState();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (_isPaused)
        {
            ResumeGame();
            return;
        }

        // 通关后不再弹暂停菜单，避免与通关面板叠加。
        if (_gameRuleController != null && _gameRuleController.IsLevelComplete)
            return;

        PauseGame();
    }

    private void OnDisable()
    {
        EnsureRunningState();
    }

    private void OnDestroy()
    {
        EnsureRunningState();
    }

    private void PauseGame()
    {
        _isPaused = true;
        ShowMenu();
        Time.timeScale = 0f;

        if (_inputController != null)
            _inputController.InputEnabled = false;
    }

    private void ResumeGame()
    {
        _isPaused = false;
        HideMenu();
        Time.timeScale = 1f;

        if (_inputController != null)
            _inputController.InputEnabled = true;
    }

    private void EnsureRunningState()
    {
        _isPaused = false;
        HideMenu();
        Time.timeScale = 1f;

        if (_inputController != null)
            _inputController.InputEnabled = true;
    }

    private void OnContinueClicked()
    {
        ResumeGame();
    }

    private void OnRestartClicked()
    {
        Time.timeScale = 1f;
        if (_inputController != null)
            _inputController.InputEnabled = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnExitClicked()
    {
        Time.timeScale = 1f;
        if (_inputController != null)
            _inputController.InputEnabled = true;
        AudioController.Instance.StopBgm();
        SceneManager.LoadScene(SceneNameModel.SelectScene);
    }

    private void ShowMenu()
    {
        _overlay?.RemoveFromClassList("hidden");
    }

    private void HideMenu()
    {
        _overlay?.AddToClassList("hidden");
    }
}
