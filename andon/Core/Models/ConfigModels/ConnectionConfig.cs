namespace Andon.Core.Models.ConfigModels;

/// <summary>
/// PLC接続設定
/// </summary>
public class ConnectionConfig
{
    /// <summary>
    /// IPアドレス
    /// </summary>
    public required string IpAddress { get; init; }

    /// <summary>
    /// ポート番号
    /// </summary>
    public required int Port { get; init; }

    /// <summary>
    /// TCP使用フラグ（false=UDP）
    /// </summary>
    public bool UseTcp { get; init; }

    /// <summary>
    /// 接続タイプ文字列（"TCP" or "UDP"）
    /// </summary>
    public string ConnectionType => UseTcp ? "TCP" : "UDP";

    /// <summary>
    /// バイナリ形式フラグ（false=ASCII）
    /// </summary>
    public bool IsBinary { get; init; }

    /// <summary>
    /// フレームバージョン
    /// </summary>
    public FrameVersion FrameVersion { get; init; } = FrameVersion.Frame4E;
}
