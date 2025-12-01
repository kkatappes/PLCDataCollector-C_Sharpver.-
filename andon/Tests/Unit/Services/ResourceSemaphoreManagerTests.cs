using Andon.Core.Models;
using Andon.Services;
using Xunit;

namespace Andon.Tests.Unit.Services;

/// <summary>
/// ResourceSemaphoreManager単体テスト
/// </summary>
public class ResourceSemaphoreManagerTests
{
    private readonly ResourceSemaphoreManager _manager;

    public ResourceSemaphoreManagerTests()
    {
        _manager = new ResourceSemaphoreManager();
    }

    [Fact]
    public void Constructor_InitializesSemaphores()
    {
        // Assert
        Assert.NotNull(_manager.LogFileSemaphore);
        Assert.NotNull(_manager.ConfigFileSemaphore);
        Assert.NotNull(_manager.OutputFileSemaphore);
    }

    [Fact]
    public void LogFileSemaphore_HasCorrectMaxCount()
    {
        // Assert - 同時アクセス数1
        Assert.Equal(1, _manager.LogFileSemaphore.CurrentCount);
    }

    [Fact]
    public void ConfigFileSemaphore_HasCorrectMaxCount()
    {
        // Assert - 同時アクセス数3
        Assert.Equal(3, _manager.ConfigFileSemaphore.CurrentCount);
    }

    [Fact]
    public void OutputFileSemaphore_HasCorrectMaxCount()
    {
        // Assert - 同時アクセス数2
        Assert.Equal(2, _manager.OutputFileSemaphore.CurrentCount);
    }

    [Fact]
    public async Task ExecuteWithSemaphoreAsync_Success_ReturnsResult()
    {
        // Arrange
        var semaphore = new SemaphoreSlim(1, 1);
        var expectedResult = 42;
        Func<Task<int>> operation = () => Task.FromResult(expectedResult);

        // Act
        var result = await _manager.ExecuteWithSemaphoreAsync(semaphore, operation);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(1, semaphore.CurrentCount); // セマフォが解放されている
    }

    [Fact]
    public async Task ExecuteWithSemaphoreAsync_Exception_ReleasesSemaphore()
    {
        // Arrange
        var semaphore = new SemaphoreSlim(1, 1);
        Func<Task<int>> operation = () => throw new InvalidOperationException("Test exception");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _manager.ExecuteWithSemaphoreAsync(semaphore, operation));
        
        Assert.Equal(1, semaphore.CurrentCount); // セマフォが解放されている
    }

    [Fact]
    public async Task ExecuteWithSemaphoreAsync_Cancellation_ReleaseSemaphore()
    {
        // Arrange
        var semaphore = new SemaphoreSlim(1, 1);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        
        Func<Task<int>> operation = async () =>
        {
            await Task.Delay(100, cts.Token);
            return 42;
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _manager.ExecuteWithSemaphoreAsync(semaphore, operation, cts.Token));
        
        Assert.Equal(1, semaphore.CurrentCount); // セマフォが解放されている
    }

    [Fact]
    public async Task ExecuteWithSemaphoreAsync_Timeout_ThrowsException()
    {
        // Arrange
        var semaphore = new SemaphoreSlim(0, 1); // セマフォを取得不可状態に
        var timeout = TimeSpan.FromMilliseconds(50);
        Func<Task<int>> operation = () => Task.FromResult(42);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _manager.ExecuteWithSemaphoreAsync(semaphore, operation, default, timeout));
    }

    [Fact]
    public async Task ExecuteWithSemaphoreAsync_MultipleOperations_EnforcesExclusivity()
    {
        // Arrange
        var semaphore = new SemaphoreSlim(1, 1);
        var counter = 0;
        var tasks = new List<Task<int>>();

        Func<Task<int>> operation = async () =>
        {
            var currentValue = ++counter;
            await Task.Delay(50);
            Assert.Equal(currentValue, counter); // 排他制御確認
            return currentValue;
        };

        // Act
        for (int i = 0; i < 3; i++)
        {
            tasks.Add(_manager.ExecuteWithSemaphoreAsync(semaphore, operation));
        }
        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(3, counter);
        Assert.Equal(1, semaphore.CurrentCount);
    }

    [Fact]
    public void GetResourceSemaphore_LogFile_ReturnsLogFileSemaphore()
    {
        // Act
        var semaphore = _manager.GetResourceSemaphore(ResourceType.LogFile);

        // Assert
        Assert.Same(_manager.LogFileSemaphore, semaphore);
    }

    [Fact]
    public void GetResourceSemaphore_ConfigFile_ReturnsConfigFileSemaphore()
    {
        // Act
        var semaphore = _manager.GetResourceSemaphore(ResourceType.ConfigFile);

        // Assert
        Assert.Same(_manager.ConfigFileSemaphore, semaphore);
    }

    [Fact]
    public void GetResourceSemaphore_OutputFile_ReturnsOutputFileSemaphore()
    {
        // Act
        var semaphore = _manager.GetResourceSemaphore(ResourceType.OutputFile);

        // Assert
        Assert.Same(_manager.OutputFileSemaphore, semaphore);
    }
}
