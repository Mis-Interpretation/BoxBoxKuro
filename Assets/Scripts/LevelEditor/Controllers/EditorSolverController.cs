using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 编辑器求解控制器：点击 Solve 按钮后，后台运行 A* 求解，
/// 实时显示进度，成功后进入试玩模式并自动回放解法。
/// </summary>
public class EditorSolverController : MonoBehaviour
{
    [Tooltip("求解器参数配置。通过 Create > Sokoban > Solver Settings 创建。")]
    public SolverSettings Settings;

    private EditorStateModel _state;
    private EditorValidateController _validateController;
    private EditorMetadataController _metadata;
    private SolverProgressView _progressView;
    private CancellationTokenSource _cts;
    private bool _isSolving;

    /// <summary>
    /// 是否正在求解中。
    /// </summary>
    public bool IsSolving => _isSolving;

    /// <summary>
    /// 当前求解进度（求解中时非 null）。
    /// </summary>
    public SolverProgress CurrentProgress { get; private set; }

    private void Awake()
    {
        _state = FindAnyObjectByType<EditorStateModel>();
        _validateController = FindAnyObjectByType<EditorValidateController>();
        _metadata = FindAnyObjectByType<EditorMetadataController>();
        _progressView = FindAnyObjectByType<SolverProgressView>();
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    /// <summary>
    /// 取得当前生效的参数（有 Settings 用 Settings，否则用默认值）。
    /// </summary>
    private int MaxNodes => Settings != null ? Settings.MaxNodes : 1000000;
    private float TimeoutSec => Settings != null ? Settings.TimeoutSeconds : 60f;
    private int ReportInterval => Settings != null ? Settings.ProgressReportInterval : 100;
    private float PlaybackInterval => Settings != null ? Settings.PlaybackInterval : 0.15f;
    private float PlaybackStartDelay => Settings != null ? Settings.PlaybackStartDelay : 0.5f;

    /// <summary>
    /// 由 EditorHUDView 的 Solve 按钮调用。
    /// </summary>
    public async void OnSolve()
    {
        if (_isSolving)
        {
            Debug.Log("正在求解中，请稍候...");
            return;
        }

        if (_validateController != null && _validateController.IsValidating)
        {
            Debug.Log("请先退出试玩模式");
            return;
        }

        var level = _state.CurrentLevel;

        if (level.Entities.Count == 0)
        {
            Debug.LogWarning("当前关卡为空，无法求解！");
            return;
        }

        // 求解器支持四种基础类型，以及明确标记为纯装饰物的自定义类型。
        if (TryGetUnsupportedEntityType(level, out int unsupportedType))
        {
            const string message =
                "当前关卡包含求解器不支持的实体类型。\n" +
                "自动求解器目前支持：\n" +
                "- 0 Player\n" +
                "- 1 Block\n" +
                "- 2 Box\n" +
                "- 3 Endpoint\n" +
                "- 已标记 IsPureDecoration=true 的纯装饰物\n\n" +
                "检测到不支持的 TypeIndex: {0}\n\n" +
                "因此无法自动求解此关卡。";

            ShowAlert(string.Format(message, unsupportedType));
            return;
        }

        _isSolving = true;
        _cts?.Cancel();

        // 超时设置：0 表示不限时
        if (TimeoutSec > 0)
            _cts = new CancellationTokenSource((int)(TimeoutSec * 1000));
        else
            _cts = new CancellationTokenSource();

        // 创建进度报告
        CurrentProgress = new SolverProgress();

        // 显示进度 UI
        if (_progressView != null)
            _progressView.Show(CurrentProgress, () => _cts?.Cancel());

        Debug.Log($"开始求解... (上限: {MaxNodes} 节点, 超时: {(TimeoutSec > 0 ? $"{TimeoutSec}s" : "无")})");

        SolverResult result = null;

        try
        {
            // 深拷贝关卡数据，在后台线程安全使用
            var levelCopy = DeepCopyLevel(level);
            var ct = _cts.Token;
            int maxNodes = MaxNodes;
            int reportInterval = ReportInterval;
            var progress = CurrentProgress;

            // 后台线程运行求解器
            result = await Task.Run(() =>
            {
                var solver = new SokobanSolver();
                return solver.Solve(levelCopy, ct, maxNodes, progress, reportInterval);
            });

            // 回到主线程
            Debug.Log(result.Message);

            if (result.Success)
            {
                Debug.Log($"找到解法：{result.Moves.Count} 步移动，{result.PushCount} 次推箱子");
                if (_metadata != null) _metadata.MarkSolvable();

                // 显示成功结果后短暂延迟再回放
                if (_progressView != null)
                    _progressView.ShowResult(
                        $"求解成功! {result.Moves.Count} 步, {result.PushCount} 推, {CurrentProgress.ElapsedSeconds:F2}s",
                        2f);

                await Task.Delay(1500); // 让用户看到结果

                // 隐藏进度 UI
                if (_progressView != null)
                    _progressView.Hide();

                // 进入试玩模式
                _validateController.StartValidation();

                // 等一帧让试玩模式初始化完成
                await Task.Yield();

                // 附加回放控制器并开始回放
                if (_validateController.GameplayHost != null)
                {
                    var playback = _validateController.GameplayHost.AddComponent<SolverPlaybackController>();
                    playback.MoveInterval = PlaybackInterval;
                    playback.StartDelay = PlaybackStartDelay;
                    playback.PlaySolution(result.Moves);
                }
            }
            else
            {
                // 显示失败结果
                if (_progressView != null)
                    _progressView.ShowResult(
                        $"求解失败: {result.Message}\n耗时 {CurrentProgress.ElapsedSeconds:F2}s",
                        5f);
            }
        }
        catch (TaskCanceledException)
        {
            Debug.Log("求解超时或被取消");
            if (_progressView != null)
                _progressView.ShowResult($"求解超时 ({CurrentProgress.ElapsedSeconds:F1}s)", 3f);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"求解出错: {e.Message}\n{e.StackTrace}");
            if (_progressView != null)
                _progressView.ShowResult($"出错: {e.Message}", 5f);
        }
        finally
        {
            _isSolving = false;
            CurrentProgress = null;
        }
    }

    private bool TryGetUnsupportedEntityType(LevelDataModel level, out int unsupportedType)
    {
        unsupportedType = -1;
        if (level?.Entities == null) return false;

        for (int i = 0; i < level.Entities.Count; i++)
        {
            int type = level.Entities[i].Type;
            if (type <= 3) continue;

            var config = _state?.ConfigReader?.GetConfigByIndex(type);
            if (config != null && config.IsPureDecoration) continue;

            unsupportedType = type;
            return true;
        }
        return false;
    }

    private static void ShowAlert(string message)
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.DisplayDialog("提示", message, "确定");
#else
        Debug.LogWarning(message);
#endif
    }

    private static LevelDataModel DeepCopyLevel(LevelDataModel source)
    {
        var copy = new LevelDataModel();
        copy.LevelName = source.LevelName;
        copy.Width = source.Width;
        copy.Height = source.Height;

        foreach (var e in source.Entities)
        {
            copy.Entities.Add(new EntityData
            {
                Type = e.Type,
                X = e.X,
                Y = e.Y
            });
        }

        if (source.Metadata != null)
            copy.Metadata = source.Metadata.DeepCopy();

        return copy;
    }
}
