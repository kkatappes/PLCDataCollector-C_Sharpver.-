using Andon.Core.Models.ConfigModels;

namespace Andon.Tests.TestUtilities.Stubs;

/// <summary>
/// 設定スタブ
/// </summary>
public static class ConfigurationStubs
{
    /// <summary>
    /// 有効な接続設定を作成
    /// </summary>
    public static ConnectionConfig CreateValidConnectionConfig()
    {
        return new ConnectionConfig
        {
            IpAddress = "192.168.1.100",
            Port = 5007,
            UseTcp = true
        };
    }

    /// <summary>
    /// 無効な接続設定を作成（テスト用）
    /// </summary>
    public static ConnectionConfig CreateInvalidConnectionConfig()
    {
        return new ConnectionConfig
        {
            IpAddress = "999.999.999.999", // 無効なIPアドレス
            Port = 99999, // 無効なポート
            UseTcp = true
        };
    }

    /// <summary>
    /// UDP接続設定を作成
    /// </summary>
    public static ConnectionConfig CreateUdpConnectionConfig()
    {
        return new ConnectionConfig
        {
            IpAddress = "192.168.1.100",
            Port = 5007,
            UseTcp = false
        };
    }

    /// <summary>
    /// 有効なタイムアウト設定を作成
    /// </summary>
    public static TimeoutConfig CreateValidTimeoutConfig()
    {
        return new TimeoutConfig
        {
            ConnectTimeoutMs = 5000,
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 5000,
            SendIntervalMs = 100
        };
    }

    /// <summary>
    /// 短いタイムアウト設定を作成（テスト用）
    /// </summary>
    public static TimeoutConfig CreateShortTimeoutConfig()
    {
        return new TimeoutConfig
        {
            ConnectTimeoutMs = 100,
            SendTimeoutMs = 100,
            ReceiveTimeoutMs = 100,
            SendIntervalMs = 50
        };
    }
}