using Microsoft.Extensions.Logging;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Core.Interfaces;
using System.Text;

namespace Andon.Core.Managers;

/// <summary>
/// ログ機能（Phase 2-1: ハードコード化版）
/// - ファイル出力
/// - ログレベル設定
/// - ログファイルローテーション
///
/// Phase 2-1完了: IOptions<LoggingConfig>依存を削除し、ハードコード値を使用
/// appsettings.jsonのLoggingConfigセクションが不要になりました
/// </summary>
public class LoggingManager : ILoggingManager, IDisposable
{
    // ハードコード定数定義（appsettings.jsonの現在値を使用）
    private const string LOG_LEVEL = "Debug";
    private const bool ENABLE_FILE_OUTPUT = true;
    private const bool ENABLE_CONSOLE_OUTPUT = true;
    private const string LOG_FILE_PATH = "logs/andon.log";
    private const int MAX_LOG_FILE_SIZE_MB = 10;
    private const int MAX_LOG_FILE_COUNT = 7;
    private const bool ENABLE_DATE_BASED_ROTATION = false;

    private readonly ILogger<LoggingManager> _logger;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private StreamWriter? _fileWriter;
    private bool _disposed = false;

    // ログレベル定義
    private enum LogLevel
    {
        Debug = 0,
        Information = 1,
        Warning = 2,
        Error = 3
    }

    private readonly LogLevel _currentLogLevel;

    public LoggingManager(ILogger<LoggingManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _currentLogLevel = ParseLogLevel(LOG_LEVEL);

        if (ENABLE_FILE_OUTPUT)
        {
            InitializeFileWriter();
        }
    }

    private LogLevel ParseLogLevel(string levelString)
    {
        return levelString switch
        {
            "Debug" => LogLevel.Debug,
            "Information" => LogLevel.Information,
            "Warning" => LogLevel.Warning,
            "Error" => LogLevel.Error,
            _ => throw new ArgumentException($"無効なLogLevel: {levelString}。有効な値: Debug, Information, Warning, Error", nameof(levelString))
        };
    }

    private void InitializeFileWriter()
    {
        try
        {
            // ディレクトリが存在しない場合は作成
            var directory = Path.GetDirectoryName(LOG_FILE_PATH);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // StreamWriterを初期化（追記モード、自動フラッシュ有効、読み書き共有）
            var fileStream = new FileStream(LOG_FILE_PATH, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            _fileWriter = new StreamWriter(fileStream, Encoding.UTF8)
            {
                AutoFlush = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"ファイル出力の初期化に失敗: {LOG_FILE_PATH}");
            throw;
        }
    }

    private bool ShouldLog(LogLevel level)
    {
        return level >= _currentLogLevel;
    }

    private async Task WriteToFileAsync(string level, string message)
    {
        if (!ENABLE_FILE_OUTPUT || _fileWriter == null)
            return;

        await _fileLock.WaitAsync();
        try
        {
            // ローテーションチェック
            await CheckAndRotateFileAsync();

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] [{level}] {message}";
            await _fileWriter.WriteLineAsync(logEntry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ファイル書き込みエラー");
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task CheckAndRotateFileAsync()
    {
        if (_fileWriter == null || string.IsNullOrEmpty(LOG_FILE_PATH))
            return;

        try
        {
            // 日付ベースのローテーション
            if (ENABLE_DATE_BASED_ROTATION)
            {
                // 簡易実装: 日付変更は頻繁ではないため、ここでは基本的なサポートのみ
                // 実際の日付変更検知は別途タイマーで実装する必要がある
            }

            // サイズベースのローテーション
            var fileInfo = new FileInfo(LOG_FILE_PATH);
            if (fileInfo.Exists && fileInfo.Length > MAX_LOG_FILE_SIZE_MB * 1024 * 1024)
            {
                await RotateLogFileAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ローテーションチェックエラー");
        }
    }

    private async Task RotateLogFileAsync()
    {
        try
        {
            // StreamWriterを一時的にクローズ
            _fileWriter?.Close();
            _fileWriter?.Dispose();
            _fileWriter = null;

            // 既存のローテーションファイルをリネーム
            for (int i = MAX_LOG_FILE_COUNT - 1; i >= 1; i--)
            {
                var oldFile = $"{LOG_FILE_PATH}.{i}";
                var newFile = $"{LOG_FILE_PATH}.{i + 1}";

                if (File.Exists(oldFile))
                {
                    if (i + 1 > MAX_LOG_FILE_COUNT)
                    {
                        // 最大ファイル数を超える場合は削除
                        File.Delete(oldFile);
                    }
                    else
                    {
                        if (File.Exists(newFile))
                            File.Delete(newFile);
                        File.Move(oldFile, newFile);
                    }
                }
            }

            // 現在のファイルを.1にリネーム
            if (File.Exists(LOG_FILE_PATH))
            {
                var rotatedFile = $"{LOG_FILE_PATH}.1";
                if (File.Exists(rotatedFile))
                    File.Delete(rotatedFile);
                File.Move(LOG_FILE_PATH, rotatedFile);
            }

            // 新しいStreamWriterを初期化（読み取り共有）
            var fileStream = new FileStream(LOG_FILE_PATH, FileMode.Append, FileAccess.Write, FileShare.Read);
            _fileWriter = new StreamWriter(fileStream, Encoding.UTF8)
            {
                AutoFlush = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ログファイルローテーションエラー");

            // エラーが発生した場合でも、新しいStreamWriterを初期化
            try
            {
                var fileStream = new FileStream(LOG_FILE_PATH, FileMode.Append, FileAccess.Write, FileShare.Read);
                _fileWriter = new StreamWriter(fileStream, Encoding.UTF8)
                {
                    AutoFlush = true
                };
            }
            catch
            {
                // 初期化に失敗した場合は諦める
            }
        }
    }

    public async Task LogInfo(string message)
    {
        if (ShouldLog(LogLevel.Information))
        {
            if (ENABLE_CONSOLE_OUTPUT)
            {
                _logger.LogInformation(message);
            }
            await WriteToFileAsync("INFO", message);
        }
    }

    public async Task LogWarning(string message)
    {
        if (ShouldLog(LogLevel.Warning))
        {
            if (ENABLE_CONSOLE_OUTPUT)
            {
                _logger.LogWarning(message);
            }
            await WriteToFileAsync("WARN", message);
        }
    }

    public async Task LogError(Exception? ex, string message)
    {
        if (ShouldLog(LogLevel.Error))
        {
            if (ENABLE_CONSOLE_OUTPUT)
            {
                if (ex != null)
                {
                    _logger.LogError(ex, message);
                }
                else
                {
                    _logger.LogError(message);
                }
            }

            var errorMessage = ex != null ? $"{message} | Exception: {ex.Message}" : message;
            await WriteToFileAsync("ERROR", errorMessage);
        }
    }

    public async Task LogDebug(string message)
    {
        if (ShouldLog(LogLevel.Debug))
        {
            if (ENABLE_CONSOLE_OUTPUT)
            {
                _logger.LogDebug(message);
            }
            await WriteToFileAsync("DEBUG", message);
        }
    }

    public async Task CloseAndFlushAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            if (_fileWriter != null)
            {
                await _fileWriter.FlushAsync();
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// データ取得のログ記録（ReadRandom対応、Phase4仕様対応）
    /// </summary>
    /// <param name="data">処理済みレスポンスデータ</param>
    public void LogDataAcquisition(ProcessedResponseData data)
    {
        // Phase4仕様: Dictionary<string, DeviceData>型のProcessedDataプロパティ使用
        var deviceList = string.Join(", ", data.ProcessedData.Keys.Take(5));
        int deviceCount = data.ProcessedData.Count;

        var message = deviceCount <= 5
            ? $"[ReadRandom] {deviceCount}点取得: {deviceList}"
            : $"[ReadRandom] {deviceCount}点取得: {deviceList}... （他{deviceCount - 5}点）";

        if (ENABLE_CONSOLE_OUTPUT)
        {
            _logger.LogInformation(message);
        }

        // 非同期メソッドを同期的に呼び出す（互換性のため）
        WriteToFileAsync("INFO", message).Wait();
    }

    /// <summary>
    /// フレーム送信のログ記録
    /// </summary>
    /// <param name="frame">送信フレーム</param>
    /// <param name="commandType">コマンド種別</param>
    public void LogFrameSent(byte[] frame, string commandType)
    {
        var message = $"[送信] {commandType}フレーム: {frame.Length}バイト";

        if (ENABLE_CONSOLE_OUTPUT)
        {
            _logger.LogDebug(message);
        }

        WriteToFileAsync("DEBUG", message).Wait();
    }

    /// <summary>
    /// レスポンス受信のログ記録
    /// </summary>
    /// <param name="response">受信レスポンス</param>
    public void LogResponseReceived(byte[] response)
    {
        var message = $"[受信] レスポンス: {response.Length}バイト";

        if (ENABLE_CONSOLE_OUTPUT)
        {
            _logger.LogDebug(message);
        }

        WriteToFileAsync("DEBUG", message).Wait();
    }

    /// <summary>
    /// エラーのログ記録（レガシーメソッド、下位互換性用）
    /// </summary>
    /// <param name="ex">例外</param>
    /// <param name="context">エラー発生コンテキスト</param>
    public void LogErrorLegacy(Exception ex, string context)
    {
        var message = $"[エラー] {context}: {ex.Message}";

        if (ENABLE_CONSOLE_OUTPUT)
        {
            _logger.LogError(ex, message);
        }

        WriteToFileAsync("ERROR", $"{message} | StackTrace: {ex.StackTrace}").Wait();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _fileLock.Wait();
        try
        {
            _fileWriter?.Flush();
            _fileWriter?.Dispose();
            _fileWriter = null;
        }
        finally
        {
            _fileLock.Release();
            _fileLock.Dispose();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    #region Phase6: PLC処理ログ

    /// <summary>
    /// PLC処理開始ログ
    /// </summary>
    public void LogPlcProcessStart(PlcConfiguration config)
    {
        _logger.LogInformation(
            $"[{config.EffectivePlcName}] PLC処理開始: " +
            $"{config.IpAddress}:{config.Port}");
    }

    /// <summary>
    /// PLC処理完了ログ
    /// </summary>
    public void LogPlcProcessComplete(PlcConfiguration config, int deviceCount)
    {
        _logger.LogInformation(
            $"[{config.EffectivePlcName}] PLC処理完了: " +
            $"デバイス数={deviceCount}");
    }

    /// <summary>
    /// PLC通信エラーログ
    /// </summary>
    public void LogPlcCommunicationError(PlcConfiguration config, Exception ex)
    {
        _logger.LogError(ex,
            $"[{config.EffectivePlcName}] PLC通信エラー: " +
            $"{config.IpAddress}:{config.Port}");
    }

    #endregion
}
