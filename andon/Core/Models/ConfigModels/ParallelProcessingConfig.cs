namespace Andon.Core.Models.ConfigModels;

/// <summary>
/// 並列処理設定
/// </summary>
public class ParallelProcessingConfig
{
    /// <summary>
    /// 並列処理を有効化（false: 順次処理）
    /// </summary>
    public bool EnableParallel { get; set; } = true;

    /// <summary>
    /// 最大並列度（0: 制限なし）
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 0;

    /// <summary>
    /// タイムアウト（全PLC処理完了まで、ミリ秒）
    /// </summary>
    public int OverallTimeoutMs { get; set; } = 30000;
}
