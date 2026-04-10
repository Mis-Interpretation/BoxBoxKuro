using System.Diagnostics;
using System.Threading;

/// <summary>
/// 线程安全的求解进度报告。后台求解线程写入，主线程 UI 读取。
/// </summary>
public class SolverProgress
{
    private int _nodesExpanded;
    private int _openListSize;
    private int _closedListSize;
    private int _currentBestFCost;
    private int _status; // 0=待机, 1=求解中, 2=成功, 3=失败, 4=已取消

    private readonly Stopwatch _stopwatch = new Stopwatch();

    public enum SolveStatus
    {
        Idle = 0,
        Solving = 1,
        Success = 2,
        Failed = 3,
        Cancelled = 4
    }

    // --- 后台线程写入 ---

    public void Start()
    {
        _stopwatch.Restart();
        Interlocked.Exchange(ref _status, (int)SolveStatus.Solving);
    }

    public void ReportNodes(int expanded, int openSize, int closedSize)
    {
        Interlocked.Exchange(ref _nodesExpanded, expanded);
        Interlocked.Exchange(ref _openListSize, openSize);
        Interlocked.Exchange(ref _closedListSize, closedSize);
    }

    public void ReportBestFCost(int fCost)
    {
        Interlocked.Exchange(ref _currentBestFCost, fCost);
    }

    public void Finish(SolveStatus status)
    {
        _stopwatch.Stop();
        Interlocked.Exchange(ref _status, (int)status);
    }

    // --- 主线程读取 ---

    public int NodesExpanded => Interlocked.CompareExchange(ref _nodesExpanded, 0, 0);
    public int OpenListSize => Interlocked.CompareExchange(ref _openListSize, 0, 0);
    public int ClosedListSize => Interlocked.CompareExchange(ref _closedListSize, 0, 0);
    public int CurrentBestFCost => Interlocked.CompareExchange(ref _currentBestFCost, 0, 0);
    public SolveStatus Status => (SolveStatus)Interlocked.CompareExchange(ref _status, 0, 0);

    /// <summary>
    /// 已用时间（秒）。
    /// </summary>
    public float ElapsedSeconds => (float)_stopwatch.Elapsed.TotalSeconds;

    /// <summary>
    /// 每秒展开节点数。
    /// </summary>
    public float NodesPerSecond
    {
        get
        {
            float elapsed = ElapsedSeconds;
            return elapsed > 0.001f ? NodesExpanded / elapsed : 0f;
        }
    }
}
