namespace Andon.Core.Models.ConfigModels;

/// <summary>
/// PlcConfiguration用拡張メソッド
/// Phase 3-4: ConnectionConfig/TimeoutConfigへの変換を共通化
///
/// 目的: ApplicationController/ExecutionOrchestratorでの重複コード削除
/// バグの温床となっていた重複処理（計28行）を2行に削減
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// PlcConfigurationからConnectionConfigを生成
    /// </summary>
    /// <param name="config">PLC設定（Excel読み込み済み）</param>
    /// <returns>接続設定オブジェクト</returns>
    public static ConnectionConfig ToConnectionConfig(this PlcConfiguration config)
    {
        return new ConnectionConfig
        {
            IpAddress = config.IpAddress,
            Port = config.Port,
            UseTcp = config.ConnectionMethod == "TCP",
            IsBinary = config.IsBinary
        };
    }

    /// <summary>
    /// PlcConfigurationからTimeoutConfigを生成
    /// </summary>
    /// <param name="config">PLC設定（Excel読み込み済み）</param>
    /// <returns>タイムアウト設定オブジェクト</returns>
    public static TimeoutConfig ToTimeoutConfig(this PlcConfiguration config)
    {
        return new TimeoutConfig
        {
            ConnectTimeoutMs = config.Timeout,
            SendTimeoutMs = config.Timeout,
            ReceiveTimeoutMs = config.Timeout
        };
    }
}
