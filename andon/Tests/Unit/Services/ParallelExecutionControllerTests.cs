using Andon.Core.Interfaces;
using Andon.Core.Models;
using Andon.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Andon.Tests.Unit.Services;

/// <summary>
/// ParallelExecutionController単体テスト
/// </summary>
public class ParallelExecutionControllerTests
{
    private readonly Mock<ILogger<ParallelExecutionController>> _mockLogger;
    private readonly ParallelExecutionController _controller;

    public ParallelExecutionControllerTests()
    {
        _mockLogger = new Mock<ILogger<ParallelExecutionController>>();
        _controller = new ParallelExecutionController(_mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteParallelPlcOperationsAsync_MultipleManagers_ExecutesAllInParallel()
    {
        // Arrange
        var managers = new List<TestConfigManager>
        {
            new TestConfigManager { PlcId = "PLC1" },
            new TestConfigManager { PlcId = "PLC2" },
            new TestConfigManager { PlcId = "PLC3" }
        };

        Func<TestConfigManager, CancellationToken, Task<CycleExecutionResult>> executeAsync = async (mgr, ct) =>
        {
            await Task.Delay(50, ct);
            return new CycleExecutionResult
            {
                IsSuccess = true,
                CompletedAt = DateTime.Now
            };
        };

        // Act
        var result = await _controller.ExecuteParallelPlcOperationsAsync(
            managers,
            executeAsync,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalPlcCount);
        Assert.Equal(3, result.SuccessfulPlcCount);
        Assert.Equal(0, result.FailedPlcCount);
        Assert.True(result.IsOverallSuccess);
        Assert.Equal(3, result.PlcResults.Count);
    }

    [Fact]
    public async Task ExecuteParallelPlcOperationsAsync_PartialFailure_ReturnsCorrectCounts()
    {
        // Arrange
        var managers = new List<TestConfigManager>
        {
            new TestConfigManager { PlcId = "PLC1" },
            new TestConfigManager { PlcId = "PLC2" },
            new TestConfigManager { PlcId = "PLC3" }
        };

        Func<TestConfigManager, CancellationToken, Task<CycleExecutionResult>> executeAsync = async (mgr, ct) =>
        {
            await Task.Delay(50, ct);
            return new CycleExecutionResult
            {
                IsSuccess = mgr.PlcId != "PLC2", // PLC2 fails
                CompletedAt = DateTime.Now,
                ErrorMessage = mgr.PlcId == "PLC2" ? "Connection error" : null
            };
        };

        // Act
        var result = await _controller.ExecuteParallelPlcOperationsAsync(
            managers,
            executeAsync,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalPlcCount);
        Assert.Equal(2, result.SuccessfulPlcCount);
        Assert.Equal(1, result.FailedPlcCount);
        Assert.False(result.IsOverallSuccess);
    }

    [Fact]
    public async Task ExecuteParallelPlcOperationsAsync_EmptyList_ReturnsEmptyResult()
    {
        // Arrange
        var managers = new List<TestConfigManager>();
        Func<TestConfigManager, CancellationToken, Task<CycleExecutionResult>> executeAsync =
            (mgr, ct) => Task.FromResult(new CycleExecutionResult());

        // Act
        var result = await _controller.ExecuteParallelPlcOperationsAsync(
            managers,
            executeAsync,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalPlcCount);
        Assert.Equal(0, result.SuccessfulPlcCount);
        Assert.Equal(0, result.FailedPlcCount);
    }

    [Fact]
    public async Task ExecuteParallelPlcOperationsAsync_Cancellation_PropagatesCancellation()
    {
        // Arrange
        var managers = new List<TestConfigManager>
        {
            new TestConfigManager { PlcId = "PLC1" }
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<TestConfigManager, CancellationToken, Task<CycleExecutionResult>> executeAsync =
            async (mgr, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(100, ct);
                return new CycleExecutionResult();
            };

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _controller.ExecuteParallelPlcOperationsAsync(
                managers,
                executeAsync,
                cts.Token));
    }

    [Fact]
    public async Task MonitorParallelExecutionAsync_WithProgress_ReportsProgress()
    {
        // Arrange
        var tasks = new List<Task<CycleExecutionResult>>
        {
            Task.Run(async () =>
            {
                await Task.Delay(100);
                return new CycleExecutionResult { IsSuccess = true };
            }),
            Task.Run(async () =>
            {
                await Task.Delay(200);
                return new CycleExecutionResult { IsSuccess = true };
            })
        };

        var progressReports = new List<ParallelProgressInfo>();
        var progress = new Progress<ParallelProgressInfo>(info => progressReports.Add(info));

        // Act
        await _controller.MonitorParallelExecutionAsync(
            tasks,
            progress,
            CancellationToken.None);

        // Assert
        Assert.NotEmpty(progressReports);
    }

    [Fact]
    public async Task MonitorParallelExecutionAsync_NoProgress_CompletesWithoutError()
    {
        // Arrange
        var tasks = new List<Task<CycleExecutionResult>>
        {
            Task.FromResult(new CycleExecutionResult { IsSuccess = true })
        };

        // Act & Assert (should not throw)
        await _controller.MonitorParallelExecutionAsync(
            tasks,
            null,
            CancellationToken.None);
    }

    // Helper class for testing
    private class TestConfigManager
    {
        public string PlcId { get; set; } = string.Empty;
    }
}
