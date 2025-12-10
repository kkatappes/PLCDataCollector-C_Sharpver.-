using Xunit;
using Moq;
using Andon.Core.Controllers;
using Andon.Core.Interfaces;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Core.Managers;
using Andon.Infrastructure.Configuration;
using Andon.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Andon.Tests.Integration;

/// <summary>
/// Step 4-4: GracefulShutdownHandler統合テスト
/// Program.csシグナルハンドラとApplicationController.StopAsync()統合
/// </summary>
public class Step4_4_GracefulShutdown_IntegrationTests
{
    /// <summary>
    /// TDDサイクル1: ApplicationControllerがStopAsync()でリソースを適切に解放する
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Phase", "Step4-4")]
    public async Task StopAsync_PLCマネージャーと設定監視を適切に停止する()
    {
        // Arrange
        var mockConfigManager = new Mock<MultiPlcConfigManager>(MockBehavior.Loose, (ConfigurationLoaderExcel?)null);
        var mockOrchestrator = new Mock<IExecutionOrchestrator>();
        var mockLogger = new Mock<ILoggingManager>();
        var mockWatcher = new Mock<IConfigurationWatcher>();

        // LoggingManagerのモック設定
        mockLogger.Setup(l => l.LogInfo(It.IsAny<string>())).Returns(Task.CompletedTask);
        mockLogger.Setup(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        mockLogger.Setup(l => l.LogDebug(It.IsAny<string>())).Returns(Task.CompletedTask);

        var controller = new ApplicationController(
            mockConfigManager.Object,
            mockOrchestrator.Object,
            mockLogger.Object,
            mockWatcher.Object,
            configLoader: null);

        // Act
        await controller.StopAsync(CancellationToken.None);

        // Assert
        // ConfigurationWatcherの停止が呼ばれること
        mockWatcher.Verify(
            w => w.StopWatching(),
            Times.Once(),
            "ConfigurationWatcher.StopWatching()が呼ばれていません");

        // ログ出力確認
        mockLogger.Verify(
            l => l.LogInfo("Stopping application"),
            Times.Once(),
            "停止開始ログが出力されていません");

        mockLogger.Verify(
            l => l.LogInfo("Stopped configuration monitoring"),
            Times.Once(),
            "設定監視停止ログが出力されていません");
    }

    /// <summary>
    /// TDDサイクル2: GracefulShutdownHandlerがApplicationControllerのStopAsync()を呼び出す
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Phase", "Step4-4")]
    public async Task ExecuteGracefulShutdown_ApplicationControllerのStopAsyncを呼び出す()
    {
        // Arrange
        var mockLogger = new Mock<ILoggingManager>();
        var mockController = new Mock<IApplicationController>();

        // LoggingManagerのモック設定
        mockLogger.Setup(l => l.LogInfo(It.IsAny<string>())).Returns(Task.CompletedTask);
        mockLogger.Setup(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var shutdownHandler = new GracefulShutdownHandler(mockLogger.Object);

        // Act
        var result = await shutdownHandler.ExecuteGracefulShutdown(
            mockController.Object,
            TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(result.Success, "GracefulShutdownが失敗しました");

        // ApplicationController.StopAsync()が呼ばれること
        mockController.Verify(
            c => c.StopAsync(It.IsAny<CancellationToken>()),
            Times.Once(),
            "ApplicationController.StopAsync()が呼ばれていません");

        // ログ出力確認
        mockLogger.Verify(
            l => l.LogInfo(It.Is<string>(s => s.Contains("graceful shutdown"))),
            Times.AtLeastOnce(),
            "シャットダウンログが出力されていません");
    }

    /// <summary>
    /// TDDサイクル3: タイムアウト時の動作確認
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Phase", "Step4-4")]
    public async Task ExecuteGracefulShutdown_タイムアウト時にOperationCanceledExceptionが発生する()
    {
        // Arrange
        var mockLogger = new Mock<ILoggingManager>();
        var mockController = new Mock<IApplicationController>();

        // LoggingManagerのモック設定
        mockLogger.Setup(l => l.LogInfo(It.IsAny<string>())).Returns(Task.CompletedTask);
        mockLogger.Setup(l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        // StopAsync()が長時間かかるシミュレーション（タイムアウトより長い）
        mockController
            .Setup(c => c.StopAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken ct) =>
            {
                await Task.Delay(10000, ct); // 10秒待機（タイムアウトは1秒）
            });

        var shutdownHandler = new GracefulShutdownHandler(mockLogger.Object);

        // Act
        var result = await shutdownHandler.ExecuteGracefulShutdown(
            mockController.Object,
            TimeSpan.FromSeconds(1)); // 1秒タイムアウト

        // Assert
        // タイムアウトによりSuccessがfalseになること
        Assert.False(result.Success, "タイムアウト時はSuccessがfalseになる必要があります");

        // エラーログが出力されること
        mockLogger.Verify(
            l => l.LogError(It.IsAny<Exception>(), It.IsAny<string>()),
            Times.Once(),
            "エラーログが出力されていません");
    }
}
