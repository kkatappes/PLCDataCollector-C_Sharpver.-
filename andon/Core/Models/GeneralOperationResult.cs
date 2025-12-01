namespace Andon.Core.Models;

/// <summary>
/// 一般処理一括実行結果
/// </summary>
/// <summary>
/// 一般処理一括実行結果
/// </summary>
public class GeneralOperationResult
{
    /// <summary>
    /// 成功処理数
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失敗処理数
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// 全体実行時間
    /// </summary>
    public TimeSpan TotalExecutionTime { get; set; }

    /// <summary>
    /// 失敗した処理名一覧
    /// </summary>
    public List<string> FailedOperations { get; set; } = new();

    /// <summary>
    /// 発生例外一覧
    /// </summary>
    public List<Exception> Exceptions { get; set; } = new();
}
