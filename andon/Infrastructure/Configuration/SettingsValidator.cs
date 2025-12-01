using System;
using System.Linq;
using System.Net;

namespace Andon.Infrastructure.Configuration;

/// <summary>
/// 設定値検証クラス
/// PLC接続設定の妥当性を検証します
/// </summary>
public class SettingsValidator
{
    #region 定数

    private static readonly string[] ValidConnectionMethods = { "TCP", "UDP" };
    private static readonly string[] ValidFrameVersions = { "3E", "4E" };

    private const int MinPort = 1;
    private const int MaxPort = 65535;
    private const int MinTimeout = 100;
    private const int MaxTimeout = 30000;
    private const int MinMonitoringInterval = 100;
    private const int MaxMonitoringInterval = 60000;
    private const int RequiredIpv4OctetCount = 4;

    #endregion

    #region IPAddress検証

    /// <summary>
    /// IPアドレスの妥当性を検証します
    /// </summary>
    /// <param name="ipAddress">検証対象のIPアドレス</param>
    /// <exception cref="ArgumentException">IPアドレスが不正な場合</exception>
    public void ValidateIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("必須項目 'IPAddress' が設定ファイルに存在しません。");

        if (!IPAddress.TryParse(ipAddress, out var parsedIp))
            throw new ArgumentException($"IPAddressの形式が不正です: '{ipAddress}'");

        // IPv4形式の厳密な検証（オクテット4つ必須）
        var parts = ipAddress.Split('.');
        if (parts.Length != RequiredIpv4OctetCount)
            throw new ArgumentException($"IPAddressの形式が不正です: '{ipAddress}'");

        if (parsedIp.ToString() == "0.0.0.0")
            throw new ArgumentException("IPAddress '0.0.0.0' は使用できません。");
    }

    #endregion

    #region Port検証

    /// <summary>
    /// ポート番号の妥当性を検証します
    /// </summary>
    /// <param name="port">検証対象のポート番号</param>
    /// <exception cref="ArgumentException">ポート番号が範囲外の場合</exception>
    public void ValidatePort(int port)
    {
        if (port < MinPort || port > MaxPort)
            throw new ArgumentException($"Portの値が範囲外です: {port} (許可範囲: {MinPort}～{MaxPort})");
    }

    #endregion

    #region ConnectionMethod検証

    /// <summary>
    /// 接続方式の妥当性を検証します
    /// </summary>
    /// <param name="connectionMethod">検証対象の接続方式</param>
    /// <exception cref="ArgumentException">接続方式が不正な場合</exception>
    public void ValidateConnectionMethod(string connectionMethod)
    {
        if (!ValidConnectionMethods.Contains(connectionMethod.ToUpper()))
            throw new ArgumentException($"ConnectionMethodの値が不正です: '{connectionMethod}' (許可値: {string.Join(", ", ValidConnectionMethods)})");
    }

    #endregion

    #region FrameVersion検証

    /// <summary>
    /// SLMPフレームバージョンの妥当性を検証します
    /// </summary>
    /// <param name="frameVersion">検証対象のフレームバージョン</param>
    /// <exception cref="ArgumentException">フレームバージョンが不正な場合</exception>
    public void ValidateFrameVersion(string frameVersion)
    {
        if (!ValidFrameVersions.Contains(frameVersion.ToUpper()))
            throw new ArgumentException($"FrameVersionの値が不正です: '{frameVersion}' (許可値: {string.Join(", ", ValidFrameVersions)})");
    }

    #endregion

    #region Timeout検証

    /// <summary>
    /// タイムアウト値の妥当性を検証します
    /// </summary>
    /// <param name="timeoutMs">検証対象のタイムアウト値（ミリ秒）</param>
    /// <exception cref="ArgumentException">タイムアウト値が範囲外の場合</exception>
    public void ValidateTimeout(int timeoutMs)
    {
        if (timeoutMs < MinTimeout || timeoutMs > MaxTimeout)
            throw new ArgumentException($"Timeoutの値が範囲外です: {timeoutMs} (推奨範囲: {MinTimeout}～{MaxTimeout})");
    }

    #endregion

    #region MonitoringIntervalMs検証

    /// <summary>
    /// 監視間隔の妥当性を検証します
    /// </summary>
    /// <param name="intervalMs">検証対象の監視間隔（ミリ秒）</param>
    /// <exception cref="ArgumentException">監視間隔が範囲外の場合</exception>
    public void ValidateMonitoringIntervalMs(int intervalMs)
    {
        if (intervalMs < MinMonitoringInterval || intervalMs > MaxMonitoringInterval)
            throw new ArgumentException($"MonitoringIntervalMsの値が範囲外です: {intervalMs} (推奨範囲: {MinMonitoringInterval}～{MaxMonitoringInterval})");
    }

    #endregion
}
