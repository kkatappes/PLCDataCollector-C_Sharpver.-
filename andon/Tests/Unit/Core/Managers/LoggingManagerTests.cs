using Xunit;
using Andon.Core.Managers;
using Andon.Core.Models.ConfigModels;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;

namespace Andon.Tests.Unit.Core.Managers;

/// <summary>
/// LoggingManager単体テスト（Phase 2-1: ハードコード化版）
///
/// 注意: LoggingConfigがハードコード化されたため、以下の固定値を前提としています:
/// - LOG_LEVEL = "Debug"
/// - ENABLE_FILE_OUTPUT = true
/// - ENABLE_CONSOLE_OUTPUT = true
/// - LOG_FILE_PATH = "logs/andon.log"
/// - MAX_LOG_FILE_SIZE_MB = 10
/// - MAX_LOG_FILE_COUNT = 7
/// - ENABLE_DATE_BASED_ROTATION = false
/// </summary>
[Collection("LoggingManagerTests")]
public class LoggingManagerTests : IDisposable
{
    private const string LogFilePath = "logs/andon.log";
    private readonly List<string> _tempFiles = new();

    public void Dispose()
    {
        // テスト後にログファイルを削除
        try
        {
            if (File.Exists(LogFilePath))
                File.Delete(LogFilePath);

            // ローテーションファイルも削除
            for (int i = 1; i <= 10; i++)
            {
                var rotatedFile = $"{LogFilePath}.{i}";
                if (File.Exists(rotatedFile))
                    File.Delete(rotatedFile);
            }
        }
        catch
        {
            // クリーンアップエラーは無視
        }
    }

    #region ファイル出力機能テスト（5テスト）

    [Fact]
    public async Task TC_LogMgr_001_LogInfo_WritesToFile()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object);

        // Act
        await manager.LogInfo("Test info message");
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        Assert.True(File.Exists(LogFilePath), "ログファイルが作成されていません");
        var content = await ReadFileWithSharedAccessAsync(LogFilePath);
        Assert.Contains("Test info message", content);
    }

    [Fact]
    public async Task TC_LogMgr_002_LogWarning_WritesToFile()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object);

        // Act
        await manager.LogWarning("Test warning message");
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        var content = await ReadFileWithSharedAccessAsync(LogFilePath);
        Assert.Contains("Test warning message", content);
    }

    [Fact]
    public async Task TC_LogMgr_003_LogError_WritesToFile()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object);

        // Act
        var exception = new InvalidOperationException("Test exception");
        await manager.LogError(exception, "Test error message");
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        var content = await ReadFileWithSharedAccessAsync(LogFilePath);
        Assert.Contains("Test error message", content);
    }

    [Fact]
    public async Task TC_LogMgr_004_LogDebug_WritesToFile()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object);

        // Act
        await manager.LogDebug("Test debug message");
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        var content = await ReadFileWithSharedAccessAsync(LogFilePath);
        Assert.Contains("Test debug message", content);
    }

    [Fact]
    public async Task TC_LogMgr_006_CloseAndFlushAsync_FlushesBufferedLogs()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object);

        // Act
        await manager.LogInfo("Buffered message 1");
        await manager.LogInfo("Buffered message 2");
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        var content = await ReadFileWithSharedAccessAsync(LogFilePath);
        Assert.Contains("Buffered message 1", content);
        Assert.Contains("Buffered message 2", content);
    }

    #endregion

    #region ログレベル設定テスト（3テスト）
    // ハードコード値: LOG_LEVEL = "Debug"
    // 全てのログレベル（Debug, Info, Warning, Error）が出力される

    [Fact]
    public async Task TC_LogMgr_011_LogDebug_LogLevelDebug_Written()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object);

        // Act
        await manager.LogDebug("Debug message");
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        var content = await ReadFileWithSharedAccessAsync(LogFilePath);
        Assert.Contains("Debug message", content);
    }

    [Fact]
    public async Task TC_LogMgr_008_LogInfo_Written()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object);

        // Act
        await manager.LogInfo("Info message");
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        var content = await ReadFileWithSharedAccessAsync(LogFilePath);
        Assert.Contains("Info message", content);
    }

    [Fact]
    public async Task TC_LogMgr_009_LogWarning_Written()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object);

        // Act
        await manager.LogWarning("Warning message");
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        var content = await ReadFileWithSharedAccessAsync(LogFilePath);
        Assert.Contains("Warning message", content);
    }

    #endregion

    #region ログファイルローテーション機能テスト（5テスト）
    // ハードコード値: MAX_LOG_FILE_SIZE_MB = 10, MAX_LOG_FILE_COUNT = 7

    [Fact]
    public async Task TC_LogMgr_015_LogInfo_ExceedsMaxFileSize_RotatesFile()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object);

        // Act
        // 10MBを超えるログを書き込み（約1500バイト × 7500回 = 11.25MB）
        for (int i = 0; i < 7500; i++)
        {
            await manager.LogInfo($"Large message {new string('x', 1400)} - iteration {i}");
        }
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        Assert.True(File.Exists($"{LogFilePath}.1"), "ローテーションファイル(.1)が作成されていません");
    }

    [Fact]
    public async Task TC_LogMgr_016_LogInfo_MultipleRotations_KeepsMaxFileCount()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object);

        // Act
        // 複数回ローテーションが発生するまで書き込み（約25000メッセージ = 約37.5MB）
        for (int i = 0; i < 25000; i++)
        {
            await manager.LogInfo($"Message {i} {new string('x', 1400)}");
        }
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        Assert.True(File.Exists(LogFilePath), "現在のログファイルが存在しません");
        Assert.True(File.Exists($"{LogFilePath}.1"), "ローテーションファイル(.1)が存在しません");
        Assert.False(File.Exists($"{LogFilePath}.8"), "MAX_LOG_FILE_COUNT=7なので、.8ファイルは存在しないべき");
    }

    [Fact]
    public async Task TC_LogMgr_018_RotateFile_OldFilesExist_RenamesCorrectly()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object);

        // Act
        // 最初のローテーション
        for (int i = 0; i < 7500; i++)
        {
            await manager.LogInfo($"Message batch 1 {new string('x', 1400)} - {i}");
        }
        await Task.Delay(100); // ファイル操作の完了を待つ

        // 2回目のローテーション
        for (int i = 0; i < 7500; i++)
        {
            await manager.LogInfo($"Message batch 2 {new string('x', 1400)} - {i}");
        }
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        Assert.True(File.Exists($"{LogFilePath}.1"), "最新のローテーションファイル(.1)が存在しません");
        Assert.True(File.Exists($"{LogFilePath}.2"), "2世代前のローテーションファイル(.2)が存在しません");
    }

    [Fact]
    public async Task TC_LogMgr_020_LogInfo_RotationInProgress_ThreadSafe()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object);

        // Act
        // 複数スレッドから同時に書き込み
        var tasks = Enumerable.Range(0, 10).Select(async threadId =>
        {
            for (int i = 0; i < 100; i++)
            {
                await manager.LogInfo($"Thread {threadId} message {i} {new string('x', 1400)}");
            }
        });

        await Task.WhenAll(tasks);
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        Assert.True(File.Exists(LogFilePath), "ログファイルが作成されていません");
        // スレッドセーフであれば例外が発生せずに完了する
    }

    [Fact]
    public async Task TC_LogMgr_021_LogInfo_DirectoryNotExists_CreatesDirectory()
    {
        // Arrange
        var logDir = Path.GetDirectoryName(LogFilePath);
        if (!string.IsNullOrEmpty(logDir) && Directory.Exists(logDir))
        {
            // テスト用に一時的にディレクトリを削除
            try
            {
                Directory.Delete(logDir, true);
            }
            catch { }
        }

        var mockLogger = new Mock<ILogger<LoggingManager>>();

        // Act & Assert
        // ディレクトリが存在しない場合、LoggingManagerが自動作成する
        var exception = Record.Exception(() => new LoggingManager(mockLogger.Object));
        Assert.Null(exception); // 例外が発生しないことを確認

        // ディレクトリが作成されたことを確認
        if (!string.IsNullOrEmpty(logDir))
        {
            Assert.True(Directory.Exists(logDir), "ディレクトリが自動作成されていません");
        }
    }

    #endregion

    #region Phase6: EffectivePlcName Tests（4テスト）

    [Fact]
    public void LogPlcProcessStart_PlcNameが設定されている場合_PlcName使用()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var loggingManager = new LoggingManager(mockLogger.Object);
        var plcConfig = new PlcConfiguration
        {
            PlcName = "ライン1-炉A",
            PlcId = "192.168.1.10_8192",
            IpAddress = "192.168.1.10",
            Port = 8192
        };

        // Act
        loggingManager.LogPlcProcessStart(plcConfig);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[ライン1-炉A]")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogPlcProcessStart_PlcNameが空の場合_PlcIdを使用()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var loggingManager = new LoggingManager(mockLogger.Object);
        var plcConfig = new PlcConfiguration
        {
            PlcName = "",
            PlcId = "192.168.1.10_8192",
            IpAddress = "192.168.1.10",
            Port = 8192
        };

        // Act
        loggingManager.LogPlcProcessStart(plcConfig);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[192.168.1.10_8192]")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogPlcProcessComplete_PlcNameが設定されている場合_PlcName使用()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var loggingManager = new LoggingManager(mockLogger.Object);
        var plcConfig = new PlcConfiguration
        {
            PlcName = "ライン1-炉A",
            PlcId = "192.168.1.10_8192",
            IpAddress = "192.168.1.10",
            Port = 8192
        };

        // Act
        loggingManager.LogPlcProcessComplete(plcConfig, 10);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[ライン1-炉A]") && v.ToString()!.Contains("デバイス数=10")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogPlcCommunicationError_PlcNameが設定されている場合_PlcName使用()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var loggingManager = new LoggingManager(mockLogger.Object);
        var plcConfig = new PlcConfiguration
        {
            PlcName = "ライン1-炉A",
            PlcId = "192.168.1.10_8192",
            IpAddress = "192.168.1.10",
            Port = 8192
        };
        var exception = new Exception("通信タイムアウト");

        // Act
        loggingManager.LogPlcCommunicationError(plcConfig, exception);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[ライン1-炉A]")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region ヘルパーメソッド

    /// <summary>
    /// FileShare.ReadWriteを指定してファイルを読み取るヘルパーメソッド
    /// LoggingManagerが書き込み中でもファイルを読み取れるようにする
    /// </summary>
    private static async Task<string> ReadFileWithSharedAccessAsync(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    #endregion
}
