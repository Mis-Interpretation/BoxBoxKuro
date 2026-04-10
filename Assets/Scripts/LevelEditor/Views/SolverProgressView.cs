using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 求解器实时进度面板。在求解过程中显示时间、节点数等信息。
/// </summary>
public class SolverProgressView : MonoBehaviour
{
    private SolverProgress _progress;
    private System.Action _onCancel;

    private VisualElement _progressRow;
    private Label _statusText;
    private Label _timeText;
    private Label _nodesText;
    private Label _speedText;
    private Button _cancelButton;

    private bool _isBound;

    private void Start()
    {
        EnsureBound();
        Hide();
    }

    /// <summary>
    /// 开始显示进度。求解开始时调用。
    /// </summary>
    public void Show(SolverProgress progress, System.Action onCancel)
    {
        EnsureBound();

        _progress = progress;
        _onCancel = onCancel;

        SetVisible(true);
        if (_cancelButton != null)
            _cancelButton.SetEnabled(true);
    }

    /// <summary>
    /// 隐藏进度面板。
    /// </summary>
    public void Hide()
    {
        _progress = null;
        _onCancel = null;

        SetVisible(false);
    }

    /// <summary>
    /// 显示最终结果信息（求解完成后短暂显示）。
    /// </summary>
    public void ShowResult(string message, float duration = 3f)
    {
        EnsureBound();

        SetVisible(true);
        if (_cancelButton != null)
            _cancelButton.SetEnabled(false);

        if (_statusText != null)
        {
            _statusText.text = message;
            _statusText.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
        }

        if (_timeText != null) _timeText.text = "";
        if (_nodesText != null) _nodesText.text = "";
        if (_speedText != null) _speedText.text = "";

        CancelInvoke(nameof(HideDelayed));
        Invoke(nameof(HideDelayed), duration);
    }

    private void HideDelayed()
    {
        Hide();
    }

    private void Update()
    {
        if (!_isBound)
            EnsureBound();

        if (_progress == null) return;

        var status = _progress.Status;
        float elapsed = _progress.ElapsedSeconds;
        int nodes = _progress.NodesExpanded;
        float nps = _progress.NodesPerSecond;

        if (_statusText != null)
        {
            switch (status)
            {
                case SolverProgress.SolveStatus.Solving:
                    _statusText.text = "求解中...";
                    _statusText.style.color = new StyleColor(new Color(1f, 0.85f, 0.3f)); // 黄色
                    break;
                case SolverProgress.SolveStatus.Success:
                    _statusText.text = "求解成功!";
                    _statusText.style.color = new StyleColor(new Color(0.3f, 1f, 0.4f)); // 绿色
                    break;
                case SolverProgress.SolveStatus.Failed:
                    _statusText.text = "求解失败";
                    _statusText.style.color = new StyleColor(new Color(1f, 0.4f, 0.3f)); // 红色
                    break;
                case SolverProgress.SolveStatus.Cancelled:
                    _statusText.text = "已取消";
                    _statusText.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f)); // 灰色
                    break;
                default:
                    _statusText.text = "待机";
                    _statusText.style.color = new StyleColor(new Color(0.8f, 0.8f, 0.8f));
                    break;
            }
        }

        if (_timeText != null)
        {
            if (elapsed < 60f)
                _timeText.text = $"耗时: {elapsed:F1}s";
            else
                _timeText.text = $"耗时: {elapsed / 60f:F1}min";
        }

        if (_nodesText != null)
        {
            if (nodes >= 1000000)
                _nodesText.text = $"节点: {nodes / 1000000f:F2}M";
            else if (nodes >= 1000)
                _nodesText.text = $"节点: {nodes / 1000f:F1}K";
            else
                _nodesText.text = $"节点: {nodes}";
        }

        if (_speedText != null)
        {
            if (nps >= 1000f)
                _speedText.text = $"速度: {nps / 1000f:F1}K/s";
            else
                _speedText.text = $"速度: {nps:F0}/s";
        }
    }

    private void EnsureBound()
    {
        if (_isBound) return;

        var hud = FindAnyObjectByType<EditorHUDView>();
        if (hud == null || hud.RootVisualElement == null) return;

        _progressRow = hud.RootVisualElement.Q("solver-progress-root");
        if (_progressRow == null) return;

        _statusText = _progressRow.Q<Label>("solver-status");
        _timeText = _progressRow.Q<Label>("solver-time");
        _nodesText = _progressRow.Q<Label>("solver-nodes");
        _speedText = _progressRow.Q<Label>("solver-speed");
        _cancelButton = _progressRow.Q<Button>("solver-cancel-btn");

        if (_cancelButton != null)
        {
            _cancelButton.clicked -= OnCancelClicked;
            _cancelButton.clicked += OnCancelClicked;
        }

        _isBound = true;
    }

    private void SetVisible(bool visible)
    {
        if (_progressRow == null) return;

        if (visible)
            _progressRow.RemoveFromClassList("hidden");
        else
            _progressRow.AddToClassList("hidden");
    }

    private void OnCancelClicked()
    {
        _onCancel?.Invoke();
        if (_cancelButton != null)
            _cancelButton.SetEnabled(false);
    }
}
