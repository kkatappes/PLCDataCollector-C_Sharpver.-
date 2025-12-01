namespace Andon.Core.Models.ConfigModels;

/// <summary>
/// タイムアウト設定
/// </summary>
public class TimeoutConfig
{
    /// <summary>
    /// 接続タイムアウト（ミリ秒）
    /// </summary>
    public int ConnectTimeoutMs { get; init; } = 5000;

    /// <summary>
    /// 送信タイムアウト（ミリ秒）
    /// </summary>
    public int SendTimeoutMs { get; init; } = 3000;

    /// <summary>
    /// 受信タイムアウト（ミリ秒）
    /// </summary>
    public int ReceiveTimeoutMs { get; init; } = 5000;

    /// <summary>
    /// フレーム送信間隔（ミリ秒）- 複数フレーム送信時の間隔制御
    /// </summary>
    public int SendIntervalMs { get; init; } = 100;
}
