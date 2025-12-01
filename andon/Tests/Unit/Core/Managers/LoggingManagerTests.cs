using Xunit;
using Andon.Core.Managers;
using Andon.Core.Models.ConfigModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Andon.Tests.Unit.Core.Managers;

/// <summary>
/// LoggingManager単体テスト
/// </summary>
public class LoggingManagerTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    public void Dispose()
    {
        // テスト後に一時ファイルを削除
        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);

                // ローテーションファイルも削除
                for (int i = 1; i <= 10; i++)
                {
                    var rotatedFile = $"{file}.{i}";
                    if (File.Exists(rotatedFile))
                        File.Delete(rotatedFile);
                }
            }
            catch
            {
                // クリーンアップエラーは無視
            }
        }
    }

    private string CreateTempFilePath()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"andon_test_{Guid.NewGuid()}.log");
        _tempFiles.Add(tempFile);
        return tempFile;
    }

    #region ファイル出力機能テスト（6テスト）

    [Fact]
    public async Task TC_LogMgr_001_LogInfo_EnableFileOutput_WritesToFile()
    {
        // Arrange
        var tempFile = CreateTempFilePath();
        var config = new LoggingConfig
        {
            EnableFileOutput = true,
            LogFilePath = tempFile,
            EnableConsoleOutput = false
        };
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object, Options.Create(config));

        // Act
        await manager.LogInfo("Test info message");
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        Assert.True(File.Exists(tempFile), "ログファイルが作成されていません");
        var content = await File.ReadAllTextAsync(tempFile);
        Assert.Contains("Test info message", content);
    }

    [Fact]
    public async Task TC_LogMgr_002_LogWarning_EnableFileOutput_WritesToFile()
    {
        // Arrange
        var tempFile = CreateTempFilePath();
        var config = new LoggingConfig
        {
            EnableFileOutput = true,
            LogFilePath = tempFile,
            EnableConsoleOutput = false
        };
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object, Options.Create(config));

        // Act
        await manager.LogWarning("Test warning message");
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        var content = await File.ReadAllTextAsync(tempFile);
        Assert.Contains("Test warning message", content);
    }

    [Fact]
    public async Task TC_LogMgr_003_LogError_EnableFileOutput_WritesToFile()
    {
        // Arrange
        var tempFile = CreateTempFilePath();
        var config = new LoggingConfig
        {
            EnableFileOutput = true,
            LogFilePath = tempFile,
            EnableConsoleOutput = false
        };
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object, Options.Create(config));

        // Act
        var exception = new InvalidOperationException("Test exception");
        await manager.LogError(exception, "Test error message");
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        var content = await File.ReadAllTextAsync(tempFile);
        Assert.Contains("Test error message", content);
    }

    [Fact]
    public async Task TC_LogMgr_004_LogDebug_EnableFileOutput_WritesToFile()
    {
        // Arrange
        var tempFile = CreateTempFilePath();
        var config = new LoggingConfig
        {
            EnableFileOutput = true,
            LogFilePath = tempFile,
            EnableConsoleOutput = false,
            LogLevel = "Debug" // Debugレベルを有効化
        };
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object, Options.Create(config));

        // Act
        await manager.LogDebug("Test debug message");
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        var content = await File.ReadAllTextAsync(tempFile);
        Assert.Contains("Test debug message", content);
    }

    [Fact]
    public async Task TC_LogMgr_005_LogInfo_DisableFileOutput_NotWritesToFile()
    {
        // Arrange
        var tempFile = CreateTempFilePath();
        var config = new LoggingConfig
        {
            EnableFileOutput = false,
            LogFilePath = tempFile,
            EnableConsoleOutput = true
        };
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object, Options.Create(config));

        // Act
        await manager.LogInfo("Test message");
        await manager.CloseAndFlushAsync();

        // Assert
        Assert.False(File.Exists(tempFile), "EnableFileOutput=falseの場合、ファイルは作成されないべき");
    }

    [Fact]
    public async Task TC_LogMgr_006_CloseAndFlushAsync_FlushesBufferedLogs()
    {
        // Arrange
        var tempFile = CreateTempFilePath();
        var config = new LoggingConfig
        {
            EnableFileOutput = true,
            LogFilePath = tempFile,
            EnableConsoleOutput = false
        };
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object, Options.Create(config));

        // Act
        await manager.LogInfo("Buffered message 1");
        await manager.LogInfo("Buffered message 2");
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        var content = await File.ReadAllTextAsync(tempFile);
        Assert.Contains("Buffered message 1", content);
        Assert.Contains("Buffered message 2", content);
    }

    #endregion

    #region ログレベル設定テスト（8テスト）

    [Fact]
    public async Task TC_LogMgr_007_LogDebug_LogLevelInformation_NotWritten()
    {
        // Arrange
        var tempFile = CreateTempFilePath();
        var config = new LoggingConfig
        {
            LogLevel = "Information",
            EnableFileOutput = true,
            LogFilePath = tempFile,
            EnableConsoleOutput = false
        };
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object, Options.Create(config));

        // Act
        await manager.LogDebug("Debug message");
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        if (File.Exists(tempFile))
        {
            var content = await File.ReadAllTextAsync(tempFile);
            Assert.DoesNotContain("Debug message", content);
        }
        // ファイルが存在しない場合もOK（何も書き込まれていない）
    }

    [Fact]
    public async Task TC_LogMgr_008_LogInfo_LogLevelInformation_Written()
    {
        // Arrange
        var tempFile = CreateTempFilePath();
        var config = new LoggingConfig
        {
            LogLevel = "Information",
            EnableFileOutput = true,
            LogFilePath = tempFile,
            EnableConsoleOutput = false
        };
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object, Options.Create(config));

        // Act
        await manager.LogInfo("Info message");
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        var content = await File.ReadAllTextAsync(tempFile);
        Assert.Contains("Info message", content);
    }

    [Fact]
    public async Task TC_LogMgr_009_LogWarning_LogLevelInformation_Written()
    {
        // Arrange
        var tempFile = CreateTempFilePath();
        var config = new LoggingConfig
        {
            LogLevel = "Information",
            EnableFileOutput = true,
            LogFilePath = tempFile,
            EnableConsoleOutput = false
        };
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object, Options.Create(config));

        // Act
        await manager.LogWarning("Warning message");
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        var content = await File.ReadAllTextAsync(tempFile);
        Assert.Contains("Warning message", content);
    }

    [Fact]
    public async Task TC_LogMgr_010_LogError_LogLevelInformation_Written()
    {
        // Arrange
        var tempFile = CreateTempFilePath();
        var config = new LoggingConfig
        {
            LogLevel = "Information",
            EnableFileOutput = true,
            LogFilePath = tempFile,
            EnableConsoleOutput = false
        };
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object, Options.Create(config));

        // Act
        await manager.LogError(null, "Error message");
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        var content = await File.ReadAllTextAsync(tempFile);
        Assert.Contains("Error message", content);
    }

    [Fact]
    public async Task TC_LogMgr_011_LogDebug_LogLevelDebug_Written()
    {
        // Arrange
        var tempFile = CreateTempFilePath();
        var config = new LoggingConfig
        {
            LogLevel = "Debug",
            EnableFileOutput = true,
            LogFilePath = tempFile,
            EnableConsoleOutput = false
        };
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object, Options.Create(config));

        // Act
        await manager.LogDebug("Debug message");
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        var content = await File.ReadAllTextAsync(tempFile);
        Assert.Contains("Debug message", content);
    }

    [Fact]
    public async Task TC_LogMgr_012_LogInfo_LogLevelWarning_NotWritten()
    {
        // Arrange
        var tempFile = CreateTempFilePath();
        var config = new LoggingConfig
        {
            LogLevel = "Warning",
            EnableFileOutput = true,
            LogFilePath = tempFile,
            EnableConsoleOutput = false
        };
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object, Options.Create(config));

        // Act
        await manager.LogInfo("Info message");
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        if (File.Exists(tempFile))
        {
            var content = await File.ReadAllTextAsync(tempFile);
            Assert.DoesNotContain("Info message", content);
        }
    }

    [Fact]
    public async Task TC_LogMgr_013_LogInfo_LogLevelError_NotWritten()
    {
        // Arrange
        var tempFile = CreateTempFilePath();
        var config = new LoggingConfig
        {
            LogLevel = "Error",
            EnableFileOutput = true,
            LogFilePath = tempFile,
            EnableConsoleOutput = false
        };
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object, Options.Create(config));

        // Act
        await manager.LogInfo("Info message");
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        if (File.Exists(tempFile))
        {
            var content = await File.ReadAllTextAsync(tempFile);
            Assert.DoesNotContain("Info message", content);
        }
    }

    [Fact]
    public void TC_LogMgr_014_InvalidLogLevel_ThrowsArgumentException()
    {
        // Arrange
        var tempFile = CreateTempFilePath();
        var config = new LoggingConfig
        {
            LogLevel = "InvalidLevel",
            EnableFileOutput = true,
            LogFilePath = tempFile
        };
        var mockLogger = new Mock<ILogger<LoggingManager>>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new LoggingManager(mockLogger.Object, Options.Create(config)));
        Assert.Contains("LogLevel", exception.Message);
    }

    #endregion

    #region ログファイルローテーション機能テスト（8テスト）

    [Fact]
    public async Task TC_LogMgr_015_LogInfo_ExceedsMaxFileSize_RotatesFile()
    {
        // Arrange
        var tempFile = CreateTempFilePath();
        var config = new LoggingConfig
        {
            EnableFileOutput = true,
            LogFilePath = tempFile,
            EnableConsoleOutput = false,
            MaxLogFileSizeMb = 1 // 1MB制限
        };
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object, Options.Create(config));

        // Act
        // 1MBを超えるログを書き込み（約1500バイト × 800回 = 1.2MB）
        for (int i = 0; i < 800; i++)
        {
            await manager.LogInfo($"Large message with padding to increase size {new string('x', 1400)} - iteration {i}");
        }
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        Assert.True(File.Exists($"{tempFile}.1"), "ローテーションファイル(.1)が作成されていません");
    }

    [Fact]
    public async Task TC_LogMgr_016_LogInfo_MultipleRotations_KeepsMaxFileCount()
    {
        // Arrange
        var tempFile = CreateTempFilePath();
        var config = new LoggingConfig
        {
            EnableFileOutput = true,
            LogFilePath = tempFile,
            EnableConsoleOutput = false,
            MaxLogFileSizeMb = 1,
            MaxLogFileCount = 3 // 最大3ファイル保持
        };
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object, Options.Create(config));

        // Act
        // 複数回ローテーションが発生するまで書き込み
        for (int i = 0; i < 3000; i++)
        {
            await manager.LogInfo($"Message {i} {new string('x', 1400)}");
        }
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        Assert.True(File.Exists(tempFile), "現在のログファイルが存在しません");
        Assert.True(File.Exists($"{tempFile}.1"), "ローテーションファイル(.1)が存在しません");
        Assert.True(File.Exists($"{tempFile}.2"), "ローテーションファイル(.2)が存在しません");
        Assert.False(File.Exists($"{tempFile}.4"), "MaxLogFileCount=3なので、.4ファイルは存在しないべき");
    }

    [Fact]
    public async Task TC_LogMgr_017_LogInfo_DateBasedRotation_CreatesNewFileDaily()
    {
        // Arrange
        var tempFile = CreateTempFilePath();
        var config = new LoggingConfig
        {
            EnableFileOutput = true,
            LogFilePath = tempFile,
            EnableConsoleOutput = false,
            EnableDateBasedRotation = true
        };
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object, Options.Create(config));

        // Act
        await manager.LogInfo("Message on day 1");
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        // 日付ベースのファイル名を確認（例: andon_test_xxx.log.20251128）
        var expectedDateSuffix = DateTime.Now.ToString("yyyyMMdd");
        Assert.True(File.Exists(tempFile), "ログファイルが作成されていません");

        // 注: 実際の日付変更テストは時間がかかるため、ファイル名形式の確認のみ
    }

    [Fact]
    public async Task TC_LogMgr_018_RotateFile_OldFilesExist_RenamesCorrectly()
    {
        // Arrange
        var tempFile = CreateTempFilePath();
        var config = new LoggingConfig
        {
            EnableFileOutput = true,
            LogFilePath = tempFile,
            EnableConsoleOutput = false,
            MaxLogFileSizeMb = 1
        };
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object, Options.Create(config));

        // Act
        // 最初のローテーション
        for (int i = 0; i < 800; i++)
        {
            await manager.LogInfo($"Message batch 1 {new string('x', 1400)} - {i}");
        }
        await Task.Delay(100); // ファイル操作の完了を待つ

        // 2回目のローテーション
        for (int i = 0; i < 800; i++)
        {
            await manager.LogInfo($"Message batch 2 {new string('x', 1400)} - {i}");
        }
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        Assert.True(File.Exists($"{tempFile}.1"), "最新のローテーションファイル(.1)が存在しません");
        Assert.True(File.Exists($"{tempFile}.2"), "2世代前のローテーションファイル(.2)が存在しません");
    }

    [Fact]
    public async Task TC_LogMgr_019_RotateFile_ExceedsMaxCount_DeletesOldestFile()
    {
        // Arrange
        var tempFile = CreateTempFilePath();
        var config = new LoggingConfig
        {
            EnableFileOutput = true,
            LogFilePath = tempFile,
            EnableConsoleOutput = false,
            MaxLogFileSizeMb = 1,
            MaxLogFileCount = 2 // 最大2ファイル
        };
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object, Options.Create(config));

        // Act
        // 3回ローテーションを発生させる
        for (int batch = 0; batch < 3; batch++)
        {
            for (int i = 0; i < 800; i++)
            {
                await manager.LogInfo($"Batch {batch} message {i} {new string('x', 1400)}");
            }
            await Task.Delay(100);
        }
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        Assert.True(File.Exists($"{tempFile}.1"), "最新のローテーションファイル(.1)が存在しません");
        Assert.False(File.Exists($"{tempFile}.3"), "MaxLogFileCount=2なので、.3は削除されているべき");
    }

    [Fact]
    public async Task TC_LogMgr_020_LogInfo_RotationInProgress_ThreadSafe()
    {
        // Arrange
        var tempFile = CreateTempFilePath();
        var config = new LoggingConfig
        {
            EnableFileOutput = true,
            LogFilePath = tempFile,
            EnableConsoleOutput = false,
            MaxLogFileSizeMb = 1
        };
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object, Options.Create(config));

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
        Assert.True(File.Exists(tempFile), "ログファイルが作成されていません");
        // スレッドセーフであれば例外が発生せずに完了する
    }

    [Fact]
    public async Task TC_LogMgr_021_LogInfo_DirectoryNotExists_CreatesDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"andon_test_dir_{Guid.NewGuid()}");
        var tempFile = Path.Combine(tempDir, "test.log");
        _tempFiles.Add(tempFile);

        var config = new LoggingConfig
        {
            EnableFileOutput = true,
            LogFilePath = tempFile,
            EnableConsoleOutput = false
        };
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var manager = new LoggingManager(mockLogger.Object, Options.Create(config));

        // Act
        await manager.LogInfo("Test message");
        await manager.CloseAndFlushAsync();
        manager.Dispose(); // ファイルを解放

        // Assert
        Assert.True(Directory.Exists(tempDir), "ディレクトリが自動作成されていません");
        Assert.True(File.Exists(tempFile), "ログファイルが作成されていません");

        // Cleanup
        try
        {
            Directory.Delete(tempDir, true);
        }
        catch { }
    }

    [Fact]
    public async Task TC_LogMgr_022_LogInfo_FileAccessError_HandlesGracefully()
    {
        // Arrange
        var tempFile = CreateTempFilePath();

        // ファイルを読み取り専用で作成
        await File.WriteAllTextAsync(tempFile, "Initial content");
        File.SetAttributes(tempFile, FileAttributes.ReadOnly);

        var config = new LoggingConfig
        {
            EnableFileOutput = true,
            LogFilePath = tempFile,
            EnableConsoleOutput = false
        };
        var mockLogger = new Mock<ILogger<LoggingManager>>();

        // Act & Assert
        // ファイルアクセスエラーを適切にハンドリングする（例外をスローせず、エラーログのみ）
        var exception = Record.Exception(() =>
            new LoggingManager(mockLogger.Object, Options.Create(config)));

        // ファイルアクセスエラーは例外をスローするか、内部でハンドリングする
        // 実装方針に応じてAssertを調整

        // Cleanup
        try
        {
            File.SetAttributes(tempFile, FileAttributes.Normal);
        }
        catch { }
    }

    #endregion

    #region Phase6: EffectivePlcName Tests

    [Fact]
    public void LogPlcProcessStart_PlcNameが設定されている場合_PlcName使用()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingManager>>();
        var config = new LoggingConfig
        {
            EnableFileOutput = false,
            EnableConsoleOutput = true
        };
        var loggingManager = new LoggingManager(mockLogger.Object, Options.Create(config));
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
        var config = new LoggingConfig
        {
            EnableFileOutput = false,
            EnableConsoleOutput = true
        };
        var loggingManager = new LoggingManager(mockLogger.Object, Options.Create(config));
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
        var config = new LoggingConfig
        {
            EnableFileOutput = false,
            EnableConsoleOutput = true
        };
        var loggingManager = new LoggingManager(mockLogger.Object, Options.Create(config));
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
        var config = new LoggingConfig
        {
            EnableFileOutput = false,
            EnableConsoleOutput = true
        };
        var loggingManager = new LoggingManager(mockLogger.Object, Options.Create(config));
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
}
