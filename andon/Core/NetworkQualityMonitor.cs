using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SlmpClient.Core
{
    /// <summary>
    /// ネットワーク品質監視クラス
    /// 接続安定性、パケットロス、ネットワーク遅延を監視
    /// </summary>
    public class NetworkQualityMonitor : IDisposable
    {
        #region Private Fields

        private readonly ILogger<NetworkQualityMonitor> _logger;
        private readonly string _targetAddress;
        private readonly Timer _monitoringTimer;
        private readonly ConcurrentQueue<PingResult> _pingHistory = new();
        private readonly ConcurrentQueue<ConnectionEvent> _connectionEvents = new();
        private readonly object _qualityLock = new();

        private bool _disposed = false;
        private DateTime _monitoringStartTime = DateTime.Now;
        private long _totalPings = 0;
        private long _successfulPings = 0;
        private long _connectionDrops = 0;
        private long _reconnections = 0;

        #endregion

        #region Properties

        /// <summary>
        /// パケットロス率（パーセント）
        /// </summary>
        public double PacketLossRate => _totalPings > 0 ? ((_totalPings - _successfulPings) * 100.0) / _totalPings : 0;

        /// <summary>
        /// 平均Ping時間（ミリ秒）
        /// </summary>
        public double AveragePingTime
        {
            get
            {
                var successfulPings = _pingHistory.Where(p => p.Success).ToList();
                return successfulPings.Any() ? successfulPings.Average(p => p.RoundTripTime) : 0;
            }
        }

        /// <summary>
        /// 接続安定性スコア（0-100）
        /// </summary>
        public double ConnectionStabilityScore
        {
            get
            {
                var packetLoss = PacketLossRate;
                var avgPing = AveragePingTime;

                // パケットロスとPing時間を基にスコアを計算
                var score = 100.0;
                score -= packetLoss * 2; // パケットロス1%につき2点減点
                score -= Math.Max(0, (avgPing - 50) / 10); // 50ms超過分について10msごとに1点減点

                return Math.Max(0, Math.Min(100, score));
            }
        }

        /// <summary>
        /// 監視間隔
        /// </summary>
        public TimeSpan MonitoringInterval { get; set; } = TimeSpan.FromSeconds(10);

        #endregion

        public NetworkQualityMonitor(
            ILogger<NetworkQualityMonitor> logger,
            string targetAddress,
            TimeSpan? monitoringInterval = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _targetAddress = targetAddress ?? throw new ArgumentNullException(nameof(targetAddress));

            if (monitoringInterval.HasValue)
                MonitoringInterval = monitoringInterval.Value;

            // 定期監視開始
            _monitoringTimer = new Timer(PerformNetworkCheck, null, TimeSpan.Zero, MonitoringInterval);
        }

        /// <summary>
        /// 接続イベントを記録
        /// </summary>
        public void RecordConnectionEvent(ConnectionEventType eventType, string? details = null)
        {
            var connectionEvent = new ConnectionEvent
            {
                Timestamp = DateTime.Now,
                EventType = eventType,
                Details = details ?? string.Empty
            };

            _connectionEvents.Enqueue(connectionEvent);

            // 最大500イベントを保持
            while (_connectionEvents.Count > 500)
            {
                _connectionEvents.TryDequeue(out _);
            }

            // 統計更新
            lock (_qualityLock)
            {
                switch (eventType)
                {
                    case ConnectionEventType.Disconnected:
                        _connectionDrops++;
                        break;
                    case ConnectionEventType.Reconnected:
                        _reconnections++;
                        break;
                }
            }

            // 重要イベントのログ出力
            switch (eventType)
            {
                case ConnectionEventType.Connected:
                    _logger.LogInformation("🔗 ネットワーク接続確立: {Details}", details);
                    break;
                case ConnectionEventType.Disconnected:
                    _logger.LogWarning("🔌 ネットワーク接続切断: {Details}", details);
                    break;
                case ConnectionEventType.Reconnected:
                    _logger.LogInformation("🔄 ネットワーク再接続: {Details}", details);
                    break;
                case ConnectionEventType.Timeout:
                    _logger.LogWarning("⏰ ネットワークタイムアウト: {Details}", details);
                    break;
            }
        }

        /// <summary>
        /// ネットワーク品質レポートを取得
        /// </summary>
        public NetworkQualityReport GetQualityReport()
        {
            lock (_qualityLock)
            {
                var recentPings = _pingHistory
                    .Where(p => p.Timestamp > DateTime.Now.AddMinutes(-10))
                    .ToList();

                var recentEvents = _connectionEvents
                    .Where(e => e.Timestamp > DateTime.Now.AddHours(-1))
                    .GroupBy(e => e.EventType)
                    .ToDictionary(g => g.Key, g => g.Count());

                return new NetworkQualityReport
                {
                    MonitoringDuration = DateTime.Now - _monitoringStartTime,
                    TotalPings = _totalPings,
                    SuccessfulPings = _successfulPings,
                    PacketLossRate = PacketLossRate,
                    AveragePingTime = AveragePingTime,
                    ConnectionStabilityScore = ConnectionStabilityScore,
                    ConnectionDrops = _connectionDrops,
                    Reconnections = _reconnections,
                    Recent10Minutes = recentPings.Any() ? new RecentNetworkStats
                    {
                        PingCount = recentPings.Count,
                        SuccessfulPings = recentPings.Count(p => p.Success),
                        AveragePingTime = recentPings.Where(p => p.Success).Any() ?
                            recentPings.Where(p => p.Success).Average(p => p.RoundTripTime) : 0,
                        MinPingTime = recentPings.Where(p => p.Success).Any() ?
                            recentPings.Where(p => p.Success).Min(p => p.RoundTripTime) : 0,
                        MaxPingTime = recentPings.Where(p => p.Success).Any() ?
                            recentPings.Where(p => p.Success).Max(p => p.RoundTripTime) : 0
                    } : null,
                    RecentEventCounts = recentEvents
                };
            }
        }

        /// <summary>
        /// 品質アラートをチェック
        /// </summary>
        public void CheckQualityAlerts()
        {
            var report = GetQualityReport();

            // パケットロス警告
            if (report.PacketLossRate > 5.0)
            {
                _logger.LogWarning("📡 ネットワーク品質警告: パケットロス率 {PacketLossRate:F1}%", report.PacketLossRate);
            }

            // 高遅延警告
            if (report.AveragePingTime > 100)
            {
                _logger.LogWarning("📡 ネットワーク品質警告: 高遅延 {AveragePingTime:F1}ms", report.AveragePingTime);
            }

            // 接続安定性警告
            if (report.ConnectionStabilityScore < 70)
            {
                _logger.LogWarning("📡 ネットワーク品質警告: 接続安定性スコア {Score:F1}/100", report.ConnectionStabilityScore);
            }

            // 頻繁な切断警告
            if (report.ConnectionDrops > 10 && report.MonitoringDuration.TotalHours > 1)
            {
                var dropsPerHour = report.ConnectionDrops / report.MonitoringDuration.TotalHours;
                if (dropsPerHour > 5)
                {
                    _logger.LogWarning("📡 ネットワーク品質警告: 頻繁な接続切断 {DropsPerHour:F1}回/時間", dropsPerHour);
                }
            }
        }

        /// <summary>
        /// 詳細品質レポートを表示
        /// </summary>
        public void DisplayDetailedQualityReport()
        {
            var report = GetQualityReport();

            _logger.LogInformation("📡 ネットワーク品質詳細レポート");
            _logger.LogInformation("================================");
            _logger.LogInformation("対象アドレス: {TargetAddress}", _targetAddress);
            _logger.LogInformation("監視時間: {MonitoringDuration}", FormatTimeSpan(report.MonitoringDuration));
            _logger.LogInformation("");

            _logger.LogInformation("🔸 接続品質:");
            _logger.LogInformation("  接続安定性スコア: {Score:F1}/100", report.ConnectionStabilityScore);
            _logger.LogInformation("  パケットロス率: {PacketLossRate:F2}%", report.PacketLossRate);
            _logger.LogInformation("  平均Ping時間: {AveragePingTime:F2}ms", report.AveragePingTime);

            _logger.LogInformation("🔸 接続イベント:");
            _logger.LogInformation("  接続切断回数: {ConnectionDrops}", report.ConnectionDrops);
            _logger.LogInformation("  再接続回数: {Reconnections}", report.Reconnections);

            if (report.Recent10Minutes != null)
            {
                var recent = report.Recent10Minutes;
                _logger.LogInformation("🔸 最近10分間:");
                _logger.LogInformation("  Ping成功率: {SuccessRate:F1}% ({SuccessfulPings}/{Total})",
                    recent.PingCount > 0 ? (recent.SuccessfulPings * 100.0) / recent.PingCount : 0,
                    recent.SuccessfulPings, recent.PingCount);
                _logger.LogInformation("  Ping時間範囲: {MinPingTime:F1}ms - {MaxPingTime:F1}ms",
                    recent.MinPingTime, recent.MaxPingTime);
            }

            if (report.RecentEventCounts.Any())
            {
                _logger.LogInformation("🔸 最近1時間のイベント:");
                foreach (var eventCount in report.RecentEventCounts)
                {
                    _logger.LogInformation("  {EventType}: {Count}回", eventCount.Key, eventCount.Value);
                }
            }

            _logger.LogInformation("================================");
        }

        /// <summary>
        /// ネットワークチェック実行（タイマーコールバック）
        /// </summary>
        private async void PerformNetworkCheck(object? state)
        {
            if (_disposed) return;

            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(_targetAddress, 5000);

                var pingResult = new PingResult
                {
                    Timestamp = DateTime.Now,
                    Success = reply.Status == IPStatus.Success,
                    RoundTripTime = reply.RoundtripTime,
                    Status = reply.Status
                };

                // 履歴に追加
                _pingHistory.Enqueue(pingResult);
                while (_pingHistory.Count > 1000)
                {
                    _pingHistory.TryDequeue(out _);
                }

                // 統計更新
                lock (_qualityLock)
                {
                    _totalPings++;
                    if (pingResult.Success)
                    {
                        _successfulPings++;
                    }
                }

                // 品質アラートチェック（5分間隔）
                if (_totalPings % 30 == 0) // 10秒間隔なので30回で5分
                {
                    CheckQualityAlerts();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ネットワーク品質チェック中にエラーが発生しました");
            }
        }

        /// <summary>
        /// 時間間隔をフォーマット
        /// </summary>
        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
                return $"{timeSpan.Days}d {timeSpan.Hours:D2}h {timeSpan.Minutes:D2}m";
            if (timeSpan.TotalHours >= 1)
                return $"{timeSpan.Hours}h {timeSpan.Minutes:D2}m {timeSpan.Seconds:D2}s";
            if (timeSpan.TotalMinutes >= 1)
                return $"{timeSpan.Minutes}m {timeSpan.Seconds:D2}s";
            return $"{timeSpan.Seconds}.{timeSpan.Milliseconds:D3}s";
        }

        public void Dispose()
        {
            if (_disposed) return;

            _monitoringTimer?.Dispose();
            _disposed = true;
        }
    }

    #region Data Classes

    /// <summary>
    /// Ping結果
    /// </summary>
    public class PingResult
    {
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
        public long RoundTripTime { get; set; }
        public IPStatus Status { get; set; }
    }

    /// <summary>
    /// 接続イベント
    /// </summary>
    public class ConnectionEvent
    {
        public DateTime Timestamp { get; set; }
        public ConnectionEventType EventType { get; set; }
        public string Details { get; set; } = string.Empty;
    }

    /// <summary>
    /// 接続イベントタイプ
    /// </summary>
    public enum ConnectionEventType
    {
        Connected,
        Disconnected,
        Reconnected,
        Timeout,
        Error
    }

    /// <summary>
    /// ネットワーク品質レポート
    /// </summary>
    public class NetworkQualityReport
    {
        public TimeSpan MonitoringDuration { get; set; }
        public long TotalPings { get; set; }
        public long SuccessfulPings { get; set; }
        public double PacketLossRate { get; set; }
        public double AveragePingTime { get; set; }
        public double ConnectionStabilityScore { get; set; }
        public long ConnectionDrops { get; set; }
        public long Reconnections { get; set; }
        public RecentNetworkStats? Recent10Minutes { get; set; }
        public Dictionary<ConnectionEventType, int> RecentEventCounts { get; set; } = new();
    }

    /// <summary>
    /// 最近のネットワーク統計
    /// </summary>
    public class RecentNetworkStats
    {
        public int PingCount { get; set; }
        public int SuccessfulPings { get; set; }
        public double AveragePingTime { get; set; }
        public double MinPingTime { get; set; }
        public double MaxPingTime { get; set; }
    }

    #endregion
}