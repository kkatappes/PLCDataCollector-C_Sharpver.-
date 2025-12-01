using System.Net.Sockets;

namespace Andon.Core.Models;

/// <summary>
/// ConnectAsync戻り値
/// </summary>
public class ConnectionResponse
{
    /// <summary>
    /// 接続状態
    /// </summary>
    public required ConnectionStatus Status { get; init; }

    /// <summary>
    /// ソケットインスタンス（接続成功時のみ）
    /// </summary>
    public Socket? Socket { get; init; }

    /// <summary>
    /// 接続完了時刻
    /// </summary>
    public DateTime? ConnectedAt { get; init; }

    /// <summary>
    /// 接続所要時間（ミリ秒）
    /// </summary>
    public double? ConnectionTime { get; init; }

    /// <summary>
    /// エラーメッセージ（失敗時のみ）
    /// </summary>
    public string? ErrorMessage { get; init; }
}
