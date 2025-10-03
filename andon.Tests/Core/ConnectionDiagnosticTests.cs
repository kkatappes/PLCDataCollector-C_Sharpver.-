using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using SlmpClient.Core;

namespace andon.Tests.Core
{
    /// <summary>
    /// ConnectionDiagnostic のテストクラス
    /// TDD Red-Green-Refactor サイクルに基づく実装
    /// </summary>
    public class ConnectionDiagnosticTests
    {
        private readonly Mock<ILogger<ConnectionDiagnostic>> _mockLogger;
        private readonly Mock<ISlmpClient> _mockSlmpClient;

        public ConnectionDiagnosticTests()
        {
            _mockLogger = new Mock<ILogger<ConnectionDiagnostic>>();
            _mockSlmpClient = new Mock<ISlmpClient>();
        }

        [Fact]
        public async Task TestNetworkConnectivity_ShouldReturnTrueForSuccessfulConnection()
        {
            // Arrange
            var diagnostic = new ConnectionDiagnostic(_mockLogger.Object);
            var connectionTarget = "192.168.1.100:8192";

            // Act
            var result = await diagnostic.TestNetworkConnectivityAsync(connectionTarget);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.NotEmpty(result.Details);
            Assert.True(result.ResponseTimeMs >= 0);
        }

        [Fact]
        public async Task TestPlcSystemInfo_ShouldReturnPlcInformation()
        {
            // Arrange
            _mockSlmpClient.Setup(x => x.IsConnected).Returns(true);
            _mockSlmpClient.Setup(x => x.IsAliveAsync(It.IsAny<CancellationToken>()))
                          .ReturnsAsync(true);

            var diagnostic = new ConnectionDiagnostic(_mockLogger.Object);

            // Act
            var result = await diagnostic.TestPlcSystemInfoAsync(_mockSlmpClient.Object);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.NotEmpty(result.CpuModel);
            Assert.NotEmpty(result.CpuStatus);
            Assert.NotEmpty(result.SlmpVersion);
            Assert.False(result.HasErrors);
        }

        [Fact]
        public async Task TestDeviceAccessibility_ShouldVerifyDeviceAccess()
        {
            // Arrange
            var diagnostic = new ConnectionDiagnostic(_mockLogger.Object);
            var bitDevices = new[] { ("M", 100u), ("M", 101u) };
            var wordDevices = new[] { ("D", 200u), ("D", 201u) };

            // Act
            var result = await diagnostic.TestDeviceAccessibilityAsync(
                _mockSlmpClient.Object, bitDevices, wordDevices);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.NotNull(result.BitDeviceResults);
            Assert.NotNull(result.WordDeviceResults);
            Assert.True(result.AllDevicesAccessible);
        }

        [Fact]
        public async Task MeasureCommunicationQuality_ShouldReturnQualityMetrics()
        {
            // Arrange
            _mockSlmpClient.Setup(x => x.IsAliveAsync(It.IsAny<CancellationToken>()))
                          .ReturnsAsync(true);

            var diagnostic = new ConnectionDiagnostic(_mockLogger.Object);

            // Act
            var result = await diagnostic.MeasureCommunicationQualityAsync(
                _mockSlmpClient.Object, sampleCount: 5);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.True(result.SuccessRate >= 0 && result.SuccessRate <= 100);
            Assert.True(result.AverageResponseTime >= 0);
            Assert.True(result.MaxResponseTime >= result.MinResponseTime);
            Assert.Equal(5, result.TotalSamples);
        }

        [Fact]
        public async Task RunCompleteDiagnostic_ShouldPerformAllTests()
        {
            // Arrange
            _mockSlmpClient.Setup(x => x.Address).Returns("192.168.1.100:8192");
            _mockSlmpClient.Setup(x => x.IsConnected).Returns(true);
            _mockSlmpClient.Setup(x => x.IsAliveAsync(It.IsAny<CancellationToken>()))
                          .ReturnsAsync(true);

            var diagnostic = new ConnectionDiagnostic(_mockLogger.Object);
            var diagConfig = new ConnectionDiagnosticConfiguration
            {
                TestNetworkConnectivity = true,
                TestPlcSystemInfo = true,
                TestDeviceAccessibility = true,
                MeasureCommunicationQuality = true,
                QualitySampleCount = 3,
                BitDevicesToTest = new[] { ("M", 100u), ("M", 101u) },
                WordDevicesToTest = new[] { ("D", 200u), ("D", 201u) }
            };

            // Act
            var result = await diagnostic.RunCompleteDiagnosticAsync(_mockSlmpClient.Object, diagConfig);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.NetworkConnectivity);
            Assert.NotNull(result.PlcSystemInfo);
            Assert.NotNull(result.DeviceAccessibility);
            Assert.NotNull(result.CommunicationQuality);
            Assert.True(result.OverallSuccess);
        }

        [Fact]
        public async Task GenerateDiagnosticReport_ShouldCreateReadableReport()
        {
            // Arrange
            _mockSlmpClient.Setup(x => x.Address).Returns("192.168.1.100:8192");
            _mockSlmpClient.Setup(x => x.IsConnected).Returns(true);

            var diagnostic = new ConnectionDiagnostic(_mockLogger.Object);
            var diagConfig = new ConnectionDiagnosticConfiguration
            {
                TestNetworkConnectivity = true,
                TestPlcSystemInfo = true,
                QualitySampleCount = 2
            };

            // Act
            var diagnosticResult = await diagnostic.RunCompleteDiagnosticAsync(_mockSlmpClient.Object, diagConfig);
            var report = diagnostic.GenerateDiagnosticReport(diagnosticResult);

            // Assert
            Assert.NotEmpty(report);
            Assert.Contains("PLC接続詳細診断", report);
            Assert.Contains("ネットワーク接続テスト", report);
            Assert.Contains("診断結果", report);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ConnectionDiagnostic(null!));
        }

        [Fact]
        public async Task TestNetworkConnectivity_ShouldHandleInvalidAddress()
        {
            // Arrange
            var diagnostic = new ConnectionDiagnostic(_mockLogger.Object);
            var invalidAddress = "invalid-address";

            // Act
            var result = await diagnostic.TestNetworkConnectivityAsync(invalidAddress);

            // Assert
            Assert.False(result.IsSuccessful);
            Assert.NotEmpty(result.ErrorMessage);
        }

        [Fact]
        public async Task TestPlcSystemInfo_ShouldHandleDisconnectedClient()
        {
            // Arrange
            _mockSlmpClient.Setup(x => x.IsConnected).Returns(false);
            var diagnostic = new ConnectionDiagnostic(_mockLogger.Object);

            // Act
            var result = await diagnostic.TestPlcSystemInfoAsync(_mockSlmpClient.Object);

            // Assert
            Assert.False(result.IsSuccessful);
            Assert.Contains("接続されていません", result.ErrorMessage);
        }
    }
}