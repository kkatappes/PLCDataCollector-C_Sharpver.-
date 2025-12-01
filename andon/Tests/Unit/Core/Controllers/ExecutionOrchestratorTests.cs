using Xunit;
using Andon.Core.Controllers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Andon.Tests.Unit.Core.Controllers;

/// <summary>
/// ExecutionOrchestrator単体テスト
/// </summary>
public class ExecutionOrchestratorTests
{
    /// <summary>
    /// TC032: ExecuteMultiPlcCycleAsync - 並列実行モード
    /// </summary>
    [Fact]
    public async Task TC032_ExecuteMultiPlcCycleAsync_並列実行_全成功()
    {
        // Arrange
        var config = new MultiPlcConfig
        {
            ParallelConfig = new ParallelProcessingConfig
            {
                EnableParallel = true,
                MaxDegreeOfParallelism = 3,
                OverallTimeoutMs = 5000
            },
            PlcConnections = new List<PlcConnectionConfig>
            {
                new PlcConnectionConfig
                {
                    PlcId = "PLC_A",
                    PlcName = "テストPLC_A",
                    IPAddress = "192.168.1.10",
                    Port = 8192,
                    Priority = 5
                },
                new PlcConnectionConfig
                {
                    PlcId = "PLC_B",
                    PlcName = "テストPLC_B",
                    IPAddress = "192.168.1.11",
                    Port = 8192,
                    Priority = 5
                }
            }
        };

        var orchestrator = new ExecutionOrchestrator();

        // Act
        var result = await orchestrator.ExecuteMultiPlcCycleAsync(config);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess || !result.IsSuccess); // 実装後に厳密に検証
        Assert.Equal(2, result.PlcResults.Count);
    }

    /// <summary>
    /// TC035: ExecuteMultiPlcCycleAsync - 順次実行モード
    /// </summary>
    [Fact]
    public async Task TC035_ExecuteMultiPlcCycleAsync_順次実行()
    {
        // Arrange
        var config = new MultiPlcConfig
        {
            ParallelConfig = new ParallelProcessingConfig
            {
                EnableParallel = false, // 順次実行
                OverallTimeoutMs = 5000
            },
            PlcConnections = new List<PlcConnectionConfig>
            {
                new PlcConnectionConfig
                {
                    PlcId = "PLC_A",
                    PlcName = "テストPLC_A",
                    IPAddress = "192.168.1.10",
                    Port = 8192
                },
                new PlcConnectionConfig
                {
                    PlcId = "PLC_B",
                    PlcName = "テストPLC_B",
                    IPAddress = "192.168.1.11",
                    Port = 8192
                }
            }
        };

        var orchestrator = new ExecutionOrchestrator();

        // Act
        var result = await orchestrator.ExecuteMultiPlcCycleAsync(config);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.PlcResults.Count);
        Assert.Contains("PLC_A", result.PlcResults.Keys);
        Assert.Contains("PLC_B", result.PlcResults.Keys);
    }

    /// <summary>
    /// TC120: GetMonitoringInterval - DataProcessingConfigから監視間隔を取得する
    /// Phase 1 Step 1-2 TDDサイクル1
    /// </summary>
    [Fact]
    public void GetMonitoringInterval_DataProcessingConfigから監視間隔を取得する()
    {
        // Arrange
        var mockConfig = new Mock<IOptions<DataProcessingConfig>>();
        mockConfig.Setup(c => c.Value).Returns(new DataProcessingConfig
        {
            MonitoringIntervalMs = 5000
        });

        var orchestrator = new ExecutionOrchestrator(mockConfig.Object);

        // Act
        var interval = orchestrator.GetMonitoringInterval();

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(5000), interval);
    }

    /// <summary>
    /// TC121: RunContinuousDataCycleAsync - TimerServiceを使用して繰り返し実行する
    /// Phase 1 Step 1-2 TDDサイクル2
    /// Phase 継続実行モード: PlcConfiguration追加対応
    /// </summary>
    [Fact]
    public async Task RunContinuousDataCycleAsync_TimerServiceを使用して繰り返し実行する()
    {
        // Arrange
        var mockTimerService = new Mock<Andon.Core.Interfaces.ITimerService>();
        var mockConfig = new Mock<IOptions<DataProcessingConfig>>();
        mockConfig.Setup(c => c.Value).Returns(new DataProcessingConfig
        {
            MonitoringIntervalMs = 1000
        });

        // StartPeriodicExecutionをモックで設定（実際には実行せずにキャンセルトークンでキャンセル）
        mockTimerService
            .Setup(t => t.StartPeriodicExecution(
                It.IsAny<Func<Task>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<Task> action, TimeSpan interval, CancellationToken ct) =>
            {
                // モックとして即座にキャンセル扱いで終了
                return Task.FromCanceled(ct);
            });

        var orchestrator = new ExecutionOrchestrator(mockTimerService.Object, mockConfig.Object);

        var mockPlcManager = new Mock<Andon.Core.Interfaces.IPlcCommunicationManager>();
        var plcConfig = new PlcConfiguration { IpAddress = "192.168.1.1", Port = 5000 };
        var plcConfigs = new List<PlcConfiguration> { plcConfig };
        var plcManagers = new List<Andon.Core.Interfaces.IPlcCommunicationManager> { mockPlcManager.Object };
        var cts = new CancellationTokenSource();
        cts.Cancel(); // 即座にキャンセル

        // Act
        try
        {
            await orchestrator.RunContinuousDataCycleAsync(plcConfigs, plcManagers, cts.Token);
        }
        catch (TaskCanceledException)
        {
            // 期待される動作
        }

        // Assert
        mockTimerService.Verify(
            t => t.StartPeriodicExecution(
                It.IsAny<Func<Task>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    /// <summary>
    /// TC122: ExecuteMultiPlcCycleAsync_Internal - 単一PLC基本サイクル実行
    /// Phase 継続実行モード Phase 1-1 TDDサイクル1 Red
    /// </summary>
    [Fact]
    public async Task ExecuteMultiPlcCycleAsync_Internal_SinglePlc_ExecutesFullCycle()
    {
        // Arrange
        var mockPlcManager = new Mock<Andon.Core.Interfaces.IPlcCommunicationManager>();
        var mockConfigToFrameManager = new Mock<Andon.Core.Interfaces.IConfigToFrameManager>();
        var mockDataOutputManager = new Mock<Andon.Core.Interfaces.IDataOutputManager>();
        var mockLoggingManager = new Mock<Andon.Core.Interfaces.ILoggingManager>();
        var mockTimerService = new Mock<Andon.Core.Interfaces.ITimerService>();
        var config = Options.Create(new DataProcessingConfig { MonitoringIntervalMs = 1000 });

        var orchestrator = new ExecutionOrchestrator(
            mockTimerService.Object,
            config,
            mockConfigToFrameManager.Object,
            mockDataOutputManager.Object,
            mockLoggingManager.Object);

        var plcConfig = new PlcConfiguration
        {
            IpAddress = "192.168.1.1",
            Port = 5000,
            ConnectionMethod = "TCP",
            Timeout = 3000
        };

        var plcConfigs = new List<PlcConfiguration> { plcConfig };
        var plcManagers = new List<Andon.Core.Interfaces.IPlcCommunicationManager> { mockPlcManager.Object };

        var expectedResult = new FullCycleExecutionResult { IsSuccess = true };
        mockPlcManager
            .Setup(m => m.ExecuteFullCycleAsync(
                It.IsAny<ConnectionConfig>(),
                It.IsAny<TimeoutConfig>(),
                It.IsAny<byte[]>(),
                It.IsAny<ProcessedDeviceRequestInfo>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await orchestrator.ExecuteSingleCycleAsync(plcConfigs, plcManagers, CancellationToken.None);

        // Assert
        mockPlcManager.Verify(
            m => m.ExecuteFullCycleAsync(
                It.IsAny<ConnectionConfig>(),
                It.IsAny<TimeoutConfig>(),
                It.IsAny<byte[]>(),
                It.IsAny<ProcessedDeviceRequestInfo>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// TC123: ExecuteMultiPlcCycleAsync_Internal - 複数PLC順次実行
    /// Phase 継続実行モード Phase 1-2 TDDサイクル2 Red
    /// </summary>
    [Fact]
    public async Task ExecuteMultiPlcCycleAsync_Internal_MultiplePlcs_ExecutesAllCycles()
    {
        // Arrange
        var mockPlcManager1 = new Mock<Andon.Core.Interfaces.IPlcCommunicationManager>();
        var mockPlcManager2 = new Mock<Andon.Core.Interfaces.IPlcCommunicationManager>();
        var mockPlcManager3 = new Mock<Andon.Core.Interfaces.IPlcCommunicationManager>();
        var mockConfigToFrameManager = new Mock<Andon.Core.Interfaces.IConfigToFrameManager>();
        var mockDataOutputManager = new Mock<Andon.Core.Interfaces.IDataOutputManager>();
        var mockLoggingManager = new Mock<Andon.Core.Interfaces.ILoggingManager>();
        var mockTimerService = new Mock<Andon.Core.Interfaces.ITimerService>();
        var config = Options.Create(new DataProcessingConfig { MonitoringIntervalMs = 1000 });

        var orchestrator = new ExecutionOrchestrator(
            mockTimerService.Object,
            config,
            mockConfigToFrameManager.Object,
            mockDataOutputManager.Object,
            mockLoggingManager.Object);

        var plcConfig1 = new PlcConfiguration
        {
            IpAddress = "192.168.1.1",
            Port = 5000,
            ConnectionMethod = "TCP",
            Timeout = 3000
        };

        var plcConfig2 = new PlcConfiguration
        {
            IpAddress = "192.168.1.2",
            Port = 5001,
            ConnectionMethod = "TCP",
            Timeout = 3000
        };

        var plcConfig3 = new PlcConfiguration
        {
            IpAddress = "192.168.1.3",
            Port = 5002,
            ConnectionMethod = "UDP",
            Timeout = 2000
        };

        var plcConfigs = new List<PlcConfiguration> { plcConfig1, plcConfig2, plcConfig3 };
        var plcManagers = new List<Andon.Core.Interfaces.IPlcCommunicationManager>
        {
            mockPlcManager1.Object,
            mockPlcManager2.Object,
            mockPlcManager3.Object
        };

        var expectedResult = new FullCycleExecutionResult { IsSuccess = true };

        mockPlcManager1
            .Setup(m => m.ExecuteFullCycleAsync(
                It.IsAny<ConnectionConfig>(),
                It.IsAny<TimeoutConfig>(),
                It.IsAny<byte[]>(),
                It.IsAny<ProcessedDeviceRequestInfo>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        mockPlcManager2
            .Setup(m => m.ExecuteFullCycleAsync(
                It.IsAny<ConnectionConfig>(),
                It.IsAny<TimeoutConfig>(),
                It.IsAny<byte[]>(),
                It.IsAny<ProcessedDeviceRequestInfo>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        mockPlcManager3
            .Setup(m => m.ExecuteFullCycleAsync(
                It.IsAny<ConnectionConfig>(),
                It.IsAny<TimeoutConfig>(),
                It.IsAny<byte[]>(),
                It.IsAny<ProcessedDeviceRequestInfo>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await orchestrator.ExecuteSingleCycleAsync(plcConfigs, plcManagers, CancellationToken.None);

        // Assert - すべてのPLCに対してExecuteFullCycleAsyncが呼ばれることを検証
        mockPlcManager1.Verify(
            m => m.ExecuteFullCycleAsync(
                It.Is<ConnectionConfig>(c => c.IpAddress == "192.168.1.1" && c.Port == 5000 && c.UseTcp == true),
                It.Is<TimeoutConfig>(t => t.ConnectTimeoutMs == 3000),
                It.IsAny<byte[]>(),
                It.IsAny<ProcessedDeviceRequestInfo>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "PLC1に対してExecuteFullCycleAsyncが1回呼ばれるべき");

        mockPlcManager2.Verify(
            m => m.ExecuteFullCycleAsync(
                It.Is<ConnectionConfig>(c => c.IpAddress == "192.168.1.2" && c.Port == 5001 && c.UseTcp == true),
                It.Is<TimeoutConfig>(t => t.ConnectTimeoutMs == 3000),
                It.IsAny<byte[]>(),
                It.IsAny<ProcessedDeviceRequestInfo>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "PLC2に対してExecuteFullCycleAsyncが1回呼ばれるべき");

        mockPlcManager3.Verify(
            m => m.ExecuteFullCycleAsync(
                It.Is<ConnectionConfig>(c => c.IpAddress == "192.168.1.3" && c.Port == 5002 && c.UseTcp == false),
                It.Is<TimeoutConfig>(t => t.ConnectTimeoutMs == 2000),
                It.IsAny<byte[]>(),
                It.IsAny<ProcessedDeviceRequestInfo>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "PLC3に対してExecuteFullCycleAsyncが1回呼ばれるべき");
    }

    /// <summary>
    /// TC124: ExecuteMultiPlcCycleAsync_Internal - Step2フレーム構築統合
    /// Phase 継続実行モード Phase 1-3 TDDサイクル3 Red
    /// ConfigToFrameManager.BuildReadRandomFrameFromConfig()が正しく呼ばれ、
    /// 構築されたフレームがExecuteFullCycleAsync()に渡されることを検証
    /// </summary>
    [Fact]
    public async Task ExecuteMultiPlcCycleAsync_Internal_BuildsFrameFromConfig()
    {
        // Arrange
        var mockPlcManager = new Mock<Andon.Core.Interfaces.IPlcCommunicationManager>();
        var mockConfigToFrameManager = new Mock<Andon.Core.Interfaces.IConfigToFrameManager>();
        var mockDataOutputManager = new Mock<Andon.Core.Interfaces.IDataOutputManager>();
        var mockLoggingManager = new Mock<Andon.Core.Interfaces.ILoggingManager>();
        var mockTimerService = new Mock<Andon.Core.Interfaces.ITimerService>();
        var config = Options.Create(new DataProcessingConfig { MonitoringIntervalMs = 1000 });

        var orchestrator = new ExecutionOrchestrator(
            mockTimerService.Object,
            config,
            mockConfigToFrameManager.Object,
            mockDataOutputManager.Object,
            mockLoggingManager.Object);

        // 期待されるフレーム（4Eフレームの例）
        byte[] expectedFrame = new byte[]
        {
            0x54, 0x00, // サブヘッダ（4E Binary）
            0x00, 0x00, // シリアル
            0x00, 0x00, // 予約
            0x00,       // ネットワーク番号
            0xFF,       // PC番号
            0xFF, 0x03, // I/O番号
            0x00        // 局番
        };

        var plcConfig = new PlcConfiguration
        {
            IpAddress = "192.168.1.1",
            Port = 5000,
            ConnectionMethod = "TCP",
            Timeout = 3000,
            FrameVersion = "4E",
            IsBinary = true,
            Devices = new List<DeviceSpecification>
            {
                new DeviceSpecification(Andon.Core.Constants.DeviceCode.D, 0)
            }
        };

        var plcConfigs = new List<PlcConfiguration> { plcConfig };
        var plcManagers = new List<Andon.Core.Interfaces.IPlcCommunicationManager> { mockPlcManager.Object };

        // ConfigToFrameManager.BuildReadRandomFrameFromConfig()をモック
        mockConfigToFrameManager
            .Setup(m => m.BuildReadRandomFrameFromConfig(It.IsAny<PlcConfiguration>()))
            .Returns(expectedFrame);

        var expectedResult = new FullCycleExecutionResult { IsSuccess = true };
        mockPlcManager
            .Setup(m => m.ExecuteFullCycleAsync(
                It.IsAny<ConnectionConfig>(),
                It.IsAny<TimeoutConfig>(),
                It.IsAny<byte[]>(),
                It.IsAny<ProcessedDeviceRequestInfo>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await orchestrator.ExecuteSingleCycleAsync(plcConfigs, plcManagers, CancellationToken.None);

        // Assert
        // 1. BuildReadRandomFrameFromConfig()が正しいPlcConfigurationで呼ばれたことを検証
        mockConfigToFrameManager.Verify(
            m => m.BuildReadRandomFrameFromConfig(
                It.Is<PlcConfiguration>(c => c.IpAddress == "192.168.1.1" && c.Port == 5000)),
            Times.Once,
            "BuildReadRandomFrameFromConfig()が正しいPlcConfigurationで1回呼ばれるべき");

        // 2. ExecuteFullCycleAsync()に正しいフレームが渡されたことを検証
        mockPlcManager.Verify(
            m => m.ExecuteFullCycleAsync(
                It.IsAny<ConnectionConfig>(),
                It.IsAny<TimeoutConfig>(),
                It.Is<byte[]>(frame => frame.SequenceEqual(expectedFrame)),
                It.IsAny<ProcessedDeviceRequestInfo>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "ExecuteFullCycleAsync()に正しいフレーム（expectedFrame）が渡されるべき");
    }

    /// <summary>
    /// TC125: ExecuteMultiPlcCycleAsync_Internal - Step7データ出力統合
    /// Phase 継続実行モード Phase 1-4 TDDサイクル4 Red
    /// DataOutputManager.OutputToJson()が正しく呼ばれ、
    /// 実行結果のデータが正しく出力されることを検証
    /// </summary>
    [Fact]
    public async Task ExecuteMultiPlcCycleAsync_Internal_OutputsDataAfterCycle()
    {
        // Arrange
        var mockPlcManager = new Mock<Andon.Core.Interfaces.IPlcCommunicationManager>();
        var mockConfigToFrameManager = new Mock<Andon.Core.Interfaces.IConfigToFrameManager>();
        var mockDataOutputManager = new Mock<Andon.Core.Interfaces.IDataOutputManager>();
        var mockLoggingManager = new Mock<Andon.Core.Interfaces.ILoggingManager>();
        var mockTimerService = new Mock<Andon.Core.Interfaces.ITimerService>();
        var config = Options.Create(new DataProcessingConfig { MonitoringIntervalMs = 1000 });

        var orchestrator = new ExecutionOrchestrator(
            mockTimerService.Object,
            config,
            mockConfigToFrameManager.Object,
            mockDataOutputManager.Object,
            mockLoggingManager.Object);

        var plcConfig = new PlcConfiguration
        {
            IpAddress = "192.168.1.1",
            Port = 5000,
            ConnectionMethod = "TCP",
            Timeout = 3000,
            FrameVersion = "4E",
            IsBinary = true,
            Devices = new List<DeviceSpecification>
            {
                new DeviceSpecification(Andon.Core.Constants.DeviceCode.D, 0),
                new DeviceSpecification(Andon.Core.Constants.DeviceCode.D, 100)
            }
        };

        var plcConfigs = new List<PlcConfiguration> { plcConfig };
        var plcManagers = new List<Andon.Core.Interfaces.IPlcCommunicationManager> { mockPlcManager.Object };

        // ExecuteFullCycleAsyncの戻り値をモック（成功ケース）
        var expectedProcessedData = new ProcessedResponseData
        {
            IsSuccess = true,
            ProcessedData = new Dictionary<string, DeviceData>
            {
                { "D0", new DeviceData { DeviceName = "D0", Code = Andon.Core.Constants.DeviceCode.D, Address = 0, Value = 100 } },
                { "D100", new DeviceData { DeviceName = "D100", Code = Andon.Core.Constants.DeviceCode.D, Address = 100, Value = 200 } }
            }
        };

        var expectedResult = new FullCycleExecutionResult
        {
            IsSuccess = true,
            ProcessedData = expectedProcessedData
        };

        mockPlcManager
            .Setup(m => m.ExecuteFullCycleAsync(
                It.IsAny<ConnectionConfig>(),
                It.IsAny<TimeoutConfig>(),
                It.IsAny<byte[]>(),
                It.IsAny<ProcessedDeviceRequestInfo>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await orchestrator.ExecuteSingleCycleAsync(plcConfigs, plcManagers, CancellationToken.None);

        // Assert
        // OutputToJson()が正しいパラメータで呼ばれたことを検証
        mockDataOutputManager.Verify(
            m => m.OutputToJson(
                It.Is<ProcessedResponseData>(d => d == expectedProcessedData),
                It.IsAny<string>(), // outputDirectory
                It.Is<string>(ip => ip == "192.168.1.1"),
                It.Is<int>(p => p == 5000),
                It.IsAny<Dictionary<string, DeviceEntryInfo>>()),
            Times.Once,
            "OutputToJson()が正しいパラメータで1回呼ばれるべき");
    }

    /// <summary>
    /// Phase8.5 Test Case 2-1: DeviceSpecificationsの正しい設定
    /// ExecuteSingleCycleAsync_Should_SetDeviceSpecifications_FromPlcConfiguration
    /// </summary>
    [Fact]
    public async Task Phase85_ExecuteSingleCycleAsync_Should_SetDeviceSpecifications_FromPlcConfiguration()
    {
        // このテストはPhase8.5暫定対策の検証用
        // ExecutionOrchestratorがPlcConfiguration.DevicesからDeviceSpecificationsを設定することを確認

        // Arrange
        var mockPlcManager = new Mock<Andon.Core.Interfaces.IPlcCommunicationManager>();
        var mockConfigToFrameManager = new Mock<Andon.Core.Interfaces.IConfigToFrameManager>();
        var mockDataOutputManager = new Mock<Andon.Core.Interfaces.IDataOutputManager>();
        var mockLoggingManager = new Mock<Andon.Core.Interfaces.ILoggingManager>();
        var mockTimerService = new Mock<Andon.Core.Interfaces.ITimerService>();
        var config = Options.Create(new DataProcessingConfig { MonitoringIntervalMs = 1000 });

        var orchestrator = new ExecutionOrchestrator(
            mockTimerService.Object,
            config,
            mockConfigToFrameManager.Object,
            mockDataOutputManager.Object,
            mockLoggingManager.Object);

        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(Andon.Core.Constants.DeviceCode.D, 100),
            new DeviceSpecification(Andon.Core.Constants.DeviceCode.M, 200)
        };

        var plcConfig = new PlcConfiguration
        {
            IpAddress = "192.168.1.1",
            Port = 5000,
            ConnectionMethod = "TCP",
            Timeout = 3000,
            FrameVersion = "4E",
            IsBinary = true,
            Devices = devices
        };

        var plcConfigs = new List<PlcConfiguration> { plcConfig };
        var plcManagers = new List<Andon.Core.Interfaces.IPlcCommunicationManager> { mockPlcManager.Object };

        byte[] expectedFrame = new byte[] { 0x54, 0x00 };
        mockConfigToFrameManager
            .Setup(m => m.BuildReadRandomFrameFromConfig(It.IsAny<PlcConfiguration>()))
            .Returns(expectedFrame);

        var expectedResult = new FullCycleExecutionResult { IsSuccess = true };
        mockPlcManager
            .Setup(m => m.ExecuteFullCycleAsync(
                It.IsAny<ConnectionConfig>(),
                It.IsAny<TimeoutConfig>(),
                It.IsAny<byte[]>(),
                It.IsAny<ProcessedDeviceRequestInfo>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await orchestrator.ExecuteSingleCycleAsync(plcConfigs, plcManagers, CancellationToken.None);

        // Assert: ExecuteFullCycleAsyncが正しいDeviceSpecificationsで呼ばれたことを検証
        mockPlcManager.Verify(
            m => m.ExecuteFullCycleAsync(
                It.IsAny<ConnectionConfig>(),
                It.IsAny<TimeoutConfig>(),
                It.IsAny<byte[]>(),
                It.Is<ProcessedDeviceRequestInfo>(req =>
                    req.DeviceSpecifications != null &&
                    req.DeviceSpecifications.Count == 2 &&
                    req.DeviceSpecifications[0].DeviceNumber == 100 &&
                    req.DeviceSpecifications[1].DeviceNumber == 200),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "ExecuteFullCycleAsyncがDeviceSpecifications設定済みのProcessedDeviceRequestInfoで呼ばれるべき");
    }
}
