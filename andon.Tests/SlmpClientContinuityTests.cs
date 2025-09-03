using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using SlmpClient.Core;
using SlmpClient.Constants;
using SlmpClient.Exceptions;
using SlmpClient.Transport;

namespace SlmpClient.Tests
{
    /// <summary>
    /// SLMP継続機能テスト
    /// 稼働第一の思想に基づく継続動作の検証
    /// </summary>
    public class SlmpClientContinuityTests : IDisposable
    {
        #region Private Fields

        private readonly ILogger<SlmpClient.Core.SlmpClient> _logger;
        private readonly Mock<ISlmpTransport> _mockTransport;
        private SlmpConnectionSettings _settings;
        private SlmpClient.Core.SlmpClient? _client;

        #endregion

        #region Constructor & Disposal

        public SlmpClientContinuityTests()
        {
            _logger = NullLogger<SlmpClient.Core.SlmpClient>.Instance;
            _mockTransport = new Mock<ISlmpTransport>();
            
            // 稼働第一の設定を適用
            _settings = new SlmpConnectionSettings();
            _settings.ApplyManufacturingOperationFirstSettings();
        }

        public void Dispose()
        {
            _client?.Dispose();
        }

        #endregion

        #region Test Helpers

        /// <summary>
        /// モックTransportを使用したSlmpClientを作成
        /// </summary>
        /// <returns>設定済みSlmpClient</returns>
        private SlmpClient.Core.SlmpClient CreateClientWithMockTransport()
        {
            // モックTransportの基本設定
            _mockTransport.Setup(t => t.IsConnected).Returns(true);
            _mockTransport.Setup(t => t.ConnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockTransport.Setup(t => t.DisconnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockTransport.Setup(t => t.IsAliveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            // プライベートコンストラクタを回避するため、Reflectionを使用
            var client = new SlmpClient.Core.SlmpClient("127.0.0.1", _settings, _logger);
            
            // プライベートフィールド _transport を置き換え
            var transportField = typeof(SlmpClient.Core.SlmpClient).GetField("_transport", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            transportField?.SetValue(client, _mockTransport.Object);

            return client;
        }

        #endregion

        #region Continuity Mode Tests

        /// <summary>
        /// 継続モード：タイムアウト時のビットデバイス読み取りテスト
        /// </summary>
        [Fact]
        public async Task ReadBitDevicesAsync_WithTimeout_ReturnDefaultValues_Test()
        {
            // Arrange
            _client = CreateClientWithMockTransport();
            await _client.ConnectAsync();

            // SendAndReceiveAsyncでタイムアウト例外をスロー
            _mockTransport.Setup(t => t.SendAndReceiveAsync(
                    It.IsAny<byte[]>(), 
                    It.IsAny<TimeSpan>(), 
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new SlmpTimeoutException("UDP communication timeout", TimeSpan.FromSeconds(1)));

            // Act
            var result = await _client.ReadBitDevicesAsync(DeviceCode.M, 100, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.Length);
            Assert.All(result, value => Assert.Equal(_settings.ContinuitySettings.DefaultBitValue, value));

            // 統計情報を確認
            var stats = _client.ErrorStatistics.GetSummary();
            Assert.Equal(1, stats.TotalOperations);
            Assert.Equal(1, stats.TotalErrors);
            Assert.Equal(1, stats.TotalContinuedOperations);
            Assert.Equal(100.0, stats.ContinuityRate, 1);
        }

        /// <summary>
        /// 継続モード：通信エラー時のワードデバイス読み取りテスト
        /// </summary>
        [Fact]
        public async Task ReadWordDevicesAsync_WithCommunicationError_ReturnDefaultValues_Test()
        {
            // Arrange  
            _client = CreateClientWithMockTransport();
            await _client.ConnectAsync();

            // SendAndReceiveAsyncで通信例外をスロー
            _mockTransport.Setup(t => t.SendAndReceiveAsync(
                    It.IsAny<byte[]>(), 
                    It.IsAny<TimeSpan>(), 
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new SlmpCommunicationException("UDP socket error"));

            // Act
            var result = await _client.ReadWordDevicesAsync(DeviceCode.D, 200, 5);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Length);
            Assert.All(result, value => Assert.Equal(_settings.ContinuitySettings.DefaultWordValue, value));

            // 統計情報を確認
            var stats = _client.ErrorStatistics.GetSummary();
            Assert.Equal(1, stats.TotalOperations);
            Assert.Equal(1, stats.TotalErrors);
            Assert.Equal(1, stats.TotalContinuedOperations);
            Assert.Single(stats.TopErrors);
            Assert.Equal("SlmpCommunicationException", stats.TopErrors[0].ErrorType);
        }

        /// <summary>
        /// 継続モード：複数回エラー発生時の統計確認テスト
        /// </summary>
        [Fact]
        public async Task MultipleErrors_StatisticsAccumulation_Test()
        {
            // Arrange
            _client = CreateClientWithMockTransport();
            await _client.ConnectAsync();

            _mockTransport.Setup(t => t.SendAndReceiveAsync(
                    It.IsAny<byte[]>(), 
                    It.IsAny<TimeSpan>(), 
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new SlmpTimeoutException("UDP communication timeout", TimeSpan.FromSeconds(1)));

            // Act - 複数回エラーを発生させる
            var tasks = new Task[5];
            for (int i = 0; i < 5; i++)
            {
                tasks[i] = _client.ReadBitDevicesAsync(DeviceCode.M, (uint)(100 + i), 10);
            }
            await Task.WhenAll(tasks);

            // Assert
            var stats = _client.ErrorStatistics.GetSummary();
            Assert.Equal(5, stats.TotalOperations);
            Assert.Equal(5, stats.TotalErrors);
            Assert.Equal(5, stats.TotalContinuedOperations);
            Assert.Equal(100.0, stats.ErrorRate, 1);
            Assert.Equal(100.0, stats.ContinuityRate, 1);
        }

        #endregion

        #region Exception Mode Tests

        /// <summary>
        /// 例外モード：タイムアウト時に例外をスローすることを確認
        /// </summary>
        [Fact]
        public async Task ReadBitDevicesAsync_ExceptionMode_ThrowsException_Test()
        {
            // Arrange
            _settings.ContinuitySettings.Mode = ErrorHandlingMode.ThrowException;
            _client = CreateClientWithMockTransport();
            await _client.ConnectAsync();

            _mockTransport.Setup(t => t.SendAndReceiveAsync(
                    It.IsAny<byte[]>(), 
                    It.IsAny<TimeSpan>(), 
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new SlmpTimeoutException("UDP communication timeout", TimeSpan.FromSeconds(1)));

            // Act & Assert
            await Assert.ThrowsAsync<SlmpTimeoutException>(
                () => _client.ReadBitDevicesAsync(DeviceCode.M, 100, 10));

            // 統計情報を確認（エラーは記録されるが継続動作はなし）
            var stats = _client.ErrorStatistics.GetSummary();
            Assert.Equal(1, stats.TotalOperations);
            Assert.Equal(1, stats.TotalErrors);
            Assert.Equal(0, stats.TotalContinuedOperations); // 継続動作なし
        }

        #endregion

        #region Custom Default Values Tests

        /// <summary>
        /// カスタムデフォルト値テスト
        /// </summary>
        [Fact]
        public async Task ReadBitDevicesAsync_CustomDefaultValue_Test()
        {
            // Arrange
            _settings.ContinuitySettings.DefaultBitValue = true; // デフォルト値をtrueに設定
            _client = CreateClientWithMockTransport();
            await _client.ConnectAsync();

            _mockTransport.Setup(t => t.SendAndReceiveAsync(
                    It.IsAny<byte[]>(), 
                    It.IsAny<TimeSpan>(), 
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new SlmpTimeoutException("UDP communication timeout", TimeSpan.FromSeconds(1)));

            // Act
            var result = await _client.ReadBitDevicesAsync(DeviceCode.M, 100, 8);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(8, result.Length);
            Assert.All(result, value => Assert.True(value)); // すべてtrueであることを確認
        }

        /// <summary>
        /// カスタムワードデフォルト値テスト
        /// </summary>
        [Fact]
        public async Task ReadWordDevicesAsync_CustomDefaultValue_Test()
        {
            // Arrange
            _settings.ContinuitySettings.DefaultWordValue = 0xFFFF; // デフォルト値を65535に設定
            _client = CreateClientWithMockTransport();
            await _client.ConnectAsync();

            _mockTransport.Setup(t => t.SendAndReceiveAsync(
                    It.IsAny<byte[]>(), 
                    It.IsAny<TimeSpan>(), 
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new SlmpTimeoutException("UDP communication timeout", TimeSpan.FromSeconds(1)));

            // Act
            var result = await _client.ReadWordDevicesAsync(DeviceCode.D, 100, 3);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Length);
            Assert.All(result, value => Assert.Equal(0xFFFF, value));
        }

        #endregion

        #region Statistics Reset Tests

        /// <summary>
        /// 統計リセット機能テスト
        /// </summary>
        [Fact]
        public async Task ErrorStatistics_Reset_Test()
        {
            // Arrange
            _client = CreateClientWithMockTransport();
            await _client.ConnectAsync();

            _mockTransport.Setup(t => t.SendAndReceiveAsync(
                    It.IsAny<byte[]>(), 
                    It.IsAny<TimeSpan>(), 
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new SlmpTimeoutException("UDP communication timeout", TimeSpan.FromSeconds(1)));

            // Act - エラーを発生させる
            await _client.ReadBitDevicesAsync(DeviceCode.M, 100, 10);
            
            var statsBeforeReset = _client.ErrorStatistics.GetSummary();
            Assert.Equal(1, statsBeforeReset.TotalErrors);

            // 統計をリセット
            _client.ErrorStatistics.Reset();

            // Assert
            var statsAfterReset = _client.ErrorStatistics.GetSummary();
            Assert.Equal(0, statsAfterReset.TotalOperations);
            Assert.Equal(0, statsAfterReset.TotalErrors);
            Assert.Equal(0, statsAfterReset.TotalContinuedOperations);
            Assert.Empty(statsAfterReset.TopErrors);
        }

        #endregion

        #region Integration Tests

        /// <summary>
        /// 正常動作とエラー継続の混合テスト
        /// </summary>
        [Fact]
        public async Task MixedOperations_SuccessAndContinuity_Test()
        {
            // Arrange
            _client = CreateClientWithMockTransport();
            await _client.ConnectAsync();

            // 正常レスポンス（5ビット分の適切なSLMPレスポンス）
            var normalResponse = new byte[]
            {
                0xD4, 0x00, 0x00, 0x00, 0x00, 0x00, // 4E response header
                0x01, 0x01, 0x03, 0xFF, 0x00,       // target response
                0x01, 0x00,                          // data length (1 byte for 5 bits)
                0x00, 0x00,                          // end code (success)
                0x00                                 // bit data: all 5 bits false
            };
            var callCount = 0;

            _mockTransport.Setup(t => t.SendAndReceiveAsync(
                    It.IsAny<byte[]>(), 
                    It.IsAny<TimeSpan>(), 
                    It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    callCount++;
                    if (callCount % 2 == 0) // 偶数回目はエラー
                    {
                        throw new SlmpTimeoutException("UDP communication timeout", TimeSpan.FromSeconds(1));
                    }
                    return Task.FromResult(normalResponse); // 奇数回目は正常
                });

            // Act - 4回実行（2回成功、2回エラー継続）
            var results = new bool[4][];
            for (int i = 0; i < 4; i++)
            {
                results[i] = await _client.ReadBitDevicesAsync(DeviceCode.M, (uint)(100 + i), 5);
            }

            // Assert
            Assert.All(results, result => 
            {
                Assert.NotNull(result);
                Assert.Equal(5, result.Length);
            });

            var stats = _client.ErrorStatistics.GetSummary();
            Assert.Equal(4, stats.TotalOperations);
            Assert.Equal(2, stats.TotalErrors);        // 2回エラー
            Assert.Equal(2, stats.TotalContinuedOperations); // 2回継続動作
            Assert.Equal(50.0, stats.ErrorRate, 1);   // 50%エラー率
            Assert.Equal(100.0, stats.ContinuityRate, 1); // 100%継続率
        }

        #endregion
    }
}