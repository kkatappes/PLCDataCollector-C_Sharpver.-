using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SlmpClient.Utils
{
    /// <summary>
    /// 圧縮エンジンインターフェース（Interface Segregation Principle適用）
    /// </summary>
    public interface ICompressionEngine : IDisposable
    {
        /// <summary>
        /// データを圧縮
        /// </summary>
        /// <param name="data">圧縮対象データ</param>
        /// <returns>圧縮済みデータ</returns>
        ReadOnlyMemory<byte> Compress(ReadOnlySpan<byte> data);

        /// <summary>
        /// データを展開
        /// </summary>
        /// <param name="compressedData">圧縮済みデータ</param>
        /// <returns>展開済みデータ</returns>
        ReadOnlyMemory<byte> Decompress(ReadOnlySpan<byte> compressedData);

        /// <summary>
        /// 圧縮率を取得
        /// </summary>
        /// <param name="originalSize">元サイズ</param>
        /// <param name="compressedSize">圧縮後サイズ</param>
        /// <returns>圧縮率（0.0-1.0）</returns>
        double GetCompressionRatio(int originalSize, int compressedSize);
    }

    /// <summary>
    /// 差分更新エンジンインターフェース
    /// </summary>
    public interface IDifferentialUpdateEngine : IDisposable
    {
        /// <summary>
        /// 差分データを計算
        /// </summary>
        /// <param name="previous">前回データ</param>
        /// <param name="current">現在データ</param>
        /// <returns>差分データ</returns>
        ReadOnlyMemory<byte> CalculateDifference(ReadOnlySpan<byte> previous, ReadOnlySpan<byte> current);

        /// <summary>
        /// 差分を適用してデータを復元
        /// </summary>
        /// <param name="previous">前回データ</param>
        /// <param name="difference">差分データ</param>
        /// <returns>復元データ</returns>
        ReadOnlyMemory<byte> ApplyDifference(ReadOnlySpan<byte> previous, ReadOnlySpan<byte> difference);

        /// <summary>
        /// キャッシュから前回データを取得
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns>前回データ（存在しない場合はnull）</returns>
        ReadOnlyMemory<byte>? GetCachedData(string key);

        /// <summary>
        /// データをキャッシュに保存
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="data">データ</param>
        void CacheData(string key, ReadOnlyMemory<byte> data);
    }

    /// <summary>
    /// 圧縮エンジン実装クラス（GZip圧縮を使用）
    /// </summary>
    public class GZipCompressionEngine : ICompressionEngine
    {
        private readonly ILogger<GZipCompressionEngine> _logger;
        private readonly IMemoryOptimizer _memoryOptimizer;
        private volatile bool _disposed = false;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="memoryOptimizer">メモリ最適化器</param>
        /// <param name="logger">ロガー</param>
        public GZipCompressionEngine(IMemoryOptimizer memoryOptimizer, ILogger<GZipCompressionEngine>? logger = null)
        {
            _memoryOptimizer = memoryOptimizer ?? throw new ArgumentNullException(nameof(memoryOptimizer));
            _logger = logger ?? NullLogger<GZipCompressionEngine>.Instance;
        }

        /// <summary>
        /// データを圧縮
        /// </summary>
        /// <param name="data">圧縮対象データ</param>
        /// <returns>圧縮済みデータ</returns>
        public ReadOnlyMemory<byte> Compress(ReadOnlySpan<byte> data)
        {
            ThrowIfDisposed();

            if (data.IsEmpty)
                return ReadOnlyMemory<byte>.Empty;

            try
            {
                using var inputStream = new MemoryStream(data.ToArray());
                using var outputStream = new MemoryStream();
                using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal))
                {
                    inputStream.CopyTo(gzipStream);
                }

                var compressedData = outputStream.ToArray();
                var compressionRatio = GetCompressionRatio(data.Length, compressedData.Length);

                _logger.LogTrace("Data compressed: {OriginalSize} -> {CompressedSize} bytes (ratio: {Ratio:P2})", 
                    data.Length, compressedData.Length, compressionRatio);

                return new ReadOnlyMemory<byte>(compressedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compressing data of size {Size}", data.Length);
                throw;
            }
        }

        /// <summary>
        /// データを展開
        /// </summary>
        /// <param name="compressedData">圧縮済みデータ</param>
        /// <returns>展開済みデータ</returns>
        public ReadOnlyMemory<byte> Decompress(ReadOnlySpan<byte> compressedData)
        {
            ThrowIfDisposed();

            if (compressedData.IsEmpty)
                return ReadOnlyMemory<byte>.Empty;

            try
            {
                using var inputStream = new MemoryStream(compressedData.ToArray());
                using var outputStream = new MemoryStream();
                using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
                {
                    gzipStream.CopyTo(outputStream);
                }

                var decompressedData = outputStream.ToArray();

                _logger.LogTrace("Data decompressed: {CompressedSize} -> {DecompressedSize} bytes", 
                    compressedData.Length, decompressedData.Length);

                return new ReadOnlyMemory<byte>(decompressedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decompressing data of size {Size}", compressedData.Length);
                throw;
            }
        }

        /// <summary>
        /// 圧縮率を取得
        /// </summary>
        /// <param name="originalSize">元サイズ</param>
        /// <param name="compressedSize">圧縮後サイズ</param>
        /// <returns>圧縮率（0.0-1.0）</returns>
        public double GetCompressionRatio(int originalSize, int compressedSize)
        {
            if (originalSize == 0)
                return 0.0;

            return 1.0 - ((double)compressedSize / originalSize);
        }

        /// <summary>
        /// オブジェクトが破棄済みかチェック
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GZipCompressionEngine));
        }

        /// <summary>
        /// リソースを解放
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _logger.LogDebug("GZipCompressionEngine disposing");
                _disposed = true;
            }
        }

        /// <summary>
        /// ファイナライザー
        /// </summary>
        ~GZipCompressionEngine()
        {
            Dispose();
        }
    }

    /// <summary>
    /// 差分更新エンジン実装クラス
    /// LRUキャッシュによる前回データ保持とビット単位差分計算
    /// </summary>
    public class DifferentialUpdateEngine : IDifferentialUpdateEngine
    {
        private readonly ILogger<DifferentialUpdateEngine> _logger;
        private readonly IMemoryOptimizer _memoryOptimizer;
        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private readonly int _maxCacheEntries;
        private volatile bool _disposed = false;

        /// <summary>
        /// キャッシュエントリ
        /// </summary>
        private class CacheEntry
        {
            public ReadOnlyMemory<byte> Data { get; }
            public DateTime LastAccessed { get; set; }

            public CacheEntry(ReadOnlyMemory<byte> data)
            {
                Data = data;
                LastAccessed = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="memoryOptimizer">メモリ最適化器</param>
        /// <param name="maxCacheEntries">最大キャッシュエントリ数</param>
        /// <param name="logger">ロガー</param>
        public DifferentialUpdateEngine(
            IMemoryOptimizer memoryOptimizer, 
            int maxCacheEntries = 100, 
            ILogger<DifferentialUpdateEngine>? logger = null)
        {
            _memoryOptimizer = memoryOptimizer ?? throw new ArgumentNullException(nameof(memoryOptimizer));
            _maxCacheEntries = maxCacheEntries > 0 ? maxCacheEntries : 100;
            _logger = logger ?? NullLogger<DifferentialUpdateEngine>.Instance;
            _cache = new ConcurrentDictionary<string, CacheEntry>();

            _logger.LogDebug("DifferentialUpdateEngine initialized with max cache entries: {MaxEntries}", _maxCacheEntries);
        }

        /// <summary>
        /// 差分データを計算（XOR差分）
        /// </summary>
        /// <param name="previous">前回データ</param>
        /// <param name="current">現在データ</param>
        /// <returns>差分データ</returns>
        public ReadOnlyMemory<byte> CalculateDifference(ReadOnlySpan<byte> previous, ReadOnlySpan<byte> current)
        {
            ThrowIfDisposed();

            if (previous.IsEmpty && current.IsEmpty)
                return ReadOnlyMemory<byte>.Empty;

            // サイズが異なる場合は全データを差分として扱う
            if (previous.Length != current.Length)
            {
                _logger.LogTrace("Size difference detected: {PreviousSize} -> {CurrentSize}, returning full data", 
                    previous.Length, current.Length);
                return new ReadOnlyMemory<byte>(current.ToArray());
            }

            // XOR差分を計算
            var difference = new byte[current.Length];
            int changedBytes = 0;

            for (int i = 0; i < current.Length; i++)
            {
                difference[i] = (byte)(previous[i] ^ current[i]);
                if (difference[i] != 0)
                    changedBytes++;
            }

            var changeRatio = (double)changedBytes / current.Length;
            
            _logger.LogTrace("Difference calculated: {ChangedBytes}/{TotalBytes} bytes changed ({ChangeRatio:P2})", 
                changedBytes, current.Length, changeRatio);

            // 変更が50%以上の場合は差分ではなく全データを返す
            if (changeRatio > 0.5)
            {
                _logger.LogTrace("High change ratio detected, returning full data instead of difference");
                return new ReadOnlyMemory<byte>(current.ToArray());
            }

            return new ReadOnlyMemory<byte>(difference);
        }

        /// <summary>
        /// 差分を適用してデータを復元
        /// </summary>
        /// <param name="previous">前回データ</param>
        /// <param name="difference">差分データ</param>
        /// <returns>復元データ</returns>
        public ReadOnlyMemory<byte> ApplyDifference(ReadOnlySpan<byte> previous, ReadOnlySpan<byte> difference)
        {
            ThrowIfDisposed();

            if (difference.IsEmpty)
                return ReadOnlyMemory<byte>.Empty;

            // サイズが異なる場合は差分データがそのまま結果データ
            if (previous.Length != difference.Length)
            {
                _logger.LogTrace("Size mismatch: returning difference as full data");
                return new ReadOnlyMemory<byte>(difference.ToArray());
            }

            // XOR差分を適用して復元
            var result = new byte[previous.Length];
            for (int i = 0; i < previous.Length; i++)
            {
                result[i] = (byte)(previous[i] ^ difference[i]);
            }

            _logger.LogTrace("Data restored from difference: {Size} bytes", result.Length);
            return new ReadOnlyMemory<byte>(result);
        }

        /// <summary>
        /// キャッシュから前回データを取得
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns>前回データ（存在しない場合はnull）</returns>
        public ReadOnlyMemory<byte>? GetCachedData(string key)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(key))
                return null;

            if (_cache.TryGetValue(key, out var entry))
            {
                entry.LastAccessed = DateTime.UtcNow;
                _logger.LogTrace("Cache hit for key: {Key}", key);
                return entry.Data;
            }

            _logger.LogTrace("Cache miss for key: {Key}", key);
            return null;
        }

        /// <summary>
        /// データをキャッシュに保存
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="data">データ</param>
        public void CacheData(string key, ReadOnlyMemory<byte> data)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(key))
                return;

            // キャッシュサイズ制限チェック
            if (_cache.Count >= _maxCacheEntries)
            {
                EvictOldestEntries();
            }

            var entry = new CacheEntry(data);
            _cache.AddOrUpdate(key, entry, (k, oldEntry) => entry);

            _logger.LogTrace("Data cached for key: {Key}, size: {Size} bytes", key, data.Length);
        }

        /// <summary>
        /// 古いキャッシュエントリを削除（LRU）
        /// </summary>
        private void EvictOldestEntries()
        {
            var entriesToRemove = _cache.Count - (_maxCacheEntries * 3 / 4); // 25%削除
            
            var sortedEntries = new List<KeyValuePair<string, CacheEntry>>();
            foreach (var kvp in _cache)
            {
                sortedEntries.Add(kvp);
            }

            sortedEntries.Sort((a, b) => a.Value.LastAccessed.CompareTo(b.Value.LastAccessed));

            for (int i = 0; i < entriesToRemove && i < sortedEntries.Count; i++)
            {
                _cache.TryRemove(sortedEntries[i].Key, out _);
            }

            _logger.LogDebug("Evicted {Count} cache entries", entriesToRemove);
        }

        /// <summary>
        /// オブジェクトが破棄済みかチェック
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DifferentialUpdateEngine));
        }

        /// <summary>
        /// リソースを解放
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _cache.Clear();
                _logger.LogDebug("DifferentialUpdateEngine disposing");
                _disposed = true;
            }
        }

        /// <summary>
        /// ファイナライザー
        /// </summary>
        ~DifferentialUpdateEngine()
        {
            Dispose();
        }
    }
}