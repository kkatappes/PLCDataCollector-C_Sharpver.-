using Xunit;
using Moq;
using Andon.Core.Interfaces;
using Andon.Services;

namespace Andon.Tests.Unit.Services;

/// <summary>
/// AndonHostedService単体テスト
/// </summary>
public class AndonHostedServiceTests
{
    [Fact]
    public async Task StartAsync_ApplicationControllerを呼び出す()
    {
        // Arrange
        var mockController = new Mock<IApplicationController>();
        var mockLogger = new Mock<ILoggingManager>();
        var service = new AndonHostedService(mockController.Object, mockLogger.Object);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        mockLogger.Verify(m => m.LogInfo("AndonHostedService starting"), Times.Once());
    }

    [Fact]
    public async Task ExecuteAsync_ApplicationControllerのStartAsyncを呼び出す()
    {
        // Arrange
        var mockController = new Mock<IApplicationController>();
        var mockLogger = new Mock<ILoggingManager>();
        var service = new AndonHostedService(mockController.Object, mockLogger.Object);
        var cts = new CancellationTokenSource();

        // Act
        var executeTask = service.StartAsync(cts.Token);
        cts.CancelAfter(100); // 短時間で停止

        try
        {
            await executeTask;
        }
        catch (OperationCanceledException)
        {
            // キャンセル例外は期待される
        }

        // Assert
        mockController.Verify(
            c => c.StartAsync(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());
    }

    [Fact]
    public async Task StopAsync_ApplicationControllerのStopAsyncを呼び出す()
    {
        // Arrange
        var mockController = new Mock<IApplicationController>();
        var mockLogger = new Mock<ILoggingManager>();
        var service = new AndonHostedService(mockController.Object, mockLogger.Object);

        // Act
        await service.StopAsync(CancellationToken.None);

        // Assert
        mockLogger.Verify(m => m.LogInfo("AndonHostedService stopping"), Times.Once());
        mockController.Verify(
            c => c.StopAsync(It.IsAny<CancellationToken>()),
            Times.Once());
    }
}
