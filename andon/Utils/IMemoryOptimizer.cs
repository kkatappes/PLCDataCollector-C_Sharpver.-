using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace SlmpClient.Utils
{
    /// <summary>
    /// メモリ最適化インターフェース（Interface Segregation Principle適用）
    /// </summary>
    public interface IMemoryOptimizer : IDisposable
    {
        /// <summary>
        /// 現在のメモリ使用量（バイト）
        /// </summary>
        long CurrentMemoryUsage { get; }

        /// <summary>
        /// ピークメモリ使用量（バイト）
        /// </summary>
        long PeakMemoryUsage { get; }

        /// <summary>
        /// メモリプールからバッファを借用
        /// </summary>
        /// <param name="minimumLength">最小必要長</param>
        /// <returns>借用したバッファ</returns>
        IMemoryOwner<byte> RentBuffer(int minimumLength);

        /// <summary>
        /// メモリ使用量をリセット
        /// </summary>
        void ResetMemoryTracking();

        /// <summary>
        /// メモリしきい値超過イベント
        /// </summary>
        event Action<long> MemoryThresholdExceeded;
    }

    /// <summary>
    /// ストリーミングフレーム処理インターフェース
    /// </summary>
    public interface IStreamingFrameProcessor : IDisposable
    {
        /// <summary>
        /// フレームを非同期でストリーミング処理
        /// </summary>
        /// <param name="stream">入力ストリーム</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>処理済みフレーム</returns>
        Task<byte[]> ProcessFrameAsync(System.IO.Stream stream, CancellationToken cancellationToken = default);

        /// <summary>
        /// フレームサイズを事前に決定
        /// </summary>
        /// <param name="headerBytes">ヘッダーバイト</param>
        /// <returns>予想フレームサイズ</returns>
        int DetermineFrameSize(ReadOnlySpan<byte> headerBytes);
    }

    /// <summary>
    /// チャンク処理インターフェース
    /// </summary>
    public interface IChunkProcessor<T> : IDisposable
    {
        /// <summary>
        /// データを非同期でチャンク処理
        /// </summary>
        /// <param name="totalCount">総データ数</param>
        /// <param name="chunkSize">チャンクサイズ</param>
        /// <param name="processor">各チャンクの処理関数</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>処理結果のAsync Enumerable</returns>
        IAsyncEnumerable<T> ProcessChunksAsync<TResult>(
            int totalCount,
            int chunkSize,
            Func<int, int, CancellationToken, Task<T>> processor,
            CancellationToken cancellationToken = default);
    }
}