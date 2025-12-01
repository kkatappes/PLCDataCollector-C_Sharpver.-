namespace Andon.Core.Models.ConfigModels;

/// <summary>
/// データ処理設定
/// </summary>
public class DataProcessingConfig
{
    /// <summary>
    /// 監視間隔（ミリ秒）
    /// </summary>
    public int MonitoringIntervalMs { get; set; } = 5000; // デフォルト5秒
}
