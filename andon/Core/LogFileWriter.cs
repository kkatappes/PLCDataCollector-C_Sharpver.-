using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SlmpClient.Core
{
    /// <summary>
    /// ログファイルライター実装
    /// SOLID原則: Single Responsibility Principle適用
    /// ファイル書き込み処理のみに特化
    /// </summary>
    public class LogFileWriter : ILogFileWriter
    {
        private readonly ILogger<LogFileWriter> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly SemaphoreSlim _writeSemaphore = new(1, 1);

        public string LogFilePath { get; }

        public LogFileWriter(ILogger<LogFileWriter> logger, string logFilePath)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (string.IsNullOrEmpty(logFilePath))
                throw new ArgumentException("Log file path cannot be null or empty", nameof(logFilePath));

            LogFilePath = logFilePath;

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            // ログディレクトリを作成
            var logDir = Path.GetDirectoryName(LogFilePath);
            if (!string.IsNullOrEmpty(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
        }

        /// <summary>ログエントリをファイルに書き込み</summary>
        public async Task WriteLogEntryAsync(object logEntry)
        {
            await WriteLogEntryWithRetryAsync(logEntry);
        }

        /// <summary>ログエントリを実際にファイルに書き込み（リトライ付き）</summary>
        private async Task WriteLogEntryWithRetryAsync(object logEntry, int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await WriteLogEntryToFileAsync(logEntry);
                    break; // 成功したらリトライ終了
                }
                catch (IOException ex) when (attempt < maxRetries)
                {
                    _logger.LogWarning("ログ書き込み失敗 (試行 {Attempt}/{MaxRetries}): {Error}",
                                      attempt, maxRetries, ex.Message);

                    // 指数バックオフ
                    await Task.Delay(100 * attempt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ログ書き込みでエラーが発生しました (試行 {Attempt}/{MaxRetries})",
                                    attempt, maxRetries);

                    if (attempt == maxRetries)
                    {
                        // 最後の試行でも失敗した場合は諦める（例外は再スローしない）
                        _logger.LogError("ログ書き込みを放棄します: {LogEntry}",
                                        JsonSerializer.Serialize(logEntry, _jsonOptions));
                    }
                    else
                    {
                        await Task.Delay(100 * attempt);
                    }
                }
            }
        }

        /// <summary>ログエントリを実際にファイルに書き込み（排他制御付き）</summary>
        private async Task WriteLogEntryToFileAsync(object logEntry)
        {
            await _writeSemaphore.WaitAsync();
            try
            {
                var json = JsonSerializer.Serialize(logEntry, _jsonOptions);

                // FileShare.ReadWrite で適切な排他制御
                using var stream = new FileStream(LogFilePath, FileMode.Append,
                                                 FileAccess.Write, FileShare.ReadWrite);
                using var writer = new StreamWriter(stream);
                await writer.WriteLineAsync(json);

                _logger.LogDebug("統合ログエントリを出力しました: {LogFilePath}", LogFilePath);
            }
            finally
            {
                _writeSemaphore.Release();
            }
        }
    }
}