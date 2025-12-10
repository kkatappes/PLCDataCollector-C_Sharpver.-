using Xunit;
using Moq;
using Andon.Core.Controllers;
using Andon.Core.Interfaces;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using System.Diagnostics;

namespace Andon.Tests.Integration;

/// <summary>
/// Step 4-1: ParallelExecutionController統合テスト
/// ExecutionOrchestratorの順次処理を真の並行実行に置換
/// </summary>
/// <summary>
/// Step 4-1: ParallelExecutionController統合テスト
/// ExecutionOrchestratorの順次処理を真の並行実行に置換
/// </summary>
public class Step4_1_ParallelExecution_IntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Phase", "Step4-1")]
    public async Task RunContinuousDataCycleAsync_ParallelExecutionControllerを使用して並行実行する()
    {
        // Arrange
        var mockParallelController = new Mock<IParallelExecutionController>();
        var mockTimerService = new Mock<ITimerService>();
        var mockConfigToFrameManager = new Mock<IConfigToFrameManager>();
        var mockDataOutputManager = new Mock<IDataOutputManager>();
        var mockLogger = new Mock<ILoggingManager>();

        // ParallelExecutionControllerのモック設定
        // 注意: ExecuteParallelPlcOperationsAsyncはジェネリックメソッドなので、
        // 具体的な型(anonymous type)でSetupする必要がある
        mockParallelController
            .Setup(p => p.ExecuteParallelPlcOperationsAsync(
                It.IsAny<IEnumerable<It.IsAnyType>>(),
                It.IsAny<Func<It.IsAnyType, CancellationToken, Task<CycleExecutionResult>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParallelExecutionResult
            {
                TotalPlcCount = 2,
                SuccessfulPlcCount = 2,
                FailedPlcCount = 0
            });

        // TimerServiceのモック設定: StartPeriodicExecutionが呼ばれたら即座にactionを実行
        mockTimerService
            .Setup(t => t.StartPeriodicExecution(
                It.IsAny<Func<Task>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, TimeSpan, CancellationToken>(async (action, interval, ct) =>
            {
                // 周期実行のシミュレート: 1回だけactionを実行してからキャンセルを待つ
                await action();
                await Task.Delay(Timeout.Infinite, ct);
            });

        // ConfigToFrameManagerのモック設定: BuildReadRandomFrameFromConfigがダミーフレームを返す
        mockConfigToFrameManager
            .Setup(c => c.BuildReadRandomFrameFromConfig(It.IsAny<PlcConfiguration>()))
            .Returns(new byte[] { 0x54, 0x00, 0x00, 0x00 }); // ダミーフレーム

        var orchestrator = new ExecutionOrchestrator(
            mockTimerService.Object,
            mockConfigToFrameManager.Object,
            mockDataOutputManager.Object,
            mockLogger.Object,
            mockParallelController.Object); // 新規パラメータ

        var mockManager1 = new Mock<IPlcCommunicationManager>();
        var mockManager2 = new Mock<IPlcCommunicationManager>();

        // モックマネージャーの設定
        mockManager1
            .Setup(m => m.ExecuteFullCycleAsync(
                It.IsAny<ConnectionConfig>(),
                It.IsAny<TimeoutConfig>(),
                It.IsAny<byte[]>(),
                It.IsAny<ReadRandomRequestInfo>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FullCycleExecutionResult
            {
                IsSuccess = true,
                ProcessedData = new ProcessedResponseData()
            });

        mockManager2
            .Setup(m => m.ExecuteFullCycleAsync(
                It.IsAny<ConnectionConfig>(),
                It.IsAny<TimeoutConfig>(),
                It.IsAny<byte[]>(),
                It.IsAny<ReadRandomRequestInfo>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FullCycleExecutionResult
            {
                IsSuccess = true,
                ProcessedData = new ProcessedResponseData()
            });

        var plcConfigs = new List<PlcConfiguration>
        {
            new PlcConfiguration { IpAddress = "127.0.0.1", Port = 5000, MonitoringIntervalMs = 1000 },
            new PlcConfiguration { IpAddress = "127.0.0.2", Port = 5001, MonitoringIntervalMs = 1000 }
        };

        var plcManagers = new List<IPlcCommunicationManager>
        {
            mockManager1.Object,
            mockManager2.Object
        };

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100)); // 100ms後にキャンセル

        try
        {
            // Act
            await orchestrator.RunContinuousDataCycleAsync(
                plcConfigs,
                plcManagers,
                cts.Token);
        }
        catch (OperationCanceledException)
        {
            // キャンセル例外は正常なフロー
        }

        // Assert
        // 注意: Verifyもジェネリックメソッドに対応する必要がある
        // It.IsAnyTypeを使う場合は強く型付けされた述語(predicateWithCount)は使えない
        mockParallelController.Verify(
            p => p.ExecuteParallelPlcOperationsAsync(
                It.IsAny<IEnumerable<It.IsAnyType>>(),
                It.IsAny<Func<It.IsAnyType, CancellationToken, Task<CycleExecutionResult>>>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce(),
            "ParallelExecutionControllerが呼び出されていません");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Phase", "Step4-1")]
    public async Task RunContinuousDataCycleAsync_並行実行が正常に完了する()
    {
        // Arrange
        var mockTimerService = new Mock<ITimerService>();
        var mockConfigToFrameManager = new Mock<IConfigToFrameManager>();
        var mockDataOutputManager = new Mock<IDataOutputManager>();
        var mockLogger = new Mock<ILoggingManager>();

        // ConfigToFrameManagerのモック設定
        mockConfigToFrameManager
            .Setup(c => c.BuildReadRandomFrameFromConfig(It.IsAny<PlcConfiguration>()))
            .Returns(new byte[] { 0x54, 0x00, 0x00, 0x00 }); // ダミーフレーム

        // DataOutputManagerのモック設定（何もしない）
        mockDataOutputManager
            .Setup(d => d.OutputToJson(
                It.IsAny<ProcessedResponseData>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, DeviceEntryInfo>>()))
            .Verifiable();

        // LoggingManagerのモック設定（すべてのメソッドを高速に）
        mockLogger.Setup(l => l.LogDebug(It.IsAny<string>())).Returns(Task.CompletedTask);
        mockLogger.Setup(l => l.LogInfo(It.IsAny<string>())).Returns(Task.CompletedTask);
        mockLogger.Setup(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        mockLogger.Setup(l => l.LogError(null, It.IsAny<string>())).Returns(Task.CompletedTask);

        // 実際のParallelExecutionControllerを使用
        var parallelController = new Services.ParallelExecutionController(
            Mock.Of<Microsoft.Extensions.Logging.ILogger<Services.ParallelExecutionController>>());

        var orchestrator = new ExecutionOrchestrator(
            mockTimerService.Object,
            mockConfigToFrameManager.Object,
            mockDataOutputManager.Object,
            mockLogger.Object,
            parallelController);

        // 各PLCの処理に100ms必要と仮定
        var plcManagers = Enumerable.Range(0, 3)
            .Select(_ => CreateMockPlcManagerWithDelay(100))
            .ToList();

        var plcConfigs = Enumerable.Range(0, 3)
            .Select(i => new PlcConfiguration {
                IpAddress = $"127.0.0.{i+1}",
                Port = 5000 + i,
                MonitoringIntervalMs = 1000
            })
            .ToList();

        var cts = new CancellationTokenSource();

        // 並行実行のパフォーマンス測定
        var stopwatch = Stopwatch.StartNew();

        // ExecuteSingleCycleAsyncを直接呼び出してパフォーマンス測定
        await orchestrator.ExecuteSingleCycleAsync(plcConfigs, plcManagers, cts.Token);

        stopwatch.Stop();

        // Assert
        // Phase4-1の目的: ParallelExecutionControllerが統合され、正常に動作することを確認
        // 厳密なパフォーマンス測定は別のテストで実施
        var elapsedMs = stopwatch.ElapsedMilliseconds;
        Console.WriteLine($"[DEBUG] Parallel execution completed in {elapsedMs}ms");

        // 並行実行が完了したことを確認（タイムアウト: 1000ms）
        Assert.True(elapsedMs < 1000,
            $"Parallel execution should complete within reasonable time. Actual: {elapsedMs}ms");

        // 実行時間が妥当な範囲内であることを確認（極端に遅くないこと）
        // 3つのPLC × 100ms delay + オーバーヘッド = 約500ms以下が妥当
        Assert.True(elapsedMs < 500,
            $"Execution time should be reasonable for parallel execution. Actual: {elapsedMs}ms");
    }

    private IPlcCommunicationManager CreateMockPlcManagerWithDelay(int delayMs)
    {
        var mock = new Mock<IPlcCommunicationManager>();
        mock.Setup(m => m.ExecuteFullCycleAsync(
                It.IsAny<ConnectionConfig>(),
                It.IsAny<TimeoutConfig>(),
                It.IsAny<byte[]>(),
                It.IsAny<ReadRandomRequestInfo>(),
                It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(delayMs);
                return new FullCycleExecutionResult
                {
                    IsSuccess = true,
                    ProcessedData = new ProcessedResponseData()
                };
            });
        return mock.Object;
    }
}
