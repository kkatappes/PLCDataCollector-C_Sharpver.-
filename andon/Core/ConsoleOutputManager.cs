using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SlmpClient.Core
{
    /// <summary>
    /// コンソール出力管理システム
    /// 人間可読なコンソール出力をJSON構造化してファイルに記録
    /// ハイブリッド統合ログシステムの一部として実装
    /// </summary>
    public class ConsoleOutputManager : IAsyncDisposable
    {
        private readonly ILogger<ConsoleOutputManager> _logger;
        private readonly string _outputFilePath;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Channel<ConsoleEntry> _outputQueue;
        private readonly SemaphoreSlim _writeSemaphore;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly string _sessionId; // GREEN: セッション一貫性のためのセッションID保持
        private Task? _queueProcessorTask;

        public ConsoleOutputManager(ILogger<ConsoleOutputManager> logger, string outputFilePath)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _outputFilePath = outputFilePath ?? throw new ArgumentNullException(nameof(outputFilePath));

            // GREEN: セッション開始時に一度生成して一貫性を保つ
            _sessionId = $"session_{DateTime.Now:yyyyMMdd_HHmmss}";

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            _outputQueue = Channel.CreateUnbounded<ConsoleEntry>();
            _writeSemaphore = new SemaphoreSlim(1, 1);
            _cancellationTokenSource = new CancellationTokenSource();

            // 出力ディレクトリを作成
            var outputDir = Path.GetDirectoryName(_outputFilePath);
            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // キュープロセッサーを起動
            _queueProcessorTask = ProcessOutputQueueAsync();
        }

        /// <summary>
        /// 情報メッセージを出力
        /// </summary>
        public async Task WriteInfoAsync(string message, string category = "General",
                                        int? stepNumber = null, object? context = null)
        {
            var entry = new ConsoleEntry
            {
                EntryType = "INFO",
                Level = "Information",
                Category = category,
                Message = message,
                Context = context != null ? new ConsoleContext
                {
                    StepNumber = stepNumber,
                    AdditionalData = context
                } : null
            };

            await EnqueueEntryAsync(entry);
        }

        /// <summary>
        /// 進捗メッセージを出力
        /// </summary>
        public async Task WriteProgressAsync(string message, int stepNumber,
                                            string phaseInfo, double? progressPercentage = null)
        {
            var entry = new ConsoleEntry
            {
                EntryType = "PROGRESS",
                Level = "Information",
                Category = "Step" + stepNumber,
                Message = message,
                Context = new ConsoleContext
                {
                    StepNumber = stepNumber,
                    PhaseInfo = phaseInfo,
                    ProgressPercentage = progressPercentage
                }
            };

            await EnqueueEntryAsync(entry);
        }

        /// <summary>
        /// 結果メッセージを出力
        /// </summary>
        public async Task WriteResultAsync(string message, int stepNumber,
                                          string phaseInfo, object? resultData = null)
        {
            var entry = new ConsoleEntry
            {
                EntryType = "RESULT",
                Level = "Information",
                Category = "Step" + stepNumber,
                Message = message,
                Context = new ConsoleContext
                {
                    StepNumber = stepNumber,
                    PhaseInfo = phaseInfo,
                    ResultData = resultData
                }
            };

            await EnqueueEntryAsync(entry);
        }

        /// <summary>
        /// エラーメッセージを出力
        /// </summary>
        public async Task WriteErrorAsync(string message, string category = "General",
                                         int? stepNumber = null, object? errorDetails = null)
        {
            var entry = new ConsoleEntry
            {
                EntryType = "ERROR",
                Level = "Error",
                Category = category,
                Message = message,
                Context = new ConsoleContext
                {
                    StepNumber = stepNumber,
                    ErrorDetails = errorDetails
                }
            };

            await EnqueueEntryAsync(entry);
        }

        /// <summary>
        /// ヘッダーメッセージを出力
        /// </summary>
        public async Task WriteHeaderAsync(string message, string headerType,
                                          object? context = null)
        {
            var entry = new ConsoleEntry
            {
                EntryType = "HEADER",
                Level = "Information",
                Category = headerType,
                Message = message,
                Context = context != null ? new ConsoleContext
                {
                    AdditionalData = context
                } : null
            };

            await EnqueueEntryAsync(entry);
        }

        /// <summary>
        /// エントリをキューに追加
        /// </summary>
        private async Task EnqueueEntryAsync(ConsoleEntry entry)
        {
            entry.SessionId = GetCurrentSessionId();
            await _outputQueue.Writer.WriteAsync(entry);
        }

        /// <summary>
        /// 出力キューを処理
        /// </summary>
        private async Task ProcessOutputQueueAsync()
        {
            try
            {
                await foreach (var entry in _outputQueue.Reader.ReadAllAsync(_cancellationTokenSource.Token))
                {
                    await WriteEntryToFileAsync(entry);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常な終了
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing console output queue");
            }
        }

        /// <summary>
        /// エントリをファイルに書き込み
        /// </summary>
        private async Task WriteEntryToFileAsync(ConsoleEntry entry)
        {
            await _writeSemaphore.WaitAsync();
            try
            {
                var json = JsonSerializer.Serialize(entry, _jsonOptions);
                await File.AppendAllTextAsync(_outputFilePath, json + Environment.NewLine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing console entry to file: {FilePath}", _outputFilePath);
            }
            finally
            {
                _writeSemaphore.Release();
            }
        }

        /// <summary>
        /// 現在のセッションIDを取得
        /// GREEN: セッション一貫性のため、コンストラクタで生成されたセッションIDを返す
        /// </summary>
        private string GetCurrentSessionId()
        {
            return _sessionId;
        }

        /// <summary>
        /// リソースを非同期的に解放
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            // キューを停止
            _cancellationTokenSource.Cancel();
            _outputQueue.Writer.Complete();

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

            // リソースを解放
            _writeSemaphore.Dispose();
            _cancellationTokenSource.Dispose();

            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// コンソール出力エントリ
    /// </summary>
    public class ConsoleEntry
    {
        public string EntryType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string SessionId { get; set; } = string.Empty;
        public string Level { get; set; } = "Information";
        public string Category { get; set; } = "General";
        public string Message { get; set; } = string.Empty;
        public ConsoleContext? Context { get; set; }
    }

    /// <summary>
    /// コンソール出力コンテキスト
    /// </summary>
    public class ConsoleContext
    {
        public int? StepNumber { get; set; }
        public string? PhaseInfo { get; set; }
        public double? ProgressPercentage { get; set; }
        public object? ResultData { get; set; }
        public object? ErrorDetails { get; set; }
        public object? AdditionalData { get; set; }
    }
}