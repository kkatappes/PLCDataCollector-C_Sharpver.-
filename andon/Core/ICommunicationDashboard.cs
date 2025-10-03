using System;
using System.Threading.Tasks;

namespace SlmpClient.Core
{
    /// <summary>
    /// リアルタイム通信ダッシュボードインターフェース
    /// SOLID原則: インターフェース分離原則適用
    /// 依存性逆転原則による抽象化定義
    /// </summary>
    public interface ICommunicationDashboard : IAsyncDisposable, IDisposable
    {
        /// <summary>ダッシュボード表示が有効かどうか</summary>
        bool IsDisplayEnabled { get; set; }

        /// <summary>更新間隔</summary>
        TimeSpan UpdateInterval { get; set; }

        /// <summary>リアルタイム接続状況表示</summary>
        Task DisplayRealtimeConnectionStatusAsync(RealtimeConnectionInfo connectionInfo);

        /// <summary>パフォーマンスメトリクス表示</summary>
        Task DisplayPerformanceMetricsAsync();

        /// <summary>エラー統計・アラート表示</summary>
        Task DisplayErrorStatisticsAndAlertsAsync();

        /// <summary>自動更新開始</summary>
        Task StartAutoUpdateAsync();

        /// <summary>自動更新停止</summary>
        Task StopAutoUpdateAsync();

        /// <summary>ハイブリッド統合ログ出力</summary>
        Task WriteHybridDashboardLogAsync(DashboardSnapshot snapshot);
    }

    #region CommunicationDashboard Data Models

    public class RealtimeConnectionInfo
    {
        public string TargetAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool IsConnected { get; set; }
        public DateTime SessionStartTime { get; set; }
        public string ConnectionMethod { get; set; } = string.Empty;
    }


    public class ErrorStatisticsSnapshot
    {
        public double ErrorRate { get; set; }
        public int TotalErrors { get; set; }
        public int TotalContinuedOperations { get; set; }
    }

    public class DashboardSnapshot
    {
        public DateTime Timestamp { get; set; }
        public string ConnectionStatus { get; set; } = string.Empty;
        public PerformanceStatistics? PerformanceMetrics { get; set; }
        public ErrorStatisticsSnapshot? ErrorStatistics { get; set; }
        public string AlertLevel { get; set; } = string.Empty;
    }

    #endregion
}