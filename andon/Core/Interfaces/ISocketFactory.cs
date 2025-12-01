using System.Net.Sockets;

namespace Andon.Core.Interfaces;

/// <summary>
/// Socketファクトリーインターフェース
/// テスト時にモック化可能にするためのFactory Pattern
/// </summary>
public interface ISocketFactory
{
    /// <summary>
    /// Socket作成
    /// </summary>
    /// <param name="useTcp">TCP使用フラグ（false=UDP）</param>
    /// <returns>作成されたSocketインスタンス</returns>
    Socket CreateSocket(bool useTcp);

    /// <summary>
    /// Socket接続（非同期）
    /// </summary>
    /// <param name="socket">接続するSocket</param>
    /// <param name="ipAddress">接続先IPアドレス</param>
    /// <param name="port">接続先ポート番号</param>
    /// <param name="timeoutMs">接続タイムアウト（ミリ秒）</param>
    /// <returns>接続成功時true、タイムアウト時false</returns>
    Task<bool> ConnectAsync(Socket socket, string ipAddress, int port, int timeoutMs);
}
