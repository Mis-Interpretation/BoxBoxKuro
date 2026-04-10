using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 通关 UI 视图：显示/隐藏通关遮罩面板，暴露按钮事件。
/// </summary>
public class LevelCompleteView : MonoBehaviour
{
    public event System.Action OnNextLevelClicked;
    public event System.Action OnBackToSelectClicked;

    private VisualElement _overlay;
    private Button _nextLevelBtn;
    private Button _backToSelectBtn;

    private void Awake()
    {
        var uiDoc = GetComponent<UIDocument>();
        var root = uiDoc.rootVisualElement;

        _overlay = root.Q("level-complete-overlay");
        _nextLevelBtn = root.Q<Button>("next-level-btn");
        _backToSelectBtn = root.Q<Button>("back-to-select-btn");

        _nextLevelBtn.clicked += () => OnNextLevelClicked?.Invoke();
        _backToSelectBtn.clicked += () => OnBackToSelectClicked?.Invoke();

        Hide();
    }

    public void Show(bool hasNextLevel)
    {
        _nextLevelBtn.style.display = hasNextLevel ? DisplayStyle.Flex : DisplayStyle.None;
        _overlay.RemoveFromClassList("hidden");
    }

    public void Hide()
    {
        _overlay.AddToClassList("hidden");
    }
}
