using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SlmpClient.Core
{
    /// <summary>
    /// リアルタイム通信ダッシュボード実装
    /// TDD手法REFACTOR Phase: コード最適化・定数統一・メッセージ統一
    /// SOLID原則適用: 単一責任原則、依存性逆転原則、インターフェース分離原則
    /// </summary>
    public class CommunicationDashboard : ICommunicationDashboard
    {
        #region Constants

        // ダッシュボードイベントタイプ定数
        private const string EventType_ConnectionStatus = "DASHBOARD_CONNECTION_STATUS";
        private const string EventType_PerformanceMetrics = "DASHBOARD_PERFORMANCE_METRICS";
        private const string EventType_ErrorStatistics = "DASHBOARD_ERROR_STATISTICS";
        private const string EventType_Alert = "DASHBOARD_ALERT";
        private const string EventType_TechnicalSnapshot = "DASHBOARD_TECHNICAL_SNAPSHOT";
        private const string EventType_DisplayStatus = "DASHBOARD_DISPLAY_STATUS";

        // 警告閾値定数
        private const double DefaultErrorRateThreshold = 5.0;
        private const double DefaultResponseTimeThreshold = 100.0;

        // エラーメッセージ定数
        private const string ErrorMessage_ConnectionStatusDisplay = "接続状況表示中にエラーが発生しました";
        private const string ErrorMessage_PerformanceMetricsDisplay = "パフォーマンスメトリクス表示中にエラーが発生しました";
        private const string ErrorMessage_ErrorStatisticsDisplay = "エラー統計表示中にエラーが発生しました";
        private const string ErrorMessage_HybridLogOutput = "ハイブリッド統合ログ出力中にエラーが発生しました";
        private const string ErrorMessage_AutoUpdate = "自動更新実行中にエラーが発生しました";

        #endregion

        #region Private Fields

        private readonly ILogger<CommunicationDashboard> _logger;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly ISlmpErrorStatistics _errorStatistics;
        private readonly IConnectionInfoLogger _connectionLogger;
        private readonly IUnifiedLogWriter _unifiedLogWriter;
        private Timer? _updateTimer;
        private bool _disposed = false;

        #endregion

        #region Properties

        /// <summary>ダッシュボード表示が有効かどうか</summary>
        public bool IsDisplayEnabled { get; set; } = true;

        /// <summary>更新間隔</summary>
        public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromSeconds(5);

        #endregion

        #region Constructor

        /// <summary>
        /// コンストラクタ - 依存性注入によるSOLID原則適用
        /// </summary>
        public CommunicationDashboard(
            ILogger<CommunicationDashboard> logger,
            IPerformanceMonitor performanceMonitor,
            ISlmpErrorStatistics errorStatistics,
            IConnectionInfoLogger connectionLogger,
            IUnifiedLogWriter unifiedLogWriter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
            _errorStatistics = errorStatistics ?? throw new ArgumentNullException(nameof(errorStatistics));
            _connectionLogger = connectionLogger ?? throw new ArgumentNullException(nameof(connectionLogger));
            _unifiedLogWriter = unifiedLogWriter ?? throw new ArgumentNullException(nameof(unifiedLogWriter));
        }

        #endregion

        #region ICommunicationDashboard Implementation

        /// <summary>リアルタイム接続状況表示</summary>
        public async Task DisplayRealtimeConnectionStatusAsync(RealtimeConnectionInfo connectionInfo)
        {
            if (_disposed || !IsDisplayEnabled) return;

            try
            {
                var message = $"接続状況: {connectionInfo.TargetAddress}:{connectionInfo.Port} - " +
                            $"{(connectionInfo.IsConnected ? "接続中" : "切断")} " +
                            $"({connectionInfo.ConnectionMethod})";

                _logger.LogInformation("🔗 {ConnectionStatus}", message);

                // UnifiedLogWriterによるハイブリッド統合ログ出力
                await _unifiedLogWriter.WriteSystemEventAsync(
                    EventType_ConnectionStatus,
                    EventType_ConnectionStatus,
                    message,
                    new {
                        TargetAddress = connectionInfo.TargetAddress,
                        Port = connectionInfo.Port,
                        IsConnected = connectionInfo.IsConnected,
                        SessionStartTime = connectionInfo.SessionStartTime,
                        ConnectionMethod = connectionInfo.ConnectionMethod
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, ErrorMessage_ConnectionStatusDisplay);
            }
        }

        /// <summary>パフォーマンスメトリクス表示</summary>
        public async Task DisplayPerformanceMetricsAsync()
        {
            if (_disposed || !IsDisplayEnabled) return;

            try
            {
                var stats = _performanceMonitor.GetCurrentStatistics();
                var message = $"パフォーマンス: {stats.OperationsPerSecond:F2} ops/sec, " +
                            $"平均応答時間: {stats.AverageResponseTime:F2}ms";

                _logger.LogInformation("📊 {PerformanceMetrics}", message);

                // UnifiedLogWriterによるハイブリッド統合ログ出力
                await _unifiedLogWriter.WriteSystemEventAsync(
                    EventType_PerformanceMetrics,
                    EventType_PerformanceMetrics,
                    message,
                    new {
                        TotalOperations = stats.TotalOperations,
                        AverageResponseTime = stats.AverageResponseTime,
                        OperationsPerSecond = stats.OperationsPerSecond,
                        TotalMonitoringTime = stats.TotalMonitoringTime
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, ErrorMessage_PerformanceMetricsDisplay);
            }
        }

        /// <summary>エラー統計・アラート表示</summary>
        public async Task DisplayErrorStatisticsAndAlertsAsync()
        {
            if (_disposed || !IsDisplayEnabled) return;

            try
            {
                var errorRate = _errorStatistics.ErrorRate;
                var totalErrors = _errorStatistics.TotalErrors;
                var continuedOps = _errorStatistics.TotalContinuedOperations;

                var message = $"エラー統計: エラー率 {errorRate:F1}%, " +
                            $"総エラー数: {totalErrors}, 継続動作数: {continuedOps}";

                _logger.LogInformation("⚠️ {ErrorStatistics}", message);

                // UnifiedLogWriterによるハイブリッド統合ログ出力
                await _unifiedLogWriter.WriteSystemEventAsync(
                    EventType_ErrorStatistics,
                    EventType_ErrorStatistics,
                    message,
                    new {
                        ErrorRate = errorRate,
                        TotalErrors = totalErrors,
                        TotalContinuedOperations = continuedOps
                    });

                // アラート閾値チェック
                await CheckAndDisplayAlertsAsync(errorRate);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, ErrorMessage_ErrorStatisticsDisplay);
            }
        }

        /// <summary>自動更新開始</summary>
        public async Task StartAutoUpdateAsync()
        {
            if (_disposed) return;

            _updateTimer = new Timer(async _ => await PerformAutoUpdateAsync(),
                                   null, UpdateInterval, UpdateInterval);

            _logger.LogInformation("🔄 ダッシュボード自動更新開始 (間隔: {UpdateInterval})", UpdateInterval);
            await Task.CompletedTask;
        }

        /// <summary>自動更新停止</summary>
        public async Task StopAutoUpdateAsync()
        {
            _updateTimer?.Dispose();
            _updateTimer = null;

            _logger.LogInformation("⏹️ ダッシュボード自動更新停止");
            await Task.CompletedTask;
        }

        /// <summary>ハイブリッド統合ログ出力</summary>
        public async Task WriteHybridDashboardLogAsync(DashboardSnapshot snapshot)
        {
            if (_disposed) return;

            try
            {
                // rawdata_analysis.log への技術詳細情報
                await _unifiedLogWriter.WriteSystemEventAsync(
                    EventType_TechnicalSnapshot,
                    EventType_TechnicalSnapshot,
                    "Dashboard Technical Snapshot",
                    new {
                        Timestamp = snapshot.Timestamp,
                        ConnectionStatus = snapshot.ConnectionStatus,
                        PerformanceMetrics = snapshot.PerformanceMetrics,
                        ErrorStatistics = snapshot.ErrorStatistics,
                        AlertLevel = snapshot.AlertLevel
                    });

                // console_output.json への人間可読情報
                await _unifiedLogWriter.WriteSystemEventAsync(
                    EventType_DisplayStatus,
                    EventType_DisplayStatus,
                    $"Dashboard Status: {snapshot.ConnectionStatus} - Alert Level: {snapshot.AlertLevel}",
                    new {
                        Timestamp = snapshot.Timestamp,
                        ConnectionStatus = snapshot.ConnectionStatus,
                        AlertLevel = snapshot.AlertLevel
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, ErrorMessage_HybridLogOutput);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// アラートチェックと表示
        /// </summary>
        private async Task CheckAndDisplayAlertsAsync(double errorRate)
        {
            var performanceStats = _performanceMonitor.GetCurrentStatistics();

            // エラー率アラート
            if (errorRate > DefaultErrorRateThreshold)
            {
                var alertMessage = $"高エラー率警告: {errorRate:F1}% (閾値: {DefaultErrorRateThreshold}%)";
                await _unifiedLogWriter.WriteSystemEventAsync(
                    EventType_Alert,
                    EventType_Alert,
                    alertMessage,
                    new { Type = "ErrorRate", Value = errorRate, Threshold = DefaultErrorRateThreshold });
            }

            // レスポンス時間アラート
            if (performanceStats.AverageResponseTime > DefaultResponseTimeThreshold)
            {
                var alertMessage = $"高レスポンス時間警告: {performanceStats.AverageResponseTime:F2}ms (閾値: {DefaultResponseTimeThreshold}ms)";
                await _unifiedLogWriter.WriteSystemEventAsync(
                    EventType_Alert,
                    EventType_Alert,
                    alertMessage,
                    new { Type = "ResponseTime", Value = performanceStats.AverageResponseTime, Threshold = DefaultResponseTimeThreshold });
            }
        }

        /// <summary>
        /// 自動更新実行
        /// </summary>
        private async Task PerformAutoUpdateAsync()
        {
            if (_disposed || !IsDisplayEnabled) return;

            try
            {
                await DisplayPerformanceMetricsAsync();
                await DisplayErrorStatisticsAndAlertsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, ErrorMessage_AutoUpdate);
            }
        }

        #endregion

        #region Dispose Pattern

        /// <summary>
        /// 非同期リソース廃棄
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                await StopAutoUpdateAsync();
                _disposed = true;
            }
        }

        /// <summary>
        /// 同期リソース廃棄
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _updateTimer?.Dispose();
                _disposed = true;
            }
        }

        #endregion

        #region Backward Compatibility Methods

        /// <summary>後方互換性: 接続情報を更新</summary>
        public void UpdateConnectionInfo(string targetAddress, int port, bool isConnected)
        {
            // TDD設計では非同期メソッドに移行済み
            // 互換性のためメソッドは残すが、実装は最小限
            _logger.LogDebug("Legacy UpdateConnectionInfo called: {TargetAddress}:{Port} - {IsConnected}",
                targetAddress, port, isConnected);
        }

        /// <summary>後方互換性: ダッシュボードを即座に表示</summary>
        public void DisplayNow()
        {
            if (_disposed || !IsDisplayEnabled) return;

            // 非同期バージョンを同期呼び出し（簡易版）
            try
            {
                DisplayPerformanceMetricsAsync().Wait();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Legacy DisplayNow failed");
            }
        }

        /// <summary>後方互換性: 詳細統計レポートを表示</summary>
        public void DisplayDetailedReport()
        {
            if (_disposed || !IsDisplayEnabled) return;

            try
            {
                var stats = _performanceMonitor.GetCurrentStatistics();
                _logger.LogInformation("📈 詳細パフォーマンスレポート");
                _logger.LogInformation("  総操作数: {TotalOperations}", stats.TotalOperations);
                _logger.LogInformation("  平均レスポンス時間: {AverageResponseTime:F2}ms", stats.AverageResponseTime);
                _logger.LogInformation("  スループット: {OperationsPerSecond:F2} ops/sec", stats.OperationsPerSecond);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Legacy DisplayDetailedReport failed");
            }
        }

        /// <summary>後方互換性: アラート状況を表示</summary>
        public void DisplayAlerts()
        {
            if (_disposed || !IsDisplayEnabled) return;

            try
            {
                var stats = _performanceMonitor.GetCurrentStatistics();
                var errorRate = _errorStatistics.ErrorRate;

                if (stats.AverageResponseTime > DefaultResponseTimeThreshold)
                {
                    _logger.LogWarning("⚠️ 高レスポンス時間: {AverageResponseTime:F2}ms", stats.AverageResponseTime);
                }

                if (errorRate > DefaultErrorRateThreshold)
                {
                    _logger.LogWarning("❌ 高エラー率: {ErrorRate:F1}%", errorRate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Legacy DisplayAlerts failed");
            }
        }

        #endregion
    }
}