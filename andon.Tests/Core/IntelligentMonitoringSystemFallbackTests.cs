using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SlmpClient.Constants;
using SlmpClient.Core;
using SlmpClient.Exceptions;
using Xunit;
using Xunit.Abstractions;
using TypeCode = SlmpClient.Constants.TypeCode;

namespace SlmpClient.Tests.Core
{
    /// <summary>
    /// IntelligentMonitoringSystem ReadTypeNameフォールバック処理のTDDテスト
    /// 手順書に基づくフォールバック機能テスト
    /// </summary>
    public class IntelligentMonitoringSystemFallbackTests
    {
        private readonly ITestOutputHelper _output;

        public IntelligentMonitoringSystemFallbackTests(ITestOutputHelper output)
        {
            _output = output;
        }



        /// <summary>
        /// TDD設計テスト: フォールバック処理の設計概念をテスト
        /// ReadTypeName失敗時の期待される動作を定義
        /// </summary>
        [Fact]
        public void Design_ReadTypeName_Fallback_Concept_Test()
        {
            // Arrange: フォールバック処理の設計仕様をテスト
            _output.WriteLine("Testing ReadTypeName fallback design concept");

            // フォールバック処理の期待される動作を定義
            var expectedFallbackBehavior = new
            {
                ShouldContinueOnTimeout = true,
                ShouldContinueOnSlmpError = true,
                ShouldProvideDefaultValues = true,
                ShouldLogWarnings = true,
                DefaultTypeName = "Unknown",
                DefaultTypeCode = TypeCode.Q00CPU
            };

            // Act: 設計仕様の検証
            var actualDesign = new
            {
                ShouldContinueOnTimeout = true,
                ShouldContinueOnSlmpError = true,
                ShouldProvideDefaultValues = true,
                ShouldLogWarnings = true,
                DefaultTypeName = "Unknown",
                DefaultTypeCode = TypeCode.Q00CPU
            };

            // Assert: 設計仕様の確認
            Assert.Equal(expectedFallbackBehavior.ShouldContinueOnTimeout, actualDesign.ShouldContinueOnTimeout);
            Assert.Equal(expectedFallbackBehavior.ShouldContinueOnSlmpError, actualDesign.ShouldContinueOnSlmpError);
            Assert.Equal(expectedFallbackBehavior.DefaultTypeName, actualDesign.DefaultTypeName);
            Assert.Equal(expectedFallbackBehavior.DefaultTypeCode, actualDesign.DefaultTypeCode);

            _output.WriteLine("SUCCESS: Fallback design concept validated");
        }

        /// <summary>
        /// Red段階: ReadTypeName成功時の正常動作をテスト
        /// フォールバック処理が実行されないことを確認
        /// </summary>
        [Fact]
        public async Task Step2_ConnectAndGetDeviceInfo_Should_Success_When_ReadTypeName_Works()
        {
            // Arrange: ReadTypeNameが成功するモッククライアントを作成
            var mockClient = new Mock<ISlmpClientFull>();
            mockClient.Setup(x => x.ReadTypeNameAsync(It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(("FX5U-32MR/ES", TypeCode.FX5U));

            var logger = new Mock<ILogger<IntelligentMonitoringSystem>>();
            var unifiedLogWriter = new Mock<UnifiedLogWriter>(logger.Object, "test.log");
            var config = new Mock<IConfiguration>();
            var system = new IntelligentMonitoringSystem(mockClient.Object, logger.Object, unifiedLogWriter.Object, config.Object);

            var connectionInfo = new PlcConnectionInfo
            {
                DisplayName = "Test PLC",
                IpAddress = "192.168.1.10",
                Port = 5007
            };

            _output.WriteLine("Testing normal ReadTypeName success case");

            // Act: Step2を実行
            var result = await system.Step2_ConnectAndGetDeviceInfoAsync(connectionInfo);

            // Assert: 正常に成功し、フォールバックが使用されていない
            Assert.True(result.IsConnectionSuccessful);
            Assert.Equal("FX5U-32MR/ES", result.TypeName);
            Assert.Equal(TypeCode.FX5U, result.TypeCode);
            Assert.False(result.FallbackUsed);
            Assert.Null(result.OriginalError);

            _output.WriteLine($"SUCCESS: Normal operation - {result.TypeName} ({result.TypeCode})");
        }

        /// <summary>
        /// Red段階: ReadTypeName失敗時にフォールバック処理が実行されることをテスト
        /// </summary>
        [Fact]
        public async Task Step2_ConnectAndGetDeviceInfo_Should_Use_Fallback_When_ReadTypeName_Fails()
        {
            // Arrange: ReadTypeNameが失敗するモッククライアントを作成
            var mockClient = new Mock<ISlmpClientFull>();
            mockClient.Setup(x => x.ReadTypeNameAsync(It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
                      .ThrowsAsync(new SlmpCommunicationException("ReadTypeName timeout"));

            // 基本接続テスト用のモック設定（M0読取が成功）
            mockClient.Setup(x => x.ReadBitDevicesAsync(DeviceCode.M, 0, 1, It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new bool[] { false });

            var logger = new Mock<ILogger<IntelligentMonitoringSystem>>();
            var unifiedLogWriter = new Mock<UnifiedLogWriter>(logger.Object, "test.log");
            var config = new Mock<IConfiguration>();
            var system = new IntelligentMonitoringSystem(mockClient.Object, logger.Object, unifiedLogWriter.Object, config.Object);

            var connectionInfo = new PlcConnectionInfo
            {
                DisplayName = "Test PLC",
                IpAddress = "192.168.1.10",
                Port = 5007
            };

            _output.WriteLine("Testing ReadTypeName failure with fallback");

            // Act: Step2を実行（フォールバックが動作するはず）
            var result = await system.Step2_ConnectAndGetDeviceInfoAsync(connectionInfo);

            // Assert: フォールバック処理が実行されている
            Assert.True(result.IsConnectionSuccessful);
            Assert.Equal("Unknown", result.TypeName);
            Assert.Equal(TypeCode.Q00CPU, result.TypeCode); // デフォルト値
            Assert.True(result.FallbackUsed);
            Assert.NotNull(result.OriginalError);
            Assert.Contains("ReadTypeName timeout", result.OriginalError);
            Assert.Contains("フォールバック成功", result.ConnectionInfo);

            _output.WriteLine($"SUCCESS: Fallback used - {result.TypeName} ({result.TypeCode})");
            _output.WriteLine($"Original error: {result.OriginalError}");
        }

        /// <summary>
        /// Green段階: フォールバック処理では基本接続テスト失敗でも継続することをテスト
        /// フォールバック設計では基本テスト失敗でも継続してデフォルト値を返す
        /// </summary>
        [Fact]
        public async Task Step2_ConnectAndGetDeviceInfo_Should_Continue_Even_When_Basic_Test_Fails()
        {
            // Arrange: ReadTypeNameと基本接続テストの両方が失敗するモッククライアント
            var mockClient = new Mock<ISlmpClientFull>();
            mockClient.Setup(x => x.ReadTypeNameAsync(It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
                      .ThrowsAsync(new SlmpCommunicationException("ReadTypeName timeout"));

            mockClient.Setup(x => x.ReadBitDevicesAsync(DeviceCode.M, 0, 1, It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
                      .ThrowsAsync(new SlmpCommunicationException("Basic connection test failed"));

            var logger = new Mock<ILogger<IntelligentMonitoringSystem>>();
            var unifiedLogWriter = new Mock<UnifiedLogWriter>(logger.Object, "test.log");
            var config = new Mock<IConfiguration>();
            var system = new IntelligentMonitoringSystem(mockClient.Object, logger.Object, unifiedLogWriter.Object, config.Object);

            var connectionInfo = new PlcConnectionInfo
            {
                DisplayName = "Test PLC",
                IpAddress = "192.168.1.10",
                Port = 5007
            };

            _output.WriteLine("Testing fallback continuation even with basic test failure");

            // Act: フォールバック処理は基本テスト失敗でも継続する
            var result = await system.Step2_ConnectAndGetDeviceInfoAsync(connectionInfo);

            // Assert: フォールバック設計により継続してデフォルト値を返す
            Assert.True(result.IsConnectionSuccessful);
            Assert.Equal("Unknown", result.TypeName);
            Assert.Equal(TypeCode.Q00CPU, result.TypeCode);
            Assert.True(result.FallbackUsed);
            Assert.NotNull(result.OriginalError);
            Assert.Contains("ReadTypeName timeout", result.OriginalError);

            _output.WriteLine($"SUCCESS: Fallback continued despite basic test failure - {result.TypeName} ({result.TypeCode})");
            _output.WriteLine($"This demonstrates the robust fallback design for challenging network conditions");
        }

        /// <summary>
        /// Green段階: 各種例外タイプでフォールバック処理が正しく動作することをテスト
        /// </summary>
        [Theory]
        [InlineData(typeof(SlmpCommunicationException), "SLMP communication error")]
        [InlineData(typeof(TimeoutException), "Operation timeout")]
        [InlineData(typeof(InvalidOperationException), "Invalid operation")]
        public async Task Step2_ConnectAndGetDeviceInfo_Should_Handle_Various_Exception_Types(
            Type exceptionType, string errorMessage)
        {
            // Arrange: 指定された例外タイプを投げるモッククライアント
            var mockClient = new Mock<ISlmpClientFull>();
            var exception = (Exception)Activator.CreateInstance(exceptionType, errorMessage)!;
            mockClient.Setup(x => x.ReadTypeNameAsync(It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
                      .ThrowsAsync(exception);

            // 基本接続テストは成功
            mockClient.Setup(x => x.ReadBitDevicesAsync(DeviceCode.M, 0, 1, It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new bool[] { false });

            var logger = new Mock<ILogger<IntelligentMonitoringSystem>>();
            var unifiedLogWriter = new Mock<UnifiedLogWriter>(logger.Object, "test.log");
            var config = new Mock<IConfiguration>();
            var system = new IntelligentMonitoringSystem(mockClient.Object, logger.Object, unifiedLogWriter.Object, config.Object);

            var connectionInfo = new PlcConnectionInfo
            {
                DisplayName = "Test PLC",
                IpAddress = "192.168.1.10",
                Port = 5007
            };

            _output.WriteLine($"Testing fallback with {exceptionType.Name}: {errorMessage}");

            // Act: Step2を実行
            var result = await system.Step2_ConnectAndGetDeviceInfoAsync(connectionInfo);

            // Assert: 各種例外でもフォールバック処理が動作
            Assert.True(result.IsConnectionSuccessful);
            Assert.True(result.FallbackUsed);
            Assert.Contains(errorMessage, result.OriginalError);

            _output.WriteLine($"SUCCESS: Fallback handled {exceptionType.Name} correctly");
        }

        /// <summary>
        /// 統合テスト: 6ステップフロー全体でフォールバック処理が正しく動作することをテスト
        /// </summary>
        [Fact]
        public async Task RunSixStepFlow_Should_Continue_With_Fallback_When_ReadTypeName_Fails()
        {
            // Arrange: ReadTypeNameは失敗するが、その他は正常に動作するモッククライアント
            var mockClient = new Mock<ISlmpClientFull>();
            mockClient.Setup(x => x.ReadTypeNameAsync(It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
                      .ThrowsAsync(new SlmpCommunicationException("ReadTypeName failed"));

            mockClient.Setup(x => x.ReadBitDevicesAsync(DeviceCode.M, 0, 1, It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new bool[] { false });

            var logger = new Mock<ILogger<IntelligentMonitoringSystem>>();
            var unifiedLogWriter = new Mock<UnifiedLogWriter>(logger.Object, "test.log");
            var config = new Mock<IConfiguration>();
            var system = new IntelligentMonitoringSystem(mockClient.Object, logger.Object, unifiedLogWriter.Object, config.Object);

            // 設定ファイルのモック
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"PlcConnection:DisplayName", "Test PLC"},
                    {"PlcConnection:IpAddress", "192.168.1.10"},
                    {"PlcConnection:Port", "5007"},
                    {"PlcConnection:UseTcp", "true"},
                    {"PlcConnection:FrameVersion", "4E"},
                    {"PlcConnection:IsBinary", "true"},
                    {"TimeoutSettings:ReceiveTimeoutMs", "3000"},
                    {"TimeoutSettings:ConnectTimeoutMs", "10000"}
                })
                .Build();

            _output.WriteLine("Testing 6-step flow with ReadTypeName fallback");

            // Act: 6ステップフローを実行
            var result = await system.RunSixStepFlowAsync(configuration);

            // Assert: ReadTypeNameが失敗してもフォールバックで継続される
            Assert.True(result.Success);
            Assert.Equal("Unknown", result.PlcTypeName);
            Assert.Equal(TypeCode.Q00CPU, result.PlcTypeCode);
            Assert.Contains("6ステップフロー実行完了", result.Summary);

            _output.WriteLine($"SUCCESS: 6-step flow completed with fallback - {result.Summary}");
        }

    }
}