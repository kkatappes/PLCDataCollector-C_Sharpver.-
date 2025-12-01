namespace Andon.Core.Models;

/// <summary>
/// 複数PLC実行結果
/// </summary>
public class MultiPlcExecutionResult
{
    /// <summary>
    /// 全体の成功/失敗
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 実行開始時刻
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 実行終了時刻
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 総実行時間
    /// </summary>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>
    /// 各PLCの実行結果（PlcId -> PlcExecutionResult）
    /// </summary>
    public Dictionary<string, PlcExecutionResult> PlcResults { get; set; } = new();

    /// <summary>
    /// 成功したPLC数
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失敗したPLC数
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// エラーメッセージ（全体エラー時）
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 単一PLC実行結果
/// </summary>
public class PlcExecutionResult
{
    /// <summary>
    /// PLC識別ID
    /// </summary>
    public string PlcId { get; set; } = string.Empty;

    /// <summary>
    /// PLC名称
    /// </summary>
    public string PlcName { get; set; } = string.Empty;

    /// <summary>
    /// 実行成功/失敗
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 実行開始時刻
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 実行終了時刻
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 実行時間
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// デバイスデータ（バイナリ）
    /// </summary>
    public byte[]? DeviceData { get; set; }

    /// <summary>
    /// パース済みデバイスデータ（将来のPhase5実装用）
    /// </summary>
    public Dictionary<string, DeviceData>? ParsedDeviceData { get; set; }

    /// <summary>
    /// エラーメッセージ
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 発生した例外
    /// </summary>
    public Exception? Exception { get; set; }
}
