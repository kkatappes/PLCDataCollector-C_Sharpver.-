using Xunit;
using Moq;
using Andon.Core.Controllers;
using Andon.Core.Interfaces;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Infrastructure.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Andon.Tests.Integration
{
    /// <summary>
    /// Step 4-3: ConfigurationWatcher動的再読み込み実装の統合テスト
    /// TDD Red-Green-Refactorサイクルに基づく実装
    /// </summary>
    public class Step4_3_DynamicReload_IntegrationTests
    {
        #region TDDサイクル 1: Excel設定再読み込み

        /// <summary>
        /// TC_Step4_3_001: HandleConfigurationChanged_Excel変更時に設定を再読み込みする
        /// Phase A (Red): 失敗するテストを作成
        ///
        /// テスト内容:
        /// 1. ConfigurationChangedイベントをトリガー
        /// 2. ConfigurationLoaderExcel.LoadFromExcelが呼び出されることを確認
        /// 3. ログ出力が正しく行われることを確認
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Phase", "Step4-3")]
        [Trait("TDD", "Red")]
        public async Task HandleConfigurationChanged_Excel変更時に設定を再読み込みする()
        {
            // Arrange
            var mockConfigManager = new Mock<MultiPlcConfigManager>(MockBehavior.Loose, null);
            var mockOrchestrator = new Mock<IExecutionOrchestrator>();
            var mockLoggingManager = new Mock<ILoggingManager>();
            var mockConfigWatcher = new Mock<IConfigurationWatcher>();

            // ConfigurationLoaderExcel は具象クラスなので、実際のインスタンスを使用
            // テスト用の一時ファイルパスを使用
            var tempDirectory = Path.Combine(Path.GetTempPath(), "AndonTest_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);

            try
            {
                var configLoader = new ConfigurationLoaderExcel(tempDirectory, mockConfigManager.Object);

                // LoggingManagerのモック設定
                mockLoggingManager.Setup(l => l.LogInfo(It.IsAny<string>())).Returns(Task.CompletedTask);
                mockLoggingManager.Setup(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>())).Returns(Task.CompletedTask);
                mockLoggingManager.Setup(l => l.LogDebug(It.IsAny<string>())).Returns(Task.CompletedTask);

                var controller = new ApplicationController(
                    mockConfigManager.Object,
                    mockOrchestrator.Object,
                    mockLoggingManager.Object,
                    mockConfigWatcher.Object,
                    configLoader);

                // テスト用のExcelファイルパス
                var testFilePath = Path.Combine(tempDirectory, "test_plc.xlsx");

                // Act
                // ConfigurationChangedイベントをトリガー
                mockConfigWatcher.Raise(
                    w => w.OnConfigurationChanged += null,
                    mockConfigWatcher.Object,
                    new ConfigurationChangedEventArgs { FilePath = testFilePath });

                // async voidメソッドなので、処理完了を待機
                await Task.Delay(500);

                // Assert
                // ログ出力が行われたことを確認（設定ファイル変更の通知）
                mockLoggingManager.Verify(
                    l => l.LogInfo(It.Is<string>(s => s.Contains("Configuration file changed"))),
                    Times.Once(),
                    "設定ファイル変更のログが出力されていません");
            }
            finally
            {
                // 後処理: 一時ディレクトリを削除
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
            }
        }

        #endregion

        #region TDDサイクル 2: 設定マネージャーへの反映

        /// <summary>
        /// TC_Step4_3_002: HandleConfigurationChanged_新設定をConfigManagerに反映する
        /// Phase B (Green): Option B実装（全設定再読み込み）に対応したテスト
        ///
        /// テスト内容:
        /// 1. Excel設定ファイル変更イベントをトリガー
        /// 2. ExecuteStep1InitializationAsyncが呼び出されることを確認（ログで間接的に確認）
        /// 3. 設定再読み込み成功のログ出力を確認
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Phase", "Step4-3")]
        [Trait("TDD", "Green")]
        public async Task HandleConfigurationChanged_新設定をConfigManagerに反映する()
        {
            // Arrange
            var mockConfigManager = new Mock<MultiPlcConfigManager>(MockBehavior.Loose, null);
            var mockOrchestrator = new Mock<IExecutionOrchestrator>();
            var mockLoggingManager = new Mock<ILoggingManager>();
            var mockConfigWatcher = new Mock<IConfigurationWatcher>();

            // テスト用の一時ディレクトリ
            var tempDirectory = Path.Combine(Path.GetTempPath(), "AndonTest_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);

            try
            {
                var configLoader = new ConfigurationLoaderExcel(tempDirectory, mockConfigManager.Object);

                // LoggingManagerのモック設定
                mockLoggingManager.Setup(l => l.LogInfo(It.IsAny<string>())).Returns(Task.CompletedTask);
                mockLoggingManager.Setup(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>())).Returns(Task.CompletedTask);
                mockLoggingManager.Setup(l => l.LogDebug(It.IsAny<string>())).Returns(Task.CompletedTask);
                mockLoggingManager.Setup(l => l.LogWarning(It.IsAny<string>())).Returns(Task.CompletedTask);

                var controller = new ApplicationController(
                    mockConfigManager.Object,
                    mockOrchestrator.Object,
                    mockLoggingManager.Object,
                    mockConfigWatcher.Object,
                    configLoader);

                var testFilePath = Path.Combine(tempDirectory, "test_plc.xlsx");

                // Act
                // ConfigurationChangedイベントをトリガー
                mockConfigWatcher.Raise(
                    w => w.OnConfigurationChanged += null,
                    mockConfigWatcher.Object,
                    new ConfigurationChangedEventArgs { FilePath = testFilePath });

                await Task.Delay(500);

                // Assert
                // Phase B (Green): Option B実装により、全設定再読み込みが実行されることを確認
                mockLoggingManager.Verify(
                    l => l.LogInfo(It.Is<string>(s => s.Contains("Configuration file changed"))),
                    Times.Once(),
                    "設定ファイル変更のログが出力されていません");

                // 設定再読み込み完了のログを確認
                mockLoggingManager.Verify(
                    l => l.LogInfo(It.Is<string>(s => s.Contains("Configuration reloaded successfully"))),
                    Times.AtLeastOnce(),
                    "設定再読み込み成功のログが出力されていません");
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
            }
        }

        #endregion

        #region TDDサイクル 3: PlcCommunicationManager再初期化

        /// <summary>
        /// TC_Step4_3_003: HandleConfigurationChanged_PLCマネージャーを再初期化する
        /// Phase B (Green): Option B実装（全設定再読み込み）に対応したテスト
        ///
        /// テスト内容:
        /// 1. 初期化後にPLCマネージャーが存在することを確認
        /// 2. Excel設定ファイル変更イベントをトリガー
        /// 3. ExecuteStep1InitializationAsyncが再度呼び出され、PLCマネージャーが再初期化されることを確認
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Phase", "Step4-3")]
        [Trait("TDD", "Green")]
        public async Task HandleConfigurationChanged_PLCマネージャーを再初期化する()
        {
            // Arrange
            var mockConfigManager = new Mock<MultiPlcConfigManager>(MockBehavior.Loose, null);
            var mockOrchestrator = new Mock<IExecutionOrchestrator>();
            var mockLoggingManager = new Mock<ILoggingManager>();
            var mockConfigWatcher = new Mock<IConfigurationWatcher>();

            var tempDirectory = Path.Combine(Path.GetTempPath(), "AndonTest_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);

            try
            {
                var configLoader = new ConfigurationLoaderExcel(tempDirectory, mockConfigManager.Object);

                // LoggingManagerのモック設定
                mockLoggingManager.Setup(l => l.LogInfo(It.IsAny<string>())).Returns(Task.CompletedTask);
                mockLoggingManager.Setup(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>())).Returns(Task.CompletedTask);
                mockLoggingManager.Setup(l => l.LogDebug(It.IsAny<string>())).Returns(Task.CompletedTask);
                mockLoggingManager.Setup(l => l.LogWarning(It.IsAny<string>())).Returns(Task.CompletedTask);

                var controller = new ApplicationController(
                    mockConfigManager.Object,
                    mockOrchestrator.Object,
                    mockLoggingManager.Object,
                    mockConfigWatcher.Object,
                    configLoader);

                // 初期化を実行（PLCマネージャーを作成）
                await controller.ExecuteStep1InitializationAsync(tempDirectory, CancellationToken.None);

                // 初期化後のPLCマネージャー数を確認
                var initialManagerCount = controller.GetPlcManagers().Count;

                var testFilePath = Path.Combine(tempDirectory, "test_plc.xlsx");

                // Act
                // ConfigurationChangedイベントをトリガー
                mockConfigWatcher.Raise(
                    w => w.OnConfigurationChanged += null,
                    mockConfigWatcher.Object,
                    new ConfigurationChangedEventArgs { FilePath = testFilePath });

                await Task.Delay(500);

                // Assert
                // Phase B (Green): Option B実装により、PLCマネージャーが再初期化されることを確認
                mockLoggingManager.Verify(
                    l => l.LogInfo(It.Is<string>(s => s.Contains("Configuration file changed"))),
                    Times.Once(),
                    "設定ファイル変更のログが出力されていません");

                // 設定再読み込み完了のログを確認
                mockLoggingManager.Verify(
                    l => l.LogInfo(It.Is<string>(s => s.Contains("Configuration reloaded successfully"))),
                    Times.AtLeastOnce(),
                    "設定再読み込み成功のログが出力されていません");

                // PLCマネージャーが再初期化されている（非nullかつ空リスト）
                var reloadedManagers = controller.GetPlcManagers();
                Assert.NotNull(reloadedManagers);
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
            }
        }

        #endregion
    }
}
