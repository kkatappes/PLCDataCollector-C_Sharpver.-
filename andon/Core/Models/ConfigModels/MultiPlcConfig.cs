namespace Andon.Core.Models.ConfigModels;

/// <summary>
/// 複数PLC設定のルート
/// </summary>
public class MultiPlcConfig
{
    /// <summary>
    /// PLC接続設定リスト
    /// </summary>
    public List<PlcConnectionConfig> PlcConnections { get; set; } = new();

    /// <summary>
    /// 並列処理設定
    /// </summary>
    public ParallelProcessingConfig ParallelConfig { get; set; } = new();
}
