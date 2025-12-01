using Andon.Core.Interfaces;
using Andon.Services;
using Moq;
using Xunit;

namespace Andon.Tests.Unit.Services;

/// <summary>
/// CancellationCoordinator単体テスト
/// </summary>
public class CancellationCoordinatorTests
{
    private readonly Mock<ILoggingManager> _mockLogger;
    private readonly CancellationCoordinator _coordinator;

    public CancellationCoordinatorTests()
    {
        _mockLogger = new Mock<ILoggingManager>();
        _coordinator = new CancellationCoordinator(_mockLogger.Object);
    }

    [Fact]
    public void CreateHierarchicalToken_WithParentOnly_ReturnsLinkedTokenSource()
    {
        // Arrange
        using var parentCts = new CancellationTokenSource();

        // Act
        using var childCts = _coordinator.CreateHierarchicalToken(parentCts.Token);

        // Assert
        Assert.NotNull(childCts);
        Assert.False(childCts.Token.IsCancellationRequested);
    }

    [Fact]
    public void CreateHierarchicalToken_ParentCanceled_ChildIsCanceled()
    {
        // Arrange
        using var parentCts = new CancellationTokenSource();
        using var childCts = _coordinator.CreateHierarchicalToken(parentCts.Token);

        // Act
        parentCts.Cancel();

        // Assert
        Assert.True(childCts.Token.IsCancellationRequested);
    }

    [Fact]
    public void CreateHierarchicalToken_WithTimeout_CancelsAfterTimeout()
    {
        // Arrange
        using var parentCts = new CancellationTokenSource();
        var timeout = TimeSpan.FromMilliseconds(50);

        // Act
        using var childCts = _coordinator.CreateHierarchicalToken(parentCts.Token, timeout);
        Thread.Sleep(100);

        // Assert
        Assert.True(childCts.Token.IsCancellationRequested);
    }

    [Fact]
    public void CreateHierarchicalToken_ChildCanceled_ParentNotAffected()
    {
        // Arrange
        using var parentCts = new CancellationTokenSource();
        using var childCts = _coordinator.CreateHierarchicalToken(parentCts.Token);

        // Act
        childCts.Cancel();

        // Assert
        Assert.True(childCts.Token.IsCancellationRequested);
        Assert.False(parentCts.Token.IsCancellationRequested);
    }

    [Fact]
    public async Task RegisterCancellationCallback_TokenCanceled_CallbackExecuted()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var callbackExecuted = false;
        Func<Task> callback = () =>
        {
            callbackExecuted = true;
            return Task.CompletedTask;
        };

        // Act
        using var registration = _coordinator.RegisterCancellationCallback(cts.Token, callback, "TestCallback");
        cts.Cancel();
        await Task.Delay(50); // Give time for callback to execute

        // Assert
        Assert.True(callbackExecuted);
    }

    [Fact]
    public void RegisterCancellationCallback_TokenNotCanceled_CallbackNotExecuted()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var callbackExecuted = false;
        Func<Task> callback = () =>
        {
            callbackExecuted = true;
            return Task.CompletedTask;
        };

        // Act
        using var registration = _coordinator.RegisterCancellationCallback(cts.Token, callback, "TestCallback");

        // Assert
        Assert.False(callbackExecuted);
    }

    [Fact]
    public async Task RegisterCancellationCallback_MultipleCallbacks_AllExecuted()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var callback1Executed = false;
        var callback2Executed = false;
        
        Func<Task> callback1 = () =>
        {
            callback1Executed = true;
            return Task.CompletedTask;
        };
        
        Func<Task> callback2 = () =>
        {
            callback2Executed = true;
            return Task.CompletedTask;
        };

        // Act
        using var registration1 = _coordinator.RegisterCancellationCallback(cts.Token, callback1, "Callback1");
        using var registration2 = _coordinator.RegisterCancellationCallback(cts.Token, callback2, "Callback2");
        cts.Cancel();
        await Task.Delay(50);

        // Assert
        Assert.True(callback1Executed);
        Assert.True(callback2Executed);
    }

    [Fact]
    public async Task RegisterCancellationCallback_CallbackThrowsException_ExceptionLogged()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        Func<Task> callback = () => throw new InvalidOperationException("Test exception");

        // Act
        using var registration = _coordinator.RegisterCancellationCallback(cts.Token, callback, "FailingCallback");
        cts.Cancel();
        await Task.Delay(50);

        // Assert
        _mockLogger.Verify(
            x => x.LogError(It.IsAny<Exception>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public void RegisterCancellationCallback_Disposed_NoCallbackExecution()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var callbackExecuted = false;
        Func<Task> callback = () =>
        {
            callbackExecuted = true;
            return Task.CompletedTask;
        };

        // Act
        var registration = _coordinator.RegisterCancellationCallback(cts.Token, callback, "TestCallback");
        registration.Dispose(); // Unregister before cancellation
        cts.Cancel();

        // Assert
        Assert.False(callbackExecuted);
    }
}
