using UnityEngine;

/// <summary>
/// 求解器可序列化参数。在 Inspector 中调整。
/// 通过 Project 窗口 Create > Sokoban > Solver Settings 创建。
/// </summary>
[CreateAssetMenu(fileName = "SolverSettings", menuName = "Sokoban/Solver Settings")]
public class SolverSettings : ScriptableObject
{
    [Header("搜索限制")]

    [Tooltip("最大展开节点数。复杂谜题可调高（如 5000000），但内存占用会增大。")]
    [Min(1000)]
    public int MaxNodes = 1000000;

    [Tooltip("超时时间（秒）。0 表示不限时（仅受节点数限制）。")]
    [Min(0)]
    public float TimeoutSeconds = 60f;

    [Header("回放设置")]

    [Tooltip("回放时每步的间隔时间（秒）。")]
    [Range(0.02f, 1f)]
    public float PlaybackInterval = 0.15f;

    [Tooltip("回放开始前的等待时间（秒）。")]
    [Range(0f, 3f)]
    public float PlaybackStartDelay = 0.5f;

    [Header("进度报告")]

    [Tooltip("每展开多少个节点向主线程报告一次进度。值越小 UI 越实时，但有微小性能开销。")]
    [Range(10, 5000)]
    public int ProgressReportInterval = 100;
}
