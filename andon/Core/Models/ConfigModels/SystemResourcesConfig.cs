namespace Andon.Core.Models.ConfigModels;

/// <summary>
/// リソース制限設定
/// </summary>
public class SystemResourcesConfig
{
    /// <summary>
    /// 最大同時接続数
    /// </summary>
    public int MaxConcurrentConnections { get; init; } = 10;

    /// <summary>
    /// メモリ使用量上限（MB）
    /// </summary>
    public int MaxMemoryUsageMb { get; init; } = 512;

    /// <summary>
    /// 最大ログファイルサイズ（MB）
    /// </summary>
    public int MaxLogFileSizeMb { get; init; } = 100;
}
