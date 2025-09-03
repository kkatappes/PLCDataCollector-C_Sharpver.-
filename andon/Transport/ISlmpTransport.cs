using System;
using System.Threading;
using System.Threading.Tasks;

namespace SlmpClient.Transport
{
    /// <summary>
    /// SLMP通信トランスポート層インターフェース
    /// TCP/UDP通信を抽象化
    /// </summary>
    public interface ISlmpTransport : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// 接続状態
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 通信先アドレス
        /// </summary>
        string Address { get; }

        /// <summary>
        /// 通信先ポート
        /// </summary>
        int Port { get; }

        /// <summary>
        /// PLC接続を確立
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>接続完了時のTask</returns>
        Task ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// PLC接続を切断
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>切断完了時のTask</returns>
        Task DisconnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// SLMPフレームを送信し、応答を受信
        /// </summary>
        /// <param name="requestFrame">送信するフレーム</param>
        /// <param name="timeout">タイムアウト時間</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>受信したフレーム</returns>
        Task<byte[]> SendAndReceiveAsync(byte[] requestFrame, TimeSpan timeout, CancellationToken cancellationToken = default);

        /// <summary>
        /// SLMPフレームを送信のみ実行（応答なし）
        /// </summary>
        /// <param name="requestFrame">送信するフレーム</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>送信完了時のTask</returns>
        Task SendAsync(byte[] requestFrame, CancellationToken cancellationToken = default);

        /// <summary>
        /// 接続状態をチェック
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>接続中の場合はtrue</returns>
        Task<bool> IsAliveAsync(CancellationToken cancellationToken = default);
    }
}