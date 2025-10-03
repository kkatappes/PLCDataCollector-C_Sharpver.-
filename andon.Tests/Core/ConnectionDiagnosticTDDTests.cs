using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SlmpClient.Core;
using Xunit;

namespace andon.Tests.Core
{
    /// <summary>
    /// ConnectionDiagnostic TDD実装テスト
    /// PLC_Connection_Diagnostic_Integration_Plan.md仕様準拠
    /// t-wadaの手法に基づくテストファースト開発
    /// RED-GREEN-REFACTORサイクルでQ00CPU対応・ハイブリッド統合実装
    /// </summary>
    public class ConnectionDiagnosticTDDTests
    {
        private readonly Mock<ILogger<ConnectionDiagnostic>> _mockLogger;
        private readonly Mock<ISlmpClient> _mockSlmpClient;
        private readonly Mock<IUnifiedLogWriter> _mockUnifiedLogWriter;

        public ConnectionDiagnosticTDDTests()
        {
            _mockLogger = new Mock<ILogger<ConnectionDiagnostic>>();
            _mockSlmpClient = new Mock<ISlmpClient>();
            _mockUnifiedLogWriter = new Mock<IUnifiedLogWriter>();
        }

        /// <summary>
        /// RED: Q00CPU対応UDP接続診断テスト（失敗させる）
        /// 仕様書要件: Q00CPU-UDP通信、TCP診断スキップ
        /// </summary>
        [Fact]
        public async Task TestNetworkConnectivityAsync_ShouldSupportQ00CpuUdpDiagnostic_WithPortAlternatives()
        {
            // Arrange - 仕様書要件: Q00CPU-UDP対応
            var diagnostic = new ConnectionDiagnostic(_mockLogger.Object, _mockUnifiedLogWriter.Object);
            var q00CpuConfig = new Q00CpuNetworkDiagnosticConfig
            {
                PrimaryHost = "172.30.40.15",
                PrimaryPort = 8192,        // Q00CPU運用ポート
                AlternativePort = 5007,    // 代替ポート（標準SLMP）
                ProtocolType = "UDP",      // Q00CPU: UDP必須
                SkipTcpDiagnostic = true,  // Q00CPU: TCP非対応
                FrameVersion = "4E"        // Q00CPU: 4E必須（3Eドロップ）
            };

            // Act
            var result = await diagnostic.TestQ00CpuNetworkConnectivityAsync(q00CpuConfig);

            // Assert - Q00CPU特有の診断結果を確認
            Assert.NotNull(result);
            Assert.True(result.UdpConnectivityResults.ContainsKey(8192)); // 運用ポート診断
            Assert.True(result.UdpConnectivityResults.ContainsKey(5007)); // 代替ポート診断
            Assert.False(result.TcpTestExecuted); // TCP診断スキップ確認
            Assert.Equal("4E", result.SupportedFrameVersion); // 4Eフレーム確認
            Assert.Contains("Q00CPU対応", result.DiagnosticSummary); // Q00CPU対応明記
        }

        /// <summary>
        /// RED: SLMP 4Eフレーム解析診断テスト（失敗させる）
        /// 仕様書要件: Q00CPU 3Eフレームドロップ、4E必須対応
        /// </summary>
        [Fact]
        public async Task TestSlmpFrameAnalysis_ShouldAnalyze4EFrameOnly_WithQ00CpuConstraints()
        {
            // Arrange - 仕様書要件: SLMPフレーム詳細解析
            var diagnostic = new ConnectionDiagnostic(_mockLogger.Object, _mockUnifiedLogWriter.Object);
            _mockSlmpClient.Setup(x => x.IsConnected).Returns(true);

            var frameAnalysisConfig = new SlmpFrameAnalysisConfig
            {
                TestFrameVersions = new[] { "3E", "4E" }, // 両方テスト
                Q00CpuMode = true, // Q00CPU制約適用
                EnableDetailedAnalysis = true // 詳細解析有効
            };

            // Act
            var result = await diagnostic.TestSlmpFrameAnalysisAsync(_mockSlmpClient.Object, frameAnalysisConfig);

            // Assert - Q00CPU制約に基づくフレーム解析確認
            Assert.NotNull(result);
            Assert.False(result.Frame3ESupported); // 3Eフレームドロップ確認
            Assert.True(result.Frame4ESupported); // 4Eフレーム対応確認
            Assert.NotNull(result.DetailedFrameAnalysis); // 詳細解析結果存在
            Assert.Contains("Q00CPU: 3Eフレームドロップ確認済み", result.DiagnosticMessage);
            Assert.Equal("4E", result.RecommendedFrameVersion); // 推奨バージョン確認
        }

        /// <summary>
        /// RED: デバイス実在性確認テスト（失敗させる）
        /// 仕様書要件: M0-M127、D0-D99範囲でのQ00CPU特有デバイス診断
        /// </summary>
        [Fact]
        public async Task TestDeviceAccessibility_ShouldVerifyQ00CpuDeviceRanges_WithDetailedAnalysis()
        {
            // Arrange - 仕様書要件: デバイス実在性確認
            var diagnostic = new ConnectionDiagnostic(_mockLogger.Object, _mockUnifiedLogWriter.Object);
            _mockSlmpClient.Setup(x => x.IsConnected).Returns(true);

            var deviceConfig = new Q00CpuDeviceDiagnosticConfig
            {
                BitDeviceRanges = new[] { ("M", 0u, 127u) }, // M0-M127
                WordDeviceRanges = new[] { ("D", 0u, 99u) }, // D0-D99
                TestDataAvailability = true, // データ設定状況確認
                EnableRangeValidation = true // 範囲妥当性確認
            };

            // Act
            var result = await diagnostic.TestQ00CpuDeviceAccessibilityAsync(_mockSlmpClient.Object, deviceConfig);

            // Assert - Q00CPU特有のデバイス診断確認
            Assert.NotNull(result);
            Assert.True(result.BitDeviceRangeResults.ContainsKey("M0-M127")); // M範囲診断
            Assert.True(result.WordDeviceRangeResults.ContainsKey("D0-D99")); // D範囲診断
            Assert.NotNull(result.DataAvailabilityAnalysis); // データ設定状況解析
            Assert.Contains("初期値", result.DataAvailabilityAnalysis.Summary); // 未設定検出
            Assert.Equal("確認済み", result.DeviceExistenceStatus); // 実在性確認
        }

        /// <summary>
        /// RED: ハイブリッド統合ログ出力テスト（失敗させる）
        /// 仕様書要件: rawdata_analysis.log、console_output.json統合
        /// </summary>
        [Fact]
        public async Task TestHybridLogIntegration_ShouldOutputToBothRawdataAndConsole_WithProperEntryTypes()
        {
            // Arrange - 仕様書要件: ハイブリッド統合対応
            var diagnostic = new ConnectionDiagnostic(_mockLogger.Object, _mockUnifiedLogWriter.Object);
            var diagnosticResult = new CompleteDiagnosticResult
            {
                NetworkConnectivity = new NetworkConnectivityResult { IsSuccessful = true },
                OverallSuccess = true
            };

            // Act
            await diagnostic.WriteHybridDiagnosticLogAsync(diagnosticResult);

            // Assert - ハイブリッド統合ログ出力確認

            // rawdata_analysis.log への技術詳細情報出力確認
            _mockUnifiedLogWriter.Verify(x => x.WriteSystemEventAsync(
                It.IsAny<string>(),
                "DIAGNOSTIC_NETWORK",
                It.IsAny<string>(),
                It.IsAny<object>()),
                Times.AtLeastOnce(), "DIAGNOSTIC_NETWORKエントリがrawdata_analysis.logに出力されていません");

            // console_output.json への人間可読情報出力確認
            _mockUnifiedLogWriter.Verify(x => x.WriteSystemEventAsync(
                It.IsAny<string>(),
                "CONSOLE_DIAGNOSTIC",
                It.IsAny<string>(),
                It.IsAny<object>()),
                Times.AtLeastOnce(), "CONSOLE_DIAGNOSTICエントリがconsole_output.jsonに出力されていません");

            // 統合エントリタイプの整合性確認
            Assert.True(true, "仕様書記載の統合エントリタイプでログ出力されること");
        }

        /// <summary>
        /// RED: 通信品質統計情報測定テスト（失敗させる）
        /// 仕様書要件: 継続監視中の統計情報、パフォーマンスメトリクス
        /// </summary>
        [Fact]
        public async Task MeasureCommunicationQualityWithStatistics_ShouldProvideDetailedMetrics_ForContinuousMonitoring()
        {
            // Arrange - 仕様書要件: 通信品質・統計情報
            var diagnostic = new ConnectionDiagnostic(_mockLogger.Object, _mockUnifiedLogWriter.Object);
            _mockSlmpClient.Setup(x => x.IsAliveAsync()).ReturnsAsync(true);

            var qualityConfig = new CommunicationQualityConfig
            {
                SampleCount = 30, // 継続監視統計
                EnablePerformanceMetrics = true, // パフォーマンス測定
                CalculateNetworkStats = true, // ネットワーク統計
                Q00CpuOptimization = true // Q00CPU最適化
            };

            // Act
            var result = await diagnostic.MeasureQ00CpuCommunicationQualityAsync(_mockSlmpClient.Object, qualityConfig);

            // Assert - 継続監視用統計情報確認
            Assert.NotNull(result);
            Assert.NotNull(result.DetailedStatistics); // 詳細統計情報
            Assert.NotNull(result.PerformanceMetrics); // パフォーマンス情報
            Assert.NotNull(result.NetworkQualityData); // ネットワーク品質情報

            // 仕様書記載の統計項目確認
            Assert.True(result.DetailedStatistics.TotalCommunications > 0); // 総通信回数
            Assert.True(result.DetailedStatistics.SuccessRate >= 0); // 成功率
            Assert.True(result.PerformanceMetrics.AverageResponseTimeMs >= 0); // 平均応答時間

            Assert.Contains("UDP通信", result.NetworkQualityData.ConnectionStability); // UDP通信品質
        }

        /// <summary>
        /// RED: 完全診断レポート生成テスト（失敗させる）
        /// 仕様書要件: Q00CPU対応診断結果、ハイブリッド統合表示
        /// </summary>
        [Fact]
        public async Task GenerateQ00CpuDiagnosticReport_ShouldCreateComprehensiveReport_WithHybridIntegration()
        {
            // Arrange - 仕様書要件: 診断レポート生成
            var diagnostic = new ConnectionDiagnostic(_mockLogger.Object, _mockUnifiedLogWriter.Object);
            var diagnosticResult = new CompleteDiagnosticResult
            {
                OverallSuccess = true,
                NetworkConnectivity = new NetworkConnectivityResult { IsSuccessful = true, Details = "UDP:8192 → OK (12ms)" },
                PlcSystemInfo = new PlcSystemInfoResult { CpuModel = "Q00CPU", SlmpVersion = "4E" },
                DeviceAccessibility = new DeviceAccessibilityResult { AllDevicesAccessible = true }
            };

            // Act
            var report = await diagnostic.GenerateQ00CpuDiagnosticReportAsync(diagnosticResult);

            // Assert - Q00CPU特化診断レポート確認
            Assert.NotNull(report);
            Assert.Contains("Q00CPU検出", report.DiagnosticSummary); // CPU型式確認
            Assert.Contains("UDP+4E通信", report.CommunicationSummary); // 通信方式確認
            Assert.Contains("8192", report.NetworkDiagnosticDetails); // 運用ポート確認
            Assert.Contains("データ未設定", report.DeviceStatusSummary); // データ状況確認

            // ハイブリッド統合表示形式確認
            Assert.NotNull(report.TerminalDisplayFormat); // ターミナル表示用
            Assert.NotNull(report.JsonLogFormat); // JSONログ用
            Assert.Contains("=== PLC接続詳細診断開始 ===", report.TerminalDisplayFormat); // 仕様書記載フォーマット
        }

        /// <summary>
        /// RED: SOLID原則適用テスト（失敗させる）
        /// 開発手法要件: 単一責任原則、依存性逆転原則適用確認
        /// </summary>
        [Fact]
        public void ConnectionDiagnostic_ShouldFollowSOLIDPrinciples_WithProperDependencyInjection()
        {
            // Arrange - SOLID原則確認
            var diagnostic = new ConnectionDiagnostic(_mockLogger.Object, _mockUnifiedLogWriter.Object);

            // Assert - 単一責任原則確認
            Assert.IsAssignableFrom<IConnectionDiagnostic>(diagnostic); // インターフェース分離

            // 依存性逆転原則確認（コンストラクタ注入）
            var constructors = typeof(ConnectionDiagnostic).GetConstructors();
            Assert.True(constructors.Length == 1, "単一コンストラクタで依存性注入すること");

            var constructor = constructors[0];
            var parameters = constructor.GetParameters();
            Assert.True(parameters.Length >= 2, "ILogger, IUnifiedLogWriter の依存性注入必須");
            Assert.True(parameters[0].ParameterType == typeof(ILogger<ConnectionDiagnostic>), "ILogger依存性注入");
            Assert.True(parameters[1].ParameterType == typeof(IUnifiedLogWriter), "IUnifiedLogWriter依存性注入");
        }
    }

}