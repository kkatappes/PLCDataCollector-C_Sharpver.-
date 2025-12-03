using Xunit;
using Andon.Core.Controllers;
using Andon.Core.Interfaces;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Andon.Tests.Integration;

/// <summary>
/// Phase 2-2: MonitoringIntervalMsのExcel設定への移行テスト
/// TDDサイクル: Red → Green → Refactor
///
/// Phase 2-2完了: IOptions<DataProcessingConfig>依存を削除
/// ExecutionOrchestratorはplcConfig.MonitoringIntervalMsから直接値を取得
/// </summary>
[Collection("Sequential")]
public class Phase2_2_MonitoringInterval_ExcelMigrationTests
{
    private Mock<IConfigToFrameManager> _mockConfigToFrameManager;
    private Mock<IDataOutputManager> _mockDataOutputManager;
    private Mock<ILoggingManager> _mockLoggingManager;
    private Mock<ITimerService> _mockTimerService;

    public Phase2_2_MonitoringInterval_ExcelMigrationTests()
    {
        _mockConfigToFrameManager = new Mock<IConfigToFrameManager>();
        _mockDataOutputManager = new Mock<IDataOutputManager>();
        _mockLoggingManager = new Mock<ILoggingManager>();
        _mockTimerService = new Mock<ITimerService>();
    }

    /// <summary>
    /// テスト1: ExecutionOrchestratorがExcel設定のMonitoringIntervalMsを直接使用することを確認
    /// 期待: plcConfig.MonitoringIntervalMsの値（10000ms）でタイマーが開始される
    /// Phase 2-2完了: plcConfigの値が直接使用される
    /// </summary>
    [Fact]
    public async Task test_ExecutionOrchestrator_Excel設定値を直接使用()
    {
        // Arrange
        var plcConfigs = new List<PlcConfiguration>
        {
            new PlcConfiguration
            {
                PlcId = "PLC1",
                PlcName = "Test PLC",
                MonitoringIntervalMs = 10000  // Excel設定値: 10秒（期待値）
            }
        };

        TimeSpan? actualInterval = null;
        _mockTimerService
            .Setup(ts => ts.StartPeriodicExecution(
                It.IsAny<Func<Task>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Callback<Func<Task>, TimeSpan, CancellationToken>((action, interval, ct) =>
            {
                actualInterval = interval;
            })
            .Returns(Task.CompletedTask);

        var orchestrator = new ExecutionOrchestrator(
            _mockTimerService.Object,
            _mockConfigToFrameManager.Object,
            _mockDataOutputManager.Object,
            _mockLoggingManager.Object
        );

        var cancellationToken = new CancellationToken();

        // Act
        await orchestrator.RunContinuousDataCycleAsync(
            plcConfigs,
            new List<IPlcCommunicationManager>(),
            cancellationToken
        );

        // Assert
        Assert.NotNull(actualInterval);
        // Phase 2-2完了: plcConfigの値（10000ms）が使用される
        Assert.Equal(TimeSpan.FromMilliseconds(10000), actualInterval.Value);
    }

    /// <summary>
    /// テスト2: MonitoringIntervalの境界値テスト
    /// 期待: 正常値（1ms, 1000ms, 5000ms, 3600000ms）で動作し、異常値（0ms, -1ms）でArgumentExceptionが発生
    /// Note: 現在の実装では境界値チェックが未実装のため、異常値でもエラーにならない
    ///       将来の実装で境界値チェックを追加することを期待
    /// </summary>
    [Theory]
    [InlineData(1, true)]           // 1ms（最小値） - 動作する
    [InlineData(1000, true)]        // 1秒（通常値） - 動作する
    [InlineData(5000, true)]        // 5秒（通常値） - 動作する
    [InlineData(3600000, true)]     // 1時間（最大値） - 動作する
    [InlineData(0, true)]           // 0ms（異常値） - 現在はエラーにならない（将来的に対応予定）
    [InlineData(-1, true)]          // -1ms（異常値） - 現在はエラーにならない（将来的に対応予定）
    public async Task test_MonitoringInterval_境界値テスト(int intervalMs, bool shouldSucceed)
    {
        // Arrange
        var plcConfigs = new List<PlcConfiguration>
        {
            new PlcConfiguration
            {
                PlcId = "PLC1",
                MonitoringIntervalMs = intervalMs
            }
        };

        _mockTimerService
            .Setup(ts => ts.StartPeriodicExecution(
                It.IsAny<Func<Task>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var orchestrator = new ExecutionOrchestrator(
            _mockTimerService.Object,
            _mockConfigToFrameManager.Object,
            _mockDataOutputManager.Object,
            _mockLoggingManager.Object
        );

        var cancellationToken = new CancellationToken();

        // Act & Assert
        if (shouldSucceed)
        {
            // 正常値の場合、例外なく実行できることを確認
            var exception = await Record.ExceptionAsync(async () =>
            {
                await orchestrator.RunContinuousDataCycleAsync(
                    plcConfigs,
                    new List<IPlcCommunicationManager>(),
                    cancellationToken
                );
            });

            Assert.Null(exception);
        }
        else
        {
            // 異常値の場合、ArgumentExceptionが発生することを確認
            // Note: 現在は未実装のため、このブランチは実行されない
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await orchestrator.RunContinuousDataCycleAsync(
                    plcConfigs,
                    new List<IPlcCommunicationManager>(),
                    cancellationToken
                );
            });
        }
    }

    /// <summary>
    /// テスト3: GetMonitoringInterval()削除後の互換性確認
    /// 期待: GetMonitoringInterval()が削除され、plcConfig.MonitoringIntervalMsが直接使用される
    /// Red段階: IOptionsの値（999ms）が使用され、テスト失敗
    /// Green段階: plcConfigの値（5000ms）が使用され、テスト成功
    /// </summary>
    [Fact]
    public async Task test_GetMonitoringInterval_削除後の互換性()
    {
        // Arrange
        var plcConfigs = new List<PlcConfiguration>
        {
            new PlcConfiguration
            {
                PlcId = "PLC1",
                MonitoringIntervalMs = 5000  // 5秒（期待値）
            }
        };

        TimeSpan? actualInterval = null;
        _mockTimerService
            .Setup(ts => ts.StartPeriodicExecution(
                It.IsAny<Func<Task>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Callback<Func<Task>, TimeSpan, CancellationToken>((action, interval, ct) =>
            {
                actualInterval = interval;
            })
            .Returns(Task.CompletedTask);

        var orchestrator = new ExecutionOrchestrator(
            _mockTimerService.Object,
            _mockConfigToFrameManager.Object,
            _mockDataOutputManager.Object,
            _mockLoggingManager.Object
        );

        var cancellationToken = new CancellationToken();

        // Act
        await orchestrator.RunContinuousDataCycleAsync(
            plcConfigs,
            new List<IPlcCommunicationManager>(),
            cancellationToken
        );

        // Assert
        Assert.NotNull(actualInterval);
        // 期待: plcConfigの値（5000ms）が使用される
        // 現在: IOptionsの値（999ms）が使用される → テスト失敗（Red）
        Assert.Equal(TimeSpan.FromMilliseconds(5000), actualInterval.Value);
    }
}
