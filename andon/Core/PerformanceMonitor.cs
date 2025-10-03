using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using SlmpClient.Constants;

namespace SlmpClient.Core
{
    /// <summary>
    /// SLMP通信パフォーマンス監視クラス
    /// レスポンス時間統計、スループット分析、パフォーマンス傾向を監視
    /// </summary>
    public class PerformanceMonitor : IPerformanceMonitor
    {
        #region Private Fields

        private readonly ILogger<PerformanceMonitor> _logger;
        private readonly UnifiedLogWriter _unifiedLogWriter;
        private readonly ConcurrentQueue<ResponseTimeEntry> _responseTimeHistory = new();
        private readonly ConcurrentDictionary<string, OperationStats> _operationStats = new();
        private readonly Timer _reportTimer;
        private readonly object _statsLock = new();

        private long _totalOperations = 0;
        private double _totalResponseTime = 0;
        private DateTime _startTime = DateTime.Now;
        private DateTime _lastReportTime = DateTime.Now;

        #endregion

        #region Properties

        /// <summary>
        /// 総操作数
        /// </summary>
        public long TotalOperations => _totalOperations;

        /// <summary>
        /// 平均レスポンス時間（ミリ秒）
        /// </summary>
        public double AverageResponseTime => _totalOperations > 0 ? _totalResponseTime / _totalOperations : 0;

        /// <summary>
        /// 監視開始からの経過時間
        /// </summary>
        public TimeSpan TotalMonitoringTime => DateTime.Now - _startTime;

        /// <summary>
        /// 操作/秒（スループット）
        /// </summary>
        public double OperationsPerSecond => _totalOperations / Math.Max(TotalMonitoringTime.TotalSeconds, 1);

        #endregion

        public PerformanceMonitor(
            ILogger<PerformanceMonitor> logger,
            UnifiedLogWriter unifiedLogWriter,
            TimeSpan? reportInterval = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unifiedLogWriter = unifiedLogWriter ?? throw new ArgumentNullException(nameof(unifiedLogWriter));

            // デフォルト30秒間隔でレポート出力
            var interval = reportInterval ?? TimeSpan.FromSeconds(30);
            _reportTimer = new Timer(GeneratePerformanceReport, null, interval, interval);
        }

        /// <summary>
        /// 後方互換性のためのコンストラクタ（UnifiedLogWriter無し）
        /// </summary>
        public PerformanceMonitor(
            ILogger<PerformanceMonitor> logger,
            TimeSpan? reportInterval = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unifiedLogWriter = null!; // 既存コードとの互換性のためnull許可

            // デフォルト30秒間隔でレポート出力
            var interval = reportInterval ?? TimeSpan.FromSeconds(30);
            _reportTimer = new Timer(GeneratePerformanceReport, null, interval, interval);
        }

        /// <summary>
        /// レスポンス時間を記録
        /// </summary>
        public void RecordResponseTime(
            string operationType,
            DeviceCode deviceCode,
            uint deviceAddress,
            uint deviceCount,
            TimeSpan responseTime,
            bool success = true)
        {
            var entry = new ResponseTimeEntry
            {
                Timestamp = DateTime.Now,
                OperationType = operationType,
                DeviceCode = deviceCode,
                DeviceAddress = deviceAddress,
                DeviceCount = deviceCount,
                ResponseTimeMs = responseTime.TotalMilliseconds,
                Success = success
            };

            // 履歴に追加（最大1000エントリを保持）
            _responseTimeHistory.Enqueue(entry);
            while (_responseTimeHistory.Count > 1000)
            {
                _responseTimeHistory.TryDequeue(out _);
            }

            // 統計情報を更新
            lock (_statsLock)
            {
                _totalOperations++;
                if (success)
                {
                    _totalResponseTime += responseTime.TotalMilliseconds;
                }

                // 操作タイプ別統計を更新
                var operationKey = $"{operationType}_{deviceCode}";
                _operationStats.AddOrUpdate(operationKey,
                    new OperationStats
                    {
                        OperationType = operationType,
                        DeviceCode = deviceCode,
                        Count = 1,
                        TotalResponseTime = responseTime.TotalMilliseconds,
                        MinResponseTime = responseTime.TotalMilliseconds,
                        MaxResponseTime = responseTime.TotalMilliseconds,
                        SuccessCount = success ? 1 : 0
                    },
                    (key, existing) =>
                    {
                        existing.Count++;
                        if (success)
                        {
                            existing.TotalResponseTime += responseTime.TotalMilliseconds;
                            existing.SuccessCount++;
                            existing.MinResponseTime = Math.Min(existing.MinResponseTime, responseTime.TotalMilliseconds);
                            existing.MaxResponseTime = Math.Max(existing.MaxResponseTime, responseTime.TotalMilliseconds);
                        }
                        return existing;
                    });
            }
        }

        /// <summary>
        /// 現在のパフォーマンス統計を取得
        /// </summary>
        public PerformanceStatistics GetCurrentStatistics()
        {
            lock (_statsLock)
            {
                var recentEntries = _responseTimeHistory
                    .Where(e => e.Timestamp > DateTime.Now.AddMinutes(-5))
                    .ToList();

                var recentSuccessEntries = recentEntries.Where(e => e.Success).ToList();

                return new PerformanceStatistics
                {
                    TotalOperations = (int)_totalOperations,
                    AverageResponseTime = AverageResponseTime,
                    TotalMonitoringTime = TotalMonitoringTime,
                    OperationsPerSecond = OperationsPerSecond,
                    Recent5MinuteStats = recentSuccessEntries.Any() ? new RecentStatistics
                    {
                        OperationCount = recentSuccessEntries.Count,
                        AverageResponseTime = recentSuccessEntries.Average(e => e.ResponseTimeMs),
                        MinResponseTime = recentSuccessEntries.Min(e => e.ResponseTimeMs),
                        MaxResponseTime = recentSuccessEntries.Max(e => e.ResponseTimeMs),
                        OperationsPerSecond = recentSuccessEntries.Count / 300.0 // 5分 = 300秒
                    } : null,
                    OperationBreakdown = _operationStats.Values.Select(op => new OperationTypeStats
                    {
                        OperationType = op.OperationType,
                        DeviceCode = op.DeviceCode.ToString(),
                        Count = (int)op.Count,
                        AverageResponseTime = op.AverageResponseTime,
                        SuccessRate = op.SuccessRate,
                        MinResponseTime = op.MinResponseTime,
                        MaxResponseTime = op.MaxResponseTime
                    }).ToArray()
                };
            }
        }

        /// <summary>
        /// パフォーマンスレポートを生成（定期実行）
        /// </summary>
        private async void GeneratePerformanceReport(object? state)
        {
            try
            {
                var stats = GetCurrentStatistics();
                var currentTime = DateTime.Now;
                var timeSinceLastReport = currentTime - _lastReportTime;

                _logger.LogInformation("📊 SLMP通信パフォーマンスレポート");
                _logger.LogInformation("  期間: {TimeSinceLastReport:F1}秒", timeSinceLastReport.TotalSeconds);
                _logger.LogInformation("  総操作数: {TotalOperations}", stats.TotalOperations);
                _logger.LogInformation("  平均レスポンス時間: {AverageResponseTime:F2}ms", stats.AverageResponseTime);
                _logger.LogInformation("  スループット: {OperationsPerSecond:F2} ops/sec", stats.OperationsPerSecond);

                // 最近5分間の統計
                if (stats.Recent5MinuteStats != null)
                {
                    _logger.LogInformation("  最近5分:");
                    _logger.LogInformation("    操作数: {RecentOperations}", stats.Recent5MinuteStats.OperationCount);
                    _logger.LogInformation("    平均: {RecentAvg:F2}ms", stats.Recent5MinuteStats.AverageResponseTime);
                    _logger.LogInformation("    最小: {RecentMin:F2}ms", stats.Recent5MinuteStats.MinResponseTime);
                    _logger.LogInformation("    最大: {RecentMax:F2}ms", stats.Recent5MinuteStats.MaxResponseTime);
                }

                // 操作タイプ別統計（上位5つ）
                var topOperations = stats.OperationBreakdown
                    .OrderByDescending(o => o.Count)
                    .Take(5)
                    .ToList();

                if (topOperations.Any())
                {
                    _logger.LogInformation("  操作タイプ別統計 (上位5つ):");
                    foreach (var op in topOperations)
                    {
                        _logger.LogInformation("    {OperationType}_{DeviceCode}: {Count}回, 平均{Avg:F2}ms, 成功率{SuccessRate:F1}%",
                            op.OperationType, op.DeviceCode, op.Count, op.AverageResponseTime, op.SuccessRate);
                    }
                }

                _lastReportTime = currentTime;

                // PERFORMANCE_METRICSエントリを出力
                await WritePerformanceMetricsAsync(stats, currentTime);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "パフォーマンスレポート生成中にエラーが発生しました");
            }
        }

        /// <summary>
        /// PERFORMANCE_METRICSエントリを出力
        /// </summary>
        private async Task WritePerformanceMetricsAsync(PerformanceStatistics stats, DateTime timestamp)
        {
            var sessionId = $"session_{timestamp:yyyyMMdd_HHmmss}";

            // エラー率を計算
            var totalSuccess = _operationStats.Values.Sum(o => o.SuccessCount);
            var successRate = _totalOperations > 0 ? (totalSuccess * 100.0) / _totalOperations : 100.0;

            var metricsInfo = new PerformanceMetricsInfo
            {
                SessionId = sessionId,
                NetworkQuality = new NetworkQualityData
                {
                    AverageLatency = stats.AverageResponseTime,
                    PacketLoss = 100.0 - successRate, // エラー率をパケットロス率として使用
                    ConnectionStability = successRate > 95 ? "Excellent" : successRate > 90 ? "Good" : successRate > 80 ? "Fair" : "Poor"
                },
                SlmpPerformance = new SlmpPerformanceData
                {
                    AverageResponseTime = stats.AverageResponseTime,
                    MaxResponseTime = stats.Recent5MinuteStats?.MaxResponseTime ?? 0,
                    MinResponseTime = stats.Recent5MinuteStats?.MinResponseTime ?? 0,
                    SuccessRate = successRate,
                    TotalOperations = (int)stats.TotalOperations
                },
                SystemResource = new SystemResourceData
                {
                    CpuUsage = 0, // TODO: 実際のCPU使用率取得実装
                    MemoryUsage = GC.GetTotalMemory(false) / (1024.0 * 1024.0), // MB単位のメモリ使用量
                    ThreadCount = System.Threading.ThreadPool.ThreadCount
                }
            };

            if (_unifiedLogWriter != null)
            {
                await _unifiedLogWriter.WritePerformanceMetricsAsync(metricsInfo);
            }
        }

        /// <summary>
        /// パフォーマンス警告をチェック (インターフェース実装)
        /// </summary>
        public void CheckPerformanceAlerts()
        {
            CheckPerformanceAlerts(100, 5.0);
        }

        /// <summary>
        /// パフォーマンス警告をチェック (パラメータ指定版)
        /// </summary>
        public void CheckPerformanceAlerts(double responseTimeThresholdMs = 100, double errorRateThreshold = 5.0)
        {
            var stats = GetCurrentStatistics();

            // レスポンス時間警告
            if (stats.AverageResponseTime > responseTimeThresholdMs)
            {
                _logger.LogWarning("⚠️ レスポンス時間警告: 平均{AverageResponseTime:F2}ms (閾値: {Threshold}ms)",
                    stats.AverageResponseTime, responseTimeThresholdMs);
            }

            // エラー率警告
            var totalSuccess = _operationStats.Values.Sum(o => o.SuccessCount);
            var errorRate = _totalOperations > 0 ? ((_totalOperations - totalSuccess) * 100.0) / _totalOperations : 0;

            if (errorRate > errorRateThreshold)
            {
                _logger.LogWarning("⚠️ エラー率警告: {ErrorRate:F1}% (閾値: {Threshold}%)",
                    errorRate, errorRateThreshold);
            }
        }

        public void Dispose()
        {
            _reportTimer?.Dispose();
        }
    }

    #region Data Classes

    /// <summary>
    /// レスポンス時間エントリ
    /// </summary>
    public class ResponseTimeEntry
    {
        public DateTime Timestamp { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public DeviceCode DeviceCode { get; set; }
        public uint DeviceAddress { get; set; }
        public uint DeviceCount { get; set; }
        public double ResponseTimeMs { get; set; }
        public bool Success { get; set; }
    }

    /// <summary>
    /// 操作統計
    /// </summary>
    public class OperationStats
    {
        public string OperationType { get; set; } = string.Empty;
        public DeviceCode DeviceCode { get; set; }
        public long Count { get; set; }
        public long SuccessCount { get; set; }
        public double TotalResponseTime { get; set; }
        public double MinResponseTime { get; set; }
        public double MaxResponseTime { get; set; }

        public double AverageResponseTime => SuccessCount > 0 ? TotalResponseTime / SuccessCount : 0;
        public double SuccessRate => Count > 0 ? (SuccessCount * 100.0) / Count : 0;
    }

    /// <summary>
    /// パフォーマンス統計 - TDD設計対応
    /// </summary>
    public class PerformanceStatistics
    {
        public int TotalOperations { get; set; }
        public double AverageResponseTime { get; set; }
        public TimeSpan TotalMonitoringTime { get; set; }
        public double OperationsPerSecond { get; set; }
        public RecentStatistics? Recent5MinuteStats { get; set; }
        public OperationTypeStats[] OperationBreakdown { get; set; } = Array.Empty<OperationTypeStats>();
    }

    /// <summary>
    /// 最近の統計 - TDD設計対応
    /// </summary>
    public class RecentStatistics
    {
        public int OperationCount { get; set; }
        public double AverageResponseTime { get; set; }
        public double MinResponseTime { get; set; }
        public double MaxResponseTime { get; set; }
        public double OperationsPerSecond { get; set; }
    }

    /// <summary>
    /// 操作タイプ別統計 - TDD設計対応
    /// </summary>
    public class OperationTypeStats
    {
        public string OperationType { get; set; } = string.Empty;
        public string DeviceCode { get; set; } = string.Empty;
        public int Count { get; set; }
        public double AverageResponseTime { get; set; }
        public double SuccessRate { get; set; }
        public double MinResponseTime { get; set; }
        public double MaxResponseTime { get; set; }
    }

    #endregion
}