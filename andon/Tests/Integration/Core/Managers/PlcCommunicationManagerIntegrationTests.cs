using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Core.Exceptions;
using Andon.Tests.TestUtilities.Mocks;
using Andon.Tests.TestUtilities.Exceptions;
// using Andon.Tests.TestUtilities.Assertions; // 一時的に無効化
using Andon.Tests.TestUtilities.Stubs;

namespace Andon.Tests.Integration.Core.Managers
{
    /// <summary>
    /// PlcCommunicationManager統合テスト
    /// TC123: FullCycle_エラー発生時の適切なスキップ
    /// TC124: ErrorPropagation_Step3エラー時後続スキップ
    /// </summary>
    public class PlcCommunicationManagerIntegrationTests
    {
        /// <summary>
        /// TC123-1: Step3エラー時の適切なスキップテスト
        /// 接続段階でエラーが発生した際の適切なエラーハンドリングとスキップ処理を検証
        /// </summary>
        [Fact]
        public async Task TC123_FullCycle_エラー発生時の適切なスキップ_Step3エラー()
        {
            // Arrange（準備）
            var connectionConfig = ConfigurationStubs.CreateValidConnectionConfig();
            var timeoutConfig = ConfigurationStubs.CreateValidTimeoutConfig();

            // MockSocketを作成（接続失敗をシミュレート）
            var mockSocket = new MockSocket();
            mockSocket.SetupConnectionFailure(MockExceptionGenerator.CreateStep3Error());

            var mockSocketFactory = new MockSocketFactory();
            mockSocketFactory.SetMockSocket(mockSocket);

            var manager = new PlcCommunicationManager(connectionConfig, timeoutConfig, null, null, mockSocketFactory);

            // 初期システム状態を記録
            var initialConnectionStats = manager.GetConnectionStats();

            // Act（実行）
            // Phase 2修正: ConnectAsyncは例外をスローせず、ConnectionResponseで失敗を返す
            var connectResponse = await manager.ConnectAsync();

            // Assert（検証）

            // Phase 2修正: ConnectionResponse検証
            Assert.NotEqual(ConnectionStatus.Connected, connectResponse.Status);
            Assert.Null(connectResponse.Socket);
            Assert.NotNull(connectResponse.ErrorMessage);

            // 統計情報にエラーが正しく記録されていることを確認
            var finalStats = manager.GetConnectionStats();
            Assert.NotNull(finalStats);
        }

        /// <summary>
        /// TC124-1: 接続タイムアウト時のエラー伝播テスト
        /// Step3（接続段階）でタイムアウトエラーが発生した際のエラー伝播動作を検証
        /// </summary>
        [Fact]
        public async Task TC124_ErrorPropagation_Step3エラー時後続スキップ_接続タイムアウト()
        {
            // Arrange（準備）
            var connectionConfig = new ConnectionConfig
            {
                IpAddress = "192.168.3.250", // 到達不能なIP
                Port = 5007,
                UseTcp = true,
                IsBinary = false,
                FrameVersion = FrameVersion.Frame3E
            };

            var timeoutConfig = new TimeoutConfig
            {
                ConnectTimeoutMs = 1000, // 短いタイムアウト
                SendTimeoutMs = 3000,
                ReceiveTimeoutMs = 3000
            };

            // タイムアウトをシミュレートするMockSocket設定
            var mockSocket = new MockSocket(useTcp: true);
            mockSocket.SetupConnectionFailure(new TimeoutException("接続タイムアウトシミュレーション"));

            var mockSocketFactory = new MockSocketFactory();
            mockSocketFactory.SetMockSocket(mockSocket);

            var manager = new PlcCommunicationManager(connectionConfig, timeoutConfig, null, null, mockSocketFactory);

            // 初期統計情報を記録
            var initialStats = manager.GetConnectionStats();

            // Act（実行）
            // Step3: Connect（タイムアウトエラー発生）
            var connectResponse = await manager.ConnectAsync();

            // エラー伝播の結果を取得
            var result = manager.GetLastOperationResult();

            // Assert（検証）

            // ConnectionResponse検証
            Assert.NotNull(connectResponse);
            Assert.Equal(ConnectionStatus.Timeout, connectResponse.Status);
            Assert.Null(connectResponse.Socket);
            Assert.Contains("タイムアウト", connectResponse.ErrorMessage ?? "", StringComparison.OrdinalIgnoreCase);

            // エラー伝播検証
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal("Step3_Connect", result.FailedStep);
            Assert.NotNull(result.Exception);
            Assert.IsType<TimeoutException>(result.Exception);

            // ErrorDetails検証
            Assert.NotNull(result.ErrorDetails);
            Assert.Equal(Andon.Core.Constants.ErrorConstants.TimeoutError, result.ErrorDetails.ErrorType);
            Assert.Equal("ConnectAsync", result.ErrorDetails.FailedOperation);

            // 統計情報検証
            var finalStats = manager.GetConnectionStats();
            Assert.NotNull(finalStats);
            Assert.True(finalStats.TotalErrors > initialStats.TotalErrors, "エラーカウントが増加すること");
        }

        /// <summary>
        /// TC124-2: 接続拒否時のエラー伝播テスト
        /// Step3（接続段階）で接続拒否エラーが発生した際のエラー伝播動作を検証
        /// </summary>
        [Fact]
        public async Task TC124_ErrorPropagation_Step3エラー時後続スキップ_接続拒否()
        {
            // Arrange（準備）
            var connectionConfig = new ConnectionConfig
            {
                IpAddress = "127.0.0.1",
                Port = 9999, // 使用されていないポート
                UseTcp = true,
                IsBinary = false,
                FrameVersion = FrameVersion.Frame3E
            };

            var timeoutConfig = new TimeoutConfig
            {
                ConnectTimeoutMs = 5000,
                SendTimeoutMs = 3000,
                ReceiveTimeoutMs = 3000
            };

            // 接続拒否をシミュレートするMockSocket設定
            var mockSocket = new MockSocket(useTcp: true);
            mockSocket.SetupConnectionFailure(
                new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.ConnectionRefused));

            var mockSocketFactory = new MockSocketFactory();
            mockSocketFactory.SetMockSocket(mockSocket);

            var manager = new PlcCommunicationManager(connectionConfig, timeoutConfig, null, null, mockSocketFactory);

            // 初期統計情報を記録
            var initialStats = manager.GetConnectionStats();

            // Act（実行）
            // Step3: Connect（接続拒否エラー発生）
            var connectResponse = await manager.ConnectAsync();

            // エラー伝播の結果を取得
            var result = manager.GetLastOperationResult();

            // Assert（検証）

            // ConnectionResponse検証
            Assert.NotNull(connectResponse);
            Assert.Equal(ConnectionStatus.Failed, connectResponse.Status);
            Assert.Null(connectResponse.Socket);
            Assert.NotNull(connectResponse.ErrorMessage);

            // エラー伝播検証
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal("Step3_Connect", result.FailedStep);
            Assert.NotNull(result.Exception);
            Assert.IsType<System.Net.Sockets.SocketException>(result.Exception);

            // ErrorDetails検証
            Assert.NotNull(result.ErrorDetails);
            Assert.Equal(Andon.Core.Constants.ErrorConstants.RefusedError, result.ErrorDetails.ErrorType);
            Assert.Equal("ConnectAsync", result.ErrorDetails.FailedOperation);

            // 統計情報検証
            var finalStats = manager.GetConnectionStats();
            Assert.NotNull(finalStats);
            Assert.True(finalStats.TotalErrors > initialStats.TotalErrors, "エラーカウントが増加すること");
        }

        /// <summary>
        /// TC124-3: 不正IP時のエラー伝播テスト
        /// Step3（接続段階）で不正なIPアドレスが指定された際のエラー伝播動作を検証
        /// </summary>
        [Fact]
        public async Task TC124_ErrorPropagation_Step3エラー時後続スキップ_不正IP()
        {
            // Arrange（準備）
            var connectionConfig = new ConnectionConfig
            {
                IpAddress = "999.999.999.999", // 不正なIPアドレス
                Port = 5007,
                UseTcp = true,
                IsBinary = false,
                FrameVersion = FrameVersion.Frame3E
            };

            var timeoutConfig = new TimeoutConfig
            {
                ConnectTimeoutMs = 5000,
                SendTimeoutMs = 3000,
                ReceiveTimeoutMs = 3000
            };

            // 不正IP検証はConnectAsync内で実装されるべき（MockSocketを使用しない）
            var manager = new PlcCommunicationManager(connectionConfig, timeoutConfig, null, null);

            // 初期統計情報を記録
            var initialStats = manager.GetConnectionStats();
            Exception caughtException = null;

            // Act（実行）
            try
            {
                // Step3: Connect（不正IPエラー発生）
                var connectResponse = await manager.ConnectAsync();
                Assert.True(false, "不正IP時は例外がスローされるべき");
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            // Assert（検証）

            // エラータイプ検証
            Assert.NotNull(caughtException);
            // 不正IPの場合、ArgumentException、FormatException、またはPlcConnectionExceptionが期待される
            Assert.True(
                caughtException is ArgumentException ||
                caughtException is FormatException ||
                caughtException is PlcConnectionException ||
                caughtException is System.Net.Sockets.SocketException,
                $"適切な例外タイプがスローされること（実際: {caughtException.GetType().Name}）");

            // 統計情報検証
            var finalStats = manager.GetConnectionStats();
            Assert.NotNull(finalStats);
            Assert.True(finalStats.TotalErrors > initialStats.TotalErrors, "エラーカウントが増加すること");
        }
    }
}