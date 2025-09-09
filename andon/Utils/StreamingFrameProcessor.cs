using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SlmpClient.Constants;

namespace SlmpClient.Utils
{
    /// <summary>
    /// ストリーミングフレーム処理クラス（Single Responsibility Principle適用）
    /// 固定バッファを使わず、フレームサイズに応じた最適化処理
    /// </summary>
    public class StreamingFrameProcessor : IStreamingFrameProcessor
    {
        private readonly ILogger<StreamingFrameProcessor> _logger;
        private readonly IMemoryOptimizer _memoryOptimizer;
        private volatile bool _disposed = false;

        /// <summary>
        /// コンストラクタ（Dependency Injection適用）
        /// </summary>
        /// <param name="memoryOptimizer">メモリ最適化器</param>
        /// <param name="logger">ロガー</param>
        public StreamingFrameProcessor(IMemoryOptimizer memoryOptimizer, ILogger<StreamingFrameProcessor>? logger = null)
        {
            _memoryOptimizer = memoryOptimizer ?? throw new ArgumentNullException(nameof(memoryOptimizer));
            _logger = logger ?? NullLogger<StreamingFrameProcessor>.Instance;
        }

        /// <summary>
        /// フレームを非同期でストリーミング処理
        /// メモリ使用量を最小化したフレーム受信処理
        /// </summary>
        /// <param name="stream">入力ストリーム</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>処理済みフレーム</returns>
        public async Task<byte[]> ProcessFrameAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            try
            {
                // Step 1: ヘッダーを最小バッファで読み取り
                using var headerBuffer = _memoryOptimizer.RentBuffer(11); // SLMPヘッダー最大サイズ

                int headerBytesRead = await ReadExactBytesAsync(stream, headerBuffer.Memory[..11], cancellationToken);
                if (headerBytesRead < 11)
                {
                    throw new InvalidOperationException($"Insufficient header data: {headerBytesRead} bytes");
                }

                _logger.LogTrace("Read frame header: {HeaderSize} bytes", headerBytesRead);

                // Step 2: ヘッダーからフレーム全体サイズを決定
                int totalFrameSize = DetermineFrameSize(headerBuffer.Memory.Span[..headerBytesRead]);
                int remainingDataSize = totalFrameSize - headerBytesRead;

                _logger.LogDebug("Frame size determined: total={TotalSize}, remaining={RemainingSize}", 
                    totalFrameSize, remainingDataSize);

                // Step 3: 全体フレーム用の適切サイズバッファを確保
                using var frameBuffer = _memoryOptimizer.RentBuffer(totalFrameSize);

                // ヘッダーをコピー
                headerBuffer.Memory.Span[..headerBytesRead].CopyTo(frameBuffer.Memory.Span);

                // Step 4: 残りデータを読み取り
                if (remainingDataSize > 0)
                {
                    int remainingBytesRead = await ReadExactBytesAsync(
                        stream, 
                        frameBuffer.Memory.Slice(headerBytesRead, remainingDataSize), 
                        cancellationToken);

                    if (remainingBytesRead < remainingDataSize)
                    {
                        throw new InvalidOperationException(
                            $"Insufficient frame data: expected {remainingDataSize}, got {remainingBytesRead}");
                    }
                }

                // Step 5: 結果をコピーして返却
                var result = new byte[totalFrameSize];
                frameBuffer.Memory.Span[..totalFrameSize].CopyTo(result);

                _logger.LogDebug("Frame processing completed: {FrameSize} bytes", totalFrameSize);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing frame");
                throw;
            }
        }

        /// <summary>
        /// フレームサイズを事前に決定
        /// SLMPプロトコル仕様に基づいてヘッダーから全体サイズを計算
        /// </summary>
        /// <param name="headerBytes">ヘッダーバイト</param>
        /// <returns>予想フレームサイズ</returns>
        public int DetermineFrameSize(ReadOnlySpan<byte> headerBytes)
        {
            if (headerBytes.Length < 9)
            {
                throw new ArgumentException("Header too short for SLMP frame", nameof(headerBytes));
            }

            try
            {
                // SLMP 3Eフレーム: サブヘッダー(2) + レスポンスヘッダー(7) + データ
                // データ長は headerBytes[7-8] に格納（リトルエンディアン）
                ushort dataLength = (ushort)(headerBytes[7] | (headerBytes[8] << 8));
                
                // 基本ヘッダーサイズ + データ長
                int totalSize = 9 + dataLength;

                // 4Eフレームかどうかをチェック（サブヘッダーで判定）
                ushort subHeader = (ushort)(headerBytes[0] | (headerBytes[1] << 8));
                if (subHeader == 0x54) // 4Eフレーム
                {
                    totalSize = 11 + dataLength; // 4Eは11バイトヘッダー
                }

                _logger.LogTrace("Frame size determined from header: {Size} bytes (dataLength: {DataLength})", 
                    totalSize, dataLength);

                return totalSize;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining frame size from header");
                throw;
            }
        }

        /// <summary>
        /// 指定バイト数を確実に読み取り
        /// </summary>
        /// <param name="stream">読み取り元ストリーム</param>
        /// <param name="buffer">読み取り先バッファ</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>実際に読み取ったバイト数</returns>
        private static async Task<int> ReadExactBytesAsync(
            Stream stream, 
            Memory<byte> buffer, 
            CancellationToken cancellationToken)
        {
            int totalBytesRead = 0;
            int remainingBytes = buffer.Length;

            while (remainingBytes > 0 && !cancellationToken.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(
                    buffer.Slice(totalBytesRead, remainingBytes), 
                    cancellationToken);

                if (bytesRead == 0)
                    break; // ストリーム終端

                totalBytesRead += bytesRead;
                remainingBytes -= bytesRead;
            }

            return totalBytesRead;
        }

        /// <summary>
        /// オブジェクトが破棄済みかチェック
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(StreamingFrameProcessor));
        }

        /// <summary>
        /// リソースを解放
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _logger.LogDebug("StreamingFrameProcessor disposing");
                _disposed = true;
            }
        }

        /// <summary>
        /// ファイナライザー
        /// </summary>
        ~StreamingFrameProcessor()
        {
            Dispose();
        }
    }
}