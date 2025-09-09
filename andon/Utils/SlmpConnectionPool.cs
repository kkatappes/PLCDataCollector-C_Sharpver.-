using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SlmpClient.Core;
using SlmpClient.Constants;

namespace SlmpClient.Utils
{
    /// <summary>
    /// SLMPクライアントフルインターフェース（デモ用）
    /// </summary>
    public interface ISlmpClientFull : IDisposable
    {
        bool IsConnected { get; }
        Task ConnectAsync(CancellationToken cancellationToken = default);
        Task<bool> IsAliveAsync(CancellationToken cancellationToken = default);
        Task<bool[]> ReadBitDevicesAsync(DeviceCode deviceCode, uint startAddress, ushort count, ushort timeout = 0, CancellationToken cancellationToken = default);
        Task<ushort[]> ReadWordDevicesAsync(DeviceCode deviceCode, uint startAddress, ushort count, ushort timeout = 0, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// SLMP接続プールインターフェース（Interface Segregation Principle適用）
    /// </summary>
    public interface ISlmpConnectionPool : IDisposable
    {
        /// <summary>
        /// 接続を借用
        /// </summary>
        /// <param name="address">接続先アドレス</param>
        /// <param name="port">接続先ポート</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>借用した接続</returns>
        Task<ISlmpClientFull> BorrowConnectionAsync(string address, int port, CancellationToken cancellationToken = default);

        /// <summary>
        /// 接続を返却
        /// </summary>
        /// <param name="connection">返却する接続</param>
        /// <param name="isHealthy">接続が正常かどうか</param>
        void ReturnConnection(ISlmpClientFull connection, bool isHealthy = true);

        /// <summary>
        /// アクティブ接続数
        /// </summary>
        int ActiveConnections { get; }

        /// <summary>
        /// 利用可能接続数
        /// </summary>
        int AvailableConnections { get; }

        /// <summary>
        /// 最大接続数
        /// </summary>
        int MaxConnections { get; }
    }

    /// <summary>
    /// SLMP接続プール実装クラス
    /// 複数接続の効率的な管理とメモリ共有による最適化
    /// </summary>
    public class SlmpConnectionPool : ISlmpConnectionPool
    {
        private readonly ILogger<SlmpConnectionPool> _logger;
        private readonly ConcurrentQueue<PooledConnection> _availableConnections;
        private readonly ConcurrentDictionary<string, PooledConnection> _activeConnections;
        private readonly MemoryOptimizedSlmpSettings _settings;
        private readonly IMemoryOptimizer _sharedMemoryOptimizer;
        private readonly SemaphoreSlim _connectionSemaphore;
        private readonly Timer _healthCheckTimer;
        private readonly int _maxConnections;
        private volatile bool _disposed = false;
        private volatile int _connectionIdCounter = 0;

        /// <summary>
        /// プールされた接続情報
        /// </summary>
        private class PooledConnection : IDisposable
        {
            public string Id { get; }
            public string Address { get; }
            public int Port { get; }
            public ISlmpClientFull Client { get; }
            public DateTime LastUsed { get; set; }
            public DateTime CreatedAt { get; }
            public bool IsHealthy { get; set; }
            public long UsageCount { get; set; }

            public PooledConnection(string id, string address, int port, ISlmpClientFull client)
            {
                Id = id;
                Address = address;
                Port = port;
                Client = client;
                LastUsed = DateTime.UtcNow;
                CreatedAt = DateTime.UtcNow;
                IsHealthy = true;
                UsageCount = 0;
            }

            public void UpdateUsage()
            {
                LastUsed = DateTime.UtcNow;
                UsageCount++;
            }

            public void Dispose()
            {
                try
                {
                    Client?.Dispose();
                }
                catch
                {
                    // 例外を無視（ログ出力済み）
                }
            }
        }

        /// <summary>
        /// アクティブ接続数
        /// </summary>
        public int ActiveConnections => _activeConnections.Count;

        /// <summary>
        /// 利用可能接続数
        /// </summary>
        public int AvailableConnections => _availableConnections.Count;

        /// <summary>
        /// 最大接続数
        /// </summary>
        public int MaxConnections => _maxConnections;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="settings">メモリ最適化設定</param>
        /// <param name="maxConnections">最大接続数</param>
        /// <param name="logger">ロガー</param>
        public SlmpConnectionPool(
            MemoryOptimizedSlmpSettings settings,
            int maxConnections = 5,
            ILogger<SlmpConnectionPool>? logger = null)
        {
            _settings = settings?.Clone() ?? throw new ArgumentNullException(nameof(settings));
            _maxConnections = maxConnections > 0 ? maxConnections : 5;
            _logger = logger ?? NullLogger<SlmpConnectionPool>.Instance;

            _availableConnections = new ConcurrentQueue<PooledConnection>();
            _activeConnections = new ConcurrentDictionary<string, PooledConnection>();
            _connectionSemaphore = new SemaphoreSlim(_maxConnections, _maxConnections);

            // 共有メモリ最適化器を作成
            _sharedMemoryOptimizer = new MemoryOptimizer(_logger as ILogger<MemoryOptimizer>);

            // ヘルスチェックタイマーを開始
            _healthCheckTimer = new Timer(
                HealthCheckCallback,
                null,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(1));

            _logger.LogInformation("SlmpConnectionPool initialized with max connections: {MaxConnections}", _maxConnections);
        }

        /// <summary>
        /// 接続を借用
        /// </summary>
        /// <param name="address">接続先アドレス</param>
        /// <param name="port">接続先ポート</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>借用した接続</returns>
        public async Task<ISlmpClientFull> BorrowConnectionAsync(string address, int port, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address cannot be null or empty", nameof(address));

            await _connectionSemaphore.WaitAsync(cancellationToken);

            try
            {
                // 利用可能な接続があるかチェック
                while (_availableConnections.TryDequeue(out var pooledConnection))
                {
                    if (pooledConnection.Address == address && 
                        pooledConnection.Port == port && 
                        pooledConnection.IsHealthy)
                    {
                        // 接続を再利用
                        pooledConnection.UpdateUsage();
                        _activeConnections[pooledConnection.Id] = pooledConnection;

                        _logger.LogDebug("Reused connection {ConnectionId} for {Address}:{Port}", 
                            pooledConnection.Id, address, port);

                        return pooledConnection.Client;
                    }
                    else
                    {
                        // 不適切な接続は破棄
                        pooledConnection.Dispose();
                    }
                }

                // 新しい接続を作成
                var connectionId = $"slmp-conn-{Interlocked.Increment(ref _connectionIdCounter)}";
                var connectionSettings = CreateOptimizedSettings();

                // 簡単な実装（実際のSlmpClientクラスは別途実装が必要）
                var client = CreateMockSlmpClient(address, port);
                await client.ConnectAsync(cancellationToken);

                var newPooledConnection = new PooledConnection(connectionId, address, port, client);
                _activeConnections[connectionId] = newPooledConnection;

                _logger.LogInformation("Created new connection {ConnectionId} for {Address}:{Port}", 
                    connectionId, address, port);

                return client;
            }
            catch (Exception ex)
            {
                _connectionSemaphore.Release();
                _logger.LogError(ex, "Error borrowing connection for {Address}:{Port}", address, port);
                throw;
            }
        }

        /// <summary>
        /// 接続を返却
        /// </summary>
        /// <param name="connection">返却する接続</param>
        /// <param name="isHealthy">接続が正常かどうか</param>
        public void ReturnConnection(ISlmpClientFull connection, bool isHealthy = true)
        {
            if (_disposed || connection == null)
                return;

            try
            {
                // アクティブ接続から該当接続を検索
                PooledConnection? pooledConnection = null;
                foreach (var kvp in _activeConnections)
                {
                    if (ReferenceEquals(kvp.Value.Client, connection))
                    {
                        pooledConnection = kvp.Value;
                        _activeConnections.TryRemove(kvp.Key, out _);
                        break;
                    }
                }

                if (pooledConnection != null)
                {
                    pooledConnection.IsHealthy = isHealthy;

                    if (isHealthy && !_disposed && connection.IsConnected)
                    {
                        // 正常な接続は再利用のためプールに戻す
                        _availableConnections.Enqueue(pooledConnection);
                        _logger.LogDebug("Returned connection {ConnectionId} to pool", pooledConnection.Id);
                    }
                    else
                    {
                        // 異常な接続は破棄
                        pooledConnection.Dispose();
                        _logger.LogDebug("Disposed unhealthy connection {ConnectionId}", pooledConnection.Id);
                    }
                }

                _connectionSemaphore.Release();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error returning connection to pool");
            }
        }

        /// <summary>
        /// 最適化された接続設定を作成
        /// </summary>
        /// <returns>最適化設定</returns>
        private object CreateOptimizedSettings()
        {
            // 簡易実装（実際の設定は別途実装が必要）
            return new { Port = 5000, UseTcp = true, IsBinary = true };
        }

        /// <summary>
        /// モックSLMPクライアントを作成
        /// </summary>
        /// <param name="address">アドレス</param>
        /// <param name="port">ポート</param>
        /// <returns>モッククライアント</returns>
        private ISlmpClientFull CreateMockSlmpClient(string address, int port)
        {
            return new MockSlmpClient(address, port, _logger);
        }

        /// <summary>
        /// ヘルスチェックコールバック
        /// </summary>
        /// <param name="state">状態オブジェクト</param>
        private void HealthCheckCallback(object? state)
        {
            if (_disposed)
                return;

            try
            {
                var now = DateTime.UtcNow;
                var expiredConnections = new List<PooledConnection>();

                // 期限切れ接続をチェック
                var tempConnections = new List<PooledConnection>();
                while (_availableConnections.TryDequeue(out var connection))
                {
                    if (now - connection.LastUsed > TimeSpan.FromMinutes(5) || !connection.IsHealthy)
                    {
                        expiredConnections.Add(connection);
                    }
                    else
                    {
                        tempConnections.Add(connection);
                    }
                }

                // 有効な接続を戻す
                foreach (var connection in tempConnections)
                {
                    _availableConnections.Enqueue(connection);
                }

                // 期限切れ接続を破棄
                foreach (var connection in expiredConnections)
                {
                    connection.Dispose();
                    _logger.LogDebug("Disposed expired connection {ConnectionId}", connection.Id);
                }

                if (expiredConnections.Count > 0)
                {
                    _logger.LogInformation("Health check completed: disposed {Count} expired connections", 
                        expiredConnections.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during connection health check");
            }
        }

        /// <summary>
        /// すべての接続のヘルスチェックを実行
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>チェック完了のTask</returns>
        public async Task PerformHealthCheckAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            try
            {
                var healthCheckTasks = new List<Task>();

                // アクティブ接続のヘルスチェック
                foreach (var kvp in _activeConnections)
                {
                    var connection = kvp.Value;
                    healthCheckTasks.Add(CheckConnectionHealthAsync(connection, cancellationToken));
                }

                // 利用可能接続のヘルスチェック
                var tempConnections = new List<PooledConnection>();
                while (_availableConnections.TryDequeue(out var connection))
                {
                    tempConnections.Add(connection);
                    healthCheckTasks.Add(CheckConnectionHealthAsync(connection, cancellationToken));
                }

                // すべてのヘルスチェックを並行実行
                await Task.WhenAll(healthCheckTasks);

                // 利用可能接続を戻す（正常な接続のみ）
                foreach (var connection in tempConnections)
                {
                    if (connection.IsHealthy)
                    {
                        _availableConnections.Enqueue(connection);
                    }
                    else
                    {
                        connection.Dispose();
                    }
                }

                _logger.LogDebug("Health check completed for all connections");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during comprehensive health check");
            }
        }

        /// <summary>
        /// 個別接続のヘルスチェック
        /// </summary>
        /// <param name="connection">チェック対象接続</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>チェック完了のTask</returns>
        private async Task CheckConnectionHealthAsync(PooledConnection connection, CancellationToken cancellationToken)
        {
            try
            {
                var isAlive = await connection.Client.IsAliveAsync(cancellationToken);
                connection.IsHealthy = isAlive;

                if (!isAlive)
                {
                    _logger.LogWarning("Connection {ConnectionId} failed health check", connection.Id);
                }
            }
            catch (Exception ex)
            {
                connection.IsHealthy = false;
                _logger.LogWarning(ex, "Health check failed for connection {ConnectionId}", connection.Id);
            }
        }

        /// <summary>
        /// プール統計情報を取得
        /// </summary>
        /// <returns>統計情報</returns>
        public ConnectionPoolStatistics GetStatistics()
        {
            ThrowIfDisposed();

            long totalUsage = 0;
            var oldestConnection = DateTime.UtcNow;
            var newestConnection = DateTime.MinValue;

            foreach (var kvp in _activeConnections)
            {
                var connection = kvp.Value;
                totalUsage += connection.UsageCount;
                
                if (connection.CreatedAt < oldestConnection)
                    oldestConnection = connection.CreatedAt;
                if (connection.CreatedAt > newestConnection)
                    newestConnection = connection.CreatedAt;
            }

            return new ConnectionPoolStatistics
            {
                MaxConnections = _maxConnections,
                ActiveConnections = ActiveConnections,
                AvailableConnections = AvailableConnections,
                TotalUsageCount = totalUsage,
                OldestConnectionAge = oldestConnection != DateTime.UtcNow ? DateTime.UtcNow - oldestConnection : TimeSpan.Zero,
                NewestConnectionAge = newestConnection != DateTime.MinValue ? DateTime.UtcNow - newestConnection : TimeSpan.Zero
            };
        }

        /// <summary>
        /// オブジェクトが破棄済みかチェック
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SlmpConnectionPool));
        }

        /// <summary>
        /// リソースを解放
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                _healthCheckTimer?.Dispose();

                // すべての接続を破棄
                while (_availableConnections.TryDequeue(out var connection))
                {
                    connection.Dispose();
                }

                foreach (var kvp in _activeConnections)
                {
                    kvp.Value.Dispose();
                }
                _activeConnections.Clear();

                _sharedMemoryOptimizer?.Dispose();
                _connectionSemaphore?.Dispose();

                _logger.LogInformation("SlmpConnectionPool disposed");
            }
        }

        /// <summary>
        /// ファイナライザー
        /// </summary>
        ~SlmpConnectionPool()
        {
            Dispose();
        }
    }

    /// <summary>
    /// 接続プール統計情報
    /// </summary>
    public class ConnectionPoolStatistics
    {
        /// <summary>最大接続数</summary>
        public int MaxConnections { get; set; }

        /// <summary>アクティブ接続数</summary>
        public int ActiveConnections { get; set; }

        /// <summary>利用可能接続数</summary>
        public int AvailableConnections { get; set; }

        /// <summary>総使用回数</summary>
        public long TotalUsageCount { get; set; }

        /// <summary>最古接続の経過時間</summary>
        public TimeSpan OldestConnectionAge { get; set; }

        /// <summary>最新接続の経過時間</summary>
        public TimeSpan NewestConnectionAge { get; set; }

        /// <summary>接続使用率</summary>
        public double UsageRatio => MaxConnections > 0 ? (double)ActiveConnections / MaxConnections : 0.0;

        /// <summary>
        /// 統計情報の文字列表現を取得
        /// </summary>
        /// <returns>統計情報文字列</returns>
        public override string ToString()
        {
            return $"ConnectionPoolStatistics(" +
                   $"Max: {MaxConnections}, " +
                   $"Active: {ActiveConnections}, " +
                   $"Available: {AvailableConnections}, " +
                   $"Usage: {TotalUsageCount:N0}, " +
                   $"UsageRatio: {UsageRatio:P2}, " +
                   $"OldestAge: {OldestConnectionAge:hh\\:mm\\:ss}, " +
                   $"NewestAge: {NewestConnectionAge:hh\\:mm\\:ss})";
        }
    }

    /// <summary>
    /// モックSLMPクライアント（デモ用）
    /// </summary>
    internal class MockSlmpClient : ISlmpClientFull
    {
        private readonly string _address;
        private readonly int _port;
        private readonly ILogger _logger;
        private bool _isConnected = false;

        public MockSlmpClient(string address, int port, ILogger logger)
        {
            _address = address;
            _port = port;
            _logger = logger;
        }

        public bool IsConnected => _isConnected;

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken); // 接続シミュレーション
            _isConnected = true;
            _logger.LogDebug("Mock SLMP client connected to {Address}:{Port}", _address, _port);
        }

        public async Task<bool> IsAliveAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken);
            return _isConnected;
        }

        public async Task<bool[]> ReadBitDevicesAsync(DeviceCode deviceCode, uint startAddress, ushort count, ushort timeout = 0, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken);
            return new bool[count]; // ダミーデータ
        }

        public async Task<ushort[]> ReadWordDevicesAsync(DeviceCode deviceCode, uint startAddress, ushort count, ushort timeout = 0, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken);
            return new ushort[count]; // ダミーデータ
        }

        public void Dispose()
        {
            _isConnected = false;
            _logger.LogDebug("Mock SLMP client disposed");
        }
    }
}