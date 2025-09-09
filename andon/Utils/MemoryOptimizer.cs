using System;
using System.Buffers;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SlmpClient.Utils
{
    /// <summary>
    /// メモリ最適化実装クラス（Single Responsibility Principle適用）
    /// ArrayPoolを活用したメモリ効率化
    /// </summary>
    public class MemoryOptimizer : IMemoryOptimizer
    {
        private readonly ILogger<MemoryOptimizer> _logger;
        private readonly ArrayPool<byte> _arrayPool;
        private readonly object _lockObject = new();
        private long _currentMemoryUsage = 0;
        private long _peakMemoryUsage = 0;
        private long _memoryThreshold = 512 * 1024; // デフォルト512KB
        private volatile bool _disposed = false;

        /// <summary>
        /// メモリしきい値超過イベント
        /// </summary>
        public event Action<long>? MemoryThresholdExceeded;

        /// <summary>
        /// 現在のメモリ使用量（バイト）
        /// </summary>
        public long CurrentMemoryUsage => Interlocked.Read(ref _currentMemoryUsage);

        /// <summary>
        /// ピークメモリ使用量（バイト）
        /// </summary>
        public long PeakMemoryUsage => Interlocked.Read(ref _peakMemoryUsage);

        /// <summary>
        /// メモリしきい値（バイト）
        /// </summary>
        public long MemoryThreshold
        {
            get => Interlocked.Read(ref _memoryThreshold);
            set => Interlocked.Exchange(ref _memoryThreshold, value);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="logger">ロガー</param>
        /// <param name="arrayPool">配列プール（nullの場合は共有プールを使用）</param>
        public MemoryOptimizer(ILogger<MemoryOptimizer>? logger = null, ArrayPool<byte>? arrayPool = null)
        {
            _logger = logger ?? NullLogger<MemoryOptimizer>.Instance;
            _arrayPool = arrayPool ?? ArrayPool<byte>.Shared;
            
            _logger.LogDebug("MemoryOptimizer initialized with threshold: {Threshold} bytes", _memoryThreshold);
        }

        /// <summary>
        /// メモリプールからバッファを借用
        /// </summary>
        /// <param name="minimumLength">最小必要長</param>
        /// <returns>借用したバッファ</returns>
        public IMemoryOwner<byte> RentBuffer(int minimumLength)
        {
            ThrowIfDisposed();

            if (minimumLength <= 0)
                throw new ArgumentException("Minimum length must be positive", nameof(minimumLength));

            try
            {
                var rentedArray = _arrayPool.Rent(minimumLength);
                var memoryOwner = new PooledMemoryOwner(_arrayPool, rentedArray, minimumLength, this);

                // メモリ使用量を追跡
                TrackMemoryAllocation(rentedArray.Length);

                _logger.LogTrace("Rented buffer: requested={RequestedSize}, actual={ActualSize}", 
                    minimumLength, rentedArray.Length);

                return memoryOwner;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rent buffer of size {Size}", minimumLength);
                throw;
            }
        }

        /// <summary>
        /// メモリ使用量をリセット
        /// </summary>
        public void ResetMemoryTracking()
        {
            lock (_lockObject)
            {
                Interlocked.Exchange(ref _currentMemoryUsage, 0);
                Interlocked.Exchange(ref _peakMemoryUsage, 0);
                _logger.LogDebug("Memory tracking reset");
            }
        }

        /// <summary>
        /// メモリ割り当てを追跡
        /// </summary>
        /// <param name="size">割り当てサイズ</param>
        internal void TrackMemoryAllocation(int size)
        {
            var newUsage = Interlocked.Add(ref _currentMemoryUsage, size);
            
            // ピーク使用量を更新
            var currentPeak = Interlocked.Read(ref _peakMemoryUsage);
            if (newUsage > currentPeak)
            {
                Interlocked.CompareExchange(ref _peakMemoryUsage, newUsage, currentPeak);
            }

            // しきい値チェック
            var threshold = Interlocked.Read(ref _memoryThreshold);
            if (newUsage > threshold)
            {
                _logger.LogWarning("Memory usage exceeded threshold: {Usage} > {Threshold}", newUsage, threshold);
                MemoryThresholdExceeded?.Invoke(newUsage);
            }
        }

        /// <summary>
        /// メモリ解放を追跡
        /// </summary>
        /// <param name="size">解放サイズ</param>
        internal void TrackMemoryDeallocation(int size)
        {
            var newUsage = Interlocked.Add(ref _currentMemoryUsage, -size);
            _logger.LogTrace("Memory deallocated: {Size}, current usage: {Usage}", size, newUsage);
        }

        /// <summary>
        /// オブジェクトが破棄済みかチェック
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MemoryOptimizer));
        }

        /// <summary>
        /// リソースを同期的に解放
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// リソース解放の実装
        /// </summary>
        /// <param name="disposing">Disposeメソッドから呼ばれた場合はtrue</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _logger.LogDebug("MemoryOptimizer disposing - Peak usage: {PeakUsage} bytes", PeakMemoryUsage);
                _disposed = true;
            }
        }

        /// <summary>
        /// ファイナライザー
        /// </summary>
        ~MemoryOptimizer()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// プールされたメモリオーナー実装
    /// </summary>
    internal class PooledMemoryOwner : IMemoryOwner<byte>
    {
        private readonly ArrayPool<byte> _pool;
        private readonly byte[] _array;
        private readonly MemoryOptimizer _memoryOptimizer;
        private volatile bool _disposed = false;

        public Memory<byte> Memory { get; }

        public PooledMemoryOwner(ArrayPool<byte> pool, byte[] array, int length, MemoryOptimizer memoryOptimizer)
        {
            _pool = pool;
            _array = array;
            _memoryOptimizer = memoryOptimizer;
            Memory = new Memory<byte>(array, 0, Math.Min(length, array.Length));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _pool.Return(_array, clearArray: true);
                _memoryOptimizer.TrackMemoryDeallocation(_array.Length);
                _disposed = true;
            }
        }
    }
}