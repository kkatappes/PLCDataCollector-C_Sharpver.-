using Andon.Core.Interfaces;
using Andon.Core.Models;
using Andon.Services;
using Moq;
using Xunit;

namespace Andon.Tests.Unit.Services;

/// <summary>
/// AsyncExceptionHandler単体テスト
/// </summary>
public class AsyncExceptionHandlerTests
{
    private readonly Mock<ILoggingManager> _mockLogger;
    private readonly Mock<IErrorHandler> _mockErrorHandler;
    private readonly AsyncExceptionHandler _handler;

    public AsyncExceptionHandlerTests()
    {
        _mockLogger = new Mock<ILoggingManager>();
        _mockErrorHandler = new Mock<IErrorHandler>();
        _handler = new AsyncExceptionHandler(_mockLogger.Object, _mockErrorHandler.Object);
    }

    [Fact]
    public async Task HandleCriticalOperationAsync_Success_ReturnsSuccessResult()
    {
        // Arrange
        var expectedValue = 42;
        Func<Task<int>> operation = () => Task.FromResult(expectedValue);

        // Act
        var result = await _handler.HandleCriticalOperationAsync(operation, "TestOperation");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedValue, result.Data);
        Assert.Null(result.Exception);
        Assert.Null(result.FailedStep);
    }

    [Fact]
    public async Task HandleCriticalOperationAsync_Exception_ReturnsFailureResult()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test error");
        Func<Task<int>> operation = () => throw expectedException;

        // Act
        var result = await _handler.HandleCriticalOperationAsync(operation, "TestOperation");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(default(int), result.Data);
        Assert.NotNull(result.Exception);
        Assert.IsType<InvalidOperationException>(result.Exception);
    }

    [Fact]
    public async Task HandleCriticalOperationAsync_Cancellation_ReturnsCanceledResult()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        Func<Task<int>> operation = async () =>
        {
            await Task.Delay(100, cts.Token);
            return 42;
        };

        // Act
        var result = await _handler.HandleCriticalOperationAsync(operation, "TestOperation", cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Exception);
        Assert.IsAssignableFrom<OperationCanceledException>(result.Exception);
    }

    [Fact]
    public async Task HandleCriticalOperationAsync_LogsErrorOnException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test error");
        Func<Task<int>> operation = () => throw expectedException;

        // Act
        await _handler.HandleCriticalOperationAsync(operation, "TestOperation");

        // Assert
        _mockLogger.Verify(
            x => x.LogError(It.IsAny<Exception>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleGeneralOperationsAsync_AllSuccess_ReturnsAllSuccessResult()
    {
        // Arrange
        var operations = new List<Func<Task>>
        {
            () => Task.CompletedTask,
            () => Task.CompletedTask,
            () => Task.CompletedTask
        };

        // Act
        var result = await _handler.HandleGeneralOperationsAsync(operations, "TestGroup");

        // Assert
        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.Empty(result.FailedOperations);
        Assert.Empty(result.Exceptions);
    }

    [Fact]
    public async Task HandleGeneralOperationsAsync_SomeFailures_ContinuesExecution()
    {
        // Arrange
        var operations = new List<Func<Task>>
        {
            () => Task.CompletedTask,
            () => throw new InvalidOperationException("Error 1"),
            () => Task.CompletedTask,
            () => throw new ArgumentException("Error 2")
        };

        // Act
        var result = await _handler.HandleGeneralOperationsAsync(operations, "TestGroup");

        // Assert
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(2, result.FailureCount);
        Assert.Equal(2, result.FailedOperations.Count);
        Assert.Equal(2, result.Exceptions.Count);
    }

    [Fact]
    public async Task HandleGeneralOperationsAsync_EmptyOperations_ReturnsZeroResult()
    {
        // Arrange
        var operations = new List<Func<Task>>();

        // Act
        var result = await _handler.HandleGeneralOperationsAsync(operations, "TestGroup");

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.Empty(result.FailedOperations);
        Assert.Empty(result.Exceptions);
    }

    [Fact]
    public async Task HandleGeneralOperationsAsync_MeasuresTotalExecutionTime()
    {
        // Arrange
        var operations = new List<Func<Task>>
        {
            async () => await Task.Delay(100),
            async () => await Task.Delay(100)
        };

        // Act
        var result = await _handler.HandleGeneralOperationsAsync(operations, "TestGroup");

        // Assert
        Assert.True(result.TotalExecutionTime.TotalMilliseconds > 0);
    }
}
