using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SlmpClient.Core
{
    /// <summary>
    /// ログキュープロセッサー実装
    /// SOLID原則: Single Responsibility Principle適用
    /// キューイング処理のみに特化
    /// </summary>
    public class LogQueueProcessor : ILogQueueProcessor
    {
        private readonly ILogger<LogQueueProcessor> _logger;
        private readonly ILogFileWriter _fileWriter;
        private readonly Channel<object> _logQueue = Channel.CreateUnbounded<object>();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private Task? _queueProcessorTask;

        public LogQueueProcessor(ILogger<LogQueueProcessor> logger, ILogFileWriter fileWriter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileWriter = fileWriter ?? throw new ArgumentNullException(nameof(fileWriter));
        }

        /// <summary>ログエントリをキューに追加</summary>
        public async Task EnqueueLogEntryAsync(object logEntry)
        {
            try
            {
                // ログエントリをキューに追加（ノンブロッキング）
                await _logQueue.Writer.WriteAsync(logEntry, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ログエントリのキューイングに失敗しました");
                throw;
            }
        }

        /// <summary>キュープロセッサーを開始</summary>
        public void StartProcessing()
        {
            if (_queueProcessorTask == null)
            {
                _queueProcessorTask = ProcessLogQueueAsync();
            }
        }

        /// <summary>キュープロセッサーを停止</summary>
        public async Task StopProcessingAsync()
        {
            // ログキューを停止
            _cancellationTokenSource.Cancel();
            _logQueue.Writer.Complete();

            // キュープロセッサータスクの完了を待機
            if (_queueProcessorTask != null)
            {
                try
                {
                    await _queueProcessorTask;
                }
                catch (OperationCanceledException)
                {
                    // 正常なキャンセル
                }
            }
        }

        /// <summary>ログキューを処理するバックグラウンドタスク</summary>
        private async Task ProcessLogQueueAsync()
        {
            try
            {
                await foreach (var logEntry in _logQueue.Reader.ReadAllAsync(_cancellationTokenSource.Token))
                {
                    await _fileWriter.WriteLogEntryAsync(logEntry);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常なシャットダウン
                _logger.LogDebug("ログキュープロセッサーが停止されました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ログキュープロセッサーでエラーが発生しました");
            }
        }

        /// <summary>リソースを非同期的に解放</summary>
        public async ValueTask DisposeAsync()
        {
            await StopProcessingAsync();
            _cancellationTokenSource.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>リソースを同期的に解放</summary>
        public void Dispose()
        {
            // 非同期処理を同期的に実行（デッドロック回避のためGetAwaiterを使用）
            DisposeAsync().GetAwaiter().GetResult();
        }
    }
}