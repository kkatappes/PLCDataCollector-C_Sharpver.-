using Xunit;
using Moq;
using Andon.Core.Controllers;
using Andon.Core.Interfaces;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Infrastructure.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Andon.Tests.Integration
{
    /// <summary>
    /// Step 4-2: ProgressReporter統合の統合テスト
    /// TDD Red-Green-Refactorサイクルに基づく実装
    /// </summary>
    public class Step4_2_ProgressReporting_IntegrationTests
    {
        #region TDDサイクル 1: ExecutionOrchestratorに進捗報告統合

        /// <summary>
        /// TC_Step4_2_001: ExecuteSingleCycleAsyncが進捗情報をProgressReporterに報告する
        /// Phase A (Red): 失敗するテストを作成
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Phase", "Step4-2")]
        [Trait("TDD", "Red")]
        public async Task ExecuteSingleCycleAsync_進捗情報をProgressReporterに報告する()
        {
            // Arrange
            var progressReports = new List<ParallelProgressInfo>();
            var progress = new Progress<ParallelProgressInfo>(info => progressReports.Add(info));

            var mockTimerService = new Mock<ITimerService>();
            var mockConfigToFrameManager = new Mock<IConfigToFrameManager>();
            var mockDataOutputManager = new Mock<IDataOutputManager>();
            var mockLoggingManager = new Mock<ILoggingManager>();
            var mockParallelController = new Mock<IParallelExecutionController>();

            // LoggingManagerのモック設定（すべてのメソッドを高速に）
            mockLoggingManager.Setup(l => l.LogDebug(It.IsAny<string>())).Returns(Task.CompletedTask);
            mockLoggingManager.Setup(l => l.LogInfo(It.IsAny<string>())).Returns(Task.CompletedTask);
            mockLoggingManager.Setup(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            mockLoggingManager.Setup(l => l.LogError(null, It.IsAny<string>())).Returns(Task.CompletedTask);

            // ParallelExecutionController のモック設定
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

            // ConfigToFrameManagerのモック設定
            mockConfigToFrameManager
                .Setup(c => c.BuildReadRandomFrameFromConfig(It.IsAny<PlcConfiguration>()))
                .Returns(new byte[] { 0x54, 0x00, 0x00, 0x00 }); // ダミーフレーム

            var orchestrator = new ExecutionOrchestrator(
                mockTimerService.Object,
                mockConfigToFrameManager.Object,
                mockDataOutputManager.Object,
                mockLoggingManager.Object,
                mockParallelController.Object);

            var plcConfigs = new List<PlcConfiguration>
            {
                new PlcConfiguration { IpAddress = "192.168.1.1", Port = 5000, MonitoringIntervalMs = 1000 },
                new PlcConfiguration { IpAddress = "192.168.1.2", Port = 5000, MonitoringIntervalMs = 1000 }
            };

            var mockPlcManager1 = new Mock<IPlcCommunicationManager>();
            var mockPlcManager2 = new Mock<IPlcCommunicationManager>();

            // PlcCommunicationManagerのモック設定
            mockPlcManager1
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

            mockPlcManager2
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

            var plcManagers = new List<IPlcCommunicationManager>
            {
                mockPlcManager1.Object,
                mockPlcManager2.Object
            };

            // Act
            // Phase A (Red): このメソッドシグネチャはまだ存在しない（IProgress<ParallelProgressInfo>パラメータがない）
            // コンパイルエラーが発生するはず
            await orchestrator.ExecuteSingleCycleAsync(
                plcConfigs,
                plcManagers,
                CancellationToken.None,
                progress); // 第4引数: IProgress<ParallelProgressInfo>（現在未実装）

            // Assert
            // 進捗報告が少なくとも1回行われたことを確認
            Assert.NotEmpty(progressReports);
            Assert.True(progressReports.Count >= 1, "進捗報告が行われていません");

            // 開始時の進捗報告を確認
            var firstReport = progressReports.First();
            Assert.NotNull(firstReport);
            Assert.Contains("Starting", firstReport.CurrentStep, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region TDDサイクル 2: ApplicationControllerからの進捗報告連携

        /// <summary>
        /// TC_Step4_2_002: RunContinuousDataCycleAsyncが進捗報告をサポートする
        /// Phase A (Red): 失敗するテストを作成
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Phase", "Step4-2")]
        [Trait("TDD", "Red")]
        public async Task RunContinuousDataCycleAsync_進捗報告をサポートする()
        {
            // Arrange
            var progressReports = new List<ProgressInfo>();
            var progress = new Progress<ProgressInfo>(info => progressReports.Add(info));

            var mockTimerService = new Mock<ITimerService>();
            var mockConfigToFrameManager = new Mock<IConfigToFrameManager>();
            var mockDataOutputManager = new Mock<IDataOutputManager>();
            var mockLoggingManager = new Mock<ILoggingManager>();
            var mockParallelController = new Mock<IParallelExecutionController>();

            // LoggingManagerのモック設定
            mockLoggingManager.Setup(l => l.LogDebug(It.IsAny<string>())).Returns(Task.CompletedTask);
            mockLoggingManager.Setup(l => l.LogInfo(It.IsAny<string>())).Returns(Task.CompletedTask);
            mockLoggingManager.Setup(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            mockLoggingManager.Setup(l => l.LogError(null, It.IsAny<string>())).Returns(Task.CompletedTask);

            // ConfigToFrameManagerのモック設定
            mockConfigToFrameManager
                .Setup(c => c.BuildReadRandomFrameFromConfig(It.IsAny<PlcConfiguration>()))
                .Returns(new byte[] { 0x54, 0x00, 0x00, 0x00 });

            // ParallelExecutionControllerのモック設定
            mockParallelController
                .Setup(p => p.ExecuteParallelPlcOperationsAsync(
                    It.IsAny<IEnumerable<It.IsAnyType>>(),
                    It.IsAny<Func<It.IsAnyType, CancellationToken, Task<CycleExecutionResult>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ParallelExecutionResult
                {
                    TotalPlcCount = 1,
                    SuccessfulPlcCount = 1,
                    FailedPlcCount = 0
                });

            // TimerServiceのモック設定: StartPeriodicExecutionが呼ばれたら1回実行
            mockTimerService
                .Setup(t => t.StartPeriodicExecution(
                    It.IsAny<Func<Task>>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Func<Task>, TimeSpan, CancellationToken>(async (action, interval, ct) =>
                {
                    await action();
                    await Task.Delay(Timeout.Infinite, ct);
                });

            var orchestrator = new ExecutionOrchestrator(
                mockTimerService.Object,
                mockConfigToFrameManager.Object,
                mockDataOutputManager.Object,
                mockLoggingManager.Object,
                mockParallelController.Object);

            var plcConfigs = new List<PlcConfiguration>
            {
                new PlcConfiguration { IpAddress = "192.168.1.1", Port = 5000, MonitoringIntervalMs = 1000 }
            };

            var mockPlcManager = new Mock<IPlcCommunicationManager>();
            mockPlcManager
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

            var plcManagers = new List<IPlcCommunicationManager>
            {
                mockPlcManager.Object
            };

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(200)); // 200ms後にキャンセル

            try
            {
                // Act
                // Phase A (Red): このメソッドシグネチャはまだ存在しない（IProgress<ProgressInfo>パラメータがない）
                // コンパイルエラーが発生するはず
                await orchestrator.RunContinuousDataCycleAsync(
                    plcConfigs,
                    plcManagers,
                    cts.Token,
                    progress); // 第4引数: IProgress<ProgressInfo>（現在未実装）
            }
            catch (OperationCanceledException)
            {
                // キャンセルは正常なフロー
            }

            // Assert
            // 進捗報告が少なくとも1回行われたことを確認
            Assert.NotEmpty(progressReports);
            Assert.True(progressReports.Count >= 1, "進捗報告が行われていません");
        }

        #endregion

        #region ヘルパーメソッド

        /// <summary>
        /// モックPLC通信マネージャーを作成（遅延あり）
        /// Phase13対応: ExecuteFullCycleAsyncに変更
        /// </summary>

        /// <summary>
        /// TC_Step4_2_003: RunContinuousDataCycleAsync内でParallelProgressInfoが変換されてProgressInfoとして報告される
        /// Phase A (Red): 失敗するテストを作成
        /// このテストは、ExecuteMultiPlcCycleAsync_Internal内のParallelProgressInfo報告が
        /// 型変換アダプターを通じてProgressInfoとして伝播されることを検証する
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Phase", "Step4-2-Fix")]
        [Trait("TDD", "Red")]
        public async Task RunContinuousDataCycleAsync_ParallelProgressInfoを変換してProgressInfoとして報告する()
        {
            // Arrange
            var progressReports = new List<ProgressInfo>();
            var progress = new Progress<ProgressInfo>(info => progressReports.Add(info));

            var mockTimerService = new Mock<ITimerService>();
            var mockConfigToFrameManager = new Mock<IConfigToFrameManager>();
            var mockDataOutputManager = new Mock<IDataOutputManager>();
            var mockLoggingManager = new Mock<ILoggingManager>();
            var mockParallelController = new Mock<IParallelExecutionController>();

            // LoggingManagerのモック設定
            mockLoggingManager.Setup(l => l.LogDebug(It.IsAny<string>())).Returns(Task.CompletedTask);
            mockLoggingManager.Setup(l => l.LogInfo(It.IsAny<string>())).Returns(Task.CompletedTask);
            mockLoggingManager.Setup(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            mockLoggingManager.Setup(l => l.LogError(null, It.IsAny<string>())).Returns(Task.CompletedTask);

            // ConfigToFrameManagerのモック設定
            mockConfigToFrameManager
                .Setup(c => c.BuildReadRandomFrameFromConfig(It.IsAny<PlcConfiguration>()))
                .Returns(new byte[] { 0x54, 0x00, 0x00, 0x00 });

            // ParallelExecutionControllerのモック設定
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

            // TimerServiceのモック設定: StartPeriodicExecutionが呼ばれたら1回実行
            mockTimerService
                .Setup(t => t.StartPeriodicExecution(
                    It.IsAny<Func<Task>>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Func<Task>, TimeSpan, CancellationToken>(async (action, interval, ct) =>
                {
                    await action();
                    await Task.Delay(Timeout.Infinite, ct);
                });

            var orchestrator = new ExecutionOrchestrator(
                mockTimerService.Object,
                mockConfigToFrameManager.Object,
                mockDataOutputManager.Object,
                mockLoggingManager.Object,
                mockParallelController.Object);

            var plcConfigs = new List<PlcConfiguration>
            {
                new PlcConfiguration { IpAddress = "192.168.1.1", Port = 5000, MonitoringIntervalMs = 1000 },
                new PlcConfiguration { IpAddress = "192.168.1.2", Port = 5000, MonitoringIntervalMs = 1000 }
            };

            var mockPlcManager1 = new Mock<IPlcCommunicationManager>();
            var mockPlcManager2 = new Mock<IPlcCommunicationManager>();

            mockPlcManager1
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

            mockPlcManager2
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

            var plcManagers = new List<IPlcCommunicationManager>
            {
                mockPlcManager1.Object,
                mockPlcManager2.Object
            };

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(200)); // 200ms後にキャンセル

            try
            {
                // Act
                await orchestrator.RunContinuousDataCycleAsync(
                    plcConfigs,
                    plcManagers,
                    cts.Token,
                    progress);
            }
            catch (OperationCanceledException)
            {
                // キャンセルは正常なフロー
            }

            // Assert
            // 進捗報告が行われていることを確認
            Assert.NotEmpty(progressReports);

            // 重要: ExecuteMultiPlcCycleAsync_Internal内のParallelProgressInfoが
            // 型変換アダプターを通じてProgressInfoとして報告されることを検証
            // 具体的には、"Multi-PLC Cycle - Starting" または "Multi-PLC Cycle - Completed" を含む
            // CurrentStepが報告されるはず（現在は報告されていない）
            var hasMultiPlcCycleProgress = progressReports.Any(p =>
                p.CurrentStep.Contains("Multi-PLC Cycle", StringComparison.OrdinalIgnoreCase));

            Assert.True(hasMultiPlcCycleProgress,
                "ExecuteMultiPlcCycleAsync_Internal内のParallelProgressInfo報告が伝播されていません。" +
                $"受信した進捗報告: {string.Join(", ", progressReports.Select(p => p.CurrentStep))}");
        }

        #endregion

        #region ヘルパーメソッド

        private static IPlcCommunicationManager CreateMockPlcManagerWithDelay(int delayMs)
        {
            var mock = new Mock<IPlcCommunicationManager>();
            mock.Setup(m => m.ExecuteFullCycleAsync(
                    It.IsAny<ConnectionConfig>(),
                    It.IsAny<TimeoutConfig>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<ReadRandomRequestInfo>(),
                    It.IsAny<CancellationToken>()))
                .Returns(async (ConnectionConfig connConfig, TimeoutConfig timeoutConfig, byte[] frame, ReadRandomRequestInfo reqInfo, CancellationToken ct) =>
                {
                    await Task.Delay(delayMs, ct);
                    return new FullCycleExecutionResult
                    {
                        IsSuccess = true,
                        ProcessedData = new ProcessedResponseData
                        {
                            IsSuccess = true
                        }
                    };
                });
            return mock.Object;
        }

        #endregion
    }
}
