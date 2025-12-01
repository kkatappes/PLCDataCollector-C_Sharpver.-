namespace Andon.Core.Models;

/// <summary>
/// 並行実行結果
/// </summary>
public class ParallelExecutionResult
{
    /// <summary>対象PLC総数</summary>
    public int TotalPlcCount { get; set; }

    /// <summary>成功PLC数</summary>
    public int SuccessfulPlcCount { get; set; }

    /// <summary>失敗PLC数</summary>
    public int FailedPlcCount { get; set; }

    /// <summary>PLC別実行結果（PlcId → CycleExecutionResult）</summary>
    public Dictionary<string, CycleExecutionResult> PlcResults { get; set; } = new();

    /// <summary>全体実行時間</summary>
    public TimeSpan OverallExecutionTime { get; set; }

    /// <summary>継続実行中PLC ID一覧</summary>
    public List<string> ContinuingPlcIds { get; set; } = new();

    /// <summary>並行実行が全体として成功したかどうか</summary>
    public bool IsOverallSuccess => FailedPlcCount == 0 && SuccessfulPlcCount > 0;

    /// <summary>成功率</summary>
    public double SuccessRate => TotalPlcCount > 0 
        ? (double)SuccessfulPlcCount / TotalPlcCount * 100.0 
        : 0.0;
}
