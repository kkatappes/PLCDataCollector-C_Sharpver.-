using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SlmpClient.Constants;
using SlmpClient.Core;

namespace SlmpClient.Utils
{
    /// <summary>
    /// チャンク処理実装クラス（遅延読み込み・部分読み込み対応）
    /// 大量データを小分けにして処理し、メモリ使用量を最小化
    /// </summary>
    /// <typeparam name="T">処理結果の型</typeparam>
    public class ChunkProcessor<T> : IChunkProcessor<T>
    {
        private readonly ILogger<ChunkProcessor<T>> _logger;
        private volatile bool _disposed = false;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="logger">ロガー</param>
        public ChunkProcessor(ILogger<ChunkProcessor<T>>? logger = null)
        {
            _logger = logger ?? NullLogger<ChunkProcessor<T>>.Instance;
        }

        /// <summary>
        /// データを非同期でチャンク処理
        /// 大量データを小分けして逐次処理（遅延評価）
        /// </summary>
        /// <param name="totalCount">総データ数</param>
        /// <param name="chunkSize">チャンクサイズ</param>
        /// <param name="processor">各チャンクの処理関数</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>処理結果のAsync Enumerable</returns>
        public async IAsyncEnumerable<T> ProcessChunksAsync<TResult>(
            int totalCount,
            int chunkSize,
            Func<int, int, CancellationToken, Task<T>> processor,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (totalCount <= 0)
                throw new ArgumentException("Total count must be positive", nameof(totalCount));
            if (chunkSize <= 0)
                throw new ArgumentException("Chunk size must be positive", nameof(chunkSize));
            if (processor == null)
                throw new ArgumentNullException(nameof(processor));

            _logger.LogDebug("Starting chunk processing: totalCount={TotalCount}, chunkSize={ChunkSize}", 
                totalCount, chunkSize);

            int processedCount = 0;
            for (int offset = 0; offset < totalCount; offset += chunkSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int currentChunkSize = Math.Min(chunkSize, totalCount - offset);
                
                _logger.LogTrace("Processing chunk: offset={Offset}, size={Size}", offset, currentChunkSize);

                var result = await processor(offset, currentChunkSize, cancellationToken);
                processedCount += currentChunkSize;
                
                _logger.LogTrace("Chunk completed: processed={Processed}/{Total}", 
                    processedCount, totalCount);
                
                yield return result;
            }

            _logger.LogDebug("Chunk processing completed: {ProcessedCount} items processed", processedCount);
        }

        /// <summary>
        /// オブジェクトが破棄済みかチェック
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ChunkProcessor<T>));
        }

        /// <summary>
        /// リソースを解放
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _logger.LogDebug("ChunkProcessor disposing");
                _disposed = true;
            }
        }

        /// <summary>
        /// ファイナライザー
        /// </summary>
        ~ChunkProcessor()
        {
            Dispose();
        }
    }

    /// <summary>
    /// SLMPクライアント用チャンク処理拡張メソッド
    /// </summary>
    public static class SlmpClientChunkExtensions
    {
        /// <summary>
        /// ビットデバイスのチャンク読み取り
        /// 大量データを小分けして読み取り、メモリ使用量を削減
        /// </summary>
        /// <param name="client">SLMPクライアント</param>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="totalCount">総読み取り数</param>
        /// <param name="chunkSize">チャンクサイズ（デフォルト: 256）</param>
        /// <param name="timeout">タイムアウト</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>ビットデータのAsync Enumerable</returns>
        public static async IAsyncEnumerable<bool[]> ReadBitDevicesChunkedAsync(
            this ISlmpClientFull client,
            DeviceCode deviceCode,
            uint startAddress,
            ushort totalCount,
            ushort chunkSize = 256,
            ushort timeout = 0,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var chunkProcessor = new ChunkProcessor<bool[]>();
            
            await foreach (var chunk in chunkProcessor.ProcessChunksAsync<bool[]>(
                totalCount,
                chunkSize,
                async (offset, size, ct) => await client.ReadBitDevicesAsync(
                    deviceCode, 
                    startAddress + (uint)offset, 
                    (ushort)size, 
                    timeout, 
                    ct),
                cancellationToken))
            {
                yield return chunk;
            }
        }

        /// <summary>
        /// ワードデバイスのチャンク読み取り 
        /// 大量データを小分けして読み取り、メモリ使用量を削減
        /// </summary>
        /// <param name="client">SLMPクライアント</param>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="totalCount">総読み取り数</param>
        /// <param name="chunkSize">チャンクサイズ（デフォルト: 256）</param>
        /// <param name="timeout">タイムアウト</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>ワードデータのAsync Enumerable</returns>
        public static async IAsyncEnumerable<ushort[]> ReadWordDevicesChunkedAsync(
            this ISlmpClientFull client,
            DeviceCode deviceCode,
            uint startAddress,
            ushort totalCount,
            ushort chunkSize = 256,
            ushort timeout = 0,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var chunkProcessor = new ChunkProcessor<ushort[]>();
            
            await foreach (var chunk in chunkProcessor.ProcessChunksAsync<ushort[]>(
                totalCount,
                chunkSize,
                async (offset, size, ct) => await client.ReadWordDevicesAsync(
                    deviceCode, 
                    startAddress + (uint)offset, 
                    (ushort)size, 
                    timeout, 
                    ct),
                cancellationToken))
            {
                yield return chunk;
            }
        }

        /// <summary>
        /// チャンクデータを単一配列に統合
        /// 必要に応じてチャンク結果を統合（オプション機能）
        /// </summary>
        /// <typeparam name="T">データ型</typeparam>
        /// <param name="chunkedData">チャンク化されたデータ</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>統合された配列</returns>
        public static async Task<T[]> ConsolidateChunksAsync<T>(
            this IAsyncEnumerable<T[]> chunkedData,
            CancellationToken cancellationToken = default)
        {
            var result = new List<T>();
            
            await foreach (var chunk in chunkedData.WithCancellation(cancellationToken))
            {
                result.AddRange(chunk);
            }
            
            return result.ToArray();
        }
    }
}