using Xunit;
using Moq;

namespace Andon.Tests.Unit.Services;

/// <summary>
/// GracefulShutdownHandler単体テスト
/// </summary>
public class GracefulShutdownHandlerTests
{
    /// <summary>
    /// ExecuteGracefulShutdown正常系テスト
    /// </summary>
    [Fact]
    public async Task ExecuteGracefulShutdown_正常終了時はSuccessTrue()
    {
        // Arrange
        var mockLogger = new Mock<Andon.Core.Interfaces.ILoggingManager>();
        mockLogger.Setup(m => m.LogInfo(It.IsAny<string>())).Returns(Task.CompletedTask);

        var mockController = new Mock<Andon.Core.Interfaces.IApplicationController>();
        mockController.Setup(c => c.StopAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new Andon.Services.GracefulShutdownHandler(mockLogger.Object);

        // Act
        var result = await handler.ExecuteGracefulShutdown(mockController.Object);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        mockController.Verify(c => c.StopAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    /// <summary>
    /// ExecuteGracefulShutdown異常系テスト - StopAsync例外
    /// </summary>
    [Fact]
    public async Task ExecuteGracefulShutdown_StopAsync例外時はSuccessFalse()
    {
        // Arrange
        var mockLogger = new Mock<Andon.Core.Interfaces.ILoggingManager>();
        mockLogger.Setup(m => m.LogInfo(It.IsAny<string>())).Returns(Task.CompletedTask);
        mockLogger.Setup(m => m.LogError(It.IsAny<Exception>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var mockController = new Mock<Andon.Core.Interfaces.IApplicationController>();
        mockController.Setup(c => c.StopAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        var handler = new Andon.Services.GracefulShutdownHandler(mockLogger.Object);

        // Act
        var result = await handler.ExecuteGracefulShutdown(mockController.Object);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Test exception", result.ErrorMessage);
    }

    /// <summary>
    /// ExecuteGracefulShutdown - タイムアウト指定テスト
    /// </summary>
    [Fact]
    public async Task ExecuteGracefulShutdown_タイムアウト指定可能()
    {
        // Arrange
        var mockLogger = new Mock<Andon.Core.Interfaces.ILoggingManager>();
        mockLogger.Setup(m => m.LogInfo(It.IsAny<string>())).Returns(Task.CompletedTask);

        var mockController = new Mock<Andon.Core.Interfaces.IApplicationController>();
        mockController.Setup(c => c.StopAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new Andon.Services.GracefulShutdownHandler(mockLogger.Object);

        // Act
        var result = await handler.ExecuteGracefulShutdown(mockController.Object, TimeSpan.FromSeconds(10));

        // Assert
        Assert.True(result.Success);
    }
}
