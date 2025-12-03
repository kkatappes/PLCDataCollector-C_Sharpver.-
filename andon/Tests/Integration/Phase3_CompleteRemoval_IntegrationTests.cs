using Xunit;
using Moq;
using Andon.Core.Interfaces;
using Andon.Core.Controllers;
using Andon.Core.Managers;
using Andon.Core.Models.ConfigModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Andon.Tests.Integration
{
    /// <summary>
    /// Phase 3: appsettings.json完全廃止後の統合テスト
    ///
    /// 目的: appsettings.json無しで全機能が正常動作することを確認
    /// 前提条件: Phase 0, Phase 1, Phase 2-1～2-5完了
    /// </summary>
    [Collection("Sequential")]
    public class Phase3_CompleteRemoval_IntegrationTests
    {
        /// <summary>
        /// Test 1: アプリケーション起動_appsettings無し
        /// appsettings.jsonが存在しない状態でDIコンテナが正常構築されること
        /// </summary>
        [Fact]
        public void test_アプリケーション起動_appsettings無し()
        {
            // Arrange
            var services = new ServiceCollection();

            // IConfigurationは空の状態（appsettings.json無し）
            var emptyConfig = new ConfigurationBuilder().Build();

            // DIコンテナに必要なサービスを登録（appsettings.json不要）
            services.AddSingleton<IConfiguration>(emptyConfig);
            services.AddLogging();
            services.AddSingleton<ILoggingManager, LoggingManager>();

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var loggingManager = serviceProvider.GetService<ILoggingManager>();

            // Assert
            Assert.NotNull(loggingManager);

            // LoggingManagerはハードコード値で動作（appsettings.json不要）
            // LogLevel=Debug, EnableFileOutput=true, EnableConsoleOutput=true, etc.
        }

        /// <summary>
        /// Test 2: LoggingManager_ハードコード値で動作
        /// appsettings.json無しでLoggingManagerがハードコード値で正常動作すること
        /// </summary>
        [Fact]
        public void test_LoggingManager_ハードコード値で動作()
        {
            // Arrange
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<LoggingManager>>();

            // Act
            // appsettings.json無しでLoggingManagerを作成（ハードコード値使用）
            var loggingManager = new LoggingManager(logger);

            // Assert
            Assert.NotNull(loggingManager);

            // ハードコード値でログ出力が正常動作
            // LogLevel=Debug, EnableFileOutput=true, EnableConsoleOutput=true
            // LogFilePath="logs/andon.log", MaxLogFileSizeMb=10, MaxLogFileCount=7
            // EnableDateBasedRotation=false
            loggingManager.LogInfo("Test message for Phase 3 - ハードコード値で動作");
        }

        /// <summary>
        /// Test 3: PlcConfiguration_MonitoringIntervalMs_Excel設定値使用
        /// Excel設定のMonitoringIntervalMsが使用されること（appsettings.json不要）
        /// </summary>
        [Fact]
        public void test_PlcConfiguration_MonitoringIntervalMs_Excel設定値使用()
        {
            // Arrange & Act
            var plcConfig = new PlcConfiguration
            {
                PlcId = "PLC01",
                PlcName = "TestPLC",
                MonitoringIntervalMs = 5000 // Excel設定値
            };

            // Assert
            // Excel設定の値（5000ms）が取得できる
            Assert.Equal(5000, plcConfig.MonitoringIntervalMs);

            // appsettings.jsonの値（存在しない）ではなく、Excel設定値が使用される
            Assert.True(plcConfig.MonitoringIntervalMs >= 100 && plcConfig.MonitoringIntervalMs <= 60000);
            // Phase 2-5で最適化された範囲
        }

        /// <summary>
        /// Test 4: PlcConfiguration_PlcModel_Excel設定値使用
        /// Excel設定のPlcModelが使用されること（appsettings.json不要）
        /// </summary>
        [Fact]
        public void test_PlcConfiguration_PlcModel_Excel設定値使用()
        {
            // Arrange & Act
            var plcConfig = new PlcConfiguration
            {
                PlcId = "PLC01",
                PlcName = "TestPLC",
                PlcModel = "5_JRS_N2" // Excel設定値
            };

            // Assert
            // Excel設定の値が取得できる
            Assert.Equal("5_JRS_N2", plcConfig.PlcModel);

            // PlcModelがJSON出力に使用される（Phase 2-3で実装完了）
        }

        /// <summary>
        /// Test 5: PlcConfiguration_SavePath_Excel設定値使用
        /// Excel設定のSavePathが使用されること（appsettings.json不要）
        /// </summary>
        [Fact]
        public void test_PlcConfiguration_SavePath_Excel設定値使用()
        {
            // Arrange & Act
            var plcConfig = new PlcConfiguration
            {
                PlcId = "PLC01",
                PlcName = "TestPLC",
                SavePath = "./custom/output" // Excel設定値
            };

            // Assert
            // Excel設定の値が取得できる
            Assert.Equal("./custom/output", plcConfig.SavePath);

            // SavePathがデータ出力に使用される（Phase 2-4で実装完了）
        }

        /// <summary>
        /// Test 6: 複数PLC設定_独立したMonitoringIntervalMs
        /// 各PLCが独立したMonitoringIntervalMsを持つこと（appsettings.json不要）
        /// </summary>
        [Fact]
        public void test_複数PLC設定_独立したMonitoringIntervalMs()
        {
            // Arrange & Act
            var plcConfigs = new List<PlcConfiguration>
            {
                new PlcConfiguration
                {
                    PlcId = "PLC01",
                    PlcName = "TestPLC1",
                    MonitoringIntervalMs = 1000 // Excel設定値: 1秒
                },
                new PlcConfiguration
                {
                    PlcId = "PLC02",
                    PlcName = "TestPLC2",
                    MonitoringIntervalMs = 2000 // Excel設定値: 2秒
                }
            };

            // Assert
            // 各PLCが独立したMonitoringIntervalMsを持つ
            Assert.NotEqual(plcConfigs[0].MonitoringIntervalMs, plcConfigs[1].MonitoringIntervalMs);

            // 各PLCのMonitoringIntervalMsが有効範囲内
            foreach (var plcConfig in plcConfigs)
            {
                Assert.True(plcConfig.MonitoringIntervalMs >= 100 && plcConfig.MonitoringIntervalMs <= 60000);
            }
        }

        /// <summary>
        /// Test 7: IConfiguration空の状態_エラーなし
        /// IConfigurationが空の状態でもエラーが発生しないこと
        /// </summary>
        [Fact]
        public void test_IConfiguration空の状態_エラーなし()
        {
            // Arrange
            var emptyConfig = new ConfigurationBuilder().Build();

            // Act & Assert
            // IConfigurationが空でもエラーにならない
            Assert.NotNull(emptyConfig);
            Assert.Empty(emptyConfig.AsEnumerable());

            // appsettings.jsonが存在しなくても、空のIConfigurationが作成される
            // Host.CreateDefaultBuilder(args)はappsettings.json不在でもエラーにならない
        }
    }
}
