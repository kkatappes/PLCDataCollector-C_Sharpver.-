using System;
using System.ComponentModel.DataAnnotations;

namespace SlmpClient.Core
{
    /// <summary>
    /// メモリ最適化SLMP設定クラス
    /// 設定可能なメモリ制限機能を提供
    /// </summary>
    public class MemoryOptimizedSlmpSettings
    {
        /// <summary>
        /// 最大バッファサイズ（バイト）
        /// </summary>
        [Range(256, 8192)]
        public int MaxBufferSize { get; set; } = 1024; // デフォルト1KB

        /// <summary>
        /// 最大キャッシュエントリ数
        /// </summary>
        [Range(10, 1000)]
        public int MaxCacheEntries { get; set; } = 100;

        /// <summary>
        /// 圧縮を有効にするか
        /// </summary>
        public bool EnableCompression { get; set; } = true;

        /// <summary>
        /// チャンク読み取りを使用するか
        /// </summary>
        public bool UseChunkedReading { get; set; } = true;

        /// <summary>
        /// デフォルトチャンクサイズ
        /// </summary>
        [Range(64, 1024)]
        public ushort DefaultChunkSize { get; set; } = 256;

        /// <summary>
        /// メモリしきい値（バイト）
        /// この値を超えるとイベントが発生
        /// </summary>
        [Range(1024, 10 * 1024 * 1024)] // 1KB - 10MB
        public long MemoryThreshold { get; set; } = 512 * 1024; // デフォルト512KB

        /// <summary>
        /// 自動メモリクリーンアップを有効にするか
        /// </summary>
        public bool EnableAutoCleanup { get; set; } = true;

        /// <summary>
        /// メモリ統計収集を有効にするか
        /// </summary>
        public bool EnableMemoryStatistics { get; set; } = true;

        /// <summary>
        /// 差分更新を有効にするか
        /// </summary>
        public bool EnableDifferentialUpdate { get; set; } = true;

        /// <summary>
        /// ストリーミング処理を有効にするか
        /// </summary>
        public bool EnableStreamingProcessing { get; set; } = true;

        /// <summary>
        /// ArrayPoolを使用するか
        /// </summary>
        public bool UseArrayPool { get; set; } = true;

        /// <summary>
        /// 圧縮しきい値（バイト）
        /// この値以上のデータのみ圧縮対象
        /// </summary>
        [Range(128, 4096)]
        public int CompressionThreshold { get; set; } = 512;

        /// <summary>
        /// メモリ監視間隔（ミリ秒）
        /// </summary>
        [Range(100, 10000)]
        public int MemoryMonitoringInterval { get; set; } = 1000; // 1秒

        /// <summary>
        /// 設定が有効かを検証
        /// </summary>
        /// <returns>有効な場合はtrue</returns>
        public bool IsValid()
        {
            return MaxBufferSize > 0 &&
                   MaxCacheEntries > 0 &&
                   DefaultChunkSize > 0 &&
                   MemoryThreshold > 0 &&
                   CompressionThreshold > 0 &&
                   MemoryMonitoringInterval > 0 &&
                   MaxBufferSize >= 256 &&
                   MemoryThreshold >= 1024;
        }

        /// <summary>
        /// 設定のコピーを作成
        /// </summary>
        /// <returns>設定のコピー</returns>
        public MemoryOptimizedSlmpSettings Clone()
        {
            return new MemoryOptimizedSlmpSettings
            {
                MaxBufferSize = MaxBufferSize,
                MaxCacheEntries = MaxCacheEntries,
                EnableCompression = EnableCompression,
                UseChunkedReading = UseChunkedReading,
                DefaultChunkSize = DefaultChunkSize,
                MemoryThreshold = MemoryThreshold,
                EnableAutoCleanup = EnableAutoCleanup,
                EnableMemoryStatistics = EnableMemoryStatistics,
                EnableDifferentialUpdate = EnableDifferentialUpdate,
                EnableStreamingProcessing = EnableStreamingProcessing,
                UseArrayPool = UseArrayPool,
                CompressionThreshold = CompressionThreshold,
                MemoryMonitoringInterval = MemoryMonitoringInterval
            };
        }

        /// <summary>
        /// 設定の文字列表現を取得
        /// </summary>
        /// <returns>設定文字列</returns>
        public override string ToString()
        {
            return $"MemoryOptimizedSlmpSettings(" +
                   $"MaxBuffer: {MaxBufferSize}, " +
                   $"MaxCache: {MaxCacheEntries}, " +
                   $"Compression: {EnableCompression}, " +
                   $"Chunked: {UseChunkedReading}, " +
                   $"ChunkSize: {DefaultChunkSize}, " +
                   $"Threshold: {MemoryThreshold}, " +
                   $"AutoCleanup: {EnableAutoCleanup}, " +
                   $"Statistics: {EnableMemoryStatistics}, " +
                   $"Differential: {EnableDifferentialUpdate}, " +
                   $"Streaming: {EnableStreamingProcessing}, " +
                   $"ArrayPool: {UseArrayPool}, " +
                   $"CompThreshold: {CompressionThreshold}, " +
                   $"MonitorInterval: {MemoryMonitoringInterval})";
        }
    }

    /// <summary>
    /// メモリ最適化統計情報
    /// </summary>
    public class MemoryOptimizationStatistics
    {
        /// <summary>
        /// 現在のメモリ使用量（バイト）
        /// </summary>
        public long CurrentMemoryUsage { get; set; }

        /// <summary>
        /// ピークメモリ使用量（バイト）
        /// </summary>
        public long PeakMemoryUsage { get; set; }

        /// <summary>
        /// 累計メモリ割り当て量（バイト）
        /// </summary>
        public long TotalMemoryAllocated { get; set; }

        /// <summary>
        /// 累計メモリ解放量（バイト）
        /// </summary>
        public long TotalMemoryDeallocated { get; set; }

        /// <summary>
        /// バッファ借用回数
        /// </summary>
        public long BufferRentCount { get; set; }

        /// <summary>
        /// バッファ返却回数
        /// </summary>
        public long BufferReturnCount { get; set; }

        /// <summary>
        /// 圧縮実行回数
        /// </summary>
        public long CompressionCount { get; set; }

        /// <summary>
        /// 展開実行回数
        /// </summary>
        public long DecompressionCount { get; set; }

        /// <summary>
        /// 累計圧縮率
        /// </summary>
        public double TotalCompressionRatio { get; set; }

        /// <summary>
        /// 差分更新実行回数
        /// </summary>
        public long DifferentialUpdateCount { get; set; }

        /// <summary>
        /// キャッシュヒット回数
        /// </summary>
        public long CacheHitCount { get; set; }

        /// <summary>
        /// キャッシュミス回数
        /// </summary>
        public long CacheMissCount { get; set; }

        /// <summary>
        /// チャンク処理実行回数
        /// </summary>
        public long ChunkProcessingCount { get; set; }

        /// <summary>
        /// メモリしきい値超過回数
        /// </summary>
        public long MemoryThresholdExceededCount { get; set; }

        /// <summary>
        /// 自動クリーンアップ実行回数
        /// </summary>
        public long AutoCleanupCount { get; set; }

        /// <summary>
        /// 最終更新時刻
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// キャッシュヒット率を計算
        /// </summary>
        /// <returns>キャッシュヒット率（0.0-1.0）</returns>
        public double GetCacheHitRatio()
        {
            var totalRequests = CacheHitCount + CacheMissCount;
            return totalRequests > 0 ? (double)CacheHitCount / totalRequests : 0.0;
        }

        /// <summary>
        /// 平均圧縮率を計算
        /// </summary>
        /// <returns>平均圧縮率（0.0-1.0）</returns>
        public double GetAverageCompressionRatio()
        {
            return CompressionCount > 0 ? TotalCompressionRatio / CompressionCount : 0.0;
        }

        /// <summary>
        /// メモリ効率を計算
        /// </summary>
        /// <returns>メモリ効率（使用量/ピーク使用量の逆数）</returns>
        public double GetMemoryEfficiency()
        {
            return PeakMemoryUsage > 0 ? (double)CurrentMemoryUsage / PeakMemoryUsage : 1.0;
        }

        /// <summary>
        /// 統計情報をリセット
        /// </summary>
        public void Reset()
        {
            CurrentMemoryUsage = 0;
            PeakMemoryUsage = 0;
            TotalMemoryAllocated = 0;
            TotalMemoryDeallocated = 0;
            BufferRentCount = 0;
            BufferReturnCount = 0;
            CompressionCount = 0;
            DecompressionCount = 0;
            TotalCompressionRatio = 0.0;
            DifferentialUpdateCount = 0;
            CacheHitCount = 0;
            CacheMissCount = 0;
            ChunkProcessingCount = 0;
            MemoryThresholdExceededCount = 0;
            AutoCleanupCount = 0;
            LastUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// 統計情報の文字列表現を取得
        /// </summary>
        /// <returns>統計情報文字列</returns>
        public override string ToString()
        {
            return $"MemoryOptimizationStatistics(" +
                   $"CurrentMemory: {CurrentMemoryUsage:N0} bytes, " +
                   $"PeakMemory: {PeakMemoryUsage:N0} bytes, " +
                   $"CacheHitRatio: {GetCacheHitRatio():P2}, " +
                   $"AvgCompressionRatio: {GetAverageCompressionRatio():P2}, " +
                   $"MemoryEfficiency: {GetMemoryEfficiency():P2}, " +
                   $"BufferRents: {BufferRentCount:N0}, " +
                   $"Compressions: {CompressionCount:N0}, " +
                   $"DifferentialUpdates: {DifferentialUpdateCount:N0}, " +
                   $"ChunkProcessings: {ChunkProcessingCount:N0}, " +
                   $"ThresholdExceeded: {MemoryThresholdExceededCount:N0}, " +
                   $"AutoCleanups: {AutoCleanupCount:N0})";
        }
    }
}