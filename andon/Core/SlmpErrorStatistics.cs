using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using SlmpClient.Exceptions;

namespace SlmpClient.Core
{
    /// <summary>
    /// SLMPエラー統計情報管理クラス
    /// 稼働第一の思想に基づくエラー監視機能
    /// </summary>
    public class SlmpErrorStatistics
    {
        #region Private Fields

        private readonly ILogger<SlmpErrorStatistics> _logger;
        private readonly ConcurrentDictionary<string, ErrorCounter> _errorCounters = new();
        private readonly ConcurrentDictionary<string, DateTime> _lastNotificationTimes = new();
        private readonly object _statisticsLock = new();
        private long _totalOperations = 0;
        private long _totalErrors = 0;
        private long _totalContinuedOperations = 0;

        #endregion

        #region Properties

        /// <summary>
        /// 総操作数
        /// </summary>
        public long TotalOperations => _totalOperations;

        /// <summary>
        /// 総エラー数
        /// </summary>
        public long TotalErrors => _totalErrors;

        /// <summary>
        /// 継続動作した操作数
        /// </summary>
        public long TotalContinuedOperations => _totalContinuedOperations;

        /// <summary>
        /// エラー率（パーセント）
        /// </summary>
        public double ErrorRate => _totalOperations > 0 ? (_totalErrors * 100.0) / _totalOperations : 0.0;

        /// <summary>
        /// 継続動作率（パーセント）
        /// </summary>
        public double ContinuityRate => _totalErrors > 0 ? (_totalContinuedOperations * 100.0) / _totalErrors : 0.0;

        #endregion

        #region Constructor

        /// <summary>
        /// エラー統計管理を初期化
        /// </summary>
        /// <param name="logger">ロガー</param>
        public SlmpErrorStatistics(ILogger<SlmpErrorStatistics>? logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SlmpErrorStatistics>.Instance;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 操作開始を記録
        /// </summary>
        public void RecordOperation()
        {
            Interlocked.Increment(ref _totalOperations);
        }

        /// <summary>
        /// エラー発生を記録
        /// </summary>
        /// <param name="errorType">エラー種別</param>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="exception">発生した例外</param>
        /// <param name="continuitySettings">継続動作設定</param>
        /// <returns>通知すべきかどうか</returns>
        public bool RecordError(string errorType, string deviceCode, uint startAddress, Exception exception, ContinuitySettings continuitySettings)
        {
            Interlocked.Increment(ref _totalErrors);

            var errorKey = $"{errorType}-{deviceCode}-{startAddress}";
            var counter = _errorCounters.AddOrUpdate(errorKey, 
                new ErrorCounter(errorType, deviceCode, startAddress, exception),
                (key, existing) => { existing.Increment(exception); return existing; });

            // 通知頻度制限をチェック
            bool shouldNotify = ShouldNotify(errorKey, continuitySettings.MaxNotificationFrequencySeconds);

            if (shouldNotify && continuitySettings.EnableContinuityLogging)
            {
                LogError(counter, exception, continuitySettings.NotificationLevel);
            }

            return shouldNotify;
        }

        /// <summary>
        /// 継続動作を記録
        /// </summary>
        /// <param name="errorType">エラー種別</param>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="defaultValueUsed">使用されたデフォルト値の説明</param>
        public void RecordContinuedOperation(string errorType, string deviceCode, uint startAddress, string defaultValueUsed)
        {
            Interlocked.Increment(ref _totalContinuedOperations);

            _logger.LogInformation(
                "システム継続動作: {ErrorType} で {DeviceCode}:{StartAddress} にデフォルト値 ({DefaultValue}) を返却",
                errorType, deviceCode, startAddress, defaultValueUsed);
        }

        /// <summary>
        /// エラー統計情報をリセット
        /// </summary>
        public void Reset()
        {
            lock (_statisticsLock)
            {
                _errorCounters.Clear();
                _lastNotificationTimes.Clear();
                Interlocked.Exchange(ref _totalOperations, 0);
                Interlocked.Exchange(ref _totalErrors, 0);
                Interlocked.Exchange(ref _totalContinuedOperations, 0);
            }

            _logger.LogInformation("エラー統計情報をリセットしました");
        }

        /// <summary>
        /// 統計サマリーを取得
        /// </summary>
        /// <returns>統計サマリー</returns>
        public StatisticsSummary GetSummary()
        {
            lock (_statisticsLock)
            {
                return new StatisticsSummary
                {
                    TotalOperations = _totalOperations,
                    TotalErrors = _totalErrors,
                    TotalContinuedOperations = _totalContinuedOperations,
                    ErrorRate = ErrorRate,
                    ContinuityRate = ContinuityRate,
                    TopErrors = _errorCounters.Values
                        .OrderByDescending(c => c.Count)
                        .Take(10)
                        .Select(c => new ErrorSummary
                        {
                            ErrorType = c.ErrorType,
                            DeviceCode = c.DeviceCode,
                            StartAddress = c.StartAddress,
                            Count = c.Count,
                            LastOccurred = c.LastOccurred,
                            LastExceptionMessage = c.LastExceptionMessage
                        })
                        .ToList()
                };
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 通知すべきかどうかを判定
        /// </summary>
        /// <param name="errorKey">エラーキー</param>
        /// <param name="maxFrequencySeconds">最大通知頻度（秒）</param>
        /// <returns>通知すべき場合はtrue</returns>
        private bool ShouldNotify(string errorKey, int maxFrequencySeconds)
        {
            var now = DateTime.UtcNow;
            var lastNotification = _lastNotificationTimes.GetOrAdd(errorKey, now);

            if ((now - lastNotification).TotalSeconds >= maxFrequencySeconds)
            {
                _lastNotificationTimes[errorKey] = now;
                return true;
            }

            return false;
        }

        /// <summary>
        /// エラーをログに記録
        /// </summary>
        /// <param name="counter">エラーカウンター</param>
        /// <param name="exception">例外</param>
        /// <param name="notificationLevel">通知レベル</param>
        private void LogError(ErrorCounter counter, Exception exception, ErrorNotificationLevel notificationLevel)
        {
            var message = "UDP通信エラー発生 - システム継続中: {ErrorType} {DeviceCode}:{StartAddress} (発生回数: {Count}) - {ExceptionMessage}";
            
            switch (notificationLevel)
            {
                case ErrorNotificationLevel.Warning:
                    _logger.LogWarning(message, counter.ErrorType, counter.DeviceCode, counter.StartAddress, counter.Count, exception.Message);
                    break;
                case ErrorNotificationLevel.Error:
                    _logger.LogError(exception, message, counter.ErrorType, counter.DeviceCode, counter.StartAddress, counter.Count, exception.Message);
                    break;
                case ErrorNotificationLevel.Critical:
                    _logger.LogCritical(exception, message, counter.ErrorType, counter.DeviceCode, counter.StartAddress, counter.Count, exception.Message);
                    break;
                case ErrorNotificationLevel.None:
                default:
                    // 通知なし
                    break;
            }
        }

        #endregion
    }

    /// <summary>
    /// エラーカウンター
    /// </summary>
    internal class ErrorCounter
    {
        public string ErrorType { get; }
        public string DeviceCode { get; }
        public uint StartAddress { get; }
        private long _count;
        public long Count 
        { 
            get => _count; 
            private set => _count = value; 
        }
        public DateTime FirstOccurred { get; }
        public DateTime LastOccurred { get; private set; }
        public string LastExceptionMessage { get; private set; }

        public ErrorCounter(string errorType, string deviceCode, uint startAddress, Exception exception)
        {
            ErrorType = errorType;
            DeviceCode = deviceCode;
            StartAddress = startAddress;
            _count = 1;
            FirstOccurred = DateTime.UtcNow;
            LastOccurred = FirstOccurred;
            LastExceptionMessage = exception.Message;
        }

        public void Increment(Exception exception)
        {
            Interlocked.Increment(ref _count);
            LastOccurred = DateTime.UtcNow;
            LastExceptionMessage = exception.Message;
        }
    }

    /// <summary>
    /// 統計サマリー
    /// </summary>
    public class StatisticsSummary
    {
        public long TotalOperations { get; set; }
        public long TotalErrors { get; set; }
        public long TotalContinuedOperations { get; set; }
        public double ErrorRate { get; set; }
        public double ContinuityRate { get; set; }
        public List<ErrorSummary> TopErrors { get; set; } = new();
    }

    /// <summary>
    /// エラーサマリー
    /// </summary>
    public class ErrorSummary
    {
        public string ErrorType { get; set; } = string.Empty;
        public string DeviceCode { get; set; } = string.Empty;
        public uint StartAddress { get; set; }
        public long Count { get; set; }
        public DateTime LastOccurred { get; set; }
        public string LastExceptionMessage { get; set; } = string.Empty;
    }
}