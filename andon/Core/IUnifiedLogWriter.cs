using System;
using System.Threading.Tasks;

namespace SlmpClient.Core
{
    /// <summary>
    /// 統合ログライターインターフェース
    /// SOLID原則: Interface Segregation Principle適用
    /// 依存性逆転原則でテスタビリティを向上
    /// </summary>
    public interface IUnifiedLogWriter : IAsyncDisposable, IDisposable
    {
        /// <summary>セッション開始エントリを出力</summary>
        Task WriteSessionStartAsync(SessionStartInfo sessionInfo, ConfigurationDetails configDetails);

        /// <summary>サイクル開始エントリを出力</summary>
        Task WriteCycleStartAsync(CycleStartInfo cycleInfo);

        /// <summary>通信実行詳細エントリを出力</summary>
        Task WriteCommunicationAsync(CommunicationInfo communicationInfo, RawDataAnalysis rawDataAnalysis);

        /// <summary>エラー発生エントリを出力</summary>
        Task WriteErrorAsync(ErrorInfo errorInfo, RecoveryInfo recoveryInfo);

        /// <summary>統計情報エントリを出力</summary>
        Task WriteStatisticsAsync(StatisticsInfo statisticsInfo);

        /// <summary>パフォーマンスメトリクスエントリを出力</summary>
        Task WritePerformanceMetricsAsync(PerformanceMetricsInfo metricsInfo);

        /// <summary>セッション終了エントリを出力</summary>
        Task WriteSessionEndAsync(SessionSummary sessionSummary);

        /// <summary>システムイベントエントリを出力</summary>
        Task WriteSystemEventAsync(string sessionId, string eventType, string message, object? additionalData = null);

        /// <summary>エラー発生エントリを出力（簡略版）</summary>
        Task WriteErrorAsync(string sessionId, string errorType, string errorMessage, string deviceAddress);
    }
}