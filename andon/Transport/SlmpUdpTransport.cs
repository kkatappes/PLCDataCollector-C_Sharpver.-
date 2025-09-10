using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SlmpClient.Exceptions;

namespace SlmpClient.Transport
{
    /// <summary>
    /// SLMP UDP通信トランスポート
    /// </summary>
    public class SlmpUdpTransport : ISlmpTransport
    {
        #region Private Fields

        private readonly ILogger<SlmpUdpTransport> _logger;
        private readonly object _connectionLock = new();
        private UdpClient? _udpClient;
        private IPEndPoint? _remoteEndPoint;
        private volatile bool _disposed = false;
        private volatile int _isConnected = 0; // 0: disconnected, 1: connected

        #endregion

        #region Properties

        /// <summary>
        /// 接続状態（UDPは接続指向ではないが、設定済み状態を表す）
        /// </summary>
        public bool IsConnected => _isConnected == 1 && _udpClient != null;

        /// <summary>
        /// 通信先アドレス
        /// </summary>
        public string Address { get; }

        /// <summary>
        /// 通信先ポート
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// 受信タイムアウト
        /// </summary>
        public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// 送信タイムアウト
        /// </summary>
        public TimeSpan SendTimeout { get; set; } = TimeSpan.FromSeconds(1);

        #endregion

        #region Constructor

        /// <summary>
        /// UDP通信トランスポートを初期化
        /// </summary>
        /// <param name="address">通信先IPアドレスまたはホスト名</param>
        /// <param name="port">通信先ポート番号</param>
        /// <param name="logger">ロガー</param>
        /// <exception cref="ArgumentException">addressが無効な場合</exception>
        /// <exception cref="ArgumentOutOfRangeException">portが範囲外の場合</exception>
        public SlmpUdpTransport(string address, int port, ILogger<SlmpUdpTransport>? logger = null)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address cannot be null or empty", nameof(address));
            if (port < 1 || port > 65535)
                throw new ArgumentOutOfRangeException(nameof(port), port, "Port must be between 1 and 65535");

            Address = address.Trim();
            Port = port;
            _logger = logger ?? NullLogger<SlmpUdpTransport>.Instance;
        }

        #endregion

        #region Connection Management

        /// <summary>
        /// PLC接続を確立（UDPの場合は設定を初期化）
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>接続完了時のTask</returns>
        /// <exception cref="ObjectDisposedException">オブジェクトが破棄済みの場合</exception>
        /// <exception cref="SlmpConnectionException">接続に失敗した場合</exception>
        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (IsConnected)
            {
                _logger.LogDebug("Already connected to {Address}:{Port} via UDP", Address, Port);
                return;
            }

            lock (_connectionLock)
            {
                if (IsConnected)
                    return;

                try
                {
                    _logger.LogInformation("Connecting to {Address}:{Port} via UDP", Address, Port);

                    // IPアドレスを解決
                    var ipAddresses = Dns.GetHostAddresses(Address);
                    if (ipAddresses.Length == 0)
                        throw new SlmpConnectionException($"Unable to resolve hostname: {Address}", Address, Port);

                    _remoteEndPoint = new IPEndPoint(ipAddresses[0], Port);
                    _udpClient = new UdpClient();

                    // タイムアウト設定
                    _udpClient.Client.ReceiveTimeout = (int)ReceiveTimeout.TotalMilliseconds;
                    _udpClient.Client.SendTimeout = (int)SendTimeout.TotalMilliseconds;

                    // UDPは接続指向ではないが、接続設定をして特定のエンドポイントとのみ通信する
                    _udpClient.Connect(_remoteEndPoint);

                    Interlocked.Exchange(ref _isConnected, 1);

                    _logger.LogInformation("Successfully connected to {Address}:{Port} via UDP", Address, Port);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Connection to {Address}:{Port} via UDP was cancelled", Address, Port);
                    CleanupConnection();
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to {Address}:{Port} via UDP", Address, Port);
                    CleanupConnection();
                    throw new SlmpConnectionException("UDP connection setup failed", Address, Port, ex);
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// PLC接続を切断（UDPリソースをクリーンアップ）
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>切断完了時のTask</returns>
        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                _logger.LogDebug("Already disconnected from {Address}:{Port} via UDP", Address, Port);
                return;
            }

            lock (_connectionLock)
            {
                try
                {
                    _logger.LogInformation("Disconnecting from {Address}:{Port} via UDP", Address, Port);
                    CleanupConnection();
                    _logger.LogInformation("Successfully disconnected from {Address}:{Port} via UDP", Address, Port);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during UDP disconnection from {Address}:{Port}", Address, Port);
                    // 切断時のエラーは例外をスローしない（ベストエフォート）
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 接続状態をチェック
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>接続中の場合はtrue</returns>
        public async Task<bool> IsAliveAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (!IsConnected)
                return false;

            try
            {
                // UDPの場合は実際の通信で生存確認する必要がある
                // 小さなテストパケットを送信してレスポンスを確認
                await Task.Delay(1, cancellationToken); // 非同期操作のシミュレーション
                
                // 実際の実装では小さなSLMPテストフレームを送信
                // 現在は簡易実装として設定状態をチェック
                return _udpClient != null && _remoteEndPoint != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking UDP connection to {Address}:{Port}", Address, Port);
                return false;
            }
        }

        #endregion

        #region Communication

        /// <summary>
        /// SLMPフレームを送信し、応答を受信
        /// </summary>
        /// <param name="requestFrame">送信するフレーム</param>
        /// <param name="timeout">タイムアウト時間</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>受信したフレーム</returns>
        /// <exception cref="ArgumentNullException">requestFrameがnullの場合</exception>
        /// <exception cref="ObjectDisposedException">オブジェクトが破棄済みの場合</exception>
        /// <exception cref="SlmpConnectionException">接続が確立されていない場合</exception>
        /// <exception cref="SlmpTimeoutException">通信がタイムアウトした場合</exception>
        /// <exception cref="SlmpCommunicationException">通信エラーが発生した場合</exception>
        public async Task<byte[]> SendAndReceiveAsync(byte[] requestFrame, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (requestFrame == null)
                throw new ArgumentNullException(nameof(requestFrame));

            ThrowIfDisposed();
            ThrowIfNotConnected();

            try
            {
                _logger.LogDebug("Sending {FrameSize} bytes via UDP to {Address}:{Port}", requestFrame.Length, Address, Port);

                // フレーム送信
                int bytesSent = await _udpClient!.SendAsync(requestFrame, requestFrame.Length);
                if (bytesSent != requestFrame.Length)
                {
                    throw new SlmpCommunicationException($"Failed to send complete frame: sent {bytesSent}/{requestFrame.Length} bytes");
                }

                _logger.LogDebug("Sent {FrameSize} bytes via UDP, waiting for response", requestFrame.Length);

                // 応答受信（タイムアウト付き）
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(timeout);

                var receiveTask = _udpClient.ReceiveAsync();
                var timeoutTask = Task.Delay(timeout, timeoutCts.Token);
                var completedTask = await Task.WhenAny(receiveTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    throw new SlmpTimeoutException("UDP communication timeout", timeout);
                }

                var udpResult = await receiveTask;
                
                // 応答の送信元を検証
                if (!udpResult.RemoteEndPoint.Equals(_remoteEndPoint))
                {
                    _logger.LogWarning("Received response from unexpected endpoint: {ReceivedFrom}, expected: {Expected}",
                        udpResult.RemoteEndPoint, _remoteEndPoint);
                }

                _logger.LogDebug("Received {ResponseSize} bytes via UDP from {Address}:{Port}", udpResult.Buffer.Length, Address, Port);

                return udpResult.Buffer;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("UDP communication to {Address}:{Port} was cancelled", Address, Port);
                throw;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("UDP communication to {Address}:{Port} timed out after {Timeout}ms", Address, Port, timeout.TotalMilliseconds);
                throw new SlmpTimeoutException("UDP communication timeout", timeout);
            }
            catch (SlmpTimeoutException)
            {
                throw;
            }
            catch (SlmpCommunicationException)
            {
                throw;
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "UDP socket error communicating with {Address}:{Port}", Address, Port);
                throw new SlmpCommunicationException("UDP socket error", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UDP communication error with {Address}:{Port}", Address, Port);
                throw new SlmpCommunicationException("UDP communication error", ex);
            }
        }

        /// <summary>
        /// SLMPフレームを送信のみ実行（応答なし）
        /// </summary>
        /// <param name="requestFrame">送信するフレーム</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>送信完了時のTask</returns>
        /// <exception cref="ArgumentNullException">requestFrameがnullの場合</exception>
        /// <exception cref="ObjectDisposedException">オブジェクトが破棄済みの場合</exception>
        /// <exception cref="SlmpConnectionException">接続が確立されていない場合</exception>
        /// <exception cref="SlmpCommunicationException">通信エラーが発生した場合</exception>
        public async Task SendAsync(byte[] requestFrame, CancellationToken cancellationToken = default)
        {
            if (requestFrame == null)
                throw new ArgumentNullException(nameof(requestFrame));

            ThrowIfDisposed();
            ThrowIfNotConnected();

            try
            {
                _logger.LogDebug("Sending {FrameSize} bytes via UDP to {Address}:{Port} (no response expected)", requestFrame.Length, Address, Port);

                int bytesSent = await _udpClient!.SendAsync(requestFrame, requestFrame.Length);
                if (bytesSent != requestFrame.Length)
                {
                    throw new SlmpCommunicationException($"Failed to send complete frame: sent {bytesSent}/{requestFrame.Length} bytes");
                }

                _logger.LogDebug("Sent {FrameSize} bytes via UDP (no response)", requestFrame.Length);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("UDP send to {Address}:{Port} was cancelled", Address, Port);
                throw;
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "UDP socket error sending to {Address}:{Port}", Address, Port);
                throw new SlmpCommunicationException("UDP socket error", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UDP send error to {Address}:{Port}", Address, Port);
                throw new SlmpCommunicationException("UDP send error", ex);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 接続リソースをクリーンアップ
        /// </summary>
        private void CleanupConnection()
        {
            Interlocked.Exchange(ref _isConnected, 0);

            try
            {
                _udpClient?.Close();
            }
            catch { }
            finally
            {
                _udpClient = null;
            }

            _remoteEndPoint = null;
        }

        /// <summary>
        /// オブジェクトが破棄済みかチェックし、破棄済みの場合は例外をスロー
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SlmpUdpTransport));
        }

        /// <summary>
        /// 接続が確立されているかチェックし、未接続の場合は例外をスロー
        /// </summary>
        private void ThrowIfNotConnected()
        {
            ThrowIfDisposed();
            if (!IsConnected)
                throw new SlmpConnectionException("Not connected to PLC", Address, Port);
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// リソースを解放（同期版）
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// リソースを解放（非同期版）
        /// </summary>
        /// <returns>解放完了時のValueTask</returns>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// リソース解放の実装（同期版）
        /// </summary>
        /// <param name="disposing">Disposeメソッドから呼ばれた場合はtrue</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                try
                {
                    DisconnectAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during synchronous disposal");
                }

                CleanupConnection();
            }

            _disposed = true;
        }

        /// <summary>
        /// リソース解放の実装（非同期版）
        /// </summary>
        /// <returns>解放完了時のValueTask</returns>
        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (_disposed)
                return;

            try
            {
                await DisconnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during asynchronous disposal");
            }

            CleanupConnection();
            _disposed = true;
        }

        /// <summary>
        /// ファイナライザー
        /// </summary>
        ~SlmpUdpTransport()
        {
            Dispose(false);
        }

        #endregion
    }
}