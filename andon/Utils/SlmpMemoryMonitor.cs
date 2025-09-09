using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SlmpClient.Core;

namespace SlmpClient.Utils
{
    /// <summary>
    /// SLMPメモリ監視クラス
    /// リアルタイムメモリ監視とアラート機能を提供
    /// </summary>
    public class SlmpMemoryMonitor : IDisposable
    {
        private readonly ILogger<SlmpMemoryMonitor> _logger;
        private readonly MemoryOptimizedSlmpSettings _settings;
        private readonly MemoryOptimizationStatistics _statistics;
        private readonly Timer _monitoringTimer;
        private readonly object _lockObject = new();
        private volatile bool _disposed = false;
        private volatile bool _isMonitoring = false;

        /// <summary>
        /// メモリしきい値超過イベント
        /// </summary>
        public event Action<long>? MemoryThresholdExceeded;

        /// <summary>
        /// メモリ統計更新イベント
        /// </summary>
        public event Action<MemoryOptimizationStatistics>? StatisticsUpdated;

        /// <summary>
        /// 自動クリーンアップ実行イベント
        /// </summary>
        public event Action<long>? AutoCleanupExecuted;

        /// <summary>
        /// メモリ危険レベル到達イベント
        /// </summary>
        public event Action<long, double>? CriticalMemoryLevel;

        /// <summary>
        /// 現在のメモリ使用量（バイト）
        /// </summary>
        public long CurrentMemoryUsage => _statistics.CurrentMemoryUsage;

        /// <summary>
        /// ピークメモリ使用量（バイト）
        /// </summary>
        public long PeakMemoryUsage => _statistics.PeakMemoryUsage;

        /// <summary>
        /// メモリ監視が有効かどうか
        /// </summary>
        public bool IsMonitoring => _isMonitoring;

        /// <summary>
        /// メモリ統計情報
        /// </summary>
        public MemoryOptimizationStatistics Statistics => _statistics;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="settings">メモリ最適化設定</param>
        /// <param name="logger">ロガー</param>
        public SlmpMemoryMonitor(MemoryOptimizedSlmpSettings settings, ILogger<SlmpMemoryMonitor>? logger = null)
        {
            _settings = settings?.Clone() ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? NullLogger<SlmpMemoryMonitor>.Instance;
            _statistics = new MemoryOptimizationStatistics();

            // メモリ監視タイマーを初期化
            _monitoringTimer = new Timer(
                MonitoringCallback,
                null,
                Timeout.Infinite,
                Timeout.Infinite);

            _logger.LogDebug("SlmpMemoryMonitor initialized with threshold: {Threshold} bytes", 
                _settings.MemoryThreshold);
        }

        /// <summary>
        /// メモリ監視を開始
        /// </summary>
        public void StartMonitoring()
        {
            lock (_lockObject)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(SlmpMemoryMonitor));

                if (_isMonitoring)
                    return;

                _monitoringTimer.Change(
                    TimeSpan.FromMilliseconds(_settings.MemoryMonitoringInterval),
                    TimeSpan.FromMilliseconds(_settings.MemoryMonitoringInterval));

                _isMonitoring = true;
                _logger.LogInformation("Memory monitoring started with interval: {Interval}ms", 
                    _settings.MemoryMonitoringInterval);
            }
        }

        /// <summary>
        /// メモリ監視を停止
        /// </summary>
        public void StopMonitoring()
        {
            lock (_lockObject)
            {
                if (!_isMonitoring)
                    return;

                _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _isMonitoring = false;
                _logger.LogInformation("Memory monitoring stopped");
            }
        }

        /// <summary>
        /// メモリ使用量を記録
        /// </summary>
        /// <param name="size">使用量（バイト、正数は増加、負数は減少）</param>
        public void RecordMemoryUsage(long size)
        {
            if (_disposed)
                return;

            lock (_lockObject)
            {
                if (size > 0)
                {
                    _statistics.TotalMemoryAllocated += size;
                    _statistics.CurrentMemoryUsage += size;

                    // ピーク使用量を更新
                    if (_statistics.CurrentMemoryUsage > _statistics.PeakMemoryUsage)
                    {
                        _statistics.PeakMemoryUsage = _statistics.CurrentMemoryUsage;
                    }
                }
                else if (size < 0)
                {
                    _statistics.TotalMemoryDeallocated += Math.Abs(size);
                    _statistics.CurrentMemoryUsage = Math.Max(0, _statistics.CurrentMemoryUsage + size);
                }

                _statistics.LastUpdated = DateTime.UtcNow;

                // しきい値チェック
                CheckMemoryThreshold();
            }
        }

        /// <summary>
        /// バッファ借用を記録
        /// </summary>
        /// <param name="size">借用サイズ</param>
        public void RecordBufferRent(int size)
        {
            if (_disposed)
                return;

            lock (_lockObject)
            {
                _statistics.BufferRentCount++;
                RecordMemoryUsage(size);
            }
        }

        /// <summary>
        /// バッファ返却を記録
        /// </summary>
        /// <param name="size">返却サイズ</param>
        public void RecordBufferReturn(int size)
        {
            if (_disposed)
                return;

            lock (_lockObject)
            {
                _statistics.BufferReturnCount++;
                RecordMemoryUsage(-size);
            }
        }

        /// <summary>
        /// 圧縮実行を記録
        /// </summary>
        /// <param name="originalSize">元サイズ</param>
        /// <param name="compressedSize">圧縮後サイズ</param>
        public void RecordCompression(int originalSize, int compressedSize)
        {
            if (_disposed)
                return;

            lock (_lockObject)
            {
                _statistics.CompressionCount++;
                var compressionRatio = originalSize > 0 ? 1.0 - ((double)compressedSize / originalSize) : 0.0;
                _statistics.TotalCompressionRatio += compressionRatio;
                _statistics.LastUpdated = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// 展開実行を記録
        /// </summary>
        public void RecordDecompression()
        {
            if (_disposed)
                return;

            lock (_lockObject)
            {
                _statistics.DecompressionCount++;
                _statistics.LastUpdated = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// 差分更新実行を記録
        /// </summary>
        public void RecordDifferentialUpdate()
        {
            if (_disposed)
                return;

            lock (_lockObject)
            {
                _statistics.DifferentialUpdateCount++;
                _statistics.LastUpdated = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// キャッシュヒットを記録
        /// </summary>
        public void RecordCacheHit()
        {
            if (_disposed)
                return;

            lock (_lockObject)
            {
                _statistics.CacheHitCount++;
                _statistics.LastUpdated = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// キャッシュミスを記録
        /// </summary>
        public void RecordCacheMiss()
        {
            if (_disposed)
                return;

            lock (_lockObject)
            {
                _statistics.CacheMissCount++;
                _statistics.LastUpdated = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// チャンク処理実行を記録
        /// </summary>
        public void RecordChunkProcessing()
        {
            if (_disposed)
                return;

            lock (_lockObject)
            {
                _statistics.ChunkProcessingCount++;
                _statistics.LastUpdated = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// 統計情報をリセット
        /// </summary>
        public void ResetStatistics()
        {
            lock (_lockObject)
            {
                _statistics.Reset();
                _logger.LogInformation("Memory statistics reset");
            }
        }

        /// <summary>
        /// 現在のメモリ効率を取得
        /// </summary>
        /// <returns>メモリ効率（0.0-1.0）</returns>
        public double GetMemoryEfficiency()
        {
            lock (_lockObject)
            {
                return _statistics.GetMemoryEfficiency();
            }
        }

        /// <summary>
        /// 自動クリーンアップを実行
        /// </summary>
        /// <returns>クリーンアップしたメモリ量</returns>
        public long ExecuteAutoCleanup()
        {
            if (_disposed || !_settings.EnableAutoCleanup)
                return 0;

            lock (_lockObject)
            {
                try
                {
                    // GCを強制実行
                    var beforeMemory = GC.GetTotalMemory(false);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    var afterMemory = GC.GetTotalMemory(false);

                    var cleaned = beforeMemory - afterMemory;
                    
                    _statistics.AutoCleanupCount++;
                    _statistics.LastUpdated = DateTime.UtcNow;

                    _logger.LogInformation("Auto cleanup executed: freed {Freed} bytes", cleaned);
                    AutoCleanupExecuted?.Invoke(cleaned);

                    return cleaned;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during auto cleanup");
                    return 0;
                }
            }
        }

        /// <summary>
        /// メモリしきい値をチェック
        /// </summary>
        private void CheckMemoryThreshold()
        {
            if (_statistics.CurrentMemoryUsage > _settings.MemoryThreshold)
            {
                _statistics.MemoryThresholdExceededCount++;

                var usageRatio = (double)_statistics.CurrentMemoryUsage / _settings.MemoryThreshold;
                
                _logger.LogWarning("Memory threshold exceeded: {Usage} > {Threshold} (ratio: {Ratio:P2})", 
                    _statistics.CurrentMemoryUsage, _settings.MemoryThreshold, usageRatio);

                MemoryThresholdExceeded?.Invoke(_statistics.CurrentMemoryUsage);

                // 危険レベル（しきい値の200%以上）の場合
                if (usageRatio >= 2.0)
                {
                    _logger.LogError("Critical memory level reached: {Usage} bytes (ratio: {Ratio:P2})", 
                        _statistics.CurrentMemoryUsage, usageRatio);
                    CriticalMemoryLevel?.Invoke(_statistics.CurrentMemoryUsage, usageRatio);
                }

                // 自動クリーンアップ実行
                if (_settings.EnableAutoCleanup)
                {
                    Task.Run(() => ExecuteAutoCleanup());
                }
            }
        }

        /// <summary>
        /// 監視コールバック
        /// </summary>
        /// <param name="state">状態オブジェクト</param>
        private void MonitoringCallback(object? state)
        {
            if (_disposed)
                return;

            try
            {
                lock (_lockObject)
                {
                    _statistics.LastUpdated = DateTime.UtcNow;
                    
                    if (_settings.EnableMemoryStatistics)
                    {
                        StatisticsUpdated?.Invoke(_statistics);
                    }

                    // メモリ効率ログ出力（デバッグレベル）
                    var efficiency = _statistics.GetMemoryEfficiency();
                    var cacheHitRatio = _statistics.GetCacheHitRatio();
                    var avgCompressionRatio = _statistics.GetAverageCompressionRatio();

                    _logger.LogTrace("Memory monitoring: Usage={Usage} bytes, Efficiency={Efficiency:P2}, " +
                                   "CacheHit={CacheHit:P2}, AvgCompression={AvgCompression:P2}",
                        _statistics.CurrentMemoryUsage, efficiency, cacheHitRatio, avgCompressionRatio);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in memory monitoring callback");
            }
        }

        /// <summary>
        /// オブジェクトが破棄済みかチェック
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SlmpMemoryMonitor));
        }

        /// <summary>
        /// リソースを解放
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                StopMonitoring();
                _monitoringTimer?.Dispose();
                
                _logger.LogDebug("SlmpMemoryMonitor disposing - Final statistics: {Statistics}", _statistics);
                _disposed = true;
            }
        }

        /// <summary>
        /// ファイナライザー
        /// </summary>
        ~SlmpMemoryMonitor()
        {
            Dispose();
        }
    }
}