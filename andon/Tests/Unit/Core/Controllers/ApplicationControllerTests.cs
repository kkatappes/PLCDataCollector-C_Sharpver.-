using Xunit;
using Andon.Core.Controllers;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Interfaces;
using Andon.Core.Constants;
using Andon.Infrastructure.Configuration;
using Moq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Andon.Core.Models.ConfigModels;

namespace Andon.Tests.Unit.Core.Controllers;

/// <summary>
/// ApplicationController単体テスト
/// </summary>
public class ApplicationControllerTests
{
    /// <summary>
    /// TC122: ExecuteStep1InitializationAsync - 正常系 - 成功結果を返す
    /// Phase 1 Step 1-3 TDDサイクル1
    /// </summary>
    [Fact]
    public async Task ExecuteStep1InitializationAsync_正常系_成功結果を返す()
    {
        // Arrange
        var mockConfigManagerLogger = new Mock<Microsoft.Extensions.Logging.ILogger<MultiPlcConfigManager>>();
        var configManager = new MultiPlcConfigManager(mockConfigManagerLogger.Object);

        // 実際に設定を追加
        var config1 = new PlcConfiguration { SourceExcelFile = "PLC1.xlsx" };
        var config2 = new PlcConfiguration { SourceExcelFile = "PLC2.xlsx" };
        configManager.AddConfiguration(config1);
        configManager.AddConfiguration(config2);

        var mockOrchestrator = new Mock<IExecutionOrchestrator>();
        var mockLogger = new Mock<ILoggingManager>();

        var controller = new ApplicationController(
            configManager,
            mockOrchestrator.Object,
            mockLogger.Object);

        // Act
        var result = await controller.ExecuteStep1InitializationAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.PlcCount);
        mockLogger.Verify(m => m.LogInfo("Starting Step1 initialization"), Times.Once());
        mockLogger.Verify(m => m.LogInfo("Step1 initialization completed"), Times.Once());
    }

    /// <summary>
    /// TC123: StartContinuousDataCycleAsync - 初期化成功後に継続実行を開始する
    /// Phase 1 Step 1-3 TDDサイクル2
    /// </summary>
    [Fact]
    public async Task StartContinuousDataCycleAsync_初期化成功後に継続実行を開始する()
    {
        // Arrange
        var mockConfigManagerLogger = new Mock<Microsoft.Extensions.Logging.ILogger<MultiPlcConfigManager>>();
        var configManager = new MultiPlcConfigManager(mockConfigManagerLogger.Object);
        var config1 = new PlcConfiguration { SourceExcelFile = "PLC1.xlsx" };
        configManager.AddConfiguration(config1);

        var mockOrchestrator = new Mock<IExecutionOrchestrator>();
        var mockLogger = new Mock<ILoggingManager>();

        // RunContinuousDataCycleAsyncのモックを設定（キャンセルトークンで終了するまで待機）
        mockOrchestrator
            .Setup(o => o.RunContinuousDataCycleAsync(
                It.IsAny<List<PlcConfiguration>>(),
                It.IsAny<List<IPlcCommunicationManager>>(),
                It.IsAny<System.Threading.CancellationToken>()))
            .Returns((List<PlcConfiguration> configs, List<IPlcCommunicationManager> managers, System.Threading.CancellationToken ct) =>
            {
                // キャンセルが要求されるまで待機するタスクを返す
                var tcs = new System.Threading.Tasks.TaskCompletionSource();
                ct.Register(() => tcs.TrySetResult());
                return tcs.Task;
            });

        var controller = new ApplicationController(
            configManager,
            mockOrchestrator.Object,
            mockLogger.Object);

        // 先に初期化を実行して_plcManagersを設定
        var initResult = await controller.ExecuteStep1InitializationAsync();

        var cts = new System.Threading.CancellationTokenSource();
        cts.CancelAfter(100);

        // Act
        await controller.StartContinuousDataCycleAsync(initResult, cts.Token);

        // Assert
        mockLogger.Verify(m => m.LogInfo("Starting continuous data cycle"), Times.Once());
        mockOrchestrator.Verify(
            o => o.RunContinuousDataCycleAsync(
                It.IsAny<List<PlcConfiguration>>(),
                It.IsAny<List<IPlcCommunicationManager>>(),
                It.IsAny<System.Threading.CancellationToken>(),
                It.IsAny<IProgress<ProgressInfo>>()), // Phase 4-2: 進捗報告パラメータ追加
            Times.Once());
    }

    /// <summary>
    /// TC124: StartAsync - Step1初期化後に継続実行を開始する
    /// Phase 1 Step 1-3 TDDサイクル3
    /// </summary>
    [Fact]
    public async Task StartAsync_Step1初期化後に継続実行を開始する()
    {
        // Arrange
        var mockConfigManagerLogger = new Mock<Microsoft.Extensions.Logging.ILogger<MultiPlcConfigManager>>();
        var configManager = new MultiPlcConfigManager(mockConfigManagerLogger.Object);
        var config1 = new PlcConfiguration { SourceExcelFile = "PLC1.xlsx" };
        configManager.AddConfiguration(config1);

        var mockOrchestrator = new Mock<IExecutionOrchestrator>();
        var mockLogger = new Mock<ILoggingManager>();

        // RunContinuousDataCycleAsyncのモックを設定
        // Phase 4-2: IProgress<ProgressInfo>パラメータ追加
        mockOrchestrator
            .Setup(o => o.RunContinuousDataCycleAsync(
                It.IsAny<List<PlcConfiguration>>(),
                It.IsAny<List<IPlcCommunicationManager>>(),
                It.IsAny<System.Threading.CancellationToken>(),
                It.IsAny<IProgress<ProgressInfo>>()))
            .Returns((List<PlcConfiguration> configs, List<IPlcCommunicationManager> managers, System.Threading.CancellationToken ct, IProgress<ProgressInfo> progress) =>
            {
                var tcs = new System.Threading.Tasks.TaskCompletionSource();
                ct.Register(() => tcs.TrySetResult());
                return tcs.Task;
            });

        var controller = new ApplicationController(
            configManager,
            mockOrchestrator.Object,
            mockLogger.Object);

        var cts = new System.Threading.CancellationTokenSource();
        cts.CancelAfter(100);

        // Act
        await controller.StartAsync(cts.Token);

        // Assert
        mockLogger.Verify(m => m.LogInfo("Starting Step1 initialization"), Times.Once());
        mockLogger.Verify(m => m.LogInfo("Step1 initialization completed"), Times.Once());
        mockLogger.Verify(m => m.LogInfo("Starting continuous data cycle"), Times.Once());
        mockOrchestrator.Verify(
            o => o.RunContinuousDataCycleAsync(
                It.IsAny<List<PlcConfiguration>>(),
                It.IsAny<List<IPlcCommunicationManager>>(),
                cts.Token,
                It.IsAny<IProgress<ProgressInfo>>()), // Phase 4-2: 進捗報告パラメータ追加
            Times.Once());
    }

    /// <summary>
    /// TC125: StopAsync - アプリケーション停止ログを出力する
    /// Phase 1 Step 1-3 TDDサイクル3
    /// </summary>
    [Fact]
    public async Task StopAsync_アプリケーション停止ログを出力する()
    {
        // Arrange
        var mockConfigManagerLogger = new Mock<Microsoft.Extensions.Logging.ILogger<MultiPlcConfigManager>>();
        var configManager = new MultiPlcConfigManager(mockConfigManagerLogger.Object);

        var mockOrchestrator = new Mock<IExecutionOrchestrator>();
        var mockLogger = new Mock<ILoggingManager>();

        var controller = new ApplicationController(
            configManager,
            mockOrchestrator.Object,
            mockLogger.Object);

        var cts = new System.Threading.CancellationTokenSource();

        // Act
        await controller.StopAsync(cts.Token);

        // Assert
        mockLogger.Verify(m => m.LogInfo("Stopping application"), Times.Once());
    }

    // ========== Phase3 Part7: ConfigurationWatcher統合テスト ==========

    /// <summary>
    /// コンストラクタ: ConfigurationWatcherをDI可能
    /// </summary>
    [Fact]
    public void Constructor_ConfigurationWatcher付き_正常にインスタンス化()
    {
        // Arrange
        var mockConfigManagerLogger = new Mock<Microsoft.Extensions.Logging.ILogger<MultiPlcConfigManager>>();
        var configManager = new MultiPlcConfigManager(mockConfigManagerLogger.Object);
        var mockOrchestrator = new Mock<IExecutionOrchestrator>();
        var mockLogger = new Mock<ILoggingManager>();
        var mockWatcher = new Mock<IConfigurationWatcher>();

        // Act
        var controller = new ApplicationController(
            configManager,
            mockOrchestrator.Object,
            mockLogger.Object,
            mockWatcher.Object);

        // Assert
        Assert.NotNull(controller);
    }

    /// <summary>
    /// StartAsync: ConfigurationWatcherが監視を開始する
    /// </summary>
    [Fact]
    public async Task StartAsync_ConfigurationWatcherが監視開始()
    {
        // Arrange
        var mockConfigManagerLogger = new Mock<Microsoft.Extensions.Logging.ILogger<MultiPlcConfigManager>>();
        var configManager = new MultiPlcConfigManager(mockConfigManagerLogger.Object);
        var config1 = new PlcConfiguration { SourceExcelFile = "PLC1.xlsx" };
        configManager.AddConfiguration(config1);

        var mockOrchestrator = new Mock<IExecutionOrchestrator>();
        var mockLogger = new Mock<ILoggingManager>();
        var mockWatcher = new Mock<IConfigurationWatcher>();

        mockOrchestrator
            .Setup(o => o.RunContinuousDataCycleAsync(
                It.IsAny<List<PlcConfiguration>>(),
                It.IsAny<List<IPlcCommunicationManager>>(),
                It.IsAny<System.Threading.CancellationToken>()))
            .Returns((List<PlcConfiguration> configs, List<IPlcCommunicationManager> managers, System.Threading.CancellationToken ct) =>
            {
                var tcs = new System.Threading.Tasks.TaskCompletionSource();
                ct.Register(() => tcs.TrySetResult());
                return tcs.Task;
            });

        var controller = new ApplicationController(
            configManager,
            mockOrchestrator.Object,
            mockLogger.Object,
            mockWatcher.Object);

        var cts = new System.Threading.CancellationTokenSource();
        cts.CancelAfter(100);

        // Act
        await controller.StartAsync(cts.Token);

        // Assert
        mockWatcher.Verify(w => w.StartWatchingExcel(It.IsAny<string>()), Times.Once());
    }

    /// <summary>
    /// StopAsync: ConfigurationWatcherが監視を停止する
    /// </summary>
    [Fact]
    public async Task StopAsync_ConfigurationWatcherが監視停止()
    {
        // Arrange
        var mockConfigManagerLogger = new Mock<Microsoft.Extensions.Logging.ILogger<MultiPlcConfigManager>>();
        var configManager = new MultiPlcConfigManager(mockConfigManagerLogger.Object);
        var mockOrchestrator = new Mock<IExecutionOrchestrator>();
        var mockLogger = new Mock<ILoggingManager>();
        var mockWatcher = new Mock<IConfigurationWatcher>();

        var controller = new ApplicationController(
            configManager,
            mockOrchestrator.Object,
            mockLogger.Object,
            mockWatcher.Object);

        var cts = new System.Threading.CancellationTokenSource();

        // Act
        await controller.StopAsync(cts.Token);

        // Assert
        mockWatcher.Verify(w => w.StopWatching(), Times.Once());
    }

    /// <summary>
    /// OnConfigurationChanged: Excel設定変更時に再読み込み処理が呼ばれる
    /// </summary>
    [Fact]
    public async Task OnConfigurationChanged_Excel変更時に再読み込み処理実行()
    {
        // Arrange
        var mockConfigManagerLogger = new Mock<Microsoft.Extensions.Logging.ILogger<MultiPlcConfigManager>>();
        var configManager = new MultiPlcConfigManager(mockConfigManagerLogger.Object);
        var mockOrchestrator = new Mock<IExecutionOrchestrator>();
        var mockLogger = new Mock<ILoggingManager>();

        // 実際のConfigurationWatcherを使用（イベント発火をテスト）
        var watcher = new ConfigurationWatcher();

        var controller = new ApplicationController(
            configManager,
            mockOrchestrator.Object,
            mockLogger.Object,
            watcher);

        var testConfigDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "andon_test_config_reload");
        if (System.IO.Directory.Exists(testConfigDir))
        {
            System.IO.Directory.Delete(testConfigDir, true);
        }
        System.IO.Directory.CreateDirectory(testConfigDir);

        var testFile = System.IO.Path.Combine(testConfigDir, "5JRS_N2.xlsx");

        // Act - まず監視開始（この実装は後で追加）
        watcher.StartWatchingExcel(testConfigDir);

        // Excelファイル変更をシミュレート
        await System.IO.File.WriteAllTextAsync(testFile, "dummy excel content");
        await Task.Delay(500); // イベント発火待機

        // Assert
        mockLogger.Verify(
            m => m.LogInfo(It.Is<string>(s => s.Contains("Configuration file changed"))),
            Times.Once());

        // Cleanup
        watcher.StopWatching();
        System.IO.Directory.Delete(testConfigDir, true);
    }

    // ========== Phase 2 Part1: PlcManager初期化テスト ==========

    /// <summary>
    /// TC126: ExecuteStep1InitializationAsync - 単一設定 - PlcManagerを生成する
    /// Phase 2 TDDサイクル1 Red
    /// </summary>
    [Fact]
    public async Task ExecuteStep1InitializationAsync_単一設定_PlcManagerを生成する()
    {
        // Arrange
        var mockConfigManagerLogger = new Mock<Microsoft.Extensions.Logging.ILogger<MultiPlcConfigManager>>();
        var configManager = new MultiPlcConfigManager(mockConfigManagerLogger.Object);

        // PlcConfiguration設定
        var config = new PlcConfiguration
        {
            SourceExcelFile = "PLC1.xlsx",
            IpAddress = "192.168.1.1",
            Port = 5000,
            ConnectionMethod = "TCP",
            Timeout = 3000,
            Devices = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 100)
            }
        };
        configManager.AddConfiguration(config);

        var mockOrchestrator = new Mock<IExecutionOrchestrator>();
        var mockLogger = new Mock<ILoggingManager>();

        var controller = new ApplicationController(
            configManager,
            mockOrchestrator.Object,
            mockLogger.Object);

        // Act
        var result = await controller.ExecuteStep1InitializationAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.PlcCount);

        // PlcManagersが生成されていることを検証
        var plcManagers = controller.GetPlcManagers();
        Assert.Single(plcManagers);
        Assert.NotNull(plcManagers[0]);
    }

    /// <summary>
    /// TC127: ExecuteStep1InitializationAsync - 複数設定 - 複数のPlcManagerを生成する
    /// Phase 2 TDDサイクル2
    /// </summary>
    [Fact]
    public async Task ExecuteStep1InitializationAsync_複数設定_複数のPlcManagerを生成する()
    {
        // Arrange
        var mockConfigManagerLogger = new Mock<Microsoft.Extensions.Logging.ILogger<MultiPlcConfigManager>>();
        var configManager = new MultiPlcConfigManager(mockConfigManagerLogger.Object);

        // 3つのPlcConfiguration設定
        var config1 = new PlcConfiguration
        {
            SourceExcelFile = "PLC1.xlsx",
            IpAddress = "192.168.1.1",
            Port = 5000,
            ConnectionMethod = "TCP",
            Timeout = 3000,
            Devices = new List<DeviceSpecification> { new DeviceSpecification(DeviceCode.D, 100) }
        };
        var config2 = new PlcConfiguration
        {
            SourceExcelFile = "PLC2.xlsx",
            IpAddress = "192.168.1.2",
            Port = 5001,
            ConnectionMethod = "TCP",
            Timeout = 3000,
            Devices = new List<DeviceSpecification> { new DeviceSpecification(DeviceCode.D, 200) }
        };
        var config3 = new PlcConfiguration
        {
            SourceExcelFile = "PLC3.xlsx",
            IpAddress = "192.168.1.3",
            Port = 5002,
            ConnectionMethod = "UDP",
            Timeout = 3000,
            Devices = new List<DeviceSpecification> { new DeviceSpecification(DeviceCode.M, 0) }
        };

        configManager.AddConfiguration(config1);
        configManager.AddConfiguration(config2);
        configManager.AddConfiguration(config3);

        var mockOrchestrator = new Mock<IExecutionOrchestrator>();
        var mockLogger = new Mock<ILoggingManager>();

        var controller = new ApplicationController(
            configManager,
            mockOrchestrator.Object,
            mockLogger.Object);

        // Act
        var result = await controller.ExecuteStep1InitializationAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.PlcCount);

        // 3つのPlcManagersが生成されていることを検証
        var plcManagers = controller.GetPlcManagers();
        Assert.Equal(3, plcManagers.Count);
        Assert.NotNull(plcManagers[0]);
        Assert.NotNull(plcManagers[1]);
        Assert.NotNull(plcManagers[2]);
    }

    /// <summary>
    /// TDDサイクル 2: ConfigurationLoaderExcelが注入されたとき、起動時に設定を読み込む
    /// Phase2 Step2-7
    /// </summary>
    [Fact]
    public async Task ExecuteStep1InitializationAsync_ConfigurationLoaderExcel注入_自動的に設定読み込み()
    {
        // Arrange
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<MultiPlcConfigManager>>();
        var configManager = new MultiPlcConfigManager(mockLogger.Object);
        
        // ConfigurationLoaderExcel は null（モック不要）
        // 代わりに、事前にMultiPlcConfigManagerに設定を追加
        configManager.AddConfiguration(new PlcConfiguration
        {
            IpAddress = "192.168.1.100",
            Port = 5000,
            Timeout = 3000,
            ConnectionMethod = "TCP",
            PlcName = "TestPLC",
            FrameVersion = "3E",
            IsBinary = true,
            Devices = new List<DeviceSpecification> { new DeviceSpecification(DeviceCode.D, 0) }
        });
        
        var mockOrchestrator = new Mock<IExecutionOrchestrator>();
        var mockLoggingManager = new Mock<ILoggingManager>();
        
        // ConfigurationLoaderExcel を null で注入（既に設定が追加されているため不要）
        var controller = new ApplicationController(
            configManager,
            mockOrchestrator.Object,
            mockLoggingManager.Object,
            null, // ConfigurationWatcherはnull
            null); // ConfigurationLoaderExcel もnull（既存の動作確認）
        
        // Act
        var result = await controller.ExecuteStep1InitializationAsync();
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.PlcCount);
        
        var plcManagers = controller.GetPlcManagers();
        Assert.NotNull(plcManagers);
        Assert.Single(plcManagers);
    }

    // ========== Phase 2 Step 2-7: TDDサイクル 4 - エラーケーステスト ==========

    /// <summary>
    /// TDDサイクル 4: Excelファイルがない場合でも起動できる
    /// Phase2 Step2-7 Red
    /// </summary>
    [Fact]
    public async Task ApplicationController_Excelファイルなし_警告ログで起動()
    {
        // Arrange
        var emptyDirectory = Path.Combine(Path.GetTempPath(), $"AndonTestEmpty_{Guid.NewGuid()}");
        Directory.CreateDirectory(emptyDirectory);

        try
        {
            var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<MultiPlcConfigManager>>();
            var configManager = new MultiPlcConfigManager(mockLogger.Object);
            var loader = new ConfigurationLoaderExcel(emptyDirectory, configManager);

            var mockOrchestrator = new Mock<IExecutionOrchestrator>();
            var mockLoggingManager = new Mock<ILoggingManager>();

            var controller = new ApplicationController(
                configManager,
                mockOrchestrator.Object,
                mockLoggingManager.Object,
                null,
                loader);

            // Act
            var result = await controller.ExecuteStep1InitializationAsync();

            // Assert
            Assert.True(result.Success, "設定が0件でも起動は成功する");
            Assert.Equal(0, result.PlcCount);
            mockLoggingManager.Verify(
                m => m.LogWarning(It.Is<string>(s => s.Contains("No Excel configuration files found"))),
                Times.Once());
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(emptyDirectory))
            {
                Directory.Delete(emptyDirectory, true);
            }
        }
    }

    /// <summary>
    /// TDDサイクル 4: 不正なExcelファイルはエラーハンドリングされる
    /// Phase2 Step2-7 Red
    /// </summary>
    [Fact]
    public async Task ApplicationController_不正なExcel_エラーログ出力()
    {
        // Arrange
        var testDirectory = Path.Combine(Path.GetTempPath(), $"AndonTestInvalid_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDirectory);

        try
        {
            // 不正なExcelファイル（空ファイル）を作成
            var invalidFile = Path.Combine(testDirectory, "invalid.xlsx");
            File.WriteAllText(invalidFile, "invalid content");

            var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<MultiPlcConfigManager>>();
            var configManager = new MultiPlcConfigManager(mockLogger.Object);
            var loader = new ConfigurationLoaderExcel(testDirectory, configManager);

            var mockOrchestrator = new Mock<IExecutionOrchestrator>();
            var mockLoggingManager = new Mock<ILoggingManager>();

            var controller = new ApplicationController(
                configManager,
                mockOrchestrator.Object,
                mockLoggingManager.Object,
                null,
                loader);

            // Act
            var result = await controller.ExecuteStep1InitializationAsync();

            // Assert
            // 不正なファイルがある場合、初期化は失敗する
            Assert.False(result.Success, "不正なExcelファイルがある場合は初期化失敗");
            Assert.Contains("設定ファイル読み込みエラー", result.ErrorMessage);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }

    /// <summary>
    /// TDDサイクル 4: ロック中のExcelファイルはスキップされる
    /// Phase2 Step2-7 Red
    /// </summary>
    [Fact]
    public async Task ApplicationController_ロック中Excel_スキップして起動()
    {
        // Arrange
        var testDirectory = Path.Combine(Path.GetTempPath(), $"AndonTestLocked_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDirectory);
        FileStream? lockStream = null;

        try
        {
            // ロック中のExcelファイルをシミュレート
            var lockedFile = Path.Combine(testDirectory, "locked.xlsx");

            // テスト用の有効なExcelファイルを作成
            using (var package = new OfficeOpenXml.ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("設定");
                worksheet.Cells[1, 1].Value = "PLC名";
                worksheet.Cells[2, 1].Value = "TestPLC";
                package.SaveAs(new FileInfo(lockedFile));
            }

            // ファイルをロック
            lockStream = new FileStream(lockedFile, FileMode.Open, FileAccess.Read, FileShare.None);

            var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<MultiPlcConfigManager>>();
            var configManager = new MultiPlcConfigManager(mockLogger.Object);
            var loader = new ConfigurationLoaderExcel(testDirectory, configManager);

            var mockOrchestrator = new Mock<IExecutionOrchestrator>();
            var mockLoggingManager = new Mock<ILoggingManager>();

            var controller = new ApplicationController(
                configManager,
                mockOrchestrator.Object,
                mockLoggingManager.Object,
                null,
                loader);

            // Act
            var result = await controller.ExecuteStep1InitializationAsync();

            // Assert
            // ロックファイルは除外され、設定が0件でも起動成功
            Assert.True(result.Success, "ロックファイルは除外され起動成功");
            Assert.Equal(0, result.PlcCount);
            mockLoggingManager.Verify(
                m => m.LogWarning(It.Is<string>(s => s.Contains("No Excel configuration files found"))),
                Times.Once());
        }
        finally
        {
            // ロック解除
            lockStream?.Close();
            lockStream?.Dispose();

            // Cleanup
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }

    // ========== Phase 5.0: 本番統合対応テスト ==========

    /// <summary>
    /// TC_P5_0_001: ExecuteStep1InitializationAsync - LoggingManager統合確認
    /// Phase 5.0 Step 5.0-Red
    /// ApplicationControllerがPlcCommunicationManagerにLoggingManagerを注入していることを確認
    /// </summary>
    [Fact]
    public async Task TC_P5_0_001_ExecuteStep1_LoggingManager統合確認()
    {
        // Arrange
        var mockConfigManagerLogger = new Mock<Microsoft.Extensions.Logging.ILogger<MultiPlcConfigManager>>();
        var configManager = new MultiPlcConfigManager(mockConfigManagerLogger.Object);

        // PlcConfiguration設定
        var config = new PlcConfiguration
        {
            SourceExcelFile = "PLC1.xlsx",
            IpAddress = "192.168.1.1",
            Port = 5000,
            ConnectionMethod = "TCP",
            Timeout = 3000,
            IsBinary = true,
            FrameVersion = "4E",
            Devices = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 100)
            }
        };
        configManager.AddConfiguration(config);

        var mockOrchestrator = new Mock<IExecutionOrchestrator>();
        var mockLogger = new Mock<ILoggingManager>();

        var controller = new ApplicationController(
            configManager,
            mockOrchestrator.Object,
            mockLogger.Object);

        // Act
        var result = await controller.ExecuteStep1InitializationAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.PlcCount);

        // PlcCommunicationManagerが生成されていることを確認
        var plcManagers = controller.GetPlcManagers();
        Assert.Single(plcManagers);
        Assert.NotNull(plcManagers[0]);

        // ⚠️ 注意: このテストは現状では間接的な検証のみ
        // LoggingManagerが実際に注入されているかは、統合テストTC_P5_0_002で確認
        // （PlcCommunicationManagerの内部実装に依存するため、ここでは生成確認のみ）
    }
}
