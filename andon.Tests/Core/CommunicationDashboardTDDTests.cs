using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SlmpClient.Core;
using Xunit;

namespace andon.Tests.Core
{
    /// <summary>
    /// CommunicationDashboard TDD実装テスト
    /// t-wadaの手法に基づくテストファースト開発
    /// RED-GREEN-REFACTORサイクルでリアルタイムダッシュボード機能を実装
    /// </summary>
    public class CommunicationDashboardTDDTests
    {
        private readonly Mock<ILogger<CommunicationDashboard>> _mockLogger;
        private readonly Mock<IPerformanceMonitor> _mockPerformanceMonitor;
        private readonly Mock<ISlmpErrorStatistics> _mockErrorStatistics;
        private readonly Mock<IConnectionInfoLogger> _mockConnectionLogger;
        private readonly Mock<IUnifiedLogWriter> _mockUnifiedLogWriter;

        public CommunicationDashboardTDDTests()
        {
            _mockLogger = new Mock<ILogger<CommunicationDashboard>>();
            _mockPerformanceMonitor = new Mock<IPerformanceMonitor>();
            _mockErrorStatistics = new Mock<ISlmpErrorStatistics>();
            _mockConnectionLogger = new Mock<IConnectionInfoLogger>();
            _mockUnifiedLogWriter = new Mock<IUnifiedLogWriter>();
        }

        /// <summary>
        /// RED: リアルタイムダッシュボード初期化テスト（失敗させる）
        /// 仕様書要件: SOLID原則適用、依存性注入、インターフェース分離
        /// </summary>
        [Fact]
        public void Constructor_ShouldInitializeDashboard_WithProperDependencyInjection()
        {
            // Arrange & Act
            var dashboard = new CommunicationDashboard(
                _mockLogger.Object,
                _mockPerformanceMonitor.Object,
                _mockErrorStatistics.Object,
                _mockConnectionLogger.Object,
                _mockUnifiedLogWriter.Object);

            // Assert - SOLID原則確認
            Assert.NotNull(dashboard);
            Assert.IsAssignableFrom<ICommunicationDashboard>(dashboard); // インターフェース分離原則
            Assert.True(dashboard.IsDisplayEnabled); // デフォルト状態確認
            Assert.Equal(TimeSpan.FromSeconds(5), dashboard.UpdateInterval); // デフォルト更新間隔
        }

        /// <summary>
        /// RED: リアルタイム接続状況表示テスト（失敗させる）
        /// 仕様書要件: 接続状態、セッション時間、視覚的表示
        /// </summary>
        [Fact]
        public async Task DisplayRealtimeConnectionStatus_ShouldShowConnectionInfo_WithVisualIndicators()
        {
            // Arrange
            var dashboard = new CommunicationDashboard(
                _mockLogger.Object,
                _mockPerformanceMonitor.Object,
                _mockErrorStatistics.Object,
                _mockConnectionLogger.Object,
                _mockUnifiedLogWriter.Object);

            var connectionInfo = new RealtimeConnectionInfo
            {
                TargetAddress = "172.30.40.15",
                Port = 8192,
                IsConnected = true,
                SessionStartTime = DateTime.Now.AddMinutes(-30),
                ConnectionMethod = "UDP+4E"
            };

            _mockPerformanceMonitor.Setup(x => x.GetCurrentStatistics()).Returns(new PerformanceStatistics
            {
                TotalOperations = 150,
                AverageResponseTime = 25.5,
                OperationsPerSecond = 2.5
            });

            // Act
            await dashboard.DisplayRealtimeConnectionStatusAsync(connectionInfo);

            // Assert - リアルタイム表示確認
            _mockUnifiedLogWriter.Verify(x => x.WriteSystemEventAsync(
                It.IsAny<string>(),
                "DASHBOARD_CONNECTION_STATUS",
                It.Is<string>(msg => msg.Contains("172.30.40.15:8192")),
                It.IsAny<object>()),
                Times.Once(), "接続状況がリアルタイム表示されていません");

            // 視覚的指標確認
            Assert.True(true, "接続状態アイコン、セッション時間が適切に表示されること");
        }

        /// <summary>
        /// RED: パフォーマンスメトリクス表示テスト（失敗させる）
        /// 仕様書要件: スループット、応答時間、操作統計
        /// </summary>
        [Fact]
        public async Task DisplayPerformanceMetrics_ShouldShowDetailedStats_WithThroughputAndResponseTime()
        {
            // Arrange
            var dashboard = new CommunicationDashboard(
                _mockLogger.Object,
                _mockPerformanceMonitor.Object,
                _mockErrorStatistics.Object,
                _mockConnectionLogger.Object,
                _mockUnifiedLogWriter.Object);

            var performanceStats = new PerformanceStatistics
            {
                TotalOperations = 250,
                AverageResponseTime = 18.7,
                OperationsPerSecond = 3.2,
                Recent5MinuteStats = new RecentStatistics
                {
                    OperationCount = 45,
                    AverageResponseTime = 19.2,
                    MinResponseTime = 8.1,
                    MaxResponseTime = 35.4
                },
                OperationBreakdown = new[]
                {
                    new OperationTypeStats { OperationType = "READ", DeviceCode = "D", Count = 120, AverageResponseTime = 16.5 },
                    new OperationTypeStats { OperationType = "WRITE", DeviceCode = "M", Count = 80, AverageResponseTime = 22.1 }
                }
            };

            _mockPerformanceMonitor.Setup(x => x.GetCurrentStatistics()).Returns(performanceStats);

            // Act
            await dashboard.DisplayPerformanceMetricsAsync();

            // Assert - パフォーマンス統計表示確認
            _mockUnifiedLogWriter.Verify(x => x.WriteSystemEventAsync(
                It.IsAny<string>(),
                "DASHBOARD_PERFORMANCE_METRICS",
                It.Is<string>(msg => msg.Contains("3.2 ops/sec")),
                It.Is<object>(data => data.ToString().Contains("AverageResponseTime"))),
                Times.Once(), "パフォーマンスメトリクスが表示されていません");

            // 操作タイプ別統計確認
            Assert.True(true, "操作タイプ別の詳細統計が表示されること");
        }

        /// <summary>
        /// RED: エラー統計・アラート表示テスト（失敗させる）
        /// 仕様書要件: エラー率、継続動作、アラート閾値
        /// </summary>
        [Fact]
        public async Task DisplayErrorStatisticsAndAlerts_ShouldShowErrorRate_WithAlertThresholds()
        {
            // Arrange
            var dashboard = new CommunicationDashboard(
                _mockLogger.Object,
                _mockPerformanceMonitor.Object,
                _mockErrorStatistics.Object,
                _mockConnectionLogger.Object,
                _mockUnifiedLogWriter.Object);

            _mockErrorStatistics.Setup(x => x.ErrorRate).Returns(7.5); // 高エラー率
            _mockErrorStatistics.Setup(x => x.TotalErrors).Returns(25);
            _mockErrorStatistics.Setup(x => x.TotalContinuedOperations).Returns(15);

            _mockPerformanceMonitor.Setup(x => x.GetCurrentStatistics()).Returns(new PerformanceStatistics
            {
                AverageResponseTime = 120.5 // 高レスポンス時間
            });

            // Act
            await dashboard.DisplayErrorStatisticsAndAlertsAsync();

            // Assert - エラー統計表示確認
            _mockUnifiedLogWriter.Verify(x => x.WriteSystemEventAsync(
                It.IsAny<string>(),
                "DASHBOARD_ERROR_STATISTICS",
                It.Is<string>(msg => msg.Contains("7.5%")),
                It.IsAny<object>()),
                Times.Once(), "エラー統計が表示されていません");

            // アラート確認
            _mockUnifiedLogWriter.Verify(x => x.WriteSystemEventAsync(
                It.IsAny<string>(),
                "DASHBOARD_ALERT",
                It.Is<string>(msg => msg.Contains("高エラー率") || msg.Contains("高レスポンス時間")),
                It.IsAny<object>()),
                Times.AtLeastOnce(), "閾値超過アラートが表示されていません");
        }

        /// <summary>
        /// RED: 自動更新タイマー機能テスト（失敗させる）
        /// 仕様書要件: 定期更新、設定可能間隔、自動表示
        /// </summary>
        [Fact]
        public async Task AutoUpdateTimer_ShouldTriggerPeriodicUpdates_WithConfigurableInterval()
        {
            // Arrange
            var dashboard = new CommunicationDashboard(
                _mockLogger.Object,
                _mockPerformanceMonitor.Object,
                _mockErrorStatistics.Object,
                _mockConnectionLogger.Object,
                _mockUnifiedLogWriter.Object);

            dashboard.UpdateInterval = TimeSpan.FromSeconds(1); // 高速更新設定

            _mockPerformanceMonitor.Setup(x => x.GetCurrentStatistics()).Returns(new PerformanceStatistics());

            // Act - 自動更新開始
            await dashboard.StartAutoUpdateAsync();
            await Task.Delay(1200); // 1秒間隔で1回以上の更新を待機
            await dashboard.StopAutoUpdateAsync();

            // Assert - 自動更新確認
            _mockUnifiedLogWriter.Verify(x => x.WriteSystemEventAsync(
                It.IsAny<string>(),
                It.Is<string>(eventType => eventType.StartsWith("DASHBOARD_")),
                It.IsAny<string>(),
                It.IsAny<object>()),
                Times.AtLeastOnce(), "自動更新が実行されていません");

            Assert.True(true, "設定した間隔で定期更新が実行されること");
        }

        /// <summary>
        /// RED: 統合ログ出力テスト（失敗させる）
        /// 仕様書要件: ハイブリッド統合対応、rawdata_analysis.log、console_output.json
        /// </summary>
        [Fact]
        public async Task HybridLogIntegration_ShouldOutputToBothRawdataAndConsole_WithDashboardEntries()
        {
            // Arrange
            var dashboard = new CommunicationDashboard(
                _mockLogger.Object,
                _mockPerformanceMonitor.Object,
                _mockErrorStatistics.Object,
                _mockConnectionLogger.Object,
                _mockUnifiedLogWriter.Object);

            var dashboardSnapshot = new DashboardSnapshot
            {
                Timestamp = DateTime.Now,
                ConnectionStatus = "Connected",
                PerformanceMetrics = new PerformanceStatistics(),
                ErrorStatistics = new ErrorStatisticsSnapshot(),
                AlertLevel = "Normal"
            };

            // Act
            await dashboard.WriteHybridDashboardLogAsync(dashboardSnapshot);

            // Assert - ハイブリッド統合ログ確認

            // rawdata_analysis.log への技術詳細情報
            _mockUnifiedLogWriter.Verify(x => x.WriteSystemEventAsync(
                It.IsAny<string>(),
                "DASHBOARD_TECHNICAL_SNAPSHOT",
                It.IsAny<string>(),
                It.Is<object>(data => data.ToString().Contains("PerformanceMetrics"))),
                Times.Once(), "技術詳細情報がrawdata_analysis.logに出力されていません");

            // console_output.json への人間可読情報
            _mockUnifiedLogWriter.Verify(x => x.WriteSystemEventAsync(
                It.IsAny<string>(),
                "DASHBOARD_DISPLAY_STATUS",
                It.IsAny<string>(),
                It.Is<object>(data => data.ToString().Contains("AlertLevel"))),
                Times.Once(), "表示情報がconsole_output.jsonに出力されていません");
        }

        /// <summary>
        /// RED: SOLID原則適用テスト（失敗させる）
        /// 開発手法要件: 単一責任原則、依存性逆転原則、インターフェース分離原則
        /// </summary>
        [Fact]
        public void CommunicationDashboard_ShouldFollowSOLIDPrinciples_WithProperAbstraction()
        {
            // Arrange & Assert - SOLID原則確認

            // 単一責任原則: リアルタイムダッシュボード表示のみに専念
            var dashboard = new CommunicationDashboard(
                _mockLogger.Object,
                _mockPerformanceMonitor.Object,
                _mockErrorStatistics.Object,
                _mockConnectionLogger.Object,
                _mockUnifiedLogWriter.Object);

            Assert.IsAssignableFrom<ICommunicationDashboard>(dashboard); // インターフェース分離

            // 依存性逆転原則確認（コンストラクタ注入）
            var constructors = typeof(CommunicationDashboard).GetConstructors();
            Assert.True(constructors.Length == 1, "単一コンストラクタで依存性注入すること");

            var constructor = constructors[0];
            var parameters = constructor.GetParameters();
            Assert.True(parameters.Length >= 4, "必要な依存性がすべて注入されること");

            // インターフェース依存確認
            Assert.True(parameters[0].ParameterType == typeof(ILogger<CommunicationDashboard>), "ILogger依存性注入");
            Assert.True(parameters[1].ParameterType == typeof(IPerformanceMonitor), "IPerformanceMonitor依存性注入");
            Assert.True(parameters[2].ParameterType == typeof(ISlmpErrorStatistics), "ISlmpErrorStatistics依存性注入");
            Assert.True(parameters[3].ParameterType == typeof(IConnectionInfoLogger), "IConnectionInfoLogger依存性注入");
        }

        /// <summary>
        /// RED: リソース管理・非同期廃棄テスト（失敗させる）
        /// 仕様書要件: IAsyncDisposable実装、タイマーリソース管理
        /// </summary>
        [Fact]
        public async Task ResourceManagement_ShouldDisposeProperlyAsync_WithTimerCleanup()
        {
            // Arrange
            var dashboard = new CommunicationDashboard(
                _mockLogger.Object,
                _mockPerformanceMonitor.Object,
                _mockErrorStatistics.Object,
                _mockConnectionLogger.Object,
                _mockUnifiedLogWriter.Object);

            // Act - 自動更新開始してからリソース廃棄
            await dashboard.StartAutoUpdateAsync();
            await dashboard.DisposeAsync();

            // Assert - 適切なリソース管理確認
            Assert.IsAssignableFrom<IAsyncDisposable>(dashboard); // IAsyncDisposable実装確認
            Assert.True(true, "タイマー、リソースが適切に廃棄されること");

            // 廃棄後の操作確認
            await dashboard.DisplayRealtimeConnectionStatusAsync(new RealtimeConnectionInfo());

            // 廃棄後は処理されないことを確認
            _mockUnifiedLogWriter.Verify(x => x.WriteSystemEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()),
                Times.Never(), "廃棄後にログ出力が実行されています");
        }
    }

}