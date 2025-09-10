using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SlmpClient.Exceptions;
using SlmpClient.Utils;

namespace SlmpClient.Transport
{
    /// <summary>
    /// SLMP TCP通信トランスポート（メモリ最適化版）
    /// ストリーミング処理とArrayPoolを活用した低メモリ実装
    /// </summary>
    public class SlmpTcpTransport : ISlmpTransport
    {
        #region Private Fields

        private readonly ILogger<SlmpTcpTransport> _logger;
        private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
        private readonly IMemoryOptimizer _memoryOptimizer;
        private readonly IStreamingFrameProcessor _frameProcessor;
        private TcpClient? _tcpClient;
        private NetworkStream? _networkStream;
        private volatile bool _disposed = false;
        private volatile int _isConnected = 0; // 0: disconnected, 1: connected

        #endregion

        #region Properties

        /// <summary>
        /// 接続状態
        /// </summary>
        public bool IsConnected => _isConnected == 1 && _tcpClient?.Connected == true;

        /// <summary>
        /// 通信先アドレス
        /// </summary>
        public string Address { get; }

        /// <summary>
        /// 通信先ポート
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// 接続タイムアウト
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

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
        /// TCP通信トランスポートを初期化（Dependency Injection対応）
        /// </summary>
        /// <param name="address">通信先IPアドレスまたはホスト名</param>
        /// <param name="port">通信先ポート番号</param>
        /// <param name="logger">ロガー</param>
        /// <param name="memoryOptimizer">メモリ最適化器（nullの場合は内部作成）</param>
        /// <exception cref="ArgumentException">addressが無効な場合</exception>
        /// <exception cref="ArgumentOutOfRangeException">portが範囲外の場合</exception>
        public SlmpTcpTransport(string address, int port, ILogger<SlmpTcpTransport>? logger = null, IMemoryOptimizer? memoryOptimizer = null)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address cannot be null or empty", nameof(address));
            if (port < 1 || port > 65535)
                throw new ArgumentOutOfRangeException(nameof(port), port, "Port must be between 1 and 65535");

            Address = address.Trim();
            Port = port;
            _logger = logger ?? NullLogger<SlmpTcpTransport>.Instance;
            _memoryOptimizer = memoryOptimizer ?? new MemoryOptimizer(_logger as ILogger<MemoryOptimizer>);
            _frameProcessor = new StreamingFrameProcessor(_memoryOptimizer, _logger as ILogger<StreamingFrameProcessor>);
            
            _logger.LogDebug("SlmpTcpTransport initialized with memory optimization");
        }

        #endregion

        #region Connection Management

        /// <summary>
        /// PLC接続を確立
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
                _logger.LogDebug("Already connected to {Address}:{Port}", Address, Port);
                return;
            }

            await _connectionSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (IsConnected)
                    return;

                try
                {
                    _logger.LogInformation("Connecting to {Address}:{Port} via TCP", Address, Port);

                    _tcpClient = new TcpClient();
                    
                    // タイムアウト設定
                    _tcpClient.ReceiveTimeout = (int)ReceiveTimeout.TotalMilliseconds;
                    _tcpClient.SendTimeout = (int)SendTimeout.TotalMilliseconds;

                    // 接続実行（非同期で適切に処理）
                    var connectTask = _tcpClient.ConnectAsync(Address, Port);
                    var timeoutTask = Task.Delay(ConnectTimeout, cancellationToken);
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        _tcpClient?.Close();
                        throw new SlmpConnectionException("Connection timeout", Address, Port);
                    }

                    if (connectTask.IsFaulted)
                    {
                        throw connectTask.Exception?.InnerException ?? new SlmpConnectionException("Connection failed", Address, Port);
                    }

                    _networkStream = _tcpClient.GetStream();
                    Interlocked.Exchange(ref _isConnected, 1);

                    _logger.LogInformation("Successfully connected to {Address}:{Port} via TCP", Address, Port);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Connection to {Address}:{Port} was cancelled", Address, Port);
                    CleanupConnection();
                    throw;
                }
                catch (SlmpConnectionException)
                {
                    CleanupConnection();
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to {Address}:{Port} via TCP", Address, Port);
                    CleanupConnection();
                    throw new SlmpConnectionException("Connection failed", Address, Port, ex);
                }
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        /// <summary>
        /// PLC接続を切断
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>切断完了時のTask</returns>
        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                _logger.LogDebug("Already disconnected from {Address}:{Port}", Address, Port);
                return;
            }

            await _connectionSemaphore.WaitAsync(cancellationToken);
            try
            {
                try
                {
                    _logger.LogInformation("Disconnecting from {Address}:{Port} via TCP", Address, Port);
                    CleanupConnection();
                    _logger.LogInformation("Successfully disconnected from {Address}:{Port} via TCP", Address, Port);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during TCP disconnection from {Address}:{Port}", Address, Port);
                    // 切断時のエラーは例外をスローしない（ベストエフォート）
                }
            }
            finally
            {
                _connectionSemaphore.Release();
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
                // TCPの場合は接続状態をチェックし、可能であればKeepAlive確認
                await Task.Delay(1, cancellationToken); // 非同期操作のシミュレーション
                
                // 基本的な接続状態チェック
                bool basicCheck = _tcpClient?.Connected == true && 
                                 _networkStream?.CanRead == true && 
                                 _networkStream?.CanWrite == true;
                
                if (!basicCheck)
                    return false;
                
                // 実際のアプリケーションではハートビートやKeepAliveを使用することも可能
                // 現在は基本チェックのみ
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking TCP connection to {Address}:{Port}", Address, Port);
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
                _logger.LogDebug("Sending {FrameSize} bytes via TCP to {Address}:{Port}", requestFrame.Length, Address, Port);

                // フレーム送信
                await _networkStream!.WriteAsync(requestFrame, 0, requestFrame.Length, cancellationToken);
                await _networkStream.FlushAsync(cancellationToken);

                _logger.LogDebug("Sent {FrameSize} bytes via TCP, waiting for response", requestFrame.Length);

                // 応答受信（タイムアウト付き）
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(timeout);

                var response = await ReceiveFrameAsync(timeoutCts.Token);

                _logger.LogDebug("Received {ResponseSize} bytes via TCP from {Address}:{Port}", response.Length, Address, Port);

                return response;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("TCP communication to {Address}:{Port} was cancelled", Address, Port);
                throw;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("TCP communication to {Address}:{Port} timed out after {Timeout}ms", Address, Port, timeout.TotalMilliseconds);
                throw new SlmpTimeoutException("TCP communication timeout", timeout);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "TCP I/O error communicating with {Address}:{Port}", Address, Port);
                throw new SlmpCommunicationException("TCP I/O error", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TCP communication error with {Address}:{Port}", Address, Port);
                throw new SlmpCommunicationException("TCP communication error", ex);
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
                _logger.LogDebug("Sending {FrameSize} bytes via TCP to {Address}:{Port} (no response expected)", requestFrame.Length, Address, Port);

                await _networkStream!.WriteAsync(requestFrame, 0, requestFrame.Length, cancellationToken);
                await _networkStream.FlushAsync(cancellationToken);

                _logger.LogDebug("Sent {FrameSize} bytes via TCP (no response)", requestFrame.Length);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("TCP send to {Address}:{Port} was cancelled", Address, Port);
                throw;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "TCP I/O error sending to {Address}:{Port}", Address, Port);
                throw new SlmpCommunicationException("TCP I/O error", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TCP send error to {Address}:{Port}", Address, Port);
                throw new SlmpCommunicationException("TCP send error", ex);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// フレームを受信（TCP用・メモリ最適化版）
        /// ストリーミング処理による低メモリ実装
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>受信したフレーム</returns>
        private async Task<byte[]> ReceiveFrameAsync(CancellationToken cancellationToken)
        {
            try
            {
                // ストリーミングフレーム処理を使用してメモリ効率化
                var frame = await _frameProcessor.ProcessFrameAsync(_networkStream!, cancellationToken);
                
                _logger.LogTrace("Received frame via streaming processor: {Size} bytes", frame.Length);
                return frame;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving frame via streaming processor");
                throw;
            }
        }

        /// <summary>
        /// 接続リソースをクリーンアップ
        /// </summary>
        private void CleanupConnection()
        {
            Interlocked.Exchange(ref _isConnected, 0);

            try
            {
                _networkStream?.Close();
            }
            catch { }
            finally
            {
                _networkStream = null;
            }

            try
            {
                _tcpClient?.Close();
            }
            catch { }
            finally
            {
                _tcpClient = null;
            }
        }

        /// <summary>
        /// オブジェクトが破棄済みかチェックし、破棄済みの場合は例外をスロー
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SlmpTcpTransport));
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
                
                _frameProcessor?.Dispose();
                _memoryOptimizer?.Dispose();
                _connectionSemaphore?.Dispose();
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
            _frameProcessor?.Dispose();
            _memoryOptimizer?.Dispose();
            _connectionSemaphore?.Dispose();
            _disposed = true;
        }

        /// <summary>
        /// ファイナライザー
        /// </summary>
        ~SlmpTcpTransport()
        {
            Dispose(false);
        }

        #endregion
    }
}