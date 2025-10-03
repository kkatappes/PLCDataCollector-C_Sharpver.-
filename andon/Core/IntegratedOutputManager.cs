using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SlmpClient.Core
{
    /// <summary>
    /// 統合出力管理クラス
    /// ターミナルとファイルへの同期出力、JSONログのターミナル用整形表示
    /// </summary>
    public class IntegratedOutputManager
    {
        private readonly ILogger<IntegratedOutputManager> _logger;
        private readonly UnifiedLogWriter _unifiedLogWriter;
        private readonly bool _enableTerminalOutput;
        private readonly bool _enableFileOutput;

        public IntegratedOutputManager(
            ILogger<IntegratedOutputManager> logger,
            UnifiedLogWriter unifiedLogWriter,
            bool enableTerminalOutput = true,
            bool enableFileOutput = true)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unifiedLogWriter = unifiedLogWriter ?? throw new ArgumentNullException(nameof(unifiedLogWriter));
            _enableTerminalOutput = enableTerminalOutput;
            _enableFileOutput = enableFileOutput;
        }

        /// <summary>
        /// セッション開始の統合出力
        /// </summary>
        public async Task WriteSessionStartAsync(SessionStartInfo sessionInfo, ConfigurationDetails configDetails)
        {
            // ファイル出力
            if (_enableFileOutput)
            {
                await _unifiedLogWriter.WriteSessionStartAsync(sessionInfo, configDetails);
            }

            // ターミナル出力
            if (_enableTerminalOutput)
            {
                _logger.LogInformation("=== セッション開始 ===");
                _logger.LogInformation("📋 セッションID: {SessionId}", sessionInfo.SessionId);
                _logger.LogInformation("🏢 アプリケーション: {ApplicationName} v{Version}", sessionInfo.ApplicationName, sessionInfo.Version);
                _logger.LogInformation("🎯 接続先: {ConnectionTarget}", configDetails.ConnectionTarget);
                _logger.LogInformation("⚙️ モード: {ContinuityMode}", configDetails.ContinuityMode);
                _logger.LogInformation("📄 ログ出力: {LogOutputPath}", configDetails.LogOutputPath);
            }
        }

        /// <summary>
        /// サイクル開始の統合出力
        /// </summary>
        public async Task WriteCycleStartAsync(CycleStartInfo cycleInfo)
        {
            // ファイル出力
            if (_enableFileOutput)
            {
                await _unifiedLogWriter.WriteCycleStartAsync(cycleInfo);
            }

            // ターミナル出力
            if (_enableTerminalOutput)
            {
                _logger.LogInformation("🔄 {StartMessage}", cycleInfo.StartMessage);
                if (cycleInfo.IntervalFromPrevious > 0)
                {
                    _logger.LogInformation("   前回からの間隔: {Interval:F1}秒", cycleInfo.IntervalFromPrevious);
                }
            }
        }

        /// <summary>
        /// 通信詳細の統合出力
        /// </summary>
        public async Task WriteCommunicationAsync(CommunicationInfo communicationInfo, RawDataAnalysis rawDataAnalysis)
        {
            // ファイル出力
            if (_enableFileOutput)
            {
                await _unifiedLogWriter.WriteCommunicationAsync(communicationInfo, rawDataAnalysis);
            }

            // ターミナル出力
            if (_enableTerminalOutput)
            {
                var deviceAddress = communicationInfo.CommunicationDetails.DeviceAddress;
                var responseTime = communicationInfo.CommunicationDetails.ResponseTimeMs;
                var success = communicationInfo.CommunicationDetails.Success;
                var values = communicationInfo.CommunicationDetails.Values;

                var statusIcon = success ? "✅" : "❌";
                _logger.LogInformation("{StatusIcon} {DeviceAddress} - {ResponseTime:F1}ms",
                    statusIcon, deviceAddress, responseTime);

                if (success && values.Length > 0)
                {
                    var activeCount = CountActiveValues(values, communicationInfo.CommunicationDetails.OperationType);
                    if (activeCount > 0)
                    {
                        _logger.LogInformation("   🎯 アクティブ: {ActiveCount}個", activeCount);
                    }
                }

                // 生データ情報（簡潔版）
                if (!string.IsNullOrEmpty(rawDataAnalysis.ResponseFrameHex))
                {
                    var frameLength = rawDataAnalysis.ResponseFrameHex.Length / 2; // バイト数
                    _logger.LogInformation("   📦 フレーム: {FrameLength}バイト ({FramePreview}...)",
                        frameLength, rawDataAnalysis.ResponseFrameHex.Substring(0, Math.Min(16, rawDataAnalysis.ResponseFrameHex.Length)));
                }
            }
        }

        /// <summary>
        /// エラー発生の統合出力
        /// </summary>
        public async Task WriteErrorAsync(ErrorInfo errorInfo, RecoveryInfo recoveryInfo)
        {
            // ファイル出力
            if (_enableFileOutput)
            {
                await _unifiedLogWriter.WriteErrorAsync(errorInfo, recoveryInfo);
            }

            // ターミナル出力
            if (_enableTerminalOutput)
            {
                _logger.LogWarning("⚠️ エラー発生: {ErrorType}", errorInfo.ErrorType);
                _logger.LogWarning("   デバイス: {DeviceAddress}", errorInfo.DeviceAddress);
                _logger.LogWarning("   詳細: {ErrorMessage}", errorInfo.ErrorMessage);

                if (recoveryInfo.AutoRecoveryEnabled)
                {
                    _logger.LogInformation("🔄 自動復旧: {RecoveryStatus}", recoveryInfo.RecoveryStatus);
                }
            }
        }

        /// <summary>
        /// 統計情報の統合出力
        /// </summary>
        public async Task WriteStatisticsAsync(StatisticsInfo statisticsInfo)
        {
            // ファイル出力
            if (_enableFileOutput)
            {
                await _unifiedLogWriter.WriteStatisticsAsync(statisticsInfo);
            }

            // ターミナル出力
            if (_enableTerminalOutput)
            {
                _logger.LogInformation("📊 統計情報 ({StatisticsType})", statisticsInfo.StatisticsType);
                _logger.LogInformation("   実行サイクル: {ExecutedCycles}", statisticsInfo.ExecutedCycles);
                _logger.LogInformation("   通信: 成功{Successful}/{Total} (成功率{SuccessRate})",
                    statisticsInfo.SuccessfulCommunications, statisticsInfo.TotalCommunications, statisticsInfo.SuccessRate);
                _logger.LogInformation("   応答時間: 平均{Avg:F1}ms (最小{Min:F1}ms / 最大{Max:F1}ms)",
                    statisticsInfo.AverageResponseTime, statisticsInfo.MinResponseTime, statisticsInfo.MaxResponseTime);
            }
        }

        /// <summary>
        /// パフォーマンスメトリクスの統合出力
        /// </summary>
        public async Task WritePerformanceMetricsAsync(PerformanceMetricsInfo metricsInfo)
        {
            // ファイル出力
            if (_enableFileOutput)
            {
                await _unifiedLogWriter.WritePerformanceMetricsAsync(metricsInfo);
            }

            // ターミナル出力
            if (_enableTerminalOutput)
            {
                _logger.LogInformation("⚡ パフォーマンス情報");
                _logger.LogInformation("   📡 ネットワーク: 遅延{Latency:F1}ms, 安定性{Stability}",
                    metricsInfo.NetworkQuality.AverageLatency, metricsInfo.NetworkQuality.ConnectionStability);
                _logger.LogInformation("   🔧 SLMP: 平均{Avg:F1}ms, 成功率{SuccessRate:F1}%, 総操作{Total}回",
                    metricsInfo.SlmpPerformance.AverageResponseTime, metricsInfo.SlmpPerformance.SuccessRate, metricsInfo.SlmpPerformance.TotalOperations);
                _logger.LogInformation("   💻 システム: メモリ{Memory:F1}MB, スレッド{Threads}個",
                    metricsInfo.SystemResource.MemoryUsage, metricsInfo.SystemResource.ThreadCount);
            }
        }

        /// <summary>
        /// セッション終了の統合出力
        /// </summary>
        public async Task WriteSessionEndAsync(SessionSummary sessionSummary)
        {
            // ファイル出力
            if (_enableFileOutput)
            {
                await _unifiedLogWriter.WriteSessionEndAsync(sessionSummary);
            }

            // ターミナル出力
            if (_enableTerminalOutput)
            {
                _logger.LogInformation("=== セッション終了 ===");
                _logger.LogInformation("📋 セッションID: {SessionId}", sessionSummary.SessionId);
                _logger.LogInformation("⏱️ 実行時間: {Duration}", sessionSummary.Duration);
                _logger.LogInformation("🎯 最終状態: {FinalStatus}", sessionSummary.FinalStatus);
                _logger.LogInformation("📝 終了理由: {ExitReason}", sessionSummary.ExitReason);
                _logger.LogInformation("💬 最終メッセージ: {FinalMessage}", sessionSummary.FinalMessage);
            }
        }

        /// <summary>
        /// アクティブ値の数をカウント
        /// </summary>
        private int CountActiveValues(object[] values, string operationType)
        {
            if (values == null || values.Length == 0) return 0;

            return operationType.ToLowerInvariant() switch
            {
                "bitdeviceread" => values.Count(v => v is bool b && b),
                "worddeviceread" => values.Count(v => v is ushort w && w != 0),
                _ => values.Length
            };
        }

        /// <summary>
        /// Step4スキャン結果の概要表示
        /// </summary>
        public void WriteStep4ScanSummary(string deviceCode, uint startAddress, uint endAddress, int totalScanned, int activeCount, double totalTimeMs)
        {
            if (_enableTerminalOutput)
            {
                var successIcon = activeCount > 0 ? "🎯" : "⚪";
                _logger.LogInformation("{Icon} {DeviceCode}{StartAddress}-{EndAddress}: {ActiveCount}/{TotalScanned}個アクティブ ({TimeMs:F1}ms)",
                    successIcon, deviceCode, startAddress, endAddress, activeCount, totalScanned, totalTimeMs);
            }
        }

        /// <summary>
        /// 進捗表示（Step4での使用）
        /// </summary>
        public void WriteProgressInfo(string message, int current, int total)
        {
            if (_enableTerminalOutput)
            {
                var percentage = total > 0 ? (current * 100.0) / total : 0;
                _logger.LogInformation("🔄 {Message} ({Current}/{Total} - {Percentage:F1}%)",
                    message, current, total, percentage);
            }
        }

        /// <summary>
        /// リソースを解放
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await _unifiedLogWriter.DisposeAsync();
        }
    }
}