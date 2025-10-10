using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SlmpClient.Core;
using SlmpClient.Utils;
using Xunit;
using ISlmpClientFull = SlmpClient.Core.ISlmpClientFull;

namespace SlmpClient.Tests.Core
{
    /// <summary>
    /// SimpleMonitoringService TDD Tests
    /// t-wada手法に基づくテストファースト開発
    /// Red-Green-Refactor サイクル適用
    /// </summary>
    public class SimpleMonitoringServiceTests
    {
        #region Test Setup - 依存性注入とモックオブジェクト

        private readonly Mock<ISlmpClientFull> _mockSlmpClient;
        private readonly Mock<ILogger<SimpleMonitoringService>> _mockLogger;
        private readonly Mock<UnifiedLogWriter> _mockUnifiedLogWriter;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IMemoryOptimizer> _mockMemoryOptimizer;
        private readonly Mock<IPerformanceMonitor> _mockPerformanceMonitor;

        public SimpleMonitoringServiceTests()
        {
            _mockSlmpClient = new Mock<ISlmpClientFull>();
            _mockLogger = new Mock<ILogger<SimpleMonitoringService>>();
            _mockUnifiedLogWriter = new Mock<UnifiedLogWriter>(Mock.Of<ILogger<UnifiedLogWriter>>(), "test.log");
            _mockConfiguration = new Mock<IConfiguration>();
            _mockMemoryOptimizer = new Mock<IMemoryOptimizer>();
            _mockPerformanceMonitor = new Mock<IPerformanceMonitor>();

            // デフォルト設定
            SetupDefaultConfiguration();
            SetupDefaultMemoryOptimizer();
        }

        private void SetupDefaultConfiguration()
        {
            _mockConfiguration.Setup(c => c["PlcConnection:IpAddress"]).Returns("172.30.40.15");
            _mockConfiguration.Setup(c => c["PlcConnection:Port"]).Returns("8192");
            _mockConfiguration.Setup(c => c["PlcConnection:UseTcp"]).Returns("false");
            _mockConfiguration.Setup(c => c["PlcConnection:FrameVersion"]).Returns("4E");
            _mockConfiguration.Setup(c => c["MonitoringSettings:CycleIntervalMs"]).Returns("1000");
        }

        private void SetupDefaultMemoryOptimizer()
        {
            _mockMemoryOptimizer.Setup(m => m.CurrentMemoryUsage).Returns(400 * 1024); // 400KB
            _mockMemoryOptimizer.Setup(m => m.PeakMemoryUsage).Returns(450 * 1024); // 450KB
        }

        #endregion

        #region Red Phase Tests - 失敗するテストを先に書く

        [Fact]
        public void Constructor_WithValidDependencies_ShouldCreateInstance()
        {
            // Arrange & Act
            Action createService = () => new SimpleMonitoringService(
                _mockSlmpClient.Object,
                _mockLogger.Object,
                _mockUnifiedLogWriter.Object,
                _mockConfiguration.Object,
                _mockMemoryOptimizer.Object,
                _mockPerformanceMonitor.Object);

            // Assert
            var exception = Record.Exception(createService);
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_WithNullSlmpClient_ShouldThrowArgumentNullException()
        {
            // Arrange & Act
            Action createService = () => new SimpleMonitoringService(
                null,
                _mockLogger.Object,
                _mockUnifiedLogWriter.Object,
                _mockConfiguration.Object,
                _mockMemoryOptimizer.Object,
                _mockPerformanceMonitor.Object);

            // Assert
            var exception = Assert.Throws<ArgumentNullException>(createService);
            Assert.Contains("slmpClient", exception.Message);
        }

        [Fact]
        public async Task RunTwoStepFlowAsync_WithSuccessfulConnection_ShouldReturnSuccess()
        {
            // Arrange
            _mockSlmpClient.Setup(c => c.IsConnected).Returns(true);
            _mockSlmpClient.Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var service = CreateService();

            // Act
            var result = await service.RunTwoStepFlowAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.SessionId);
            Assert.NotEmpty(result.SessionId);
            Assert.True(result.MonitoringStarted);
        }

        [Fact]
        public async Task RunTwoStepFlowAsync_WithConnectionFailure_ShouldReturnFailure()
        {
            // Arrange
            _mockSlmpClient.Setup(c => c.IsConnected).Returns(false);
            _mockSlmpClient.Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Connection failed"));

            var service = CreateService();

            // Act
            var result = await service.RunTwoStepFlowAsync();

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Connection failed", result.ErrorMessage);
            Assert.False(result.MonitoringStarted);
        }

        [Fact]
        public void GetStatusReport_WithValidMemoryUsage_ShouldReturnFormattedString()
        {
            // Arrange
            var service = CreateService();

            // Act
            var statusReport = service.GetStatusReport();

            // Assert
            Assert.NotNull(statusReport);
            Assert.NotEmpty(statusReport);
            Assert.Contains("M000-M999, D000-D999", statusReport);
            Assert.Contains("400KB", statusReport); // Current memory
            Assert.Contains("450KB", statusReport); // Peak memory
        }

        #endregion

        #region Boundary Value Tests - 境界値テスト

        [Theory]
        [InlineData(0)] // 最小値
        [InlineData(449 * 1024)] // 制限値未満
        [InlineData(450 * 1024)] // 制限値ちょうど
        public async Task RunTwoStepFlowAsync_WithMemoryUsageWithinLimit_ShouldSucceed(long memoryUsage)
        {
            // Arrange
            _mockMemoryOptimizer.Setup(m => m.CurrentMemoryUsage).Returns(memoryUsage);
            _mockSlmpClient.Setup(c => c.IsConnected).Returns(true);

            var service = CreateService();

            // Act
            var result = await service.RunTwoStepFlowAsync();

            // Assert
            Assert.True(result.Success);
        }

        [Theory]
        [InlineData(451 * 1024)] // 制限値超過
        [InlineData(1024 * 1024)] // 1MB（大幅超過）
        public async Task RunTwoStepFlowAsync_WithMemoryUsageOverLimit_ShouldLogWarning(long memoryUsage)
        {
            // Arrange
            _mockMemoryOptimizer.Setup(m => m.CurrentMemoryUsage).Returns(memoryUsage);
            _mockSlmpClient.Setup(c => c.IsConnected).Returns(true);

            var service = CreateService();

            // Act
            var result = await service.RunTwoStepFlowAsync();

            // Assert
            // メモリ制限超過でも処理は継続するが、警告ログが出力される
            Assert.True(result.Success);
            // TODO: ログ出力の検証を追加
        }

        [Fact]
        public async Task StopMonitoringAsync_WhenNotMonitoring_ShouldCompleteWithoutError()
        {
            // Arrange
            var service = CreateService();

            // Act
            var exception = await Record.ExceptionAsync(() => service.StopMonitoringAsync());

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public async Task StopMonitoringAsync_WhenMonitoring_ShouldStopGracefully()
        {
            // Arrange
            _mockSlmpClient.Setup(c => c.IsConnected).Returns(true);
            var service = CreateService();

            // 先に監視を開始
            await service.RunTwoStepFlowAsync();

            // Act
            await service.StopMonitoringAsync();

            // Assert
            // 正常停止の検証
            // TODO: 監視停止の詳細検証を追加
        }

        #endregion

        #region Error Handling Tests - エラーハンドリングテスト

        [Fact]
        public async Task RunTwoStepFlowAsync_WithCancellationToken_ShouldRespectCancellation()
        {
            // Arrange
            var service = CreateService();
            var cts = new CancellationTokenSource();
            cts.Cancel(); // 即座にキャンセル

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                service.RunTwoStepFlowAsync(cts.Token));
        }

        [Fact]
        public async Task RunTwoStepFlowAsync_WithUnexpectedError_ShouldReturnFailureResult()
        {
            // Arrange
            _mockSlmpClient.Setup(c => c.IsConnected).Throws(new InvalidOperationException("Unexpected error"));
            var service = CreateService();

            // Act
            var result = await service.RunTwoStepFlowAsync();

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Unexpected error", result.ErrorMessage);
        }

        #endregion

        #region Private Helper Methods

        private SimpleMonitoringService CreateService()
        {
            return new SimpleMonitoringService(
                _mockSlmpClient.Object,
                _mockLogger.Object,
                _mockUnifiedLogWriter.Object,
                _mockConfiguration.Object,
                _mockMemoryOptimizer.Object,
                _mockPerformanceMonitor.Object);
        }

        #endregion
    }

    #region Test Data Models - SimpleMonitoringResult Definition

    /// <summary>
    /// SimpleMonitoring実行結果（テスト用定義）
    /// 実装前にテストで期待するインターフェースを定義
    /// </summary>
    public class SimpleMonitoringResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public object? ConnectionInfo { get; set; }
        public bool MonitoringStarted { get; set; }
    }

    #endregion
}