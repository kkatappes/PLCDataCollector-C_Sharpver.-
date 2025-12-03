using Xunit;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Constants;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Andon.Tests.Unit.Core.Managers;

/// <summary>
/// LoggingManagerクラスのテスト（Phase7実装: データ取得ログ記録）
/// TDD Red Phase: 失敗するテストを先に作成
/// </summary>
[Collection("LoggingManagerTests")]
public class LoggingManagerTests_Phase7
{
    private readonly TestLogger<LoggingManager> _logger;
    private readonly LoggingManager _manager;

    public LoggingManagerTests_Phase7()
    {
        _logger = new TestLogger<LoggingManager>();
        _manager = new LoggingManager(_logger);
    }

    [Fact]
    public void LogDataAcquisition_WithFewDevices_LogsAllDeviceNames()
    {
        // Arrange - 5デバイス以下の場合、全デバイス名をログ記録
        var deviceData = new Dictionary<string, DeviceData>
        {
            { "D100", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 100, false), 256) },
            { "D105", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 105, false), 512) },
            { "M200", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.M, 200, false), 1) }
        };

        var data = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = DateTime.Now
        };

        // Act
        _manager.LogDataAcquisition(data);

        // Assert - 3点のデバイス名が全て記録される
        Assert.Single(_logger.LogEntries);
        var logEntry = _logger.LogEntries[0];
        Assert.Equal(LogLevel.Information, logEntry.LogLevel);
        Assert.Contains("[ReadRandom]", logEntry.Message);
        Assert.Contains("3点取得", logEntry.Message);
        Assert.Contains("D100", logEntry.Message);
        Assert.Contains("D105", logEntry.Message);
        Assert.Contains("M200", logEntry.Message);
    }

    [Fact]
    public void LogDataAcquisition_WithManyDevices_LogsFirstFiveAndTotal()
    {
        // Arrange - 6デバイス以上の場合、最初の5デバイスと総数をログ記録
        var deviceData = new Dictionary<string, DeviceData>
        {
            { "D100", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 100, false), 100) },
            { "D101", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 101, false), 101) },
            { "D102", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 102, false), 102) },
            { "D103", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 103, false), 103) },
            { "D104", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 104, false), 104) },
            { "D105", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 105, false), 105) },
            { "D106", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 106, false), 106) }
        };

        var data = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = DateTime.Now
        };

        // Act
        _manager.LogDataAcquisition(data);

        // Assert - 7点中、最初の5点のみ記録、残り2点を示す
        Assert.Single(_logger.LogEntries);
        var logEntry = _logger.LogEntries[0];
        Assert.Equal(LogLevel.Information, logEntry.LogLevel);
        Assert.Contains("[ReadRandom]", logEntry.Message);
        Assert.Contains("7点取得", logEntry.Message);
        Assert.Contains("D100", logEntry.Message);
        Assert.Contains("D101", logEntry.Message);
        Assert.Contains("D102", logEntry.Message);
        Assert.Contains("D103", logEntry.Message);
        Assert.Contains("D104", logEntry.Message);
        Assert.DoesNotContain("D105", logEntry.Message);
        Assert.DoesNotContain("D106", logEntry.Message);
        Assert.Contains("（他2点）", logEntry.Message);
    }

    [Fact]
    public void LogDataAcquisition_EmptyData_LogsZeroDevices()
    {
        // Arrange - デバイスなしの場合
        var data = new ProcessedResponseData
        {
            ProcessedData = new Dictionary<string, DeviceData>(),
            ProcessedAt = DateTime.Now
        };

        // Act
        _manager.LogDataAcquisition(data);

        // Assert
        Assert.Single(_logger.LogEntries);
        var logEntry = _logger.LogEntries[0];
        Assert.Equal(LogLevel.Information, logEntry.LogLevel);
        Assert.Contains("[ReadRandom]", logEntry.Message);
        Assert.Contains("0点取得", logEntry.Message);
    }

    [Fact]
    public void LogFrameSent_ReadRandomFrame_LogsCorrectly()
    {
        // Arrange
        byte[] frame = new byte[213];
        string commandType = "ReadRandom";

        // Act
        _manager.LogFrameSent(frame, commandType);

        // Assert
        Assert.Single(_logger.LogEntries);
        var logEntry = _logger.LogEntries[0];
        Assert.Equal(LogLevel.Debug, logEntry.LogLevel);
        Assert.Contains("[送信]", logEntry.Message);
        Assert.Contains("ReadRandomフレーム", logEntry.Message);
        Assert.Contains("213バイト", logEntry.Message);
    }

    [Fact]
    public void LogResponseReceived_WithResponse_LogsCorrectly()
    {
        // Arrange
        byte[] response = new byte[111];

        // Act
        _manager.LogResponseReceived(response);

        // Assert
        Assert.Single(_logger.LogEntries);
        var logEntry = _logger.LogEntries[0];
        Assert.Equal(LogLevel.Debug, logEntry.LogLevel);
        Assert.Contains("[受信]", logEntry.Message);
        Assert.Contains("レスポンス", logEntry.Message);
        Assert.Contains("111バイト", logEntry.Message);
    }

    [Fact]
    public void LogError_WithException_LogsCorrectly()
    {
        // Arrange
        var exception = new InvalidOperationException("テストエラー");
        string context = "データ処理中";

        // Act
        _manager.LogError(exception, context);

        // Assert
        Assert.Single(_logger.LogEntries);
        var logEntry = _logger.LogEntries[0];
        Assert.Equal(LogLevel.Error, logEntry.LogLevel);
        // ILogger標準動作では"[エラー]"プレフィックスは付かない
        Assert.Contains("データ処理中", logEntry.Message);
        // 例外メッセージは通常ILoggerが自動的にフォーマット
        Assert.NotNull(logEntry.Exception);
        Assert.IsType<InvalidOperationException>(logEntry.Exception);
    }
}

/// <summary>
/// テスト用のロガー実装
/// </summary>
public class TestLogger<T> : ILogger<T>
{
    public List<LogEntry> LogEntries { get; } = new List<LogEntry>();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        LogEntries.Add(new LogEntry
        {
            LogLevel = logLevel,
            Message = formatter(state, exception),
            Exception = exception
        });
    }
}

/// <summary>
/// ログエントリ
/// </summary>
public class LogEntry
{
    public LogLevel LogLevel { get; set; }
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
}
